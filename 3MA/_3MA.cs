// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace _3MA
{
    public class _3MA : Indicator
    {
        [InputParameter("Source price", 0, variants: new object[]
        {
            "Close", PriceType.Close,
            "Open", PriceType.Open,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical,
            "Median", PriceType.Median,
            "Weighted", PriceType.Weighted
        })]
        public PriceType SourcePrice = PriceType.Close;

        [InputParameter("MA1 period", 10, 1, 9999, 1, 0)]
        public int Ma1Period = 20;

        [InputParameter("MA1 mode", 11, variants: new object[]
        {
            "SMA", MaMode.SMA,
            "EMA", MaMode.EMA
        })]
        public MaMode Ma1Mode = MaMode.EMA;

        [InputParameter("MA1 color", 12)]
        public Color Ma1Color = Color.DodgerBlue;

        [InputParameter("MA2 period", 20, 1, 9999, 1, 0)]
        public int Ma2Period = 50;

        [InputParameter("MA2 mode", 21, variants: new object[]
        {
            "SMA", MaMode.SMA,
            "EMA", MaMode.EMA
        })]
        public MaMode Ma2Mode = MaMode.EMA;

        [InputParameter("MA2 color", 22)]
        public Color Ma2Color = Color.Goldenrod;

        [InputParameter("MA3 period", 30, 1, 9999, 1, 0)]
        public int Ma3Period = 100;

        [InputParameter("MA3 mode", 31, variants: new object[]
        {
            "SMA", MaMode.SMA,
            "EMA", MaMode.EMA
        })]
        public MaMode Ma3Mode = MaMode.SMA;

        [InputParameter("MA3 color", 32)]
        public Color Ma3Color = Color.MediumSeaGreen;

        [InputParameter("Cloud color", 40)]
        public Color CloudColor = Color.FromArgb(65, Color.DeepSkyBlue);

        [InputParameter("Fill between min/max period MAs", 41)]
        public bool FillMinMax = true;

        private Indicator ma1;
        private Indicator ma2;
        private Indicator ma3;

        private bool minMaxCloudActive;
        private int activeCloudLine1 = -1;
        private int activeCloudLine2 = -1;

        public _3MA()
            : base()
        {
            Name = "3MA";
            Description = "Three moving averages (SMA/EMA) with cloud fill";

            AddLineSeries("MA1", Ma1Color, 2, LineStyle.Solid);
            AddLineSeries("MA2", Ma2Color, 2, LineStyle.Solid);
            AddLineSeries("MA3", Ma3Color, 2, LineStyle.Solid);

            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            if (ma1 != null)
                RemoveIndicator(ma1);

            if (ma2 != null)
                RemoveIndicator(ma2);

            if (ma3 != null)
                RemoveIndicator(ma3);

            minMaxCloudActive = false;
            activeCloudLine1 = -1;
            activeCloudLine2 = -1;

            ma1 = Core.Instance.Indicators.BuiltIn.MA(Ma1Period, SourcePrice, Ma1Mode);
            ma2 = Core.Instance.Indicators.BuiltIn.MA(Ma2Period, SourcePrice, Ma2Mode);
            ma3 = Core.Instance.Indicators.BuiltIn.MA(Ma3Period, SourcePrice, Ma3Mode);

            AddIndicator(ma1);
            AddIndicator(ma2);
            AddIndicator(ma3);

            ApplyLineColors();
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            var v1 = ma1.GetValue();
            var v2 = ma2.GetValue();
            var v3 = ma3.GetValue();

            SetValue(v1, 0);
            SetValue(v2, 1);
            SetValue(v3, 2);

            var valuesAreFinite = IsFinite(v1) && IsFinite(v2) && IsFinite(v3);
            var (minPeriodLine, maxPeriodLine) = GetMinMaxPeriodLineIndices();

            var shouldShowCloud = FillMinMax && valuesAreFinite && minPeriodLine != maxPeriodLine;
            UpdateMinMaxCloud(minPeriodLine, maxPeriodLine, shouldShowCloud);
        }

        protected override void OnClear()
        {
            if (ma1 != null)
                RemoveIndicator(ma1);

            if (ma2 != null)
                RemoveIndicator(ma2);

            if (ma3 != null)
                RemoveIndicator(ma3);

            if (minMaxCloudActive && activeCloudLine1 >= 0 && activeCloudLine2 >= 0)
                EndCloud(activeCloudLine1, activeCloudLine2, Color.Empty);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void ApplyLineColors()
        {
            SetLineSeriesColor(0, Ma1Color);
            SetLineSeriesColor(1, Ma2Color);
            SetLineSeriesColor(2, Ma3Color);
        }

        private void SetLineSeriesColor(int lineIndex, Color color)
        {
            if (lineIndex < 0 || lineIndex >= LinesSeries.Length || LinesSeries[lineIndex] == null)
                return;

            LinesSeries[lineIndex].Color = color;
        }

        private (int minLine, int maxLine) GetMinMaxPeriodLineIndices()
        {
            var periods = new[] { Ma1Period, Ma2Period, Ma3Period };
            var minLine = 0;
            var maxLine = 0;

            for (var i = 1; i < periods.Length; i++)
            {
                if (periods[i] < periods[minLine])
                    minLine = i;

                if (periods[i] > periods[maxLine])
                    maxLine = i;
            }

            return (minLine, maxLine);
        }

        private void UpdateMinMaxCloud(int line1, int line2, bool shouldBeActive)
        {
            var pairChanged = line1 != activeCloudLine1 || line2 != activeCloudLine2;

            if (minMaxCloudActive && (pairChanged || !shouldBeActive))
            {
                EndCloud(activeCloudLine1, activeCloudLine2, Color.Empty);
                minMaxCloudActive = false;
            }

            if (shouldBeActive && (!minMaxCloudActive || pairChanged))
            {
                BeginCloud(line1, line2, CloudColor);
                minMaxCloudActive = true;
                activeCloudLine1 = line1;
                activeCloudLine2 = line2;
            }
            else if (!shouldBeActive)
            {
                activeCloudLine1 = -1;
                activeCloudLine2 = -1;
            }
        }
    }
}

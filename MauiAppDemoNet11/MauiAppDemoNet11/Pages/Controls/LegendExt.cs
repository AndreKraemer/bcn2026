using Syncfusion.Maui.Toolkit.Charts;

namespace MauiAppDemoNet11.Pages.Controls
{
    public class LegendExt : ChartLegend
    {
        protected override double GetMaximumSizeCoefficient()
        {
            return 0.5;
        }
    }
}

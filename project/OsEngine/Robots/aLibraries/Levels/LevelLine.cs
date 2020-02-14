using OsEngine.Charts.CandleChart.Elements;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.aLibraries.Levels
{
    public enum HighLowLevelTypes
    {
        Low,
        High
    }


    abstract public class LevelLine
    {
        protected LineHorisontal lineOnChart;
        protected BotTabSimple chart;

        public LevelLine(BotTabSimple chart)
        {
            this.chart = chart;
        }

        protected void DrawLine(decimal value, string name, DateTime timeStart, DateTime timeEnd, Color color)
        {

            lineOnChart = new LineHorisontal(name, "Prime", false)
            {
                Color = color,
                Value = value,
                TimeStart = timeStart,
                TimeEnd = timeEnd
            };
            chart.SetChartElement(lineOnChart);
        }

        public void RefreshOnChart()
        {
            if (lineOnChart != null)
            {
                lineOnChart.Refresh();
            }
           
        }

        abstract public void  DrawOnChart();

        abstract public void DeleteFromChart();

    }
}

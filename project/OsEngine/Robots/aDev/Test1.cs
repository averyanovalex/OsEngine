using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.aDev
{
    class Test1 : BotPanel
    {

        public LineHorisontal _line;
        public PointElement _point;
        
        public Test1(string name, StartProgram startProgram) : base(name, startProgram)
        {

            TabCreate(BotTabType.Simple);


            _line = new LineHorisontal("line", "Prime", false)
            {
                Color = Color.Green,
                Value = 19550,
                TimeStart = new DateTime(2019, 1, 4, 14, 0, 0),
                TimeEnd = new DateTime(2019, 1, 9, 14, 0, 0)
            };
            TabsSimple[0].SetChartElement(_line);

            _point = new PointElement("point", "Prime")
            {
                Color = Color.Red,
                Size = 20,
                Style = MarkerStyle.Star6,
                TimePoint = new DateTime(2019, 1, 4, 14, 0, 0),
                Y = 19450
            };

            TabsSimple[0].SetChartElement(_point);


            TabsSimple[0].CandleFinishedEvent += Test1_CandleFinishedEvent;

        }

        private void Test1_CandleFinishedEvent(List<Candle> candles)
        {
            _line.Refresh();
            _point.Refresh();
        }

        public override string GetNameStrategyType()
        {
            return "Test1";
        }
        

        public override void ShowIndividualSettingsDialog()
        {
            
        }
    }
}

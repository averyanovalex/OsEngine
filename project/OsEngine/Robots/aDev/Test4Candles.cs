using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Robots.aDev
{
    class Test4Candles : BotPanel
    {

        private BotTabSimple tab0;


        public Test4Candles(string name, StartProgram startProgram) : base(name, startProgram)
        {

            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];



            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;
            tab0.PositionOpeningSuccesEvent += Tab0_PositionOpeningSuccesEvent;

        }

        private void Tab0_PositionOpeningSuccesEvent(Position position)
        {
            int stop = 10;
            int take = 50;

            if (position.Direction == Side.Buy)
            {
                decimal stopPrice = position.EntryPrice - tab0.Securiti.PriceStep * stop;
                decimal takePrice = position.EntryPrice + tab0.Securiti.PriceStep * take;

                tab0.CloseAtStop(position, stopPrice, stopPrice);
                tab0.CloseAtProfit(position, takePrice, takePrice);

            }
            else if (position.Direction == Side.Sell)
            {
                decimal stopPrice = position.EntryPrice + tab0.Securiti.PriceStep * stop;
                decimal takePrice = position.EntryPrice - tab0.Securiti.PriceStep * take;

                tab0.CloseAtStop(position, stopPrice, stopPrice);
                tab0.CloseAtProfit(position, takePrice, takePrice);
            }
        }

        private void DrawLine(decimal value, string name, DateTime timeStart, DateTime timeEnd, Color color)
        {

            var lineOnChart = new LineHorisontal(name, "Prime", false)
            {
                Color = color,
                Value = value,
                TimeStart = timeStart,
                TimeEnd = timeEnd
            };
            tab0.SetChartElement(lineOnChart);
        }

        private decimal minVal(decimal val1, decimal val2, decimal val3, decimal val4)
        {
            var min = val1;
            if (val2 < min) min = val2;
            if (val3 < min) min = val3;
            if (val4 < min) min = val4;

            return min;
        }

        private decimal maxVal(decimal val1, decimal val2, decimal val3, decimal val4)
        {
            var max = val1;
            if (val2 > max) max = val2;
            if (val3 > max) max = val3;
            if (val4 > max) max = val4;

            return max;
        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            //parametres
            var slack = 3;


            List<Position> positions = TabsSimple[0].PositionsOpenAll;
            if (positions != null && positions.Count != 0)
            {
                return;
            }


            if (candles.Count < 5) return;


            var candle1 = candles[candles.Count - 4];
            var candle2 = candles[candles.Count - 3];
            var candle3 = candles[candles.Count - 2];
            var candle4 = candles[candles.Count - 1];


            //проверяем на Low
            var low1 = candle1.Low;
            var low2 = candle2.Low;
            var low3 = candle3.Low;
            var low4 = candle4.Low;

            var min = minVal(low1, low2, low3, low4);

            var delta1 = Math.Abs(low1 - min);
            var delta2 = Math.Abs(low2 - min);
            var delta3 = Math.Abs(low3 - min);
            var delta4 = Math.Abs(low4 - min);

            if ((delta1 <= slack) && (delta2 <= slack) && (delta3 <= slack) && (delta4 <= slack))
            {
                DrawLine(min, $"line-{Convert.ToString(candle1.TimeStart)}", candle1.TimeStart, candle4.TimeStart, Color.Blue);
                tab0.BuyAtMarket(1);
                return;
            }


            //проверяем на High
            var high1 = candle1.High;
            var high2 = candle2.High;
            var high3 = candle3.High;
            var high4 = candle4.High;

            var max = maxVal(high1, high2, high3, high4);

            delta1 = Math.Abs(high1 - max);
            delta2 = Math.Abs(high2 - max);
            delta3 = Math.Abs(high3 - max);
            delta4 = Math.Abs(high4 - max);

            if ((delta1 <= slack) && (delta2 <= slack) && (delta3 <= slack) && (delta4 <= slack))
            {
                DrawLine(max, $"line-{Convert.ToString(candle1.TimeStart)}", candle1.TimeStart, candle4.TimeStart, Color.Green);
                tab0.SellAtMarket(1);
            }




        }

        public override string GetNameStrategyType()
        {
            return "Test4Candles";
        }


        public override void ShowIndividualSettingsDialog()
        {

        }
    }
}

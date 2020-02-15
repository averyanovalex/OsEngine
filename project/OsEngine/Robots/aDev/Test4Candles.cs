using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.aDev
{
    class Test4Candles : BotPanel
    {

        private BotTabSimple tab0;

        private DateTime timeStopOrder;


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

        private decimal minVal(decimal val1, decimal val2, decimal val3, decimal val4, decimal val5, decimal val6)
        {
            var min = val1;
            if (val2 < min) min = val2;
            if (val3 < min) min = val3;
            if (val4 < min) min = val4;
            if (val5 < min) min = val5;
            if (val6 < min) min = val6;

            return min;
        }

        private decimal maxVal(decimal val1, decimal val2, decimal val3, decimal val4, decimal val5, decimal val6)
        {
            var max = val1;
            if (val2 > max) max = val2;
            if (val3 > max) max = val3;
            if (val4 > max) max = val4;
            if (val5 > max) max = val5;
            if (val6 > max) max = val6;

            return max;
        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            //parametres
            var slack = 3;


            List<Position> positions = tab0.PositionsOpenAll;
            if (positions != null && positions.Count != 0)
            {
                
                //if (positions[0].State == PositionStateType.Opening && tab0.TimeServerCurrent >= timeStopOrder)
                //{
                //    tab0.CloseAllOrderToPosition(positions[0]);
                //}
                
 
                
                return;
            }


            if (candles.Count < 7) return;


            var candle1 = candles[candles.Count - 6];
            var candle2 = candles[candles.Count - 5];
            var candle3 = candles[candles.Count - 4];
            var candle4 = candles[candles.Count - 3];
            var candle5 = candles[candles.Count - 2];
            var candle6 = candles[candles.Count - 1];


            //проверяем на Low
            var low1 = candle1.Low;
            var low2 = candle2.Low;
            var low3 = candle3.Low;
            var low4 = candle4.Low;
            var low5 = candle5.Low;
            var low6 = candle6.Low;

            var body1 = Math.Min(candle1.Close, candle1.Open);
            var body2 = Math.Min(candle2.Close, candle2.Open);
            var body3 = Math.Min(candle3.Close, candle3.Open);
            var body4 = Math.Min(candle4.Close, candle4.Open);
            var body5 = Math.Min(candle5.Close, candle5.Open);
            var body6 = Math.Min(candle6.Close, candle6.Open);

            var checkPrice = maxVal(low1, low2, low3, low4, low5, low6);

            var delta1 = Math.Abs(low1 - checkPrice);
            var delta2 = Math.Abs(low2 - checkPrice);
            var delta3 = Math.Abs(low3 - checkPrice);
            var delta4 = Math.Abs(low4 - checkPrice);
            var delta5 = Math.Abs(low3 - checkPrice);
            var delta6 = Math.Abs(low4 - checkPrice);

            var touch = 0;
            var prokol = 0;
            var error = 0;


            if (delta1 <= slack && body1 > checkPrice) touch++;
            else if (body1 > checkPrice && low1 < checkPrice) prokol++;
            else error++;

            if (delta2 <= slack && body2 > checkPrice) touch++;
            else if (body2 > checkPrice && low2 < checkPrice) prokol++;
            else error++;

            if (delta3 <= slack && body3 > checkPrice) touch++;
            else if (body3 > checkPrice && low3 < checkPrice) prokol++;
            else error++;

            if (delta4 <= slack && body4 > checkPrice) touch++;
            else if (body4 > checkPrice && low4 < checkPrice) prokol++;
            else error++;

            if (delta5 <= slack && body5 > checkPrice) touch++;
            else if (body5 > checkPrice && low5 < checkPrice) prokol++;
            else error++;

            if (delta6 <= slack && body6 > checkPrice) touch++;
            else if (body6 > checkPrice && low6 < checkPrice) prokol++;
            else error++;


            if (touch >= 4 && prokol <= 2 && error == 0)
            {
                DrawLine(checkPrice, $"line-{Convert.ToString(candle1.TimeStart)}", candle1.TimeStart, candle6.TimeStart, Color.Blue);
                tab0.BuyAtLimit(1, checkPrice + slack);
                return;
            }
            

            /*
            //проверяем на High
            var high1 = candle1.High;
            var high2 = candle2.High;
            var high3 = candle3.High;
            var high4 = candle4.High;

            var max = minVal(high1, high2, high3, high4);

            delta1 = Math.Abs(high1 - max);
            delta2 = Math.Abs(high2 - max);
            delta3 = Math.Abs(high3 - max);
            delta4 = Math.Abs(high4 - max);

            if ((delta1 <= slack) && (delta2 <= slack) && (delta3 <= slack) && (delta4 <= slack))
            {
                DrawLine(max, $"line-{Convert.ToString(candle1.TimeStart)}", candle1.TimeStart, candle4.TimeStart, Color.Green);
                tab0.SellAtLimit(1, max - slack);
                timeStopOrder = tab0.TimeServerCurrent.AddSeconds(tab0.TimeFrame.TotalSeconds * 2);
            }


            */

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

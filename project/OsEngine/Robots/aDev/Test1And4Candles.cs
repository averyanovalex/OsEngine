using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.aDev

    //Гипотезы:
    //1.Доработать управление позицией
    //2.Потестить на Ри, Си, крипте
    //3.Вылизать алгоритм, добавить рабочие ограничения, оптимизировать
{
    class Test1And4Candles : BotPanel
    {

        private BotTabSimple tab0;
        private BotTabSimple tab1;

        private MovingAverage _moving;

        private bool isLongTrend;
        private bool isShortTrend;


        public Test1And4Candles(string name, StartProgram startProgram) : base(name, startProgram)
        {

            TabCreate(BotTabType.Simple);
            //TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];
            //tab1 = TabsSimple[1];

            //_moving = new MovingAverage("moving1", false);
            //_moving = (MovingAverage)tab1.CreateCandleIndicator(_moving, "Prime");
            //_moving.Save();



            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;
            tab0.PositionOpeningSuccesEvent += Tab0_PositionOpeningSuccesEvent;

            //tab1.CandleFinishedEvent += Tab1_CandleFinishedEvent;

        }

        private void Tab1_CandleFinishedEvent(List<Candle> candles)
        {
            if (_moving.Lenght >= candles.Count) return;

            decimal lastValue = _moving.Values[_moving.Values.Count - 1];

            decimal val1 = _moving.Values[_moving.Values.Count - 2];
            decimal val2 = _moving.Values[_moving.Values.Count - 3];
            decimal val3 = _moving.Values[_moving.Values.Count - 4];
            decimal val4 = _moving.Values[_moving.Values.Count - 5];
            decimal val5 = _moving.Values[_moving.Values.Count - 6];

            if (lastValue > val1 && lastValue > val2 && lastValue > val3 )
            {
                isLongTrend = true;
            }
            else
            {
                isLongTrend = false;
            }

            if (lastValue < val1 && lastValue < val2 && lastValue < val3)
            {
                isShortTrend = true;
            }
            else
            {
                isShortTrend = false;
            }




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

        private void DrawPoint(decimal value, string name, DateTime time, Color color)
        {

            var pointOnChart = new PointElement(name, "Prime")
            {
                Color = color,
                Size = 10,
                Style = MarkerStyle.Star6,
                TimePoint = time,
                Y = value
            };

            tab0.SetChartElement(pointOnChart);
        }

        private decimal minVal(decimal val1, decimal val2)
        {
            var min = val1;
            if (val2 < min) min = val2;
  

            return min;
        }

        private decimal maxVal(decimal val1, decimal val2)
        {
            var max = val1;
            if (val2 > max) max = val2;
            
           

            return max;
        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {


            //parametres
            var slack = 1;


            List<Position> positions = tab0.PositionsOpenAll;
            if (positions != null && positions.Count != 0)
            {
                
                return;
            }


            if (candles.Count < 10) return;




            //ОСНОВНАЯ ЛОГИКА


            var candle1 = candles[candles.Count - 2];
            var candle2 = candles[candles.Count - 1];
            
            


            //проверяем на Low
            var low1 = candle1.Low;
            var low2 = candle2.Low;
            
            


            var body1 = Math.Min(candle1.Close, candle1.Open);
            var body2 = Math.Min(candle2.Close, candle2.Open);
            
            


            var checkPrice = maxVal(low1, low2);

            var delta1 = Math.Abs(low1 - checkPrice);
            var delta2 = Math.Abs(low2 - checkPrice);
            
            


            var touch = 0;
            var prokol = 0;
            var error = 0;


            if (delta1 <= slack && body1 > checkPrice) touch++;
            else if (body1 > checkPrice && low1 < checkPrice) prokol++;
            else error++;

            if (delta2 <= slack && body2 > checkPrice) touch++;
            else if (body2 > checkPrice && low2 < checkPrice) prokol++;
            else error++;


            

            //Ищем ТВХ
            var indexStart = candles.Count - 54;
            indexStart = indexStart < 0 ? 0 : indexStart;

            var tvh = 0;
            Candle tvhCandle = null;
            for (int i = candles.Count-5; i>= indexStart; i--)
            {
                if (candles[i].Low == low1 || candles[i].High == low1)
                {
                    tvh = 1;
                    tvhCandle = candles[i];
                    break;
                }
            }


            if (touch == 2  && prokol <= 0 && error == 0 && tvh == 1 )
            {


                var slack_order = 4;
                
                DrawLine(checkPrice, $"line-{Convert.ToString(candle1.TimeStart)}", candle1.TimeStart, candle2.TimeStart, Color.Blue);
                DrawPoint(tvhCandle.Low - 20, $"point-{Convert.ToString(tvhCandle.TimeStart)}", tvhCandle.TimeStart, Color.Yellow);
                tab0.BuyAtLimit(1, checkPrice + slack_order);
                return;
            }

            return;

            //ШОРТ

            var high1 = candle1.High;
            var high2 = candle2.High;


            body1 = Math.Max(candle1.Close, candle1.Open);
            body2 = Math.Max(candle2.Close, candle2.Open);




            checkPrice = minVal(high1, high2);

            delta1 = Math.Abs(high1 - checkPrice);
            delta2 = Math.Abs(high2 - checkPrice);


            touch = 0;
            prokol = 0;
            error = 0;


            if (delta1 <= slack && body1 < checkPrice) touch++;
            else if (body1 < checkPrice && high1 > checkPrice) prokol++;
            else error++;

            if (delta2 <= slack && body2 < checkPrice) touch++;
            else if (body2 < checkPrice && high2 > checkPrice) prokol++;
            else error++;




            //Ищем ТВХ
            indexStart = candles.Count - 54;
            indexStart = indexStart < 0 ? 0 : indexStart;

            tvh = 0;
            tvhCandle = null;
            for (int i = candles.Count - 5; i >= indexStart; i--)
            {
                if (candles[i].Low == high1 || candles[i].High == high1)
                {
                    tvh = 1;
                    tvhCandle = candles[i];
                    break;
                }
            }


            if (touch == 2 && prokol <= 0 && error == 0 && tvh == 1 && isShortTrend)
            {

                var slack_order = 4;

                DrawLine(checkPrice, $"line-{Convert.ToString(candle1.TimeStart)}", candle1.TimeStart, candle2.TimeStart, Color.Red);
                DrawPoint(tvhCandle.High + 20, $"point-{Convert.ToString(tvhCandle.TimeStart)}", tvhCandle.TimeStart, Color.Red);
                tab0.SellAtLimit(1, checkPrice - slack_order);
                return;
            }



        }

        public override string GetNameStrategyType()
        {
            return "Test1And4Candles";
        }


        public override void ShowIndividualSettingsDialog()
        {

        }
    }
}

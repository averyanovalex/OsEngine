using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.aDev


    //TODO:
    //1.Переписать алгоритм.
    //1.2 Вынести параметры
    //2.Рефакторить структуру модулей
    //3.Дообавить ограничения на первые и последние часы и выходные дни
    //4.Удалить лишний код из текущей ветки
    //5.Слить ветку и обновить последние обновления
    //6.Оптимизировать, подобрать лучшие параметры


    //Описание:
    //Ловля краткосрочных импульсов, отскоков от локального уровня. Если 2 или больше свечей оттолкнулись от одного уровня,
    //входим. Стопы и тейки фиксированные
{
    enum Mode
    {
        On_Long_Short,
        On_OnlyLong,
        On_OnlyShort,
        Off,
    }
    
    class MaxBot : BotPanel
    {

        private BotTabSimple tab0;

        public int stop = 10; //стоп в пунктах
        public int take = 30; //тейк в пунктах
        public int slack = 3; //люфт в пунктах
        public int slack_order = 4; //люфт для выставления ордера в пунктах
        public int candlesCount = 2; //количество проверяемых свечей
        public Mode mode = Mode.On_OnlyLong;

        public MaxBot(string name, StartProgram startProgram) : base(name, startProgram)
        {

            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];

            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;
            tab0.PositionOpeningSuccesEvent += Tab0_PositionOpeningSuccesEvent;

        }

        private void Tab0_PositionOpeningSuccesEvent(Position position)
        {

            int koef = 0;
            if (position.Direction == Side.Buy) koef = 1;
            else if (position.Direction == Side.Sell) koef = -1;
            else
            {
                tab0.CloseAtMarket(position, position.OpenVolume);
                throw new InvalidOperationException("Неверный тип позиции. Позиция должна быть Buy или Sell");
            }


            decimal stopPrice = position.EntryPrice - koef * tab0.Securiti.PriceStep * stop;
            decimal takePrice = position.EntryPrice + koef * tab0.Securiti.PriceStep * take;

            tab0.CloseAtStop(position, stopPrice, stopPrice);
            tab0.CloseAtProfit(position, takePrice, takePrice);

        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            if (mode == Mode.Off) return;
            
            List<Position> positions = tab0.PositionsOpenAll;
            if (positions != null && positions.Count != 0) return;

            TradeLogic(candles);
  
        }

        private void TradeLogic(List<Candle> candles)
        {

            if (candles.Count < candlesCount + 1) return;

            List<Candle> checkingCandles = new List<Candle>();
            for (int i = candles.Count - candlesCount; i <= candles.Count - 1; i++)
            {
                checkingCandles.Add(candles[i]);
            }


            //ищем точку входа в Лонг
            if (mode == Mode.On_Long_Short || mode == Mode.On_OnlyLong)
            {

                decimal checkPrice = calcMaxLowPrice(checkingCandles);

                List<decimal> body = new List<decimal>();
                List<decimal> delta = new List<decimal>();

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    body.Add(Math.Min(checkingCandles[i].Close, checkingCandles[i].Open));
                    delta.Add(Math.Abs(checkingCandles[i].Low - checkPrice));
                }


                int touch = 0;

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    if (delta[i] <= slack && body[i] >= checkPrice) touch++;
                }

                if (touch == candlesCount)
                {
                    tab0.BuyAtLimit(1, checkPrice + slack_order * tab0.Securiti.PriceStep);
                    return;
                }

            }



            //ищем точку входа в Шорт
            if (mode == Mode.On_Long_Short || mode == Mode.On_OnlyShort)
            {

                decimal checkPrice = calcMinHighPrice(checkingCandles);

                List<decimal> body = new List<decimal>();
                List<decimal> delta = new List<decimal>();

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    body.Add(Math.Max(checkingCandles[i].Close, checkingCandles[i].Open));
                    delta.Add(Math.Abs(checkingCandles[i].High - checkPrice));
                }


                int touch = 0;

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    if (delta[i] <= slack && body[i] <= checkPrice) touch++;
                }

                if (touch == candlesCount)
                {
                    tab0.SellAtLimit(1, checkPrice - slack_order * tab0.Securiti.PriceStep);
                    return;
                }

            }


        }

        private decimal calcMaxLowPrice(List<Candle> candles)
        {
            decimal result = 0;
            foreach (Candle candle in candles)
            {
                result = candle.Low > result ? candle.Low : result;
            }

            return result;
        }

        private decimal calcMinHighPrice(List<Candle> candles)
        {
            decimal result = candles[0].High;
            foreach (Candle candle in candles)
            {
                result = candle.High < result ? candle.High : result;
            }

            return result;
        }

        public override string GetNameStrategyType()
        {
            return "MaxBot";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }
    }
}

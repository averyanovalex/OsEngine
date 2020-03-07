using System;
using System.Collections.Generic;
using System.IO;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.aDemo
{
    //<summary>
    // Учебный робот. Свечной паттерн "Молот". Арбитраж на двух инструментах
    //Вход:
    //На одном инструменте молот направленный длиной тенью вниз,
    //на втором инструменте молот направленный длинной тенью вверх.
    //На первом входим в лонг, на втором в шорт
    //
    //Используем лимитные ордера
    //
    //Выходим через n свечей или по стопу
    //</summary>
    public class DemoHammerArbitrage : BotPanel
    {

        private BotTabSimple tab0, tab1;
        private DateTime timeStop;

        public DemoHammerArbitrage(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);

            tab0 = TabsSimple[0];
            tab1 = TabsSimple[1];

            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;
            tab1.CandleFinishedEvent += Tab1_CandleFinishedEvent;

            tab0.PositionOpeningSuccesEvent += Tab0_PositionOpeningSuccesEvent;
            tab1.PositionOpeningSuccesEvent += Tab1_PositionOpeningSuccesEvent;

        }

        private void Tab1_PositionOpeningSuccesEvent(Position position)
        {
            var stopPrice = position.EntryPrice + tab0.Securiti.PriceStep * 30;
            var takePrice = position.EntryPrice - tab0.Securiti.PriceStep * 60;
            tab1.CloseAtStop(position, stopPrice, stopPrice);
            tab1.CloseAtProfit(position, takePrice, takePrice);
        }

        private void Tab0_PositionOpeningSuccesEvent(Position position)
        {

            var stopPrice = position.EntryPrice - tab0.Securiti.PriceStep * 30;
            var takePrice = position.EntryPrice + tab0.Securiti.PriceStep * 60;
            tab0.CloseAtStop(position, stopPrice, stopPrice);
            tab0.CloseAtProfit(position, takePrice, takePrice);

        }

        private void Tab1_CandleFinishedEvent(List<Candle> candles_tab1)
        {
            List<Candle> candles_tab0 = TabsSimple[0].CandlesFinishedOnly;

            if (candles_tab0[candles_tab0.Count - 1].TimeStart == candles_tab1[candles_tab1.Count - 1].TimeStart)
            {
                TradeLogic(candles_tab0, candles_tab1);
            }
        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles_tab0)
        {
            List<Candle> candles_tab1 = TabsSimple[1].CandlesFinishedOnly;

            if (candles_tab0[candles_tab0.Count - 1].TimeStart == candles_tab1[candles_tab1.Count - 1].TimeStart)
            {
                TradeLogic(candles_tab0, candles_tab1);
            }

        }

        private void TradeLogic(List<Candle> candles0, List<Candle> candles1)
        {

            bool isCancel = false;

            if (tab0.PositionsOpenAll != null && tab0.PositionsOpenAll.Count != 0)
            {
                if (candles0[candles0.Count - 1].TimeStart >= timeStop)
                {
                    tab0.CloseAllAtMarket();
                }
                isCancel = true;
            }

            if (tab1.PositionsOpenAll != null && tab1.PositionsOpenAll.Count != 0)
            {
                if (candles1[candles1.Count - 1].TimeStart >= timeStop)
                {
                    tab1.CloseAllAtMarket();
                }
                isCancel = true;
            }

            //выходим
            if (isCancel) return;


            //проверяем, чтобы было минимум 21 свеча
            if (candles0.Count < 6 || candles1.Count < 6) return;

            Candle lastCandleTab0 = candles0[candles0.Count - 1];
            Candle lastCandleTab1 = candles1[candles1.Count - 1];


            //проверяем, чтобы последний лой на первом инструменте был самый нижний за 21 свечу
            decimal lastLow0 = lastCandleTab0.Low;
            for (int i = candles0.Count - 2; i > candles0.Count - 6; i--)
            {
                if (lastLow0 > candles0[i].Low) return;
            }

            //проверяем, чтобы последний хай на втором инструменте был самый высокий за 21 свечу
            decimal lastHigh1 = lastCandleTab1.High;
            for (int i = candles1.Count - 2; i > candles1.Count - 6; i--)
            {
                if (lastHigh1 < candles1[i].High) return;
            }

            //проверяем, что последняя свеча первого инструмента - это правильный нижний молот
            if (!isLowHammer(lastCandleTab0)) return;

            //проверяем, что последняя свеча второго инструмента - это правильный верхний молот
            if (!isHighHammer(lastCandleTab1)) return;

            //все ОК, открываем позу
            tab0.BuyAtLimit(1, lastCandleTab0.Close + tab0.Securiti.PriceStep * 10);
            tab1.SellAtLimit(1, lastCandleTab1.Close - tab1.Securiti.PriceStep * 10);

            //позу закроем автоматом через 10 свечек
            timeStop = lastCandleTab0.TimeStart.AddSeconds(tab0.TimeFrame.TotalSeconds * 10);
     
        }

        private bool isLowHammer(Candle candle)
        {

            if (candle.Open >= candle.Close)
            { //если последняя свеча (ожидаемый молот) не растущая, не входим
                return false;
            }

            //проверяем, чтобы тело было в 3 раза меньше хвоста снизу и не больше хвоста сверху
            decimal body = candle.Close - candle.Open;
            decimal shadowLow = candle.Open - candle.Low;
            decimal shadowHigh = candle.High - candle.Close;

            if (body < shadowHigh) return false;
            if (shadowLow / 3 < body) return false;

            // у нас правильный нижний молот
            return true;
        }

        private bool isHighHammer(Candle candle)
        {

            if (candle.Open <= candle.Close)
            { //если последняя свеча (ожидаемый молот) не падающая, не входим
                return false;
            }

            //проверяем, чтобы тело было в 3 раза меньше хвоста снизу и не больше хвоста сверху
            decimal body = candle.Open - candle.Close;
            decimal shadowLow = candle.Close - candle.Low;
            decimal shadowHigh = candle.High - candle.Open;

            if (body < shadowLow) return false;
            if (shadowHigh / 3 < body) return false;

            // у нас правильный верхний молот
            return true;
        }

        public override string GetNameStrategyType()
        {
            return "DemoHammerArbitrage";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //нет настраиваемых параметров
        }
    }
}

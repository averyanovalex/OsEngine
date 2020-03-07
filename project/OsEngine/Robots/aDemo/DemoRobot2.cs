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
    public class DemoRobot2 : BotPanel
    {

        //Учебный бот. Торгует сразу по 2 инструментам. Пример простого арбитража
        //Если у первой вкладки три растущих свечи,
        //а у второй три падающих, то входим в первой в шорт, а во второй в лонг

        public DemoRobot2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);

            TabsSimple[0].CandleFinishedEvent += DemoRobot2_CandleFinishedEvent_Tab0;
            TabsSimple[1].CandleFinishedEvent += DemoRobot2_CandleFinishedEvent_Tab1;

            TabsSimple[0].PositionOpeningSuccesEvent += DemoRobot2_PositionOpeningSuccesEvent_Tab0;
            TabsSimple[1].PositionOpeningSuccesEvent += DemoRobot2_PositionOpeningSuccesEvent_Tab1;
        }

        private void DemoRobot2_PositionOpeningSuccesEvent_Tab1(Position position)
        {
            TabsSimple[1].CloseAtStop(position, position.EntryPrice - TabsSimple[1].Securiti.PriceStep * 50,
                                        position.EntryPrice - TabsSimple[1].Securiti.PriceStep * 50);

            TabsSimple[1].CloseAtProfit(position, position.EntryPrice + TabsSimple[1].Securiti.PriceStep * 50,
                                        position.EntryPrice + TabsSimple[1].Securiti.PriceStep * 50);
        }

        private void DemoRobot2_PositionOpeningSuccesEvent_Tab0(Position position)
        {
            TabsSimple[0].CloseAtStop(position, position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 50,
                                        position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 50);

            TabsSimple[0].CloseAtProfit(position, position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 50,
                                        position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 50);
        }


        private void DemoRobot2_CandleFinishedEvent_Tab0(List<Candle> candles)
        {
            List<Candle> candles2 = TabsSimple[1].CandlesFinishedOnly;

            if (candles[candles.Count-1].TimeStart == candles2[candles2.Count - 1].TimeStart)
            {
                TradeLogic(candles, candles2);
            }
        }

        private void DemoRobot2_CandleFinishedEvent_Tab1(List<Candle> candles)
        {
            List<Candle> candles2 = TabsSimple[0].CandlesFinishedOnly;

            if (candles[candles.Count - 1].TimeStart == candles2[candles2.Count - 1].TimeStart)
            {
                TradeLogic(candles2, candles);
            }
        }

        public void TradeLogic(List<Candle> candlesOneTab, List<Candle> candlesTwoTab)
        {

            if (candlesOneTab.Count < 5 || candlesTwoTab.Count < 5) return;
            
            Candle candle01 = candlesOneTab[candlesOneTab.Count - 3];
            Candle candle02 = candlesOneTab[candlesOneTab.Count - 2];
            Candle candle03 = candlesOneTab[candlesOneTab.Count - 1];

            Candle candle11 = candlesTwoTab[candlesTwoTab.Count - 3];
            Candle candle12 = candlesTwoTab[candlesTwoTab.Count - 2];
            Candle candle13 = candlesTwoTab[candlesTwoTab.Count - 1];

            if (candle01.Close > candle01.Open 
                && candle02.Close > candle02.Open 
                && candle03.Close > candle03.Open 
                && candle11.Close < candle11.Open
                && candle12.Close < candle12.Open
                && candle13.Close < candle13.Open)
            {
                TabsSimple[0].SellAtMarket(1);
                TabsSimple[1].BuyAtMarket(1);
            }

        }

        public override string GetNameStrategyType()
        {
            return "DemoRobot2";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //нет параметров
        }
    }
}

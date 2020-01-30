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
    public class DemoRobot1 : BotPanel
    {

        //Учебный бот. Несколько позиций сразу.
        //Логика робота:
        // 1 вход: две подряд растущие свечи. 2 вход: две падающие свечи и одна растущая
        // все по лимитам с проскальзыванием 2 шага
        // стопы и тейк-профиты

        public DemoRobot1(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabsSimple[0].CandleFinishedEvent += DemoRobot1_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += DemoRobot1_PositionOpeningSuccesEvent;       

        }

        private void DemoRobot1_PositionOpeningSuccesEvent(Position position)
        {
            if (position.SignalTypeOpen == "PatternOne")
            { //стоп для  1 паттерна
                TabsSimple[0].CloseAtStop(position, position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 100,
                    position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 110);

                TabsSimple[0].CloseAtProfit(position, position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 80,
                    position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 70);
            }

            if (position.SignalTypeOpen == "PatternTwo")
            { //стоп для  2 паттерна
                TabsSimple[0].CloseAtStop(position, position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 200,
                    position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 220);

                TabsSimple[0].CloseAtProfit(position, position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 150,
                    position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 140);
            }

        }

        private void DemoRobot1_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count < 5) return;

            List<Position> openPositions = TabsSimple[0].PositionsOpenAll;
            if (openPositions == null || openPositions.Count == 0)
            { //ищем вход по 1 паттерну
                MethodForFindPatternOne(candles);
            }
            else if (openPositions.Count == 1)
            { //ищем вход по 2 паттерну
                MethodForFindPatternTwo(candles);
            }
        }

        public void MethodForFindPatternOne(List<Candle> candles)
        {
            Candle candle1 = candles[candles.Count - 2];
            Candle candle2 = candles[candles.Count - 1];

            if (candle1.Close > candle1.Open && candle2.Close > candle2.Open)
            {
                decimal openPrice = candle2.Close + TabsSimple[0].Securiti.PriceStep * 2;
                TabsSimple[0].BuyAtLimit(1, openPrice, "PatternOne");
            }
        }

        public void MethodForFindPatternTwo(List<Candle> candles)
        {
            Candle candle1 = candles[candles.Count - 3];
            Candle candle2 = candles[candles.Count - 2];
            Candle candle3 = candles[candles.Count - 1];

            if (candle1.Close < candle1.Open && candle2.Close < candle2.Open && candle3.Close > candle3.Open)
            {
                decimal openPrice = candle3.Close + TabsSimple[0].Securiti.PriceStep * 2;
                TabsSimple[0].BuyAtLimit(1, openPrice, "PatternTwo");
            }
        }

        public override string GetNameStrategyType()
        {
            return "DemoRobot1";
        }

        public override void ShowIndividualSettingsDialog()
        {
           //нет параметров
        }
    }
}

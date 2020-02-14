using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.aLibraries.Levels;

namespace OsEngine.Robots.aDev
{
    //Гипотезы:
    //1.Работаем на 5 минутках. Ищем воздушные уровни где цена уперлась и несколько раз бьется в один уровень
    //2.Бьется 4-Х раз в один уровень, копейка в копейку
    //3.Бьется 4-Х раз в один уровень, с учетом люфта
    //4.Бьется 4-Х раз в один уровень с учетом люфта и 1-Х проколов хвостами
    //5.Бьется 4-Х раз в один уровень с учетом люфта, проколов и ложных пробоев (телами)


    //ToDo:
    //1.Ищем уровни копейка в копейку
    //2.Ищем уровни в одну цену с учетом люфта
    //3.Ищем уровни в одну цену с учетом люфта и проколов хвостами
    //4.Ищем уровни с учетом люфта, проколов и ложных пробоев телами
    //5.Сделать условный вход для с шансами 3к1+ для тестов
    //6.Уровни в пределах люфта надо объединять в один повторяющийся (+балл)
    //

 


    class Test2 : BotPanel
    {

        //параметры
        private int candlesDepth;  //количество свечей влево для поиска экстремумов
        private int candlesForSearchExtremums;  //количество свечей слева и справа для поиска поиска экстремума
        private int candlesOnLevelCount; //количество баров для подтверждения воздушного уровня



        private BotTabSimple tab0;
        private ExtremumsSet extremums;
        private AirLevelsSet levels;

        public Test2(string name, StartProgram startProgram) : base(name, startProgram)
        {

            //инициализация параметров
            candlesDepth = 50;
            candlesForSearchExtremums = 2;
            candlesOnLevelCount = 4;


            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];

            extremums = new ExtremumsSet(tab0, candlesDepth);
            levels = new AirLevelsSet(tab0, candlesDepth, true);


            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;
            tab0.PositionOpeningSuccesEvent += Tab0_PositionOpeningSuccesEvent;

        }

        private void Tab0_PositionOpeningSuccesEvent(Position position)
        {

            int stop = 10;
            int take = 30;
            
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

        private void OpenPosition(bool longPosition)
        {
            if (longPosition)
            {
                tab0.BuyAtMarket(1);
            } else
            {
                tab0.SellAtMarket(1);
            }
            

        }
        

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            List<Position> positions = TabsSimple[0].PositionsOpenAll;
            if (positions != null && positions.Count != 0)
            {
                return;
            }


            if (candles.Count < 10)
            {             
                if (extremums.Count > 0)
                {  // если это перезапуск теста, обнуляем экстремумы и уровни
                    extremums = new ExtremumsSet(tab0, candlesDepth);
                    levels = new AirLevelsSet(tab0, candlesDepth, true);
                    tab0.DeleteAllChartElement();
                }

                return;
            }
                
            
            //ищем экстремумы     
            Extremum.FindExtremums(extremums, tab0, candles, candlesDepth, candlesForSearchExtremums);
            extremums.RefreshAllExtremumsOnChart();

            //ищем воздушные четкие уровни
            AirLevel.FindAirExactLevels(levels, extremums, tab0, candles, candlesDepth, candlesOnLevelCount);
            levels.RefreshAllExtremumsOnChart();

            //если последний уровень на последнем баре - входим
            if(levels.Count > 0)
            {
                var lastLevel = levels.items[levels.items.Count - 1];
                var lastCandle = lastLevel.confirmingCandles[lastLevel.confirmingCandles.Count - 1];

                if (candles.IndexOf(lastCandle) == candles.Count - 1)
                {
                    if (lastLevel.highLowType == HighLowLevelTypes.Low)
                    {
                        OpenPosition(true);
                    }
                    else
                    {
                        OpenPosition(false);
                    }
                }
            }
            

        }

        public override string GetNameStrategyType()
        {
            return "Test2";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }
    }
}

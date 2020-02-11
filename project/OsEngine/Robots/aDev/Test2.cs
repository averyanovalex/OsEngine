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
    //0.Выносим в отдельную библиотеку "Уровни", делаем общий предок для прориросовки. 
    //Отключаемая пророисовка только у значимых сейчас экстремумов и уровней
    //Ошибка с повтороной прорисовкой
    //1.Найти экстремумы
    //2.Найти уровни от экстремумов по пунктам 2-5
    //3.Сделать условный вход для с шансами 3к1+ для тестов
    //4.Уровни в пределах люфта надо объединять в один повторяющийся (+балл)
    //

 


    class Test2 : BotPanel
    {

        //параметры
        private int candlesDepth;  //количество свечей влево для поиска экстремумов
        private int candlesForSearchExtremums;  //количество свечей слева и справа для поиска поиска экстремума



        private BotTabSimple tab0;
        private ExtremumsSet extremums;

        public Test2(string name, StartProgram startProgram) : base(name, startProgram)
        {

            //инициализация параметров
            candlesDepth = 50;
            candlesForSearchExtremums = 2;



            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];

            extremums = new ExtremumsSet(tab0, candlesDepth);



            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;

        }


        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            if (candles.Count < 10) return;

            //ищем экстремумы     
            Extremum.FindExtremums(extremums, tab0, candles, candlesDepth, candlesForSearchExtremums);
            extremums.RefreshAllExtremumsOnChart();

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

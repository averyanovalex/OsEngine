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

namespace OsEngine.Robots.aDev
{
    //Гипотезы:
    //1.Работаем на 5 минутках. Ищем воздушные уровни где цена уперлась и несколько раз бьется в один уровень
    //2.Бьется 4-Х раз в один уровень, копейка в копейку
    //3.Бьется 4-Х раз в один уровень, с учетом люфта
    //4.Бьется 4-Х раз в один уровень с учетом люфта и 1-Х проколов хвостами
    //5.Бьется 4-Х раз в один уровень с учетом люфта, проколов и ложных пробоев (телами)


    //ToDo:
    //1.Найти экстремумы
    //2.Найти уровни от экстремумов по пунктам 2-5
    //3.Сделать условный вход для с шансами 3к1+ для тестов
    //4.Уровни в пределах люфта надо объединять в один повторяющийся (+балл)
    //

    enum ChartLevelTypes
    {
        Low,    //уровень поддержки
        High  //уровень сопротивления
    }


    class Test2 : BotPanel
    {

        //параметры
        public int candlesDepth;


        public BotTabSimple tab0;

        public Test2(string name, StartProgram startProgram) : base(name, startProgram)
        {

            //инициализация параметров
            candlesDepth = 100;
            
            
            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];

            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;

        }

        private bool isExtremum(ChartLevelTypes extremumType, List<Candle> candles, 
                                    int index, int leftCandlesCount, int rightCandlesCount)
        {
            decimal candleLow = candles[index].Low;
            decimal candleHigh = candles[index].High;

            //смотрим слева
            int indexStart = index - leftCandlesCount;
            int indexEnd = index - 1;
            for (int i = indexStart; i <= indexEnd; i++)
            {
                if (extremumType == ChartLevelTypes.Low)  
                {
                    if (candles[i].Low < candleLow) return false;
                }
                else
                {
                    if (candles[i].High > candleHigh) return false;
                }            
            }

            //если это крайний бар, смотреть справо не надо
            if (index == candles.Count - 1)
            {
                return true;
            }

            //смотрим справа
            indexStart = index + 1;
            indexEnd = index + rightCandlesCount;
            indexEnd = indexEnd > candles.Count - 1 ? candles.Count - 1 : indexEnd;
            for (int i = indexStart; i <= indexEnd; i++)
            {
                if (extremumType == ChartLevelTypes.Low)
                {
                    if (candles[i].Low < candleLow) return false;
                }
                else
                {
                    if (candles[i].High > candleHigh) return false;
                }
            }

            //похоже у проверямой свечи самый низкий Low или самый высокий High
            return true;
        }

        private void DrawLevel(decimal value, string name, DateTime timeStart, DateTime timeEnd)
        {

            LineHorisontal level = new LineHorisontal(name, "Prime", false)
            {
                Color = Color.Green,
                Value = value,
                TimeStart = timeStart,
                TimeEnd = timeEnd
            };
            TabsSimple[0].SetChartElement(level);
            level.Refresh();
        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            if (candles.Count < 10) return;

            //ищем экстремумы     
            List<int> lowExtremums = new List<int>();
            List<int> HighExtremums = new List<int>();
            tab0.DeleteAllChartElement();

            int indexStart = candles.Count - candlesDepth + 1;
            indexStart = indexStart < 5 ? 5 : indexStart;

            int indexEnd = candles.Count - 1;


            for (int i = indexStart; i <= indexEnd; i++)
            {
                if (isExtremum(ChartLevelTypes.Low, candles, i, 5, 5)){
                    lowExtremums.Add(i);
                }

                if (isExtremum(ChartLevelTypes.High, candles, i, 5, 5))
                {
                    HighExtremums.Add(i);
                }

            }

            //отрисуем уровни у экстремумов
            
            foreach (int index in lowExtremums)
            {
                DateTime timeStart = candles[index - 1].TimeStart;
                DateTime timeEnd;
                if (index + 1 > candles.Count - 1) 
                {
                    timeEnd = candles[candles.Count - 1].TimeStart;
                }
                else
                {
                    timeEnd = candles[index + 1].TimeStart;
                }

                string levelName = "level_low_" + Convert.ToString(index);
                DrawLevel(candles[index].Low, levelName, timeStart, timeEnd);
            }


            foreach (int index in HighExtremums)
            {
                DateTime timeStart = candles[index - 1].TimeStart;
                DateTime timeEnd;
                if (index + 1 > candles.Count - 1)
                {
                    timeEnd = candles[candles.Count - 1].TimeStart;
                }
                else
                {
                    timeEnd = candles[index + 1].TimeStart;
                }

                string levelName = "level_high_" + Convert.ToString(index);
                DrawLevel(candles[index].High, levelName, timeStart, timeEnd);
            }
            //System.Windows.MessageBox.Show("Пауза");

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

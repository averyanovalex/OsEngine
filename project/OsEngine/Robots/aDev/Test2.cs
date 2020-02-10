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
    //0.Выносим в отдельную библиотеку "Уровни", делаем общий предок для прориросовки. 
    //Отключаемая пророисовка только у значимых сейчас экстремумов и уровней
    //Ошибка с повтороной прорисовкой
    //1.Найти экстремумы
    //2.Найти уровни от экстремумов по пунктам 2-5
    //3.Сделать условный вход для с шансами 3к1+ для тестов
    //4.Уровни в пределах люфта надо объединять в один повторяющийся (+балл)
    //

    public enum ExtremumTypes
    {
        Low,    //уровень поддержки
        High  //уровень сопротивления
    }

    public class Extremum
    {
        public DateTime time;
        public TimeSpan timeFrame;
        public ExtremumTypes type;
        public decimal value;
        private LineHorisontal lineOnChart;

        public Extremum(DateTime time, TimeSpan timeFrame, ExtremumTypes type, decimal value=0)
        {
            this.time = time;
            this.type = type;
            this.value = value;
            this.timeFrame = timeFrame;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            
            Extremum e = obj as Extremum;
            if (e == null)
            {
                return false;
            }

            return e.time == this.time && e.type == this.type && e.timeFrame == this.timeFrame;
        }


        private void DrawLine(BotTabSimple chart, decimal value, string name, DateTime timeStart, DateTime timeEnd)
        {

            lineOnChart = new LineHorisontal(name, "Prime", false)
            {
                Color = Color.Green,
                Value = value,
                TimeStart = timeStart,
                TimeEnd = timeEnd
            };
            chart.SetChartElement(lineOnChart);
            //lineOnChart.Refresh();
        }

        public void DrawOnChart(BotTabSimple chart)
        {
            DateTime timeStart = time.AddSeconds(-1 * timeFrame.TotalSeconds);
            DateTime timeEnd = time.AddSeconds(+1 * timeFrame.TotalSeconds);
            string levelName = "level " + Convert.ToString(time);
            DrawLine(chart, value, levelName, timeStart, timeEnd);
        }

        public void DeleteFromChart(BotTabSimple chart)
        {
            chart.DeleteChartElement(lineOnChart);
            lineOnChart = null;
        }

        public void RefreshOnChart()
        {
            lineOnChart.Refresh();
        }
        
    }



    public class ExtremumsSet
    {
        private List<Extremum> items;
        private BotTabSimple chart;

        public ExtremumsSet(BotTabSimple chart)
        {
            this.chart = chart;
            
            items = new List<Extremum>();
        }

        public void Add(Extremum newItem)
        {
            if (items.Contains(newItem))
            {
                return;
            }
            else
            {
                items.Add(newItem);
                newItem.DrawOnChart(chart);
                
                //удаляем, если есть уже лишний экстремум на предыдущем баре
                DateTime date = newItem.time.AddSeconds(-1 * newItem.timeFrame.TotalSeconds);

                var mustBeDeleted = items.Find(item => item.time == date && item.timeFrame == newItem.timeFrame 
                                                    && item.type == newItem.type);
                
                if (mustBeDeleted != null)
                {
                    mustBeDeleted.DeleteFromChart(chart);
                    items.Remove(mustBeDeleted);
                }

            }
        }

        public void RefreshAllExtremumsOnChart()
        {
            foreach (Extremum item in items)
            {
                item.RefreshOnChart();
            }
        }
    }


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
            candlesDepth = 100;
            candlesForSearchExtremums = 2;



            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];

            extremums = new ExtremumsSet(tab0) ;



            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;

        }

        private bool isExtremum(ExtremumTypes extremumType, List<Candle> candles, 
                                    int index, int leftRightCandlesCount)
        {
            decimal candleLow = candles[index].Low;
            decimal candleHigh = candles[index].High;

            //смотрим слева
            int indexStart = index - leftRightCandlesCount;
            int indexEnd = index - 1;
            for (int i = indexStart; i <= indexEnd; i++)
            {
                if (extremumType == ExtremumTypes.Low)  
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
            indexEnd = index + leftRightCandlesCount;
            indexEnd = indexEnd > candles.Count - 1 ? candles.Count - 1 : indexEnd;
            for (int i = indexStart; i <= indexEnd; i++)
            {
                if (extremumType == ExtremumTypes.Low)
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



        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            if (candles.Count < 10) return;

            //ищем экстремумы     
            List<int> lowExtremums = new List<int>();
            List<int> HighExtremums = new List<int>();
            tab0.DeleteAllChartElement();

            int indexStart = candles.Count - candlesDepth + 1;
            indexStart = indexStart < candlesForSearchExtremums ? candlesForSearchExtremums : indexStart;
            int indexEnd = candles.Count - 1;

            for (int i = indexStart; i <= indexEnd; i++)
            {
                if (isExtremum(ExtremumTypes.Low, candles, i, candlesForSearchExtremums))
                {
                    
                    Extremum newExtremum = new Extremum(candles[i].TimeStart, 
                                                        tab0.TimeFrame, 
                                                        ExtremumTypes.Low, 
                                                        candles[i].Low);

                    extremums.Add(newExtremum);
                }

                if (isExtremum(ExtremumTypes.High, candles, i, candlesForSearchExtremums))
                {
                    Extremum newExtremum = new Extremum(candles[i].TimeStart,
                                                        tab0.TimeFrame,
                                                        ExtremumTypes.High,
                                                        candles[i].High);

                    extremums.Add(newExtremum);
                }

            }

            //extremums.RefreshAllExtremumsOnChart();

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

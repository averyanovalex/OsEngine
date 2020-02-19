﻿using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;


namespace OsEngine.Robots.aLibraries.Levels
{

    
    //description:
    //Класс определяем такую сущность как экстремум: локальный минимум или максимум
    public class Extremum: LevelLine
    {

        #region block: fields

        public DateTime time;
        public TimeSpan timeFrame;
        public HighLowLevelTypes type;
        public decimal value;
        public Candle candle;
        public bool marked;

        #endregion


        #region block: constructors, override methods

        public Extremum(BotTabSimple chart, DateTime time, HighLowLevelTypes type, Candle candle,  decimal value = 0)
                        : base(chart)
        {
            this.time = time;
            this.type = type;
            this.value = value;
            this.timeFrame = chart.TimeFrame;
            this.candle = candle;
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

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void DrawOnChart()
        {
            DateTime timeStart = time.AddSeconds(-1 * timeFrame.TotalSeconds);
            DateTime timeEnd = time.AddSeconds(+1 * timeFrame.TotalSeconds);
            string levelName = "level " + Convert.ToString(time);
            DrawLine(value, levelName, timeStart, timeEnd, Color.Green);
        }

        public override void DeleteFromChart()
        {
            chart.DeleteChartElement(lineOnChart);
            lineOnChart = null;
        }

        #endregion


        #region block: static methods

        //description
        //Метод проверяем является ли свеча экстремумом
        //parametres:
        //  extremumType            - тип экстремума, на который проверяем: Low или High
        //  candles                 - массив свечек
        //  index                   - индекс проверяемой свечки
        //  leftRightCandlesCount   - количество свечек слева и справа для проверки. 
        //                            Для Low проверяемая свеча должна быть ниже чем свечки слева и справа.
        //                          - Для High наоборот
        public static bool CandleIsExtremum(HighLowLevelTypes extremumType, List<Candle> candles,
                                    int index, int leftRightCandlesCount)
        {
            decimal candleLow = candles[index].Low;
            decimal candleHigh = candles[index].High;

            //смотрим слева
            int indexStart = index - leftRightCandlesCount;
            int indexEnd = index - 1;
            for (int i = indexStart; i <= indexEnd; i++)
            {
                if (extremumType == HighLowLevelTypes.Low)
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
                if (extremumType == HighLowLevelTypes.Low)
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


        //description
        //Метод ищет и возвращает все экстремумы в массиве свечек.
        //Метод принимаем на вход найденные ранее экстремумы и обновляет этот список с учетом новых свечей
        //Метод удобно использовать в событиях CandleFinishedEvent. С каждой новой свечкой,
        //список экстремумов будет обновляться
        //parametres:
        //  extremums (out param)   - набор найденных ранее экстремумов. Обновляется в методе
        //  chart                   - панель робота с графиком инструмента
        //  candles                 - актуальный массив свечек
        //  candlesDepth            - глубина поиска. Как глубоко назад в истории искать экстремумы
        //  leftRightCandlesCount   - количество свечек слева и справа анализируемой свечи, чтобы определить ее как экстремум
        public static void FindExtremums(ExtremumsSet extremums, BotTabSimple chart, List<Candle> candles, 
                                            int candlesDepth, int leftRightCandlesCount)
        {

            if (extremums == null)
            {
                extremums = new ExtremumsSet(chart, candlesDepth);
            }
            
            
            int indexStart = candles.Count - candlesDepth + 1;
            indexStart = indexStart < leftRightCandlesCount ? leftRightCandlesCount : indexStart;
            int indexEnd = candles.Count - 1;

            for (int i = indexStart; i <= indexEnd; i++)
            {
                
                //проверяем может это нижний экстремум
                if (CandleIsExtremum(HighLowLevelTypes.Low, candles, i, leftRightCandlesCount))
                {

                    Extremum newExtremum = new Extremum(chart, candles[i].TimeStart,
                                                        HighLowLevelTypes.Low, candles[i],
                                                        candles[i].Low);

                    extremums.Add(newExtremum);
                }

                //проверяем, может это верхний экстремум
                if (CandleIsExtremum(HighLowLevelTypes.High, candles, i, leftRightCandlesCount))
                {
                    Extremum newExtremum = new Extremum(chart, candles[i].TimeStart,
                                                        HighLowLevelTypes.High, candles[i],
                                                        candles[i].High);

                    extremums.Add(newExtremum);
                }

            }


        }


        #endregion



    }


    public class ExtremumsSet
    {
        public List<Extremum> items;
        private BotTabSimple chart;
        private int candlesDepth;
        
        public int Count {
            get { return items.Count; }
            set { }
        }

        public bool AutomaticlyDrawOnChart { get; set; }

        public ExtremumsSet(BotTabSimple chart, int candlesDepth, bool automaticlyDrawOnChart = false)
        {
            this.chart = chart;
            this.candlesDepth = candlesDepth;
            this.AutomaticlyDrawOnChart = automaticlyDrawOnChart;

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

                if (AutomaticlyDrawOnChart)
                {
                    newItem.DrawOnChart();
                }
                
                

                //удаляем, если есть уже лишний экстремум на предыдущем баре
                DateTime date = newItem.time.AddSeconds(-1 * newItem.timeFrame.TotalSeconds);

                var mustBeDeleted = items.Find(item => item.time == date && item.timeFrame == newItem.timeFrame
                                                    && item.type == newItem.type);

                if (mustBeDeleted != null)
                {
                    if (AutomaticlyDrawOnChart) 
                    {
                        mustBeDeleted.DeleteFromChart();
                    }           
                    items.Remove(mustBeDeleted);
                }


                //удаляем если есть экстремумы, вышедшие за границы анализа
                DateTime borderTime = newItem.time.AddSeconds(-1 * newItem.timeFrame.TotalSeconds * candlesDepth);

                var listOfMustBeDeleted = items.FindAll(item => item.time < borderTime);
                foreach (var item in listOfMustBeDeleted)
                {
                 
                    if (AutomaticlyDrawOnChart)
                    {
                        item.DeleteFromChart();
                    }   
                    items.Remove(item);
                }

            }
        }

        public void RefreshAllExtremumsOnChart()
        {

            if (!AutomaticlyDrawOnChart) return;

            foreach (Extremum item in items)
            {
                item.RefreshOnChart();
            }
        }
    }



}
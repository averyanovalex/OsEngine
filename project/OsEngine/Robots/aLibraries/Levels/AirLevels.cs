using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;


namespace OsEngine.Robots.aLibraries.Levels
{
    //description:
    //Класс определяем такую сущность как воздушный уровень
    public class AirLevel : LevelLine
    {

        #region block: fields

        public DateTime timeStart;
        public DateTime timeEnd;
        public HighLowLevelTypes highLowType;
        //Тип копейка в копейку или нет
        public decimal value;
        public List<Candle> confirmingCandles;

        #endregion


        #region block: constructors, override methods

        public AirLevel(BotTabSimple chart, DateTime timeStart, DateTime timeEnd, HighLowLevelTypes highLowType, 
                            decimal value, List<Candle> confirmingCandles) :  base(chart)
        {

            this.timeStart = timeStart;
            this.timeEnd = timeEnd;
            this.highLowType = highLowType;
            this.value = value;
            this.confirmingCandles = confirmingCandles;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            AirLevel e = obj as AirLevel;
            if (e == null)
            {
                return false;
            }

            return e.timeStart == this.timeStart && e.timeEnd == this.timeEnd && e.value == this.value;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void DeleteFromChart()
        {
            chart.DeleteChartElement(lineOnChart);
            lineOnChart = null;
        }

        public override void DrawOnChart()
        {
            string levelName = "level " + Convert.ToString(timeStart);
            DrawLine(value, levelName, timeStart, timeEnd, Color.Red);
        }

        #endregion


        #region static

        public static bool ItsAirExactLevel(Extremum extremum, int candlesCount, List<Candle> candles, out List<Candle> candlesOnLevel)
        {

            Candle candle = extremum.candle;
            int index = candles.IndexOf(candle);
            decimal extremumPrice = extremum.value;
            bool itsLowExtremum = extremum.type == HighLowLevelTypes.Low;

            int rightExtremeIndex = index + candlesCount - 1;


            for (int i = rightExtremeIndex; i >= index; i--)
            {
                if (i > candles.Count - 1) continue;


                candlesOnLevel = new List<Candle>();
                for (int j = i - candlesCount + 1; j <= i; j++)
                {
                    decimal price = itsLowExtremum ? candles[j].Low : candles[j].High;
                    if (price == extremumPrice)
                    {
                        candlesOnLevel.Add(candles[j]);
                    }

                }

                if (candlesOnLevel.Count == candlesCount)
                {
                    return true;
                }

            }

            candlesOnLevel = null;
            return false;
        }

        public static void FindAirExactLevels(AirLevelsSet levels, ExtremumsSet extremums, BotTabSimple chart, List<Candle> candles,
                                           int candlesDepth, int candlesOnLevelCount)
        {

            if (levels == null)
            {
                levels = new AirLevelsSet(chart, candlesDepth);
            }


            foreach (Extremum extremum in extremums.items)
            {
                if (extremum.marked) continue;

                List<Candle> candlesOnLevel;
                if (AirLevel.ItsAirExactLevel(extremum, candlesOnLevelCount, candles, out candlesOnLevel))
                {
                    extremum.marked = true;

                    var timeStart = candlesOnLevel[0].TimeStart;
                    var timeEnd = candlesOnLevel[candlesOnLevel.Count-1].TimeStart;
                    AirLevel newLevel = new AirLevel(chart, timeStart, timeEnd, extremum.type, extremum.value, candlesOnLevel);

                    levels.Add(newLevel);
                }
            }

            

        }


        #endregion

    }


    public class AirLevelsSet
    {
        public List<AirLevel> items;
        private BotTabSimple chart;
        private int candlesDepth;

        public int Count
        {
            get { return items.Count; }
            set { }
        }

        public bool AutomaticlyDrawOnChart { get; set; }

        public AirLevelsSet(BotTabSimple chart, int candlesDepth, bool automaticlyDrawOnChart = false)
        {
            this.chart = chart;
            this.candlesDepth = candlesDepth;
            this.AutomaticlyDrawOnChart = automaticlyDrawOnChart;

            items = new List<AirLevel>();
        }


        public void Add(AirLevel newItem)
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

                //удаляем если есть экстремумы, вышедшие за границы анализа
                DateTime borderTime = newItem.timeEnd.AddSeconds(-1 * chart.TimeFrame.TotalSeconds * candlesDepth);

                var listOfMustBeDeleted = items.FindAll(item => item.timeEnd < borderTime);
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

            foreach (AirLevel item in items)
            {
                item.RefreshOnChart();
            }
        }
    }

}
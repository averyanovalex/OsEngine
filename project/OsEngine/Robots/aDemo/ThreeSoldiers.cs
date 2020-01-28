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
    /// <summary>
    /// Учебный робот. Свечной паттерн "3 солдата"
    /// Алгоритм:
    /// 1.Вход в лонг 3 подряд зеленые свечи и Close последней свечи выше 20 последних свечек
    /// 2.Выход 3 подряд красные свечи
    /// 3.Стоп за хвост третьего солдата
    ///
    /// </summary>
    public class DemoThreeSoldiers : BotPanel
    {

        private decimal stopPrice;

        public DemoThreeSoldiers(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            TabsSimple[0].CandleFinishedEvent += DemoHammer_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += DemoHammer_PositionOpeningSuccesEvent;

        }

        private void DemoHammer_PositionOpeningSuccesEvent(Position position)
        {
            TabsSimple[0].CloseAtStop(position, stopPrice, stopPrice);
        }

        
        private bool IsGrowingCandle(Candle candle)
        {
            return candle.Close > candle.Open;
        }

        private bool IsFallingCandle(Candle candle)
        {
            return candle.Close < candle.Open;
        }

        private void DemoHammer_CandleFinishedEvent(List<Candle> candles)
        {

            if (candles.Count < 21)
            { //если свечей меньше 21, то не входим
                return;
            }

            var candle1 = candles[candles.Count - 3];
            var candle2 = candles[candles.Count - 2];
            var candle3 = candles[candles.Count - 1];

            //логика закрытия 3 подряд красных свечи, если есть  открытые позы
            if (TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count != 0)
            {

                if (IsFallingCandle(candle1) && IsFallingCandle(candle2) && IsFallingCandle(candle3))
                {
                    TabsSimple[0].CloseAllAtMarket();
                }

                return;
            }


            //если хотя бы одна из свечей не растущая - не открываем позу
            if (!IsGrowingCandle(candle1) || !IsGrowingCandle(candle2) || !IsGrowingCandle(candle3))
            {
                return;
            }

            //проверяем, чтобы close последней свечи был выше 20 последних свечек 
            decimal lastClose = candle3.Close;
            for (int i = candles.Count - 2; i > candles.Count - 21; i--)
            {
                if (lastClose < candles[i].High) return;
            }


            //можем открывать позицию
            TabsSimple[0].BuyAtMarket(1);
            stopPrice = candle3.Low - 5* TabsSimple[0].Securiti.PriceStep;

        }

        public override string GetNameStrategyType()
        {
            return "DemoThreeSoldiers";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //нет настраиваемых параметров
        }
    }
}

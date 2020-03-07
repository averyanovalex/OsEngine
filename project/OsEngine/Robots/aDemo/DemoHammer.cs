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
    /// Учебный робот. Свечной паттерн "Молот"
    /// </summary>
    public class DemoHammer : BotPanel
    {

        private DateTime timeToClose;
        private decimal stopPrice;
        
        public DemoHammer(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            TabsSimple[0].CandleFinishedEvent += DemoHammer_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += DemoHammer_PositionOpeningSuccesEvent;

        }

        private void DemoHammer_PositionOpeningSuccesEvent(Position position)
        {
            TabsSimple[0].CloseAtStop(position, stopPrice, stopPrice);
        }

        private void DemoHammer_CandleFinishedEvent(List<Candle> candles)
        {
            
            //логика закрытия по времени, если есть  открытые позы
            if(TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count != 0)
            {
                if (candles[candles.Count-1].TimeStart >= timeToClose)
                {
                    TabsSimple[0].CloseAllAtMarket();
                }

                return;
            }
            
            
            if (candles.Count < 21)
            { //если свечей меньше 21, то не входим
                return;
            }

            var lastCandle = candles[candles.Count - 1]; 
            if (lastCandle.Open >= lastCandle.Close)
            { //если последняя свеча (ожидаемый молот) не растущая, не входим
                return;
            }

            //проверяем, чтобы последний лой был самой нижней точкой за 20 последние свечки
            decimal lastLow = lastCandle.Low;
            for (int i = candles.Count-2; i > candles.Count-21; i--)
            {
                if (lastLow > candles[i].Low) return;
            }

            //проверяем, чтобы тело было в 3 раза меньше хвоста снизу и не больше хвоста сверху
            decimal body = lastCandle.Close - lastCandle.Open;
            decimal shadowLow = lastCandle.Open - lastCandle.Low;
            decimal shadowHigh = lastCandle.High - lastCandle.Close;

            if (body < shadowHigh) return;
            if (shadowLow / 3 < body) return;


            //можем открывать позицию
            TabsSimple[0].BuyAtMarket(1);
            timeToClose = lastCandle.TimeStart.AddMinutes(15);
            stopPrice = lastCandle.Low - TabsSimple[0].Securiti.PriceStep;



        }

        public override string GetNameStrategyType()
        {
            return "DemoHammer";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //нет настраиваемых параметров
        }
    }
}

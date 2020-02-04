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

    //Учебный робот. Трендовый. Работает на MA, ATR и Конвертах.  
    //Входим в лонг когда закрытие свечи выше верхнего значения конверта
    //Выходим когда закрытие свечи ниже мувинг - атр*2

    class DemoTrendEnvelopes : BotPanel
    {
        public MovingAverage _moving;
        public Atr _atr;
        public Envelops _envelops;
        
        public DemoTrendEnvelopes(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            _moving = new MovingAverage("moving1", false);
            _moving = (MovingAverage)TabsSimple[0].CreateCandleIndicator(_moving, "Prime");
            _moving.Save();

            _atr = new Atr("atr1", false);
            _atr = (Atr)TabsSimple[0].CreateCandleIndicator(_atr, "NewArea");
            _atr.Save();

            _envelops = new Envelops("envelops1", false);
            _envelops = (Envelops)TabsSimple[0].CreateCandleIndicator(_envelops, "Prime");
            _envelops.Deviation = 0.25m;
            _envelops.Save();

            TabsSimple[0].CandleFinishedEvent += DemoTrendEnvelopes_CandleFinishedEvent;
        }

        private void DemoTrendEnvelopes_CandleFinishedEvent(List<Candle> candles)
        {
            if (_moving.Lenght > candles.Count || _atr.Lenght > candles.Count) return;

            //if (candles[candles.Count - 1].TimeStart.Hour < 11) return;


            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if (positions != null && positions.Count !=0)
            { //если поза есть, но она пока не открылась
                if (positions[0].State != PositionStateType.Open)
                {
                    return;
                }
            }


            if (positions == null || positions.Count == 0)
            { // логика открытия

                if (candles[candles.Count-1].Close > _envelops.ValuesUp[_envelops.ValuesUp.Count - 1])
                {
                    TabsSimple[0].BuyAtLimit(1, candles[candles.Count - 1].Close);
                }

            }
            else
            {  // логика закрытия

                if (candles[candles.Count-1].Close < 
                        _moving.Values[_moving.Values.Count-1] - _atr.Values[_atr.Values.Count-1] * 2)
                {
                    TabsSimple[0].CloseAtLimit(positions[0], candles[candles.Count - 1].Close, positions[0].OpenVolume);
                }

            }

        }

        public override string GetNameStrategyType()
        {
            return "DemoTrendEnvelopes";
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }
    }
}

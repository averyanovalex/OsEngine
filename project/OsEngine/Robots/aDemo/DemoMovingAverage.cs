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
    //Учебный бот. Входим, когда цена выше MA и выход, когда цена опустилась ниже MA.

    class DemoMovingAverage : BotPanel
    {

        MovingAverage _moving;

        public DemoMovingAverage(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _moving = new MovingAverage("moving1", false);
            _moving = (MovingAverage)TabsSimple[0].CreateCandleIndicator(_moving, "Prime");
            _moving.Save();

            TabsSimple[0].CandleFinishedEvent += DemoMovingAverage_CandleFinishedEvent;


        }

        private void DemoMovingAverage_CandleFinishedEvent(List<Candle> candles)
        {
            if (_moving.Lenght >= candles.Count) return;

            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if (positions == null || positions.Count == 0)
            { //позиции нет, пытаемся открыть

                if(candles[candles.Count-1].Close > _moving.Values[_moving.Values.Count - 1])
                {
                    TabsSimple[0].BuyAtLimit(1, candles[candles.Count - 1].Close);
                }

            }
            else
            {  //позиция есть, ищем точку выхода

                if (candles[candles.Count-1].Close < _moving.Values[_moving.Values.Count - 1])
                {

                    if (positions[0].State != PositionStateType.Open) return;

                    TabsSimple[0].CloseAtLimit(positions[0], candles[candles.Count - 1].Close, positions[0].OpenVolume);
                }
            }

        }

        public override string GetNameStrategyType()
        {
            return "DemoMovingAverage";
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }
    }
}

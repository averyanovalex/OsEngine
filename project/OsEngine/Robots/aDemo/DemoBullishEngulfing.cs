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
    //<summary>
    //Учебный робот. Бычье поглощение. Робот с параметрами.
    //Условия:
    //1.Текущая свеча растущая, а предыдущая падающая
    //2.Тело растущей свечи минимум в 3 раза больше свечи падающей
    //3.Пять свячей назад хай выше хая последней свечи. Т.е. имеем какой-то локальный лой и разворот от него
    //</summary>
    
    public class DemoBullishEngulfing : BotPanel
    {
 
        public int Stop;
        public int Profit;
        public int Sleepage;
        public int Volume;
        public bool IsOn;
        
        public DemoBullishEngulfing(string name, StartProgram startProgram) : base(name, startProgram) 
        {
            Stop = 10;
            Profit = 20;
            Sleepage = 2;
            Volume = 2;
            IsOn = true;

            Load();

            TabCreate(BotTabType.Simple);
            TabsSimple[0].CandleFinishedEvent += DemoBullishEngulfing_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += DemoBullishEngulfing_PositionOpeningSuccesEvent;


    }

        private void DemoBullishEngulfing_PositionOpeningSuccesEvent(Position position)
        {

            TabsSimple[0].CloseAtStop(
                position,
                position.EntryPrice - Stop * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice - Stop * TabsSimple[0].Securiti.PriceStep - Sleepage * TabsSimple[0].Securiti.PriceStep
                );

            TabsSimple[0].CloseAtProfit(
                position,
                position.EntryPrice + Profit * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice + Profit * TabsSimple[0].Securiti.PriceStep - Sleepage * TabsSimple[0].Securiti.PriceStep
                );

        }

        private void DemoBullishEngulfing_CandleFinishedEvent(List<Candle> candles)
        {

            if (candles.Count < 5)
            {
                return;
            }

            if (IsOn == false)
            {
                return;
            }

            if (TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count > 0)
            {
                return;
            }
            
            Candle lastCandle = candles[candles.Count - 1];
            Candle secondCandle = candles[candles.Count - 2];

            if (lastCandle.Close > lastCandle.Open && secondCandle.Close < secondCandle.Open)
            {
                
                decimal bodyLast = lastCandle.Close - lastCandle.Open;
                decimal bodySecond = secondCandle.Open - secondCandle.Close;
                if ((bodyLast / 3) >= bodySecond)
                {

                    if (candles[candles.Count - 5].High > lastCandle.High)
                    {
                        TabsSimple[0].BuyAtLimit(Volume, lastCandle.Close + Sleepage * TabsSimple[0].Securiti.PriceStep);
                    }
                    
                } 
            }

        }

        public override string GetNameStrategyType()
        {
            return "DemoBullishEngulfing";
        }

        public override void ShowIndividualSettingsDialog()
        {
            var ui = new DemoBullishEngulfingUi(this);
            ui.ShowDialog();
        }


        public void Save()
        {
            
            try
            {
                if (string.IsNullOrWhiteSpace(NameStrategyUniq))
                {
                    return;
                }

                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @".txt", false))
                {

                    writer.WriteLine(Stop);
                    writer.WriteLine(Profit);
                    writer.WriteLine(Sleepage);
                    writer.WriteLine(Volume);
                    writer.WriteLine(IsOn);


                    writer.Close();
                }
            }
            catch (Exception)
            {
                // send to log
                // отправить в лог
            }
            
        }

        public void Load()
        {
            
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @".txt"))
            {
                return;
            }
            try
            {

                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @".txt"))
                {

                    Stop = Convert.ToInt32(reader.ReadLine());
                    Profit = Convert.ToInt32(reader.ReadLine());
                    Sleepage = Convert.ToInt32(reader.ReadLine());
                    Volume = Convert.ToInt32(reader.ReadLine());
                    IsOn = Convert.ToBoolean(reader.ReadLine());

                    reader.Close();
                }

            }
            catch (Exception)
            {
                // send to log
                // отправить в лог
            }
            
        }

      
    }
}

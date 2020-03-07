using System;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System.Collections.Generic;
using OsEngine.Robots.aLibs;
using System.IO;

namespace OsEngine.Robots.aProd
{

    //<description>
    //PingBot. Позволяет протестировать биржу сервер, коннекторы. Что все работает
    //Делает случайные сделки.
    //Как работает:
    //1.Открывает сделку на первой активной свече. И закрывает на следующей. Объем =1. 
    //Может открыть несколько сделок подряд.
    //2.Открывает от 1 до 4 сделок в течение рабочего дня в случайное время по то же схеме, что и пункт 1.
    //</description>

      
    public class PingBot : BotPanel
    {

        public string version = "1.0";
        
        private BotTabSimple tab0;

        public bool isOn;                    //Вкл./Выкл.
        public WorkingModeType workingMode;  //режим работы бота
        public bool onlyLongTrades;           //открывать только лонговые позиции
        public int countTradesAtStart;       //количество сделок при запуске бота
        public int countTradesAtRandomTime;  //количество сделок в рандомное время

        private DateTime currentDate;
        private List<DateTime> TradesPlan;
        private Random randGenerator;

        public PingBot(string name, StartProgram startProgram) : base(name, startProgram)
        {

            //инициализируем настройки
            isOn = false;
            workingMode = WorkingModeType.MoscowExchange_Stocks;
            onlyLongTrades = true;
            countTradesAtStart = 1;
            countTradesAtRandomTime = 0;

            Load();


            currentDate = new DateTime(1, 1, 1, 0, 0, 0);
            TradesPlan = new List<DateTime>();
            randGenerator = new Random();


            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];

            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;

            DeleteEvent += PingBot_DeleteEvent;

        }

        private DateTime GenerateRandomTime(DateTime NowDateTime)
        {
            int hours = (int)randGenerator.Next(11, 16);
            int minutes = (int)randGenerator.Next(0, 60);

            int day = NowDateTime.Day;
            int month = NowDateTime.Month;
            int year = NowDateTime.Year;

            return new DateTime(year, month, day, hours, minutes, 0);
        }

        private void OpenTrade()
        {

            bool longTrade = (int)randGenerator.Next(0, 2) == 1 ? true : false;

            if (longTrade || onlyLongTrades)
            {
                tab0.BuyAtMarket(1);
            }
            else
            {
                tab0.SellAtMarket(1);
            }

        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            if (!isOn) return;

            Candle lastCandle = candles[candles.Count - 1];

            if (!CommonFuns.isWorkingTimeNow(lastCandle.TimeStart, workingMode)) return;


            //закрываем позу, если есть
            List<Position> positions = tab0.PositionsOpenAll;
            if (positions != null
                && positions.Count != 0)
            {
                if (positions[0].State == PositionStateType.Open)
                {
                    tab0.CloseAllAtMarket();
                }

                return;
            }



            //открываем позиции сразу при старте, если такой режим включен и еще не все открыли
            if (countTradesAtStart > 0)
            {
                OpenTrade();
                countTradesAtStart--;
                return;
            }


            //открываем рандомные позиции в течении дня
            if (countTradesAtRandomTime > 0)
            {
                var nowDateTime = lastCandle.TimeStart;
                
                //генерим время случайных сделок на сегодня
                if (nowDateTime.Date > currentDate && TradesPlan.Count == 0)
                {
                    currentDate = nowDateTime.Date;

                    for (int i=0; i<countTradesAtRandomTime; i++)
                    {
                        TradesPlan.Add(GenerateRandomTime(nowDateTime));
                    }
                    TradesPlan.Sort();
                }


                //если время очередной сделки пришло - открываем
                if (TradesPlan.Count !=0 && lastCandle.TimeStart >= TradesPlan[0])
                {
                    OpenTrade();
                    TradesPlan.RemoveAt(0);
                }
            }

        }

        public override string GetNameStrategyType()
        {
            return "PingBot";
        }

        public override void ShowIndividualSettingsDialog()
        {
            var ui = new PingBotUi(this);
            ui.ShowDialog();
        }

        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false)
                    )
                {
                    writer.WriteLine(isOn);
                    writer.WriteLine(workingMode);
                    writer.WriteLine(onlyLongTrades);
                    writer.WriteLine(countTradesAtStart);
                    writer.WriteLine(countTradesAtRandomTime);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void Load()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    isOn = Convert.ToBoolean(reader.ReadLine());
                    Enum.TryParse(reader.ReadLine(), true, out workingMode);
                    onlyLongTrades = Convert.ToBoolean(reader.ReadLine());
                    countTradesAtStart = Convert.ToInt32(reader.ReadLine());
                    countTradesAtRandomTime = Convert.ToInt32(reader.ReadLine());

                    reader.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void PingBot_DeleteEvent()
        {
            if (File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                File.Delete(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt");
            }
        }

    }
}

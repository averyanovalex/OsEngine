using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.aLibs;

namespace OsEngine.Robots.aProd

    //Описание:
    //Ловля краткосрочных импульсов, отскоков от локального уровня. Если 2 или больше свечей оттолкнулись от одного уровня,
    //входим. Стопы и тейки фиксированные
{
    
    class MaxBot : BotPanel
    {

        public string version = "1.0";

        private BotTabSimple tab0;
        
        private StrategyParameterString param_mode; // включен, выключен, шорт или лонг
        private StrategyParameterString param_working_mode;  // торговые часы
        private StrategyParameterInt param_stop; //стоп в пунктах
        private StrategyParameterInt param_take; //тейк в пунктах
        private StrategyParameterInt param_slack; //люфт в пунктах
        private StrategyParameterInt param_slack_order; //люфт для выставления ордера в пунктах
        private StrategyParameterInt param_candlesCount; //количество проверяемых свечей
        private StrategyParameterString param_version; //версия, не меняется
        public MaxBot(string name, StartProgram startProgram) : base(name, startProgram)
        {

            TabCreate(BotTabType.Simple);
            tab0 = TabsSimple[0];

            param_mode = CreateParameter("Mode", "Off", new[] { "Off", "On_Long_Short", "On_OnlyLong", "On_OnlyShort"});
            param_working_mode = CreateParameter("Working_Mode", "DayAndNight", new[] { "DayAndNight", "MoscowExchange_Stocks", "MoscowExchange_Forts" });
            param_stop = CreateParameter("Stop", 10, 0, 100, 1);
            param_take = CreateParameter("Take", 30, 0, 300, 1);
            param_slack = CreateParameter("Slack", 3, 0, 10, 1);
            param_slack_order = CreateParameter("Slack_order", 4, 0, 10, 1);
            param_candlesCount = CreateParameter("CandlesCount", 2, 2, 5, 1);
            param_version = CreateParameter("Version", version, new[] { version });

            tab0.CandleFinishedEvent += Tab0_CandleFinishedEvent;
            tab0.PositionOpeningSuccesEvent += Tab0_PositionOpeningSuccesEvent;

        }

        private void Tab0_PositionOpeningSuccesEvent(Position position)
        {

            int stop = param_stop.ValueInt;
            int take = param_take.ValueInt;
            
            int koef = 0;
            if (position.Direction == Side.Buy) koef = 1;
            else if (position.Direction == Side.Sell) koef = -1;
            else
            {
                tab0.CloseAtMarket(position, position.OpenVolume);
                throw new InvalidOperationException("Неверный тип позиции. Позиция должна быть Buy или Sell");
            }


            decimal stopPrice = position.EntryPrice - koef * tab0.Securiti.PriceStep * stop;
            decimal takePrice = position.EntryPrice + koef * tab0.Securiti.PriceStep * take;

            tab0.CloseAtStop(position, stopPrice, stopPrice);
            tab0.CloseAtProfit(position, takePrice, takePrice);

        }

        private void Tab0_CandleFinishedEvent(List<Candle> candles)
        {

            if (param_mode.ValueString == "Off") return;


            var workingMode = WorkingModeType.DayAndNight;
            if (param_working_mode.ValueString == "DayAndNight") 
            {
                //уже установили
            }
            else if (param_working_mode.ValueString == "MoscowExchange_Forts")
            {
                workingMode = WorkingModeType.MoscowExchange_Forts;
            }
            else if (param_working_mode.ValueString == "MoscowExchange_Stocks")
            {
                workingMode = WorkingModeType.MoscowExchange_Stocks;
            }
            else
            {
                throw new InvalidOperationException("ОШИБКА: Неизвестный тип рабочего режима!");
            }

            
            List<Position> positions = tab0.PositionsOpenAll;

            if (!CommonFuns.isWorkingTimeNow(candles[candles.Count-1].TimeStart, workingMode))
            {
                if (positions != null && positions.Count != 0 && positions[0].State == PositionStateType.Open)
                {
                    //не рабочие часы, есть открытая поза. закрываем ее и выходим
                    tab0.CloseAllAtMarket();
                }
                else
                { //нерабочие часы, позы нет, выходим
                    return;
                }
            } 
            else
            { //рабочие часы, есть открытая поза, ничего не делаем
                if (positions != null && positions.Count != 0) return;
            }
                


            TradeLogic(candles);
  
        }

        private void TradeLogic(List<Candle> candles)
        {

            int slack = param_slack.ValueInt;
            int slack_order = param_slack_order.ValueInt;
            int candlesCount = param_candlesCount.ValueInt;
            
            if (candles.Count < candlesCount + 1) return;

            List<Candle> checkingCandles = new List<Candle>();
            for (int i = candles.Count - candlesCount; i <= candles.Count - 1; i++)
            {
                checkingCandles.Add(candles[i]);
            }


            //ищем точку входа в Лонг
            if (param_mode.ValueString == "On_Long_Short" || param_mode.ValueString == "On_OnlyLong")
            {

                decimal checkPrice = calcMaxLowPrice(checkingCandles);

                List<decimal> body = new List<decimal>();
                List<decimal> delta = new List<decimal>();

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    body.Add(Math.Min(checkingCandles[i].Close, checkingCandles[i].Open));
                    delta.Add(Math.Abs(checkingCandles[i].Low - checkPrice));
                }


                int touch = 0;

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    if (delta[i] <= slack * tab0.Securiti.PriceStep  && body[i] >= checkPrice) touch++;
                }

                if (touch == candlesCount)
                {
                    tab0.BuyAtLimit(1, checkPrice + slack_order * tab0.Securiti.PriceStep);
                    return;
                }

            }



            //ищем точку входа в Шорт
            if (param_mode.ValueString == "On_Long_Short" || param_mode.ValueString == "On_OnlyShort")
            {

                decimal checkPrice = calcMinHighPrice(checkingCandles);

                List<decimal> body = new List<decimal>();
                List<decimal> delta = new List<decimal>();

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    body.Add(Math.Max(checkingCandles[i].Close, checkingCandles[i].Open));
                    delta.Add(Math.Abs(checkingCandles[i].High - checkPrice));
                }


                int touch = 0;

                for (int i = 0; i < checkingCandles.Count; i++)
                {
                    if (delta[i] <= slack && body[i] <= checkPrice) touch++;
                }

                if (touch == candlesCount)
                {
                    tab0.SellAtLimit(1, checkPrice - slack_order * tab0.Securiti.PriceStep);
                    return;
                }

            }


        }

        private decimal calcMaxLowPrice(List<Candle> candles)
        {
            decimal result = 0;
            foreach (Candle candle in candles)
            {
                result = candle.Low > result ? candle.Low : result;
            }

            return result;
        }

        private decimal calcMinHighPrice(List<Candle> candles)
        {
            decimal result = candles[0].High;
            foreach (Candle candle in candles)
            {
                result = candle.High < result ? candle.High : result;
            }

            return result;
        }

        public override string GetNameStrategyType()
        {
            return "MaxBot";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }
    }
}

using System;

namespace OsEngine.Robots.aLibs
{
    public enum WorkingModeType
    {
        DayAndNight,            //круглосуточно
        MoscowExchange_Stocks,  //режим московской биржи, акции
        MoscowExchange_Forts    //режим московской биржи, ФОРТС
    }


    public static class CommonFuns
    {
        public static bool isWorkingTimeNow(DateTime timeNow, WorkingModeType workingMode)
        {
            if (workingMode == WorkingModeType.DayAndNight)
            { // круглосуточный режим работы
                return true;
            }
            else if (workingMode == WorkingModeType.MoscowExchange_Stocks)
            { //режим работы Московской биржи, акции

                if (timeNow.Hour >= 11 && timeNow.Hour < 18) return true;
                else return false;

            }
            else if (workingMode == WorkingModeType.MoscowExchange_Forts)
            {

                if (timeNow.Hour >= 11 && timeNow.Hour < 23) return true;
                else return false;

            }
            else
            {// неизвестный режим
                return false;
            }
        }

    }




}


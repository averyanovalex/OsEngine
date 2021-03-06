﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms.Integration;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.Market.Servers.Tester;
using OsEngine.OsTrader.Panels;

namespace OsEngine.OsOptimizer
{
    /// <summary>
    /// class that stores and provides settings for optimization
    /// класс хранящий и предоставляющий в себе настройки для оптимизации
    /// </summary>
    public class OptimizerMaster
    {
        public OptimizerMaster()
        {
            _log = new Log("OptimizerLog", StartProgram.IsOsOptimizer);
            _log.Listen(this);

            _threadsCount = 1;
            _startDepozit = 100000;

            Storage = new OptimizerDataStorage();
            Storage.SecuritiesChangeEvent += _storage_SecuritiesChangeEvent;
            Storage.TimeChangeEvent += _storage_TimeChangeEvent;

            _filterProfitValue = 10;
            _filterProfitIsOn = false;
            _filterMaxDrowDownValue = -10;
            _filterMaxDrowDownIsOn = false;
            _filterMiddleProfitValue = 0.001m;
            _filterMiddleProfitIsOn = false;
            _filterWinPositionValue = 40;
            _filterWinPositionIsOn = false;
            _filterProfitFactorValue = 1;
            _filterProfitFactorIsOn = false;

            _percentOnFilration = 30;

            Load();

            _fazeCount = 1;

            SendLogMessage(OsLocalization.Optimizer.Message11,LogMessageType.System);

            for (int i = 0; i < 3; i++)
            {
                Thread worker = new Thread(GetNamesStrategyToOptimization);
                worker.Name = i.ToString();
                worker.IsBackground = true;
                worker.Start();
            }

            _optimizerExecutor= new OptimizerExecutor(this);
            _optimizerExecutor.LogMessageEvent += SendLogMessage;
            _optimizerExecutor.TestingProgressChangeEvent += _optimizerExecutor_TestingProgressChangeEvent;
            _optimizerExecutor.PrimeProgressChangeEvent += _optimizerExecutor_PrimeProgressChangeEvent;
            _optimizerExecutor.TestReadyEvent += _optimizerExecutor_TestReadyEvent;
            _optimizerExecutor.NeadToMoveUiToEvent += _optimizerExecutor_NeadToMoveUiToEvent;
            ProgressBarStatuses = new List<ProgressBarStatus>();
            PrimeProgressBarStatus = new ProgressBarStatus();
        }

        /// <summary>
        /// save settings
        /// сохранить настройки
        /// </summary>
        private void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\OptimizerSettings.txt", false)
                    )
                {
                    writer.WriteLine(ThreadsCount);
                    writer.WriteLine(StrategyName);
                    writer.WriteLine(StartDepozit);

                    writer.WriteLine(_filterProfitValue);
                    writer.WriteLine(_filterProfitIsOn);
                    writer.WriteLine(_filterMaxDrowDownValue);
                    writer.WriteLine(_filterMaxDrowDownIsOn);
                    writer.WriteLine(_filterMiddleProfitValue);
                    writer.WriteLine(_filterMiddleProfitIsOn);
                    writer.WriteLine(_filterWinPositionValue);
                    writer.WriteLine(_filterWinPositionIsOn);
                    writer.WriteLine(_filterProfitFactorValue);
                    writer.WriteLine(_filterProfitFactorIsOn);


                    writer.WriteLine(_timeStart);
                    writer.WriteLine(_timeEnd);
                    writer.WriteLine(_fazeCount);
                    writer.WriteLine(_percentOnFilration);

                    writer.WriteLine(_filterDealsCountValue);
                    writer.WriteLine(_filterDealsCountIsOn);

                    writer.Close();
                }
            }
            catch (Exception error)
            {
                SendLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// load settings
        /// загрузить настройки
        /// </summary>
        private void Load()
        {
            if (!File.Exists(@"Engine\OptimizerSettings.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\OptimizerSettings.txt"))
                {
                    _threadsCount = Convert.ToInt32(reader.ReadLine());
                    _strategyName = reader.ReadLine();
                    _startDepozit = Convert.ToDecimal(reader.ReadLine());
                    _filterProfitValue = Convert.ToDecimal(reader.ReadLine());
                    _filterProfitIsOn = Convert.ToBoolean(reader.ReadLine());
                    _filterMaxDrowDownValue = Convert.ToDecimal(reader.ReadLine());
                    _filterMaxDrowDownIsOn = Convert.ToBoolean(reader.ReadLine());
                    _filterMiddleProfitValue = Convert.ToDecimal(reader.ReadLine());
                    _filterMiddleProfitIsOn = Convert.ToBoolean(reader.ReadLine());
                    _filterWinPositionValue = Convert.ToDecimal(reader.ReadLine());
                    _filterWinPositionIsOn = Convert.ToBoolean(reader.ReadLine());
                    _filterProfitFactorValue = Convert.ToDecimal(reader.ReadLine());
                    _filterProfitFactorIsOn = Convert.ToBoolean(reader.ReadLine());

                    _timeStart = Convert.ToDateTime(reader.ReadLine());
                    _timeEnd = Convert.ToDateTime(reader.ReadLine());
                    _fazeCount = Convert.ToInt32(reader.ReadLine());
                    _percentOnFilration = Convert.ToDecimal(reader.ReadLine());

                    _filterDealsCountValue = Convert.ToInt32(reader.ReadLine());
                    _filterDealsCountIsOn = Convert.ToBoolean(reader.ReadLine());
                    reader.Close();
                }
            }
            catch (Exception error)
            {
                //SendLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

//checking strategies for parameters/ проверка стратегий на наличие параметров

        /// <summary>
        /// all strategies with parameters that are in the platform
        /// все стратегии с параметрами которые есть в платформе
        /// </summary>
        private List<string> _namesWhithParams = new List<string>();

        /// <summary>
        /// take all the strategies with parameters that are in the platform
        /// взять все стратегии с параметрами которые есть в платформе
        /// </summary>
        public void GetNamesStrategyToOptimization()
        {
            List<string> names = PanelCreator.GetNamesStrategy();

            int numThread = Convert.ToInt32(Thread.CurrentThread.Name);

            for (int i = numThread; i < names.Count; i += 3)
            {
                BotPanel bot = PanelCreator.GetStrategyForName(names[i], numThread.ToString(), StartProgram.IsOsOptimizer);

                if(bot == null)
                {
                    SendLogMessage("Bot with name " + names[i] + " is not exist.", LogMessageType.Error);
                    continue;
                }

                if (bot.Parameters == null ||
                    bot.Parameters.Count == 0)
                {
                    //SendLogMessage("We are not optimizing. Without parameters/Не оптимизируем. Без параметров: " + bot.GetNameStrategyType(), LogMessageType.System);
                }
                else
                {
                    // SendLogMessage("With parameters/С параметрами: " + bot.GetNameStrategyType(), LogMessageType.System);
                    _namesWhithParams.Add(names[i]);
                }
                if (numThread == 2)
                {

                }
                bot.Delete();
            }

            if (StrategyNamesReadyEvent != null)
            {

                StrategyNamesReadyEvent(_namesWhithParams);
            }
        }

        /// <summary>
        /// changed the list of strategies with parameters that are in the system
        /// изменился список стратегий с параметрами которые есть в системе
        /// </summary>
        public event Action<List<string>> StrategyNamesReadyEvent;


// work with the progress of the optimization process/работа с прогрессом процесса оптимизации

        /// <summary>
        /// inbound event: the main optimization progress has changed
        /// входящее событие: изменился основной прогресс оптимизации
        /// </summary>
        /// <param name="curVal">the current value of the progress bar/текущее значение прогрессБара</param>
        /// <param name="maxVal">maximum progress bar/максимальное значение прогрессБара</param>
        void _optimizerExecutor_PrimeProgressChangeEvent(int curVal, int maxVal)
        {
            PrimeProgressBarStatus.CurrentValue = curVal;
            PrimeProgressBarStatus.MaxValue = maxVal;
        }

        /// <summary>
        /// inbound event: optimization completed
        /// входящее событие: оптимизация завершилась
        /// </summary>
        /// <param name="bots">InSample robots/роботы InSample</param>
        /// <param name="botsOutOfSample">OutOfSample</param>
        void _optimizerExecutor_TestReadyEvent(List<BotPanel> bots, List<BotPanel> botsOutOfSample)
        {
            PrimeProgressBarStatus.CurrentValue = PrimeProgressBarStatus.MaxValue;
            if (TestReadyEvent != null)
            {
                TestReadyEvent(bots,botsOutOfSample);
            }
        }

        /// <summary>
        /// event: testing ended
        /// событие: тестирование завершилось
        /// </summary>
        public event Action<List<BotPanel>, List<BotPanel>> TestReadyEvent;

        /// <summary>
        /// Progress on a specific robot has changed
        /// изменился прогресс по определённому роботу
        /// </summary>
        /// <param name="curVal">current value for progress bar/текущее значение для прогрессБара</param>
        /// <param name="maxVal">maximum value for progress bar/максимальное значение для прогрессБара</param>
        /// <param name="numServer">server number/номер сервера</param>
        void _optimizerExecutor_TestingProgressChangeEvent(int curVal, int maxVal, int numServer)
        {
            ProgressBarStatus status = ProgressBarStatuses.Find(st => st.Num == numServer);

            if (status == null)
            {
                status = new ProgressBarStatus();
                status.Num = numServer;
                ProgressBarStatuses.Add(status);
            }

            status.CurrentValue = curVal;
            status.MaxValue = maxVal;
        }

        /// <summary>
        /// values for drawing progressBars of individual bots
        /// значения для прорисовки прогрессБаров отдельных ботов
        /// </summary>
        public List<ProgressBarStatus> ProgressBarStatuses;

        /// <summary>
        /// value of progress for main progressBar
        /// значение прогресса для главного прогрессБара
        /// </summary>
        public ProgressBarStatus PrimeProgressBarStatus;

// data store/хранилище данных

        /// <summary>
        /// show data storage settings
        /// показать настройки хранилища данных
        /// </summary>
        public void ShowDataStorageDialog()
        {
            Storage.ShowDialog();
        }

        /// <summary>
        /// data store
        /// хранилище данных
        /// </summary>
        public OptimizerDataStorage Storage;

        /// <summary>
        /// the start and end times have changed in the repository.
        /// Means the set has been reset
        /// в хранилище изменилось время старта и завершения.
        /// Означает что сет был перезагружен
        /// </summary>
        /// <param name="timeStart">start time/время начала данных</param>
        /// <param name="timeEnd">data completion time/время завершения данных</param>
        void _storage_TimeChangeEvent(DateTime timeStart, DateTime timeEnd)
        {
            TimeStart = timeStart;
            TimeEnd = timeEnd;
        }

        /// <summary>
        /// in the repository has changed the composition of the papers.
        /// Means the set has been reset
        /// в хранилище изменился состав бумаг.
        /// Означает что сет был перезагружен
        /// </summary>
        /// <param name="securities">new list of papers/новый список бумаг</param>
        void _storage_SecuritiesChangeEvent(List<Security> securities)
        {
            if (NewSecurityEvent != null)
            {
                NewSecurityEvent(securities);
            }

            TimeStart = Storage.TimeStart;
            TimeEnd = Storage.TimeEnd;
        }

        /// <summary>
        /// event: changed the list of securities in the repository
        /// событие: изменился список бумаг в хранилище
        /// </summary>
        public event Action<List<Security>> NewSecurityEvent;

// Management 1 tab/управление 1 вкладка

        /// <summary>
        /// number of threads that will simultaneously work on optimization
        /// кол-во потоков которые будут одновременно работать над оптимизацией
        /// </summary>
        public int ThreadsCount
        {
            get { return _threadsCount; }
            set
            {
                _threadsCount = value;
                Save();
            }
        }
        private int _threadsCount;

        /// <summary>
        /// the name of the strategy that we will optimize
        /// имя стратегии которую мы будем оптимизировать
        /// </summary>
        public string StrategyName
        {
            get { return _strategyName; }
            set
            {
                _strategyName = value;
                TabsSimpleNamesAndTimeFrames = new List<TabSimpleEndTimeFrame>();
                TabsIndexNamesAndTimeFrames = new List<TabIndexEndTimeFrame>();
                Save();
            }
        }
        private string _strategyName;

        /// <summary>
        /// initial deposit
        /// начальный депозит
        /// </summary>
        public decimal StartDepozit
        {
            get { return _startDepozit; }
            set
            {
                _startDepozit = value;
                Save();
            }
        }
        private decimal _startDepozit;

        /// <summary>
        /// connection settings for robot usual tabs
        /// настройки подключения для обычных вкладок робота
        /// </summary>
        public List<TabSimpleEndTimeFrame> TabsSimpleNamesAndTimeFrames;

        /// <summary>
        /// connection settings for index tabs on the robot
        /// настройки подключения для вкладок индексов у робота
        /// </summary>
        public List<TabIndexEndTimeFrame> TabsIndexNamesAndTimeFrames;

        /// <summary>
        /// list of papers available in the vault
        /// список бумаг доступных в хранилище
        /// </summary>
        public List<SecurityTester> SecurityTester
        {
            get { return Storage.SecuritiesTester; }
        }

// tab 3, filters/вкладка 3, фильтры

        /// <summary>
        /// profit filter value
        /// значение фильтра по профиту
        /// </summary>
        public decimal FilterProfitValue
        {
            get { return _filterProfitValue; }
            set
            {
                _filterProfitValue = value;
                Save();
            }
        }
        private decimal _filterProfitValue;

        /// <summary>
        /// is profit filtering enabled
        /// включен ли фильтр по профиту
        /// </summary>
        public bool FilterProfitIsOn
        {
            get { return _filterProfitIsOn; }
            set
            {
                _filterProfitIsOn = value;
                Save();
            }
        }
        private bool _filterProfitIsOn;

        /// <summary>
        /// maximum drawdown filter value
        /// значение фильтра максимальной просадки
        /// </summary>
        public decimal FilterMaxDrowDownValue
        {
            get { return _filterMaxDrowDownValue; }
            set
            {
                _filterMaxDrowDownValue = value;
                Save();
            }
        }
        private decimal _filterMaxDrowDownValue;

        /// <summary>
        /// is the maximum drawdown filter enabled
        /// включен ли фильтр максимальной просадки
        /// </summary>
        public bool FilterMaxDrowDownIsOn
        {
            get { return _filterMaxDrowDownIsOn; }
            set
            {
                _filterMaxDrowDownIsOn = value;
                Save();
            }
        }
        private bool _filterMaxDrowDownIsOn;

        /// <summary>
        /// value of the average profit filter from the transaction
        /// значение фильтра среднего профита со сделки
        /// </summary>
        public decimal FilterMiddleProfitValue
        {
            get { return _filterMiddleProfitValue; }
            set
            {
                _filterMiddleProfitValue = value;
                Save();
            }
        }
        private decimal _filterMiddleProfitValue;

        /// <summary>
        /// Is the average profit filter included in the transaction?
        /// включен ли фильтр среднего профита со сделки
        /// </summary>
        public bool FilterMiddleProfitIsOn
        {
            get { return _filterMiddleProfitIsOn; }
            set
            {
                _filterMiddleProfitIsOn = value;
                Save();
            }
        }
        private bool _filterMiddleProfitIsOn;

        /// <summary>
        /// value of the percentage of transactions won filter
        /// значение фильтра процента выигранных сделок
        /// </summary>
        public decimal FilterWinPositionValue
        {
            get { return _filterWinPositionValue; }
            set
            {
                _filterWinPositionValue = value;
                Save();
            }
        }
        private decimal _filterWinPositionValue;

        /// <summary>
        /// whether the percentage of transactions won filter is enabled
        /// включен ли фильтр процента выигранных сделок
        /// </summary>
        public bool FilterWinPositionIsOn
        {
            get { return _filterWinPositionIsOn; }
            set
            {
                _filterWinPositionIsOn = value;
                Save();
            }
        }
        private bool _filterWinPositionIsOn;

        /// <summary>
        /// filter value by profit factor
        /// значение фильтра по профит фактору
        /// </summary>
        public decimal FilterProfitFactorValue
        {
            get { return _filterProfitFactorValue; }
            set
            {
                _filterProfitFactorValue = value;
                Save();
            }
        }
        private decimal _filterProfitFactorValue;

        /// <summary>
        /// is the filter by profit factor included
        /// включен ли фильтр по профит фактору
        /// </summary>
        public bool FilterProfitFactorIsOn
        {
            get { return _filterProfitFactorIsOn; }
            set
            {
                _filterProfitFactorIsOn = value;
                Save();
            }
        }
        private bool _filterProfitFactorIsOn;

        /// <summary>
        /// value of the filter by the number of transactions
        /// значение фильтра по количеству сделок
        /// </summary>
        public int FilterDealsCountValue
        {
            get { return _filterDealsCountValue; }
            set
            {
                _filterDealsCountValue = value;
                Save();
            }
        }
        private int _filterDealsCountValue;

        /// <summary>
        /// Is the number of deals filter enabled
        /// включен ли фильтр по количеству сделок
        /// </summary>
        public bool FilterDealsCountIsOn
        {
            get { return _filterDealsCountIsOn; }
            set
            {
                _filterDealsCountIsOn = value;
                Save();
            }
        }
        private bool _filterDealsCountIsOn;

        // tab 4, optimization/вкладка 4, оптимизация


        // tab 5, optimization phases/вкладка 5, фазы оптимизации

        /// <summary>
        /// optimization phases
        /// фазы оптимизации
        /// </summary>
        public List<OptimizerFaze> Fazes;

        /// <summary>
        /// history time to start optimization
        /// время истории для старта оптимизации
        /// </summary>
        public DateTime TimeStart
        {
            get { return _timeStart; }
            set
            {
                _timeStart = value;
                Save();

                if (DateTimeStartEndChange != null)
                {
                    DateTimeStartEndChange();
                }
            }
        }
        private DateTime _timeStart;

        /// <summary>
        /// history time to complete optimization
        /// время истории для завершения оптимизации
        /// </summary>
        public DateTime TimeEnd
        {
            get { return _timeEnd; }
            set
            {
                _timeEnd = value; 
                Save();
                if (DateTimeStartEndChange != null)
                {
                    DateTimeStartEndChange();
                }
            }
        }
        private DateTime _timeEnd;

        /// <summary>
        /// number of optimization phases
        /// количество фаз оптимизации
        /// </summary>
        public int FazeCount
        {
            get { return _fazeCount; }
            set
            {
                _fazeCount = value;
                Save();
            }
        }
        private int _fazeCount;

        /// <summary>
        /// percentage of time on outofsample
        /// процент времени на OutOfSample
        /// </summary>
        public decimal PercentOnFilration
        {
            get { return _percentOnFilration; }
            set
            {
                _percentOnFilration = value;
                Save();
            }
        }
        private decimal _percentOnFilration;

        /// <summary>
        /// break the total time into phases
        /// разбить общее время на фазы
        /// </summary>
        public void ReloadFazes()
        {
            int fazeCount = _fazeCount;

            if (fazeCount < 1)
            {
                fazeCount = 1;
            }

            fazeCount *= 2;
            
            int dayAll = Convert.ToInt32((TimeEnd - TimeStart).TotalDays);

            if (dayAll < 2)
            {
                SendLogMessage(OsLocalization.Optimizer.Message12,LogMessageType.System);
                return;
            }

            while (dayAll / fazeCount < 1)
            {
                fazeCount -= 2;
            }

            int dayOutOfSample = Convert.ToInt32(dayAll * (_percentOnFilration/100)) / (fazeCount/2);
            if (dayOutOfSample < 1)
            {
                dayOutOfSample = 1;
            }

            int dayInSample = (dayAll - (dayOutOfSample * (fazeCount / 2))) / (fazeCount / 2);
            if (dayInSample < 0)
            {
                dayInSample = 1;
            }

            List<int> fazesLenght = new List<int>();

            for (int i = 0; i < fazeCount; i++)
            {
                if (i%2 == 0)
                {
                    fazesLenght.Add(dayInSample);
                }
                else
                {
                    fazesLenght.Add(dayOutOfSample);
                }
            }

            while (fazesLenght.Sum() > dayAll)
            {
                for (int i = 0; i < fazesLenght.Count; i++)
                {
                    if (fazesLenght[i] != 1)
                    {
                        fazesLenght[i] -= 1;
                        break;
                    }
                    if (i + 1 == fazesLenght.Count)
                    {
                        SendLogMessage(OsLocalization.Optimizer.Message13,LogMessageType.System);
                        return;
                    }
                }
            }

            while (fazesLenght.Sum() < dayAll)
            {
                  fazesLenght[0] += 1;
            }


            Fazes = new List<OptimizerFaze>();

            DateTime time = _timeStart;

            for (int i = 0; i < fazeCount; i++)
            {
                OptimizerFaze newFaze = new OptimizerFaze();
                newFaze.TimeStart = time;
                time = time.AddDays(fazesLenght[i]);
                newFaze.TimeEnd = time;
                newFaze.Days = fazesLenght[i];

                if (i%2 != 0)
                {
                    newFaze.TypeFaze = OptimizerFazeType.OutOfSample;
                }
                else
                {
                    newFaze.TypeFaze = OptimizerFazeType.InSample;
                }

                Fazes.Add(newFaze);
            }
        }

        /// <summary>
        /// start time of history for optimization has changed
        /// время старта времени истории для оптимизации изменилось
        /// </summary>
        public event Action DateTimeStartEndChange;

// optimization options/параметры оптимизации

        /// <summary>
        /// actually used parameters for optimization.
        /// available to change in the interface
        /// реально применяемые параметры для оптимизации.
        /// доступны для изменения в интерфейсе
        /// </summary>
        public List<IIStrategyParameter> Parameters
        {
            get
            {
                if (string.IsNullOrEmpty(_strategyName))
                {
                    return null;
                }

                BotPanel bot = PanelCreator.GetStrategyForName(_strategyName, "", StartProgram.IsOsOptimizer);

                if (bot == null)
                {
                    return null;
                }

                if (bot.Parameters == null ||
                    bot.Parameters.Count == 0)
                {
                    return null;
                }
                _parameters = bot.Parameters;
                bot.Delete();

                return bot.Parameters;
            }
        }
        private List<IIStrategyParameter> _parameters;

        /// <summary>
        /// list of parameters that are included in the optimization
        /// список параметров которые включены в оптимизацию
        /// </summary>
        public List<bool> ParametersOn
        {
            get
            {

                    _paramOn = new List<bool>();
                    for (int i = 0; _parameters != null && i < _parameters.Count; i++)
                    {
                        _paramOn.Add(false);
                    }
                

                return _paramOn;
            }
        }
        private List<bool> _paramOn;


// job startup optimization algorithm/работа запуска алгоритма оптимизации

        /// <summary>
        /// the object that optimizes
        /// объект который производит оптимизацию
        /// </summary>
        private OptimizerExecutor _optimizerExecutor;

        /// <summary>
        /// run optimization
        /// запустить оптимизацию
        /// </summary>
        /// <returns>true - if the launch was successful/true - если запуск прошёл успешно</returns>
        public bool Start()
        {
            if (CheckReadyData() == false)
            {
                return false;
            }

            if (_optimizerExecutor.Start(_paramOn, _parameters))
            {
                ProgressBarStatuses = new List<ProgressBarStatus>();
                PrimeProgressBarStatus = new ProgressBarStatus();
            }
            return true;
        }

        /// <summary>
        /// stop the optimization process
        /// остановить процесс оптимизации
        /// </summary>
        public void Stop()
        {
            _optimizerExecutor.Stop();
        }

        /// <summary>
        /// check if everything is ready to start testing
        /// проверить, всё ли готово для старта тестирования
        /// </summary>
        /// <returns>true - everything is ready/true - всё готово</returns>
        private bool CheckReadyData()
        {
            if (Fazes == null || Fazes.Count == 0)
            {
                MessageBox.Show(OsLocalization.Optimizer.Message14);
                SendLogMessage(OsLocalization.Optimizer.Message14, LogMessageType.System);
                if (NeadToMoveUiToEvent != null)
                {
                    NeadToMoveUiToEvent(NeadToMoveUiTo.Fazes);
                }
                return false;
            }


            if (TabsSimpleNamesAndTimeFrames == null ||
                TabsSimpleNamesAndTimeFrames.Count == 0)
            {
                MessageBox.Show(OsLocalization.Optimizer.Message15);
                SendLogMessage(OsLocalization.Optimizer.Message15, LogMessageType.System);
                if (NeadToMoveUiToEvent != null)
                {
                    NeadToMoveUiToEvent(NeadToMoveUiTo.TabsAndTimeFrames);
                }
                return false;
            }

            if (string.IsNullOrEmpty(Storage.ActiveSet) ||
                Storage.SecuritiesTester == null ||
                Storage.SecuritiesTester.Count == 0)
            {
                MessageBox.Show(OsLocalization.Optimizer.Message16);
                SendLogMessage(OsLocalization.Optimizer.Message16, LogMessageType.System);

                if (NeadToMoveUiToEvent != null)
                {
                    NeadToMoveUiToEvent(NeadToMoveUiTo.Storage);
                }
                return false;
            }

            if (string.IsNullOrEmpty(_strategyName))
            {
                MessageBox.Show(OsLocalization.Optimizer.Message17);
                SendLogMessage(OsLocalization.Optimizer.Message17, LogMessageType.System);
                if (NeadToMoveUiToEvent != null)
                {
                    NeadToMoveUiToEvent(NeadToMoveUiTo.NameStrategy);
                }
                return false;
            }

            bool onParamesReady = false;

            for (int i = 0; i < _paramOn.Count; i++)
            {
                if (_paramOn[i])
                {
                    onParamesReady = true;
                    break;
                }
            }

            if (onParamesReady == false)
            {
                MessageBox.Show(OsLocalization.Optimizer.Message18);
                SendLogMessage(OsLocalization.Optimizer.Message18, LogMessageType.System);
                if (NeadToMoveUiToEvent != null)
                {
                    NeadToMoveUiToEvent(NeadToMoveUiTo.Parametrs);
                }
                return false;
            }


            return true;
        }

        /// <summary>
        /// incoming event: you need to move GUI to a certain place
        /// входящее событие: нужно переместить ГУИ в определённое место
        /// </summary>
        /// <param name="moveUiTo">place to move/место для перемещения</param>
        void _optimizerExecutor_NeadToMoveUiToEvent(NeadToMoveUiTo moveUiTo)
        {
            if (NeadToMoveUiToEvent != null)
            {
                NeadToMoveUiToEvent(moveUiTo);
            }
        }

        /// <summary>
        /// event: you need to move GUI to a certain place
        /// событие: нужно переместить ГУИ в определённое место
        /// </summary>
        public event Action<NeadToMoveUiTo> NeadToMoveUiToEvent;

// logging/логирование

        /// <summary>
        /// log
        /// лог
        /// </summary>
        private Log _log;

        /// <summary>
        /// start drawing log
        /// начать прорисовку лога
        /// </summary>
        public void StartPaintLog(WindowsFormsHost logHost)
        {
            _log.StartPaint(logHost);
        }

        /// <summary>
        /// send new message to log
        /// отправить новое сообщение в лог
        /// </summary>
        /// <param name="message">message/сообщение</param>
        /// <param name="type">message type/тип сообщения</param>
        public void SendLogMessage(string message, LogMessageType type)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message,type);
            }
        }

        /// <summary>
        /// event: new message
        /// событие: новое сообщение
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

    }

    /// <summary>
    /// an object that holds values for drawing progress
    /// in the ProgressBar
    /// объект хранящий в себе значения для прорисовки прогресса
    /// в ProgressBar
    /// </summary>
    public class ProgressBarStatus
    {
        /// <summary>
        /// present value
        /// текущее значение
        /// </summary>
        public int CurrentValue;

        /// <summary>
        /// maximum value
        /// максимальное значение
        /// </summary>
        public int MaxValue;

        /// <summary>
        /// server / robot number
        /// номер сервера / робота
        /// </summary>
        public int Num;
    }

    /// <summary>
    /// what parameter is the optimization
    /// по какому параметру проходит оптимизация
    /// </summary>
    public enum OptimizationFunctionType
    {
        /// <summary>
        /// Total profit
        /// Итоговый профит
        /// </summary>
        EndProfit,

        /// <summary>
        /// The average profit from the transaction
        /// Средний профит со сделки
        /// </summary>
        MiddleProfitFromPosition,

        /// <summary>
        /// Max drawdown
        /// Максимальная просадка
        /// </summary>
        MaxDrowDown,

        /// <summary>
        /// Profit factor
        /// Профит фактор
        /// </summary>
        ProfitFactor
    }

    /// <summary>
    /// optimization method
    /// способ оптимизации
    /// </summary>
    public enum OptimizationType
    {
        /// <summary>
        /// Annealing imitation
        /// Имитация отжига
        /// </summary>
        SimulatedAnnealing,

        /// <summary>
        /// Genetic algorithm
        /// Генетический алгоритм
        /// </summary>
        GeneticАlgorithm
    }

    /// <summary>
    /// Optimization phase
    /// Фаза оптимизации
    /// </summary>
    public class OptimizerFaze
    {
        /// <summary>
        /// type of phase. What we do
        /// тип фазы. Что делаем
        /// </summary>
        public OptimizerFazeType TypeFaze;

        /// <summary>
        /// start time
        /// время начала
        /// </summary>
        public DateTime TimeStart;

        /// <summary>
        /// completion time
        /// время завершения
        /// </summary>
        public DateTime TimeEnd;

        /// <summary>
        /// days per phase
        /// дней на фазу
        /// </summary>
        public int Days;

    }

    /// <summary>
    /// Phase type optimization
    /// Тип фазы оптимизации
    /// </summary>
    public enum OptimizerFazeType
    {
        /// <summary>
        /// optimization
        /// оптимизация
        /// </summary>
        InSample,

        /// <summary>
        /// filtration
        /// фильтрация
        /// </summary>
        OutOfSample
    }

    /// <summary>
    /// tool specification for launching a regular tab
    /// спецификация инструмента для запуска обычной вкладки
    /// </summary>
    public class TabSimpleEndTimeFrame
    {
        /// <summary>
        /// tab number
        /// номер вкладки
        /// </summary>
        public int NumberOfTab;

        /// <summary>
        /// paper name
        /// название бумаги
        /// </summary>
        public string NameSecurity;

        /// <summary>
        /// timeframe
        /// таймфрейм
        /// </summary>
        public TimeFrame TimeFrame;
    }

    /// <summary>
    /// tool specification for launching index tab
    /// спецификация инструмента для запуска вкладки индекса
    /// </summary>
    public class TabIndexEndTimeFrame
    {
        /// <summary>
        /// tab number
        /// номер вкладки
        /// </summary>
        public int NumberOfTab;

        /// <summary>
        /// list of papers at the tab
        /// список бумаг у вкладки
        /// </summary>
        public List<string> NamesSecurity;

        /// <summary>
        /// tab timeframe
        /// таймфрейм бумаг в вкладки
        /// </summary>
        public TimeFrame TimeFrame;

        /// <summary>
        /// index calculation formula
        /// формула для рассчёта индекса
        /// </summary>
        public string Formula;
    }



    /// <summary>
    /// a message about where to move the interface so that the user sees that he has not yet configured to launch the optimizer
    /// сообщение о том куда нужно сместить интерфейс, чтобы пользователь увидел что он ещё не настроил для запуска оптимизатора
    /// </summary>
    public enum NeadToMoveUiTo
    {
        /// <summary>
        /// strategy name
        /// название стратегии
        /// </summary>
        NameStrategy,
        /// <summary>
        /// optimization phases
        /// фазы оптимизации
        /// </summary>
        Fazes,
        /// <summary>
        /// storage
        /// хранилище
        /// </summary>
        Storage,
        /// <summary>
        /// table of time frames and papers for tabs
        /// таблица таймфреймов и бумаг для вкладок
        /// </summary>
        TabsAndTimeFrames,
        /// <summary>
        /// parameter table
        /// таблица параметров
        /// </summary>
        Parametrs,
        /// <summary>
        /// Filters
        /// Фильтры
        /// </summary>
        Filters
    }
}


using System;
using System.Globalization;
using System.Windows;
using OsEngine.Language;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.aLibs;

namespace OsEngine.Robots.aDev
{
    public partial class PingBotUi
    {
        private PingBot _strategy;

        public PingBotUi(PingBot strategy)
        {
            InitializeComponent();
            _strategy = strategy;

            CheckBoxIsOn.IsChecked = _strategy.isOn;

            ComboBoxWorkingMode.Items.Add(WorkingModeType.DayAndNight);
            ComboBoxWorkingMode.Items.Add(WorkingModeType.MoscowExchange_Stocks);
            ComboBoxWorkingMode.Items.Add(WorkingModeType.MoscowExchange_Forts);
            ComboBoxWorkingMode.SelectedItem = _strategy.workingMode;

            ComboBoxTradesType.Items.Add("OnlyLong");
            ComboBoxTradesType.Items.Add("LongAndShort");
            ComboBoxTradesType.SelectedItem = _strategy.onlyLongTrades ? "OnlyLong" : "LongAndShort";

            TextBoxTradesOnStart.Text = _strategy.countTradesAtStart.ToString();
            TextBoxRandomTrades.Text = _strategy.countTradesAtRandomTime.ToString();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (Convert.ToInt32(TextBoxTradesOnStart.Text) < 0 ||
                    Convert.ToInt32(TextBoxRandomTrades.Text) < 0)
                {
                    throw new Exception("");
                }
            }
            catch (Exception)
            {
                MessageBox.Show(OsLocalization.Trader.Label13);
                return;
            }

            _strategy.countTradesAtStart = Convert.ToInt32(TextBoxTradesOnStart.Text);
            _strategy.countTradesAtRandomTime = Convert.ToInt32(TextBoxRandomTrades.Text);            
            Enum.TryParse(ComboBoxWorkingMode.Text, true, out _strategy.workingMode);
            _strategy.onlyLongTrades = ComboBoxTradesType.Text == "OnlyLong";
            _strategy.isOn = CheckBoxIsOn.IsChecked.Value;

            _strategy.Save();
            Close();
        }


    }
}
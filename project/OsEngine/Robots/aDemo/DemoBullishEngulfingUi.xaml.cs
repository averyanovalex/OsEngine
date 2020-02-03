using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OsEngine.Robots.aDemo
{
    /// <summary>
    /// Логика взаимодействия для DemoBullishEngulfingUi.xaml
    /// </summary>
    public partial class DemoBullishEngulfingUi : Window
    {

        private DemoBullishEngulfing _robot;
        
        public DemoBullishEngulfingUi(DemoBullishEngulfing robot)
        {
            InitializeComponent();
            _robot = robot;

            TextBoxVolume.Text = Convert.ToString(_robot.Volume);
            TextBoxStop.Text = Convert.ToString(_robot.Stop);
            TextBoxProfit.Text = Convert.ToString(_robot.Profit);
            TextBoxSleepage.Text = Convert.ToString(_robot.Sleepage);
            CheckBoxIsOn.IsChecked = _robot.IsOn;

            ButtonSave.Click += ButtonSave_Click;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            _robot.Volume = Convert.ToInt32(TextBoxVolume.Text);
            _robot.Stop = Convert.ToInt32(TextBoxStop.Text);
            _robot.Profit = Convert.ToInt32(TextBoxProfit.Text);
            _robot.Sleepage = Convert.ToInt32(TextBoxSleepage.Text);
            _robot.IsOn = CheckBoxIsOn.IsChecked.Value;

            _robot.Save();

            Close();
        }
    }
}

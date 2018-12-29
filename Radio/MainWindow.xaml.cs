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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO.Ports;
using System.Management;
using System.IO;
using System.Threading;

namespace Radio
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {         
        SerialPort portArduino;                         // Создаю объект SerialPort.                                                  
        double freq;                                    // Частота станции.
        bool start = true;                              // Флаг старта программы.
        byte volume, threshold;                         // Значения громкости и порога для поиска.
        StringBuilder strValue = new StringBuilder();
        int delay = 100;
        bool sound = true;
        Dictionary<double, string> freqAndStation = new Dictionary<double, string>();   // Словарь частот и названий станций, которые формирую из txt.
        List<string> settings = new List<string>();        

        public void WriteCommand(string command, double digits)
        {
            strValue.Clear();
            strValue.Append(command + digits.ToString());
            portArduino.Write(strValue.ToString());
            txtBoxCommand.Text = strValue.ToString();
            Thread.Sleep(delay);
        }

        public void WriteCommand(string command)
        {
            strValue.Clear();
            strValue.Append(command);
            portArduino.Write(strValue.ToString());
            txtBoxCommand.Text = strValue.ToString();
            Thread.Sleep(delay);
        }

        public string ReadCommand(string command)
        {
            strValue.Clear();
            strValue.Append(command);
            portArduino.Write(strValue.ToString());
            txtBoxCommand.Text = strValue.ToString();
            Thread.Sleep(delay);            
            return portArduino.ReadLine().Trim('\r');            
        }


        public MainWindow()
        {
            // В первой строчке создаю объект, который понадобится для перебора имён COM-портов.
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");
            string fullNamePort;        // Строка полного имени порта типа "USB-Serial CH340 (COM3)".
            string numberCOM = "";      // Строка номера COM-порта.          


            // Перебор имён COM-портов для поиска порта с подходящим именем.
            foreach (ManagementObject x in searcher.Get())
            {
                fullNamePort = x["Name"].ToString();
                if (fullNamePort.StartsWith("USB-SERIAL CH340"))
                    numberCOM = ("COM" + fullNamePort.Reverse().ElementAt(1)).ToString();
            }

            // Задаю параметры соединения по COM порту и открываю его.
            portArduino = new SerialPort(numberCOM, 9600);
            portArduino.Open();

            string[] stations = File.ReadAllLines("stations.txt", Encoding.Default);
            string currStation;
            int firstProbel;
            for (int i = 0; i < stations.Length; i++)
            {
                currStation = stations[i];
                firstProbel = currStation.IndexOf(' ');
                freqAndStation.Add(double.Parse(currStation.Substring(0, firstProbel)),
                    currStation.Substring(firstProbel + 1, currStation.Length - firstProbel - 1));
            }

            InitializeComponent();            

            // Считываю текущую частоту и перестраиваю на неё слайдер.             
            freq = double.Parse(ReadCommand("f")) / 10;
            sliderFreq.Value = freq;

            // Считываю и отображаю значение громкости.             
            volume = byte.Parse(ReadCommand("e"));
            txtBoxVolume.Text = volume.ToString();

            // Считываю и отображаю значения порога для поиска.
            threshold = byte.Parse(ReadCommand("g"));
            txtBoxThreshold.Text = threshold.ToString();

            // Звук тоже надо считывать, а не вот это всё...
            rdbSound.IsChecked = sound;

            settings = File.ReadAllLines("settings.ini").ToList<string>();
            btn1Mem.Content = settings.ElementAt(1);
            btn2Mem.Content = settings.ElementAt(2);
            btn3Mem.Content = settings.ElementAt(3);
            btn4Mem.Content = settings.ElementAt(4);
            btn5Mem.Content = settings.ElementAt(5);
        }               

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (start)
                start = !start;
            else
            {                
                WriteCommand("s", sliderFreq.Value * 10);
                if (freqAndStation.Keys.Contains(sliderFreq.Value))
                    txtBoxStation.Text = freqAndStation[sliderFreq.Value];
                else
                    txtBoxStation.Text = "";
                txtBoxLevel.Text = ReadCommand("l") + "/63";
                Thread.Sleep(delay);
                txtBoxMode.Text = ReadCommand("o");               
            }                                 
        }

        private void txtBoxFreq_KeyDown(object sender, KeyEventArgs e)
        {
            // По клавише ENTER передаю подходящее значение слайдеру. Дальше идёт обработка события слайдера (см. выше). 
            if (e.Key == Key.Enter)
            {
                freq = double.Parse(txtBoxFreq.Text);
                if ((freq >= sliderFreq.Minimum) && (freq <= sliderFreq.Maximum))
                    sliderFreq.Value = freq;
            }
        }        

        private void txtBoxVolume_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                volume = byte.Parse(txtBoxVolume.Text);
                if ((volume >= 0) && (volume <= 18))                
                    WriteCommand("v", volume);                                    
            }
        }

        private void btnSeekDown_Click(object sender, RoutedEventArgs e)
        {
            freq = double.Parse(ReadCommand("d")) / 10;
            sliderFreq.Value = freq;
        }

        private void txtBoxThreshold_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                threshold = byte.Parse(txtBoxThreshold.Text);
                if ((volume >= 0) && (volume <= 63))
                    WriteCommand("t", threshold);
            }
        }        

        private void rdbSound_Click(object sender, RoutedEventArgs e)
        {
            sound = !sound;
            rdbSound.IsChecked = sound;
            // Записываю в блок команду выключения и передаю её.
            WriteCommand("m");
        }

        private void btn1Mem_Click(object sender, RoutedEventArgs e)
        {
            sliderFreq.Value = double.Parse((string)btn1Mem.Content);
        }        

        private void btn1Mem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            btn1Mem.Content = txtBoxFreq.Text;
            settings[1] = txtBoxFreq.Text;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            File.WriteAllLines("settings.ini", settings);
        }

        private void btn2Mem_Click(object sender, RoutedEventArgs e)
        {
            sliderFreq.Value = double.Parse((string)btn2Mem.Content);
        }

        private void btn3Mem_Click(object sender, RoutedEventArgs e)
        {
            sliderFreq.Value = double.Parse((string)btn3Mem.Content);
        }

        private void btn4Mem_Click(object sender, RoutedEventArgs e)
        {
            sliderFreq.Value = double.Parse((string)btn4Mem.Content);
        }

        private void btn5Mem_Click(object sender, RoutedEventArgs e)
        {
            sliderFreq.Value = double.Parse((string)btn5Mem.Content);
        }

        private void btn2Mem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            btn2Mem.Content = txtBoxFreq.Text;
            settings[2] = txtBoxFreq.Text;
        }

        private void btn3Mem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            btn3Mem.Content = txtBoxFreq.Text;
            settings[3] = txtBoxFreq.Text;
        }

        private void btn4Mem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            btn4Mem.Content = txtBoxFreq.Text;
            settings[4] = txtBoxFreq.Text;
        }

        private void btn5Mem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            btn5Mem.Content = txtBoxFreq.Text;
            settings[5] = txtBoxFreq.Text;
        }

        private void btnSeekUp_Click(object sender, RoutedEventArgs e)
        {            
            freq = double.Parse(ReadCommand("u")) / 10;
            sliderFreq.Value = freq;
        }        
    }
}

using System;
using System.Windows.Threading;
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
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Data;
using HtmlAgilityPack;

namespace Sotunki_TV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        DispatcherTimer Second_Update = new DispatcherTimer();
        DispatcherTimer HalfMin_Update = new DispatcherTimer();
        bool Tick_Clock = false;
        public MainWindow()
        {
            InitializeComponent();
            SetEvents();
        }

        //This sets all of the ticking events at the start of the program
        private void SetEvents()
        {
            Second_Update.Interval = new TimeSpan(0,0,0,1);
            Second_Update.Tick += this.UpdateSecond;
            Second_Update.Start();

            HalfMin_Update.Interval = new TimeSpan(0,0,0,1);
            HalfMin_Update.Tick += this.UpdateHalfMin;
            HalfMin_Update.Start();

            SetFoodLabel();
            SetBusTimes();
        }

        private void SetFoodLabel()
        {
            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load("https://ruoka.palmia.fi/fi/ravintola/koulu/sotungin-lukio-ja-koulu/");
            string currentDay = DateTime.Today.DayOfWeek.ToString();
            string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
            string food;
            int a = Array.IndexOf(days, currentDay) + 1;
            string Final = "Tänään on ruokana\n";
            for (int i = 1; i < 4; i++)
            {
                food = "/html/body/main/article/section[1]/div/div[1]/div[2]/div[2]/div/div/div[" + (int)a + "]/ul/li[" + i + "]/p[1]/span[2]";
                string printFood;
                var docus = doc.DocumentNode.SelectSingleNode(food);
                if (docus == null) { continue; }
                printFood = docus.InnerText;
                Final += printFood + "\n";


            }
            if (Final.Length < 20) { Final = "Ruokatieto ei saatavilla :("; }
            Food_Label.Content = Final;
        }
        private void UpdateClock()
        {
            Tick_Clock = !Tick_Clock;
            string Format = Tick_Clock ? "HH:mm:" : "HH:mm";
            Label_Time.Content = DateTime.Now.ToString(Format);
        }
        private void SetBusTimes()
        {

            var currentTime = DateTime.Now;
            int secondsNow = currentTime.Hour * 60 * 60 + currentTime.Minute * 60 + currentTime.Second;

            string responseFromServer;

            string[] Bus_Names;
            string[] separated = { "{\"data\":{\"stops\":[{\"stoptimesWithoutPatterns\":[{\"realtimeArrival\":", ",\"trip\":{\"route\":{\"shortName\":\"", "\"}}},{\"realtimeArrival\":", "\"}}}]}]}}" };
            void send(string postData)
            {
                WebRequest request = WebRequest.Create("https://api.digitransit.fi/routing/v1/routers/hsl/index/graphql");
                request.Method = "POST";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/graphql";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();

                using (dataStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    responseFromServer = reader.ReadToEnd();
                    reader.Close();


                }
                dataStream.Close();
                response.Close();
                request.Method = "DELETE";

            }
            send("{stops(name: \"V9424\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }");
            List<string> times = new List<string>();
            string[] final;
            void updateValues()
            {
                //Here we declare the times of buses
                int[] list = new int[5];
                times = new List<string>();
                for (int a = 0; a < 9; a += 2)
                {
                    if (a < Bus_Names.Length)
                    {
                        list[a / 2] = Int32.Parse(Bus_Names[a]);
                        list[a / 2] = (list[a / 2] - secondsNow) / 60;
                    }
                    else
                    {
                        list[a / 2] = 0;
                    }
                    if (a + 1 < Bus_Names.Length)
                    {
                        times.Add(Bus_Names[a + 1]);
                    }
                    else
                    {
                        times.Add("None");
                    }
                }

                int[] mins = { 0, 1, 2, 3, 4 };
                int[] hrs = { 0, 1, 2, 3, 4 };
                final = new string[] { " ", " ", " ", " ", " " };
                bool[] isHour = new bool[5];

                for (int g = 0; g < 5; g++)
                {
                    mins[g] = list[g] % 60;
                    hrs[g] = list[g] / 60;
                    if (hrs[g] == 0)
                    {
                        isHour[g] = true;
                    }
                    if (isHour[g])
                    {
                        final[g] = mins[g] + " min";
                    }
                    else
                    {
                        final[g] = hrs[g] + " h " + mins[g] + " min";
                    }
                }
            }
            string[] FullList = new string[4];

            string[] Send_Functions =
            {
                "{stops(name: \"V9424\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
                "{stops(name: \"V9425\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
                "{stops(name: \"V9402\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }",
                "{stops(name: \"V9403\") {stoptimesWithoutPatterns {realtimeArrival trip { route { shortName } } } } }"
            };

            string[] Stop_Names =
            {
                "V9424",
                "V9425",
                "V9402",
                "V9403"
            };

            //Set Text Numbers Bus Stops 1,2,3,4

            for(int i = 0; i < 4; i++)
            {
                send(Send_Functions[i]);
                Bus_Names = responseFromServer.Split(separated, System.StringSplitOptions.RemoveEmptyEntries);
                updateValues();
                    FullList[i] += Stop_Names[i] + '\n';
                    FullList[i] +=
                        times[0] + " | " + final[0] + '\n' +
                        times[1] + " | " + final[1] + '\n' +
                        times[2] + " | " + final[2] + '\n' +
                        times[3] + " | " + final[3] + '\n' +
                        times[4] + " | " + final[4] + '\n';
            }
            Bus_Label_1.Content = "HSL\n" + FullList[0] + FullList[1];
            Bus_Label_2.Content = "Aikataulu\n" + FullList[2] + FullList[3];

        }
        private void UpdateSecond(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateHalfMin(object sender, EventArgs e)
        {
            SetFoodLabel();
            SetBusTimes();
        }
    }
}

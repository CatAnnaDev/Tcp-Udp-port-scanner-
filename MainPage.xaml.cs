using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool stop = false;
        int startPort = 0;
        int endPort = 65536;
        string ip = "";

        List<int> openPorts = new List<int>();

        object consoleLock = new object();

        int waitingForResponses = 0;

        int maxQueriesAtOneTime = 100;

        public delegate void Msg(string str);

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            stop = false;
            ip = IPAdrr.Text;

            if(PortMin.Text == "")
            {
                PortMin.Text = "0";
            }

            if (PortMax.Text == "")
            {
                PortMax.Text = "65536";
            }

            IPAddress ipAddress;

            IPAddress.TryParse(ip, out ipAddress);

            ThreadPool.QueueUserWorkItem(StartScan, ipAddress);
        }


        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            stop = true;
        }

        void StartScan(object o)
        {
            IPAddress ipAddress = o as IPAddress;

            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                startPort = int.Parse(PortMin.Text);
                endPort = int.Parse(PortMax.Text);
            });

            for (int i = startPort; i < endPort; i++)
            {
                lock (consoleLock)
                {
                    Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        TestedPort.Text = $"Scanning port: {i}";
                    });
                }

                while (waitingForResponses >= maxQueriesAtOneTime)
                    Thread.Sleep(0);

                if (stop)
                    break;

                try
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    s.BeginConnect(new IPEndPoint(ipAddress, i), EndConnectTcp, s);

                    Interlocked.Increment(ref waitingForResponses);

                    Socket s1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);

                    s1.BeginConnect(new IPEndPoint(ipAddress, i), EndConnectUdp, s1);

                    Interlocked.Increment(ref waitingForResponses);
                }
                catch (Exception)
                {

                }
            }
        }

        void EndConnectTcp(IAsyncResult ar)
        {
            try
            {
                DecrementResponses();

                Socket s = ar.AsyncState as Socket;

                s.EndConnect(ar);

                if (s.Connected)
                {
                    int openPort = Convert.ToInt32(s.RemoteEndPoint.ToString().Split(':')[1]);

                    openPorts.Add(openPort);

                    lock (consoleLock)
                    {
                        printMsg($"Connected TCP on port: {openPort}");
                    }

                    s.Disconnect(true);
                }
            }
            catch (Exception)
            {

            }
        }

        void EndConnectUdp(IAsyncResult ar)
        {
            try
            {
                DecrementResponses();

                Socket s = ar.AsyncState as Socket;

                s.EndConnect(ar);

                if (s.Connected)
                {
                    int openPort = Convert.ToInt32(s.RemoteEndPoint.ToString().Split(':')[1]);

                    openPorts.Add(openPort);

                    lock (consoleLock)
                    {
                        printMsg($"Connected UDP on port: {openPort}");
                    }

                    s.Disconnect(true);
                }
            }
            catch (Exception)
            {

            }
        }

        void IncrementResponses()
        {
            Interlocked.Increment(ref waitingForResponses);

            PrintWaitingForResponses();
        }

        void DecrementResponses()
        {
            Interlocked.Decrement(ref waitingForResponses);

            PrintWaitingForResponses();
        }

        void PrintWaitingForResponses()
        {
            lock (consoleLock)
            {
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    ActiveThreadCount.Text = $"Waiting {waitingForResponses} sockets ";
                });
            }
        }

        void printMsg(string msg)
        {
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,() =>{OutputData.Text = $"\n{msg}";});
        }

        void btnClear_Click(object sender, RoutedEventArgs e)
        {
            OutputData.Text = "";
        }

    }
}

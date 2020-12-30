using System;
using IRE_Connect;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace IR_Connect_Service
{
    public partial class Service1 : ServiceBase
    {
        TcpClient client;
        Int32 port = 5060;
        static Socket clientSocket;
        static String serverIP = "10.206.100.230";

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread thread = new Thread(authenticate);
            thread.Start();
        }

        protected override void OnStop()
        {
        }

        static void authenticate()
        {
            Connect:
           // Console.WriteLine("Connecting to AMI session:\n");

            // Connect to the asterisk server.
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint myEndPoint = new IPEndPoint(IPAddress.Any, 9900);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), 5038);
            utility _utility = new utility();
            try
            {

                serverSocket.Bind(myEndPoint);
                serverSocket.Listen(4);
                //  serverSocket.Accept();

                clientSocket.Connect(serverEndPoint);

                // Login to the server; manager.conf needs to be setup with matching credentials.
                clientSocket.Send(Encoding.ASCII.GetBytes("Action:Login\r\nUsername: asterisk\r\nSecret: asterisk\r\nActionID: 1\r\n\r\n"));


                int bytesRead, bytes = 0;

                do
                {
                    byte[] buffer = new byte[10024];
                    byte[] buffer2 = new byte[10024];
                    bytesRead = clientSocket.Receive(buffer);

                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    serverSocket.BeginAccept(new AsyncCallback(AcceptCallBack), serverSocket);
                    //  bytes=socketAccept.Receive(buffer2);

                    string responseData = Encoding.ASCII.GetString(buffer2, 0, bytes);
                   // Console.WriteLine(responseData);

                    String[] pars = response.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);
                    if (response.IndexOf("\r\n\r\n") > -1)
                    {
                       // Console.WriteLine(response);
                        Task task = new Task(() => _utility.consumeResponse(pars));
                        task.Start();
                    }
                    if (Regex.Match(response, "Message: Authentication accepted", RegexOptions.IgnoreCase).Success)
                    {
                        // Console.Write("Login Successfull");
                        _utility.simpleSave("tblLogs", new string[] { "log" }, new string[] { "AMI Connected: Authentication accepted" });
                    }

                    //Let's get pretty parsing and checking events



                } while (bytesRead != 0);

               // Console.WriteLine("Connection to server lost.");
                _utility._sqlcon.Dispose();
                goto Connect;
                //Console.ReadLine();

            }
            catch (Exception exx)
            {
                _utility.simpleSave("tblLogs", new string[] { "log" }, new string[] {exx.Message});
            }
           
        }

        private static void AcceptCallBack(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            byte[] buffer = new byte[10024];
            byte[] buffer2 = new byte[10024];

            int bytesRead = handler.Receive(buffer);

            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
           // Console.WriteLine("SAID: " + response);
            clientSocket.Send(Encoding.ASCII.GetBytes(response));
        }
    }
}

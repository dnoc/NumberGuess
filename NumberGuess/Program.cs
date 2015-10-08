using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace NumberGuess
{
    class Server
    {
        public Server()
        {
        }

        public void createListener()
        {
            TcpListener listener = null;
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            try
            {
                // Set the listener on the local IP address 
                // and specify the port.
                listener = new TcpListener(ipAddress, 5556);
                listener.Start();
                Console.WriteLine("Waiting for a connection...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            while (true)
            {
                Thread.Sleep(10);
                Console.WriteLine("Slept, accepting");
                //TcpClient tcpClient = listener.AcceptTcpClient();
                Socket socket = listener.AcceptSocket();
                // Read the data stream from the client. 
                byte[] bytes = new byte[256];
                NetworkStream stream = new NetworkStream(socket);
                stream.Read(bytes, 0, bytes.Length);
                SocketHelper helper = new SocketHelper();
                helper.processMsg(stream, bytes);
            }
        }
     }

    class SocketHelper
    {
            //TcpClient mscClient;
            string mstrMessage;
            string mstrResponse;
            byte[] bytesSent;
            
            int max, min, answer;
            int[] guesses;
            Regex range = new Regex(@"^-?[0-9]+ -?[0-9]+$");
            Regex guess = new Regex(@"^-?[0-9]+$");
            Random rnd = new Random();

            /*
             * Game logic 
             */
            public void processMsg(NetworkStream stream, byte[] bytesReceived)
            {
                mstrMessage = Encoding.ASCII.GetString(bytesReceived, 0, bytesReceived.Length);
                //mscClient = client;

                if (mstrMessage.ToLower().Equals("start"))
                {
                    mstrResponse = "Enter a min and max range, e.g. 10 20";
                }
                else if (range.IsMatch(mstrMessage))
                {
                    mstrResponse = setRange(mstrMessage);
                }
                else if (max != min && guess.IsMatch(mstrMessage))
                {
                    mstrResponse = handleGuess(mstrMessage);
                }
                else
                {
                    mstrResponse = "Error, invalid input: " + mstrMessage;
                }               

                bytesSent = Encoding.ASCII.GetBytes(mstrResponse);
                stream.Write(bytesSent, 0, bytesSent.Length);
            }


            /* 
             * Sets the range and answer for guesses, returns output message
             */
            private string setRange(string message)
            {
                string output = String.Empty;
                string[] numbers = message.Split(' ');
                int first = Int32.Parse(numbers[0]);
                int sec = Int32.Parse(numbers[1]);
                if (first > sec)
                {
                    max = first;
                    min = sec;
                    output = "Min range = " + min + " Max range = " + max + "\n Please guess a number.";
                    answer = rnd.Next(min, max);
                }
                else if (first < sec)
                {
                    max = sec;
                    min = first;
                    output = "Min range = " + min + " Max range = " + max + "\n Please guess a number.";
                    answer = rnd.Next(min, max);
                }
                else
                {
                    output = "Error, min range = max range";
                }
                return output;
            }

            /*
             * Handle guesses, outputs message, resets values if the guess is correct 
             */
            private string handleGuess(string message)
            {
                string output = String.Empty;
                int newGuess = Int32.Parse(message);
                guesses[guesses.Length + 1] = newGuess;
                if (newGuess == answer)
                {
                    output = "Correct! Answer: " + answer
                        + "\nPrevious guesses: " + guesses.ToString()
                        + "\nIt took you " + guesses.Length + " to get to the correct answer."
                        + "\nEnter \"Start\" to play again!";
                    guesses = new int[0];
                    max = 0;
                    min = 0;
                }
                else if (newGuess > answer)
                {
                    output = "Guess is higher than the answer.";
                }
                else
                {
                    output = "Guess is lower than the answer.";
                }
                return output;
            }
    }

    class Client
    {
    
        TcpClient client;
        NetworkStream stream;

        public Client()
        {
        }

        public void Connect()
        {
            IPAddress serverIP = Dns.GetHostEntry("localhost").AddressList[0];
            Console.WriteLine("Connecting...");
            try
            {
                Int32 port = 5556;
                client = new TcpClient(serverIP.ToString(), port);
                stream = client.GetStream();
            }
            catch (ArgumentNullException e)
            {
               Console.WriteLine("ArgumentNullException: " + e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: " + e.ToString());
            }
        }

        public void Close() {
            stream.Close();
            client.Close();
        }
        
        public string sendMessage(string message)
        {
            Byte[] data = new Byte[256];
            data = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            
            data = new Byte[256];
            string responseData = String.Empty;
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            return responseData;
        }

    }

    class Program
    {
        public static void Main(string[] args) {
            string input = String.Empty;
            string output = String.Empty;
            bool play = true;

            Server server = new Server();
            server.createListener();

            Client cli = new Client();
            cli.Connect();

            while (play)
            {
                Console.WriteLine("Enter \"Start\" to play, \"Quit\" to quit.");
                input = Console.ReadLine();
                if (input.ToLower().Equals("quit"))
                {
                    play = false;
                }
                else
                {
                    output = cli.sendMessage(input);
                }
            }
            cli.Close();
        }
    }    
}

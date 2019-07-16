using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace ClientApplication
{
    public static class ClientSocket
    {
        static TcpClient tcpclnt;
        static Stream stm;
        static Int32 PortNumber;
        static string IPAddress;

        static ClientSocket()
        {

            try
            {
                IPAddress = GetLocalIPAddress();
                PortNumber = 55555;
                tcpclnt = new TcpClient();
                stm = null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static bool EstablishedConnectionWithServer()
        {
            try
            {
                tcpclnt.Connect(IPAddress, PortNumber);
                Console.WriteLine("Marvellous Web : Connection Successful");
                stm = tcpclnt.GetStream();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (stm != null)
                {
                    stm.Close();
                }
                return false;
            }

            return true;
        }

        public static void SendMessage(string msg)
        {
            byte[] ba = Encoding.UTF8.GetBytes(msg);
            Console.WriteLine("Marvellous Web : Sending data ...");
            stm.Write(ba, 0, ba.Length);
        }

        public static string ReceiveMessage(int size = 1024)
        {
            byte[] bb = new byte[size];
            int k = stm.Read(bb, 0, size);
            string receive = Encoding.UTF8.GetString(bb, 0, k);
            Console.WriteLine("Marvellous Web : Message received from server {0}…", receive);
            return receive;
        }

        public static byte[] ReceiveByte(int size)
        {
            byte[] bb = new byte[size];
            int k = stm.Read(bb, 0, size);
            return bb;
        }

        public static void SendFile(string FileName)
        {
            byte[] ba = File.ReadAllBytes(FileName);
            Console.WriteLine("Marvellous Web : Sending data ...");
            stm.Write(ba, 0, ba.Length);
        }

        public static void ReceiveFile(string fileName, int size)
        {
            byte[] bb = new byte[size];
            SendMessage("SendFile");
            int k = stm.Read(bb, 0, size);
            File.WriteAllBytes(fileName, bb);
        }

        public static string GetLocalIPAddress()
        {
            Console.WriteLine("Marvellous Web : Host name - {0}", Dns.GetHostName());
            var Marvelloushost = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in Marvelloushost.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Marvellous Web :No network adapters with an IPv4 address in the system!");
        }

    }
}

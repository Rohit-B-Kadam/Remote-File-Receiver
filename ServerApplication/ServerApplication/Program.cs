using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;

namespace ServerApplication
{
    public class ServerSocket
    {
        Int32 PortNumber;
        string IPAddress;
        Socket socket;
        TcpListener listener;
        Dictionary<string, string> rootFolder;
        DirectoryInfo currentDir = null;
        List<string> foundFilePath;

        public ServerSocket()
        {
            // Get Local Ip Address
            IPAddress = GetLocalIPAddress();
            PortNumber = 55555;
            socket = null;
            listener = null;

            rootFolder = new Dictionary<string, string>();
            rootFolder.Add("Desktop", @"C:\Users\Rohit\Desktop");
            rootFolder.Add("Basic language", @"H:\Basic language");


           // Thread t1 = new Thread(() =>
            //{
                foreach (var item in rootFolder.Values)
                {
                    DuplicateFileCollection.SearchDuplicateFile(item);
                }
            //});
            

        }


        public string GetLocalIPAddress()
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

        public void StartServer()
        {
            try
            {

                IPAddress ipAd = System.Net.IPAddress.Parse(IPAddress);
                Console.WriteLine("Marvellous Web : Server started ... ");
                listener = new TcpListener(ipAd, PortNumber);
                listener.Start();

                Console.WriteLine("Marvellous Web : Server started at port : " + PortNumber);
                Console.WriteLine("Marvellous Web : The local End point is :" + listener.LocalEndpoint);


                while (true)
                {
                    // Accepts the pending client connection and returns a socket for communciation.
                    Console.WriteLine("Marvellous Web : Server Waiting for a connection ....");
                    socket = listener.AcceptSocket();

                    Console.WriteLine("Marvellous Web : Connection Established with client....");
                    Console.WriteLine("Marvellous Web : Connection accepted from " + socket.RemoteEndPoint);

                    bool connection = true;
                    while (connection)
                    {
                        string receiveMsg = ReceiveMessage();

                        switch (receiveMsg)
                        {
                            case "LOGIN":
                                LoginProcess();
                                break;
                            case "ROOT_FOLDER":
                                SendRootFolders();
                                break;
                            case "GET_FOLDER_CONTAIN":
                                SendFolderContain();
                                break;
                            case "BACK":
                                DisplayParentContent();
                                break;
                            case "SEARCH":
                                SearchFile();
                                break;
                            case "SEND_CUR_PATH":
                                SendCurrentPath();
                                break;
                            case "CREATE_FOLDER":
                                CreateFolderInCurrentDirectory();
                                break;
                            case "UPLOAD":
                                ReceiveFile();
                                break;
                            case "DOWNLOAD":
                                SendFile();
                                break;
                            case "EXIT":
                                connection = false;
                                socket.Close();
                                break;
                            default:
                                break;
                        }

                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Marvellous Web : Exception - " + e.StackTrace);
                SendMessage("EXIT");
            }
            finally
            {
                Console.WriteLine("\nMarvellous Web : Deallocating all resources ...");
                if (socket != null)
                {
                    socket.Close();
                }
                if (listener != null)
                {
                    listener.Stop();
                }
            }

        }

        public void SendMessage(string msg)
        {
            byte[] ba = Encoding.UTF8.GetBytes(msg);
            Console.WriteLine("Marvellous Web : Sending data ...");
            socket.Send(ba);
        }

        public string ReceiveMessage(int size = 1024)
        {
            byte[] bb = new byte[size];
            int k = socket.Receive(bb);
            string receive = Encoding.UTF8.GetString(bb, 0, k);
            Console.WriteLine("Message received from client {0}…", receive);
            return receive;
        }

        public void LoginProcess()
        {
            SendMessage("REQUEST_ACCEPTED");

            //receiving username and password
            string receive = ReceiveMessage();

            string[] userInfo = receive.Split('$');

            // checking username and password is correct or not
            if( userInfo[0].Equals("Rohit Kadam") && userInfo[1].Equals("Jacksparrow") )
            {
                SendMessage("VALID");
            }
            else
            {
                SendMessage("INVALID");
            }
        }

        public void SendRootFolders()
        {
            SendMessage("OK");
            List<string> fileLists = new List<string>();
            foreach (var item in rootFolder.Keys)
            {
                fileLists.Add(item);
            }

            byte[] byteArray = Serialize(fileLists);

            string receive = ReceiveMessage();
            if(receive.Equals("SEND_SIZE"))
            {
                SendMessage(byteArray.Length.ToString());
            }

            receive = ReceiveMessage();
            if (receive.Equals("SEND_OBJECT"))
            {
                socket.Send(byteArray);
            }

            currentDir = null;
        }

        public void SendFolderContain()
        {
            SendMessage("OK");

            //Receiving Folder Name
            string fileName = ReceiveMessage();
            string folderPath;

            // Getting currentDirectory(searching the folder)
            if(rootFolder.ContainsKey(fileName))
            {
                rootFolder.TryGetValue(fileName, out folderPath);
                currentDir = new DirectoryInfo(folderPath);
            }
            else
            {
                foreach (var folder in currentDir.GetDirectories())
                {
                    if(fileName.Equals(folder.Name))
                    {
                        currentDir = folder;
                        break;
                    }
                }
            }

            // Fetch folder and file list from currentDirectory
            List<string> FolderLists = new List<string>();
            List<string> FileLists = new List<string>();
            foreach (var item in currentDir.GetDirectories())
            {
                FolderLists.Add(item.Name);
            }

            foreach (var item in currentDir.GetFiles())
            {
                FileLists.Add(item.Name);
            }

            // sending folders
            byte[] byteArray = Serialize(FolderLists);
            SendMessage(byteArray.Length.ToString());

            string receive = ReceiveMessage();
            if (receive.Equals("SEND_FOLDER"))
            {
                socket.Send(byteArray);
            }

            // sending File
            receive = ReceiveMessage();
            if(receive.Equals("NEXT"))
            {
                byteArray = Serialize(FileLists);
                SendMessage(byteArray.Length.ToString());

                receive = ReceiveMessage();
                if (receive.Equals("SEND_FILES"))
                {
                    socket.Send(byteArray);
                }

            }
        }

        public void DisplayParentContent()
        {
            if (currentDir == null || rootFolder.ContainsKey(currentDir.Name))
            {
                SendMessage("DISPLAY_ROOT");
                return;
            }

            SendMessage("OK");
            
            // Getting currentDirectory(searching the folder)
            currentDir = currentDir.Parent;


            // Fetch folder and file list from currentDirectory
            List<string> FolderLists = new List<string>();
            List<string> FileLists = new List<string>();
            foreach (var item in currentDir.GetDirectories())
            {
                FolderLists.Add(item.Name);
            }

            foreach (var item in currentDir.GetFiles())
            {
                FileLists.Add(item.Name);
            }

            // sending folders
            string receive = ReceiveMessage();      //"SENDSIZE"

            byte[] byteArray = Serialize(FolderLists);
            SendMessage(byteArray.Length.ToString());

            receive = ReceiveMessage();
            if (receive.Equals("SEND_FOLDER"))
            {
                socket.Send(byteArray);
            }

            // sending File
            receive = ReceiveMessage();
            if (receive.Equals("NEXT"))
            {
                byteArray = Serialize(FileLists);
                SendMessage(byteArray.Length.ToString());

                receive = ReceiveMessage();
                if (receive.Equals("SEND_FILES"))
                {
                    socket.Send(byteArray);
                }

            }
        }

        static byte[] Serialize( List<string> list)
        {
            // To serialize the given list,  
            // you must first open a stream for writing.
            MemoryStream ms = new MemoryStream();

            // Construct a BinaryFormatter and use it to serialize the data to the stream.
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(ms, list);
                return ms.ToArray();
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
            }
            finally
            {
                ms.Close();
            }
            return null;
        }

        public void SearchFile()
        {
            SendMessage("SEND_FILENAME");

            //Receive FileName to search
            string fileName = ReceiveMessage(256);
            foundFilePath = new List<string>();

            foreach (var item in rootFolder.Values)
            {
                SearchFileInFolder(item , fileName);

                
            }
            byte[] byteArray = Serialize(foundFilePath);
            SendMessage(byteArray.Length.ToString());

            string receive = ReceiveMessage();
            if (receive.Equals("SEND_FILES"))
            {
                socket.Send(byteArray);
            }



        }

        public void SearchFileInFolder(string FolderPath , string fileName )
        {
            DirectoryInfo di = new DirectoryInfo($@"{FolderPath}");

            try
            {
                // Determine whether the directory exists.
                if (di.Exists)
                {
                    // Indicate that the directory already exists.

                    foreach (var directory in di.GetDirectories())
                    {
                        if (!di.Name.Equals(@"$Recycle.Bin") && !di.Name.Equals(@"$Recycle.Bin"))
                            SearchFileInFolder(directory.FullName,fileName);
                    }

                    foreach (var file in di.GetFiles())
                    {
                        //Check if file is equal to given file
                        if (file.Name.Contains(fileName))
                        //if(file.FullName.Contains(FilePath))
                        {
                            foundFilePath.Add(file.FullName);
                        }
                    }
                }

            }
            catch (Exception){ }
        }

        public void SendCurrentPath()
        {
            string fullPath;
            if (currentDir == null)
            {
                fullPath = @"\";
            }
            else
            {
                fullPath = currentDir.FullName;
                foreach (var item in rootFolder.Keys)
                {
                    if (fullPath.Contains(item))
                    {
                        // getting only required path not full path
                        string value;
                        rootFolder.TryGetValue(item,out value);
                        int startindex = value.Length - item.Length;
                        fullPath = fullPath.Substring(startindex);
                        //fullPath = fullPath.Remove(0, fullPath.Length - currentDir.Name.Length);
                        break;
                    }
                }
            }

            SendMessage(fullPath);
        }

        public void CreateFolderInCurrentDirectory()
        {
            SendMessage("SEND_FOLDERNAME");
            string folderName = ReceiveMessage();

            foreach (var folder in currentDir.GetDirectories())
            {
                if(folder.Name.Equals(folderName))
                {
                    SendMessage("FOLDER_EXIST");
                    return;
                }
            }

            currentDir.CreateSubdirectory(folderName);
            SendMessage("SUCCESSFULLY_CREATED");
        }

        public void ListAllFile()
        {
            SendMessage("sending");
            DirectoryInfo di = new DirectoryInfo("Storage");
            string filesName = "";

            foreach (var file in di.GetFiles())
            {
                filesName += file.Name + "$";
            }

            int size = filesName.Length;
            SendMessage(size.ToString());   //send size of data

            if (size != 0)
            {
                string output = ReceiveMessage(256);
                if (output.Equals("okSend"))
                {
                    SendMessage(filesName);
                }
            }
        }

        public void SendFile()
        {
            SendMessage("SEND_FILENAME");

            //receive Filename
            string fileName = ReceiveMessage();

            if (!fileName.Contains(@"\"))
            {
                fileName = string.Format(@"{0}\{1}", currentDir.FullName, fileName);
            }

            if (File.Exists(fileName))
            {
                SendMessage("EXIST");
            }
            else
            {
                SendMessage("NOT_EXIST");
                return;
            }

            string output = ReceiveMessage();   //client asking file size
            // client want size of file
            FileInfo fi = new FileInfo(fileName);
            SendMessage(fi.Length.ToString());

            output = ReceiveMessage();
            if (output.Equals("SendFile"))
            {
                //send the file
                byte[] ba = File.ReadAllBytes(fileName);
                Console.WriteLine("Marvellous Web : Sending data ...");
                socket.Send(ba);
            }
        }

        public void ReceiveFile()
        {
            string output;
            SendMessage("SEND");

            //Receiveing data
            output = ReceiveMessage();
            string[] FilenameChecksum = output.Split('$');


            string fileName = string.Format(@"{0}\{1}",currentDir.FullName, FilenameChecksum[0]);
            string samefile;
            if (File.Exists(fileName))
            {
                SendMessage("FILE_EXIST");   //sending client status
                output = ReceiveMessage();
                if (output.Equals("NO"))
                {
                    return;
                }
                SendMessage("ALLOK");
            }
            else if( (samefile = DuplicateFileCollection.CheckFileIsDuplicate(FilenameChecksum[1])) != null )
            {

                SendMessage(samefile);   //sending client status
                output = ReceiveMessage();
                if (output.Equals("NO"))
                {
                    return;
                }
                SendMessage("ALLOK");
            }
            else
            {
                SendMessage("ALLOK");
            }

            int size = Convert.ToInt32(ReceiveMessage());
            byte[] bb = new byte[size];
            SendMessage("SENDFILE");
            int k = socket.Receive(bb);

            File.WriteAllBytes(fileName, bb);
            SendMessage(currentDir.Name);
        }

    }

    static class DuplicateFileCollection
    {
        //public static List<string> sameFileList;
        public static Dictionary<string, List<string> > CheckSumList;

        static DuplicateFileCollection()
        {
            CheckSumList = new Dictionary<string, List<string>>();
        }

        public static void SearchDuplicateFile(string FName)
        {
            DirectoryInfo di = new DirectoryInfo($@"{FName}");
            List<string> sameFileList;
            try
            {
                foreach (var directory in di.GetDirectories())
                {
                    if (!di.Name.Equals("$RECYCLE.BIN"))
                        SearchDuplicateFile(directory.FullName);
                }

                foreach (var file in di.GetFiles())
                {

                    string checksm = md5Checksum(file.FullName);

                    if (CheckSumList.ContainsKey(checksm))
                    {
                        CheckSumList.TryGetValue(checksm, out sameFileList);
                        sameFileList.Add(file.FullName);
                    }
                    else
                    {
                        // Add to list
                        sameFileList = new List<string>();
                        sameFileList.Add(file.FullName);
                        CheckSumList.Add(checksm, sameFileList);
                    }

                }

            }
            catch (UnauthorizedAccessException)
            { }
            catch (Exception e)
            {
                
            }
            finally { }
        }

        public static string CheckFileIsDuplicate(string checkSum)
        {
            if(CheckSumList.ContainsKey(checkSum))
            {
                List<string> lists;
                CheckSumList.TryGetValue(checkSum, out lists);
                foreach (var item in lists)
                {
                    return item;
                }
            }
            return null;
            
        }

        static string md5Checksum(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", String.Empty);
                }
            }
        }

        public static void WriteInFile()
        {
            FileInfo fobj = new FileInfo("DuplicateFileList");

            using (StreamWriter sw = fobj.AppendText())
            {

                foreach (var list in CheckSumList.Values)
                {
                    sw.WriteLine("Duplicate File:--->");
                    foreach (var file in list)
                    {
                        sw.WriteLine("\t{0}", file);
                    }
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ServerSocket ss = new ServerSocket();
            ss.StartServer();
        }
    }
}

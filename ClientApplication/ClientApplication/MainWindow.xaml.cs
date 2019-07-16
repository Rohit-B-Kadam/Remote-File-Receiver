using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ClientApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel vm;
        public string[] selectedFile;
        public MainWindow()
        {
            InitializeComponent();
            selectedFile = new string[2];
            vm = new MainViewModel();
            this.Loaded += (s, e) => DataContext = vm;
        }


        private void UploadFile(object sender, RoutedEventArgs e)
        {
            if(selectedFile.Length == 0)
            {
                MessageBox.Show("First Selected the file","Error");
                return;
            }

            if(!File.Exists(selectedFile[1]))
            {
                MessageBox.Show("File is not exist", "Error");
                return;
            }

            vm.UploadTheFile(selectedFile[0] , selectedFile[1]);

        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataItem item = (sender as ListView).SelectedItem as DataItem;

            if (item.Extension.Equals("FOLDER"))
            {
                vm.OpenFolder(item.FileName);
            }
            else if (item.Extension.Equals("PARENT"))
            {
                vm.DisplayParentData();
            }
            else if (item.Extension.Equals("FILE"))
            {
                vm.DownloadTheFile(item.FileName);
            }

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                ClientSocket.SendMessage("EXIT");
            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }

        }

        private void SearchFile(object sender, RoutedEventArgs e)
        {
            vm.CheckFileExist();
        }

        private void BrowseFile(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // selected document

                txtFileToUpload.Text = dlg.SafeFileName;
                selectedFile[0] = dlg.SafeFileName;
                selectedFile[1] = dlg.FileName;
            }
        }

        private void CreateNewFolder(object sender, RoutedEventArgs e)
        {
            if (txtNewFolderName.Name.Length != 0)
            {
                vm.CreateNewFolder();
            }
            else
            {
                MessageBox.Show("Give Name of File");
            }
        }
    }


    public class MainViewModel : INotifyPropertyChanged
    {
        //Important
        private void Notify(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        // Data Binding with UI

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                Notify("SearchText");
            }
        }

        private string _labelPath;
        public string LabelPath
        {
            get { return _labelPath; }
            set
            {
                _labelPath = value;
                Notify("LabelPath");
            }
        }

        private string _labelStatus;
        public string LabelStatus
        {
            get { return _labelStatus; }
            set
            {
                _labelStatus = value;
                Notify("LabelStatus");
            }
        }

        private string _fileToUpload;
        public string FileToUpload
        {
            get { return _fileToUpload; }
            set
            {
                _fileToUpload = value;
                Notify("FileToUpload");
            }
        }

        private string _newFolderName;
        public string NewFolderName
        {
            get { return _newFolderName; }
            set
            {
                _newFolderName = value;
                Notify("NewFolderName");
            }
        }

        private bool _enableUpload;
        public bool EnableUpload
        {
            get { return _enableUpload; }
            set
            {
                _enableUpload = value;
                Notify("EnableUpload");
            }
        }

        private ObservableCollection<DataItem> _filesList;
        public ObservableCollection<DataItem> FilesList
        {
            get
            {
                return _filesList;
            }
            set
            {
                _filesList = value;
                Notify("FilesList");
            }
        }


        public MainViewModel()
        {
            SearchText = "";
            LabelPath = "";
            LabelStatus = "";
            FileToUpload = "";
            NewFolderName = "New Folder";
            EnableUpload = false;
            FilesList = new ObservableCollection<DataItem>();
            DefaultViewOfList();
        }

        public void DefaultViewOfList()
        {
            try
            {
                FilesList.Clear();
                ClientSocket.SendMessage("ROOT_FOLDER");
                string receive = ClientSocket.ReceiveMessage();
                if (!receive.Equals("OK"))
                {
                    throw new Exception();
                }

                ClientSocket.SendMessage("SEND_SIZE");
                receive = ClientSocket.ReceiveMessage();


                ClientSocket.SendMessage("SEND_OBJECT");

                byte[] byteArray = ClientSocket.ReceiveByte(Convert.ToInt32(receive));

                List<string> lists = Deserialize(byteArray);
                foreach (var item in lists)
                {
                    FilesList.Add(new DataItem("FOLDER", item));
                }

                // get path to display
                DisplayPath();
            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }
        }

        List<string> Deserialize(byte[] arrBytes)
        {
            // Declare the List reference.
            List<string> addresses = null;

            MemoryStream ms = new MemoryStream();
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                ms.Write(arrBytes, 0, arrBytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                addresses = formatter.Deserialize(ms) as List<string>;
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                MessageBox.Show(e.ToString());
                throw;
            }
            finally
            {
                ms.Close();
            }

            return addresses;
        }

        public void OpenFolder(string fileName)
        {
            try
            {
                //clearing list
                FilesList.Clear();
                FilesList.Add(new DataItem("PARENT", ".."));
                ClientSocket.SendMessage("GET_FOLDER_CONTAIN");
                string receive = ClientSocket.ReceiveMessage();
                if (!receive.Equals("OK"))
                {
                    throw new Exception();
                }

                ClientSocket.SendMessage(fileName);


                // Receiving Folder List

                // receiving size
                int size = Convert.ToInt32(ClientSocket.ReceiveMessage());

                ClientSocket.SendMessage("SEND_FOLDER");

                byte[] byteArray = ClientSocket.ReceiveByte(size);

                List<string> lists = Deserialize(byteArray);

                foreach (var item in lists)
                {
                    FilesList.Add(new DataItem("FOLDER", item));
                }

                //Receiving File list
                ClientSocket.SendMessage("NEXT");

                size = Convert.ToInt32(ClientSocket.ReceiveMessage());

                ClientSocket.SendMessage("SEND_FILES");

                byteArray = ClientSocket.ReceiveByte(size);

                lists = Deserialize(byteArray);

                foreach (var item in lists)
                {
                    FilesList.Add(new DataItem("FILE", item));
                }


                // get path to display
                DisplayPath();
            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }
        }

        public void DisplayParentData()
        {

            try
            {
                //clearing list
                FilesList.Clear();
                FilesList.Add(new DataItem("PARENT", ".."));
                ClientSocket.SendMessage("BACK");
                string receive = ClientSocket.ReceiveMessage();

                if (receive.Equals("DISPLAY_ROOT"))
                {
                    DefaultViewOfList();
                    return;
                }


                // Receiving Folder List

                // receiving size
                ClientSocket.SendMessage("SENDSIZE");

                int size = Convert.ToInt32(ClientSocket.ReceiveMessage());

                ClientSocket.SendMessage("SEND_FOLDER");

                byte[] byteArray = ClientSocket.ReceiveByte(size);

                List<string> lists = Deserialize(byteArray);

                foreach (var item in lists)
                {
                    FilesList.Add(new DataItem("FOLDER", item));
                }

                //Receiving File list
                ClientSocket.SendMessage("NEXT");

                size = Convert.ToInt32(ClientSocket.ReceiveMessage());

                ClientSocket.SendMessage("SEND_FILES");

                byteArray = ClientSocket.ReceiveByte(size);

                lists = Deserialize(byteArray);

                foreach (var item in lists)
                {
                    FilesList.Add(new DataItem("FILE", item));
                }

                // get path to display
                DisplayPath();
            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }

        }

        public void CheckFileExist()
        {
            try
            {

                FilesList.Clear();
                FilesList.Add(new DataItem("PARENT", ".."));

                string output;
                ClientSocket.SendMessage("SEARCH");
                output = ClientSocket.ReceiveMessage();
                if (output.Equals("SEND_FILENAME"))
                {
                    ClientSocket.SendMessage(SearchText);   //send filename

                    //Receiving File list
                    int size = Convert.ToInt32(ClientSocket.ReceiveMessage());

                    ClientSocket.SendMessage("SEND_FILES");

                    byte[] byteArray = ClientSocket.ReceiveByte(size);

                    List<string> lists = Deserialize(byteArray);

                    foreach (var item in lists)
                    {
                        FilesList.Add(new DataItem("FILE", item));
                    }
                }

                //Enable to create folder
                EnableUpload = false;
            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }

        }

        public void DownloadTheFile(string fileName)
        {
            try
            {
                string output;
                ClientSocket.SendMessage("DOWNLOAD");
                output = ClientSocket.ReceiveMessage();   //Normal size will be 256
                if (output.Equals("SEND_FILENAME"))
                {
                    //Sending FileName
                    ClientSocket.SendMessage(fileName);
                    output = ClientSocket.ReceiveMessage(256);    //Receice the ack
                    if (output.Equals("NOT_EXIST"))
                    {
                        MessageBox.Show("Filenot exist");
                        return;
                    }

                    ClientSocket.SendMessage("sizeOfFile");
                    int size = Convert.ToInt32(ClientSocket.ReceiveMessage());


                    ClientSocket.ReceiveFile(fileName, size);

                    System.Diagnostics.Process.Start("explorer.exe", $@"{fileName}");
                }

            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }
        }

        public void DisplayPath()
        {
            try
            {
                ClientSocket.SendMessage("SEND_CUR_PATH");
                string path = ClientSocket.ReceiveMessage();

                // Logic to enable disable
                LabelPath = path;

                if (path.Equals(@"\"))
                {
                    EnableUpload = false;
                }
                else
                {
                    EnableUpload = true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }
    
        }

        public void CreateNewFolder()
        {
            try
            {

                ClientSocket.SendMessage("CREATE_FOLDER");
                string receive = ClientSocket.ReceiveMessage();
                if (!receive.Equals("SEND_FOLDERNAME"))
                {
                    throw new Exception();
                }

                ClientSocket.SendMessage(NewFolderName);
                receive = ClientSocket.ReceiveMessage();

                if(receive.Equals("FOLDER_EXIST"))
                {
                    MessageBox.Show("Folder already exist in current directory");
                    return;
                }
                else if(receive.Equals("SUCCESSFULLY_CREATED"))
                {
                    OpenFolder(NewFolderName);
                    MessageBox.Show("Folder is successfully created");
                }

            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }
        }


        public void UploadTheFile(string FileName , string FilePath)
        {
            try
            {
                string output,sendData;
                ClientSocket.SendMessage("UPLOAD");
                output = ClientSocket.ReceiveMessage();   //Normal size will be 256
                if (output.Equals("SEND"))
                {
                    //Sending FileName and checksum
                    sendData = string.Format("{0}${1}", FileName, md5Checksum(FilePath));
                    ClientSocket.SendMessage(sendData);

                    // Reply form server
                    output = ClientSocket.ReceiveMessage(256); // ALLOK , FILE_EXIST

                    //If file is already exist
                    if (output.Equals("FILE_EXIST"))
                    {
                        //File already present
                        MessageBoxResult result = MessageBox.Show("File is already exist, Do you wan't  to overwrite", "Error", MessageBoxButton.YesNo);
                        if (result.HasFlag(MessageBoxResult.No))
                        {
                            ClientSocket.SendMessage("NO");
                            return;
                        }
                        ClientSocket.SendMessage("YES");
                        ClientSocket.ReceiveMessage();      //Receive ALLOK
                    }
                    else if(output.Equals("ALLOK"))
                    {

                    }
                    else
                    {
                        MessageBoxResult result = MessageBox.Show("Same file is Exist at "+output, "Error", MessageBoxButton.YesNo);
                        if (result.HasFlag(MessageBoxResult.No))
                        {
                            ClientSocket.SendMessage("NO");
                            return;
                        }
                        ClientSocket.SendMessage("YES");
                        ClientSocket.ReceiveMessage();      //Receive ALLOK
                    }


                    // Sending FileSize
                    FileInfo fi = new FileInfo(FilePath);
                    ClientSocket.SendMessage(fi.Length.ToString());

                    output = ClientSocket.ReceiveMessage();
                    if (output.Equals("SENDFILE"))
                    {
                        //SendFile
                        ClientSocket.SendFile(FilePath);
                        output = ClientSocket.ReceiveMessage();

                        // Display in list
                        DisplayParentData();
                        OpenFolder(output);
                        MessageBox.Show("Successfully uploaded", "Output");
                        
                    }
                }
                
            }
            catch (Exception)
            {
                MessageBox.Show("Server have been stop");
                Environment.Exit(0);
            }
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
        
    }

    public class DataItem
    {
        public string Extension { set; get; }

        public string FileName { set; get; }

        public DataItem(string ext, string filename)
        {
            Extension = ext;
            FileName = filename;
        }
    }
}

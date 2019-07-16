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

namespace ClientApplication
{
    /// <summary>
    /// Interaction logic for LoginScreen.xaml
    /// </summary>
    public partial class LoginScreen : Window
    {
        bool closeConnectFlag = true;
        public LoginScreen()
        {
            InitializeComponent();
            ConnetToServer();
        }

        public void ConnetToServer()
        {
            if (!ClientSocket.EstablishedConnectionWithServer())
            { 
                //Exception
                MessageBox.Show("Server is not started");
                Environment.Exit(0);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {

            string userName = txtUserName.Text;
            string password = txtPassword.Password;

            if(Authentication(userName,password))
            {
                // authenication is valid
                // MessageBox.Show("Valid username or password", "Success");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                closeConnectFlag = false;
                this.Close();
            }
            else
            {
                //Authentication is invalid
                MessageBox.Show("Invalid username or password","Error");

            }

        }

        public bool Authentication(string username , string password)
        {
            ClientSocket.SendMessage("LOGIN");
            string receive = ClientSocket.ReceiveMessage();

            if (receive.Equals("REQUEST_ACCEPTED"))
            {
                // Sending username and password to server
                string msgSend = string.Format("{0}${1}", username, password);
                ClientSocket.SendMessage(msgSend);

                // Reply from server 
                receive = ClientSocket.ReceiveMessage();
                
                if(receive.Equals("VALID"))
                {
                    return true;
                }

            }

            return false;
        }


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (closeConnectFlag)
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
        }
        
    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace ClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient client = new TcpClient(); //клієнт, який відпрвляє запити
        NetworkStream ns; //потік для спілкування із сервером
        Thread thread;    //для отримання запитів від сервера
        private ChatMessage _message = new ChatMessage(); //повідомленя, яке надсилається або отримується
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fileName = "setting.txt";  //файл із налаштуванями
                IPAddress ip; //ip адерса сервера
                int port;     //порт на якому працює сервер
                using (FileStream fs = new FileStream(fileName, FileMode.Open,
                    FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))  //читання файлу
                    {
                        ip = IPAddress.Parse(sr.ReadLine()); //читаємо ip
                        port = int.Parse(sr.ReadLine());     //читаємо порт
                    }
                }
                _message.UserName = txtUserName.Text; //Вказуємо ім'я користувача
                _message.UserId=Guid.NewGuid().ToString(); //вказуємо ідентифікатор
                client.Connect(ip, port); //конектимося до сервера
                //Повідомлення про успішне підключення
                _message.Text = $"Підключаємося до сервера {ip.ToString()}:{port}";
                ShowMessage(_message); 
                //Отримуємо потік даних від сервера
                ns = client.GetStream();
                //Запускаємо другорядний потік для оримання повідомлень від сервера
                thread = new Thread(o => RecieveData((TcpClient)o));
                thread.Start(client);//Запускаємо потік
                _message.MessageType = TypeMessage.Login; //виконуємо логін
                _message.Text = "Приєднався до чату";//текст повідомлення
                byte[] buffer = _message.Serialize(); //серіалізуємо повідомлення в байти
                ns.Write(buffer, 0, buffer.Length); //відправляємо повідолмення на сервер
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem connect server: " + ex.Message);
            }

        }
        //Метод для отримання даних від сервера
        private void RecieveData(TcpClient client) //Силка на клієнт
        {
            NetworkStream ns = client.GetStream(); //Читаємо дані від сервера
            byte[] recivedBytes = new byte[1000024]; //зберігає байти, які надіслав сервер
            int byte_count; //розмір масиву байт
            while((byte_count=ns.Read(recivedBytes,0,recivedBytes.Length))>0) //читаємо повідомлення від сервер
            {
                
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        ChatMessage message = ChatMessage.Deserialize(recivedBytes);
                        switch (message.MessageType)
                        {
                            case TypeMessage.Login:
                                {
                                    if (message.UserId != _message.UserId)
                                    {
                                        ShowMessage(message); 
                                    }
                                    break;
                                }
                            case TypeMessage.Logout:
                                {
                                    if (message.UserId != _message.UserId)
                                    {
                                        ShowMessage(message);
                                    }
                                    break;
                                }
                            case TypeMessage.Message:
                                {
                                    ShowMessage(message);
                                    break;
                                }
                        }
                        lbInfo.Items.MoveCurrentToLast();
                        lbInfo.ScrollIntoView(lbInfo.Items.CurrentItem);
                    }
                    catch (Exception ex) 
                    {
                        MessageBox.Show("Problem connect server: " + ex.Message);
                    }
                }));
            }
        }

        private void ShowMessage(ChatMessage message)
        {
            var imageSource = new BitmapImage();
            using (var bmpStream = new MemoryStream(message.Image, 0, message.ImageSize))
            {
                imageSource.BeginInit();
                imageSource.StreamSource = bmpStream;
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.EndInit();
            }

            imageSource.Freeze(); // here

            Image image = new Image();
            image.Source = imageSource;
            lbInfo.Items.Add(new MessageView { Image = image, Text = message.UserName + ":" + message.Text });
        }
        //подія закриття вікна
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Формуємо повідомлення 
            _message.MessageType = TypeMessage.Logout; //Тип повідомлення вихід
            _message.Text = "Покинув чат"; //текст повідомлення
            byte [] buffer = _message.Serialize(); //Перевідомлення перетворюємо в байти
            ns.Write (buffer, 0, buffer.Length); //відправляємо повідомлення на сервер
            client.Client.Shutdown(SocketShutdown.Both); //звершуємо роботу клієнта
            thread.Join(); //Очікуємо завершення виконання задач у потоці
            ns.Close(); //Закриваємо потік зяднаня із сервером
            client.Close(); //Закриваємо клієнта
        }

        private void bntSend_Click(object sender, RoutedEventArgs e)
        {
            //Формуємо повідомлення 
            _message.MessageType = TypeMessage.Message; //Тип повідомлення текст
            _message.Text = txtText.Text; //текст повідомлення
            byte[] buffer = _message.Serialize(); //Перевідомлення перетворюємо в байти
            ns.Write(buffer, 0, buffer.Length); //відправляємо повідомлення на сервер
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image (*.bmp, *.jpg, *.png)|*.bmp; *.jpg; *.png|All (*.*)|*.*";
            if(openFileDialog.ShowDialog()==true)
            {
                Avatar.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                //Base64 - масив байті у вигляді рядка
                Byte[] bytes = File.ReadAllBytes(openFileDialog.FileName);
                _message.ImageSize = bytes.Length;
                _message.Image = bytes; 

            }
        }
    }
}

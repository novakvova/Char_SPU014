using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerApp
{
    class Program
    {
        //Для блокування потоку - в середині процесу, який якивонується
        static readonly object _lock = new object();
        //Список клієнтів, які підлкючаються на сервер
        static readonly Dictionary<int, TcpClient> list_clients =
            new Dictionary<int, TcpClient>();

        static void Main(string[] args)
        {
            string fileName = "setting.txt";  //файл із налаштуванями
            int count = 1; //Номер першого клієнта в чаті
            IPAddress ip; //ip адерса сервера
            int port;     //порт на якому працює сервер
            using(FileStream fs = new FileStream(fileName, FileMode.Open, 
                FileAccess.Read))
            {
                using(StreamReader sr = new StreamReader(fs))  //читання файлу
                {
                    ip=IPAddress.Parse(sr.ReadLine()); //читаємо ip
                    port=int.Parse(sr.ReadLine());     //читаємо порт
                }
            }
            //стволили сокет для сервера вказали кінцеву точку(айпішка і порт)
            TcpListener serverSocket = new TcpListener(ip, port);
            serverSocket.Start();
            while(true) //у циклі очікуємо запитів від клієнтів
            {
                //запит від клієнта прийшов
                TcpClient client = serverSocket.AcceptTcpClient(); //отримали клієнта
                lock(_lock) list_clients.Add(count, client); //клієнта додамає у список
                Thread t = new Thread(handle_client); //в окремому потоці обробробляємо клієнта
                t.Start(count); //запускаємо потік і вказуємо номер клієнта
                count++; //збільшуємо номер клєнта на 1
            }
        }
        //обробка запита від клієнта
        public static void handle_client(object o)
        {
            int id = (int)o; //зберегли номер клієнта
            TcpClient client = list_clients[id]; //Отримали сокет клієнта, по його номеру
            while (true) //Цикл для роботи сокета клієнта
            {
                NetworkStream stream = client.GetStream(); //Потік із даними клієнта
                //Виводимо ip адрес і порт клієнта
                Console.WriteLine("Client endpoint: "+client.Client.RemoteEndPoint);
                byte[] buffer = new byte[1000024]; //Дані про клієнта
                int byte_count = stream.Read(buffer, 0, buffer.Length); //Читаємо дані кліжнта
                if (byte_count==0) //Якщо дані відсутні, 
                {
                    break;//завершуємо цикл і перестаємо спікуватися із клієнтом
                }
                //Декодуємо байти в рядок
                //string data = Encoding.UTF8.GetString(buffer,0,byte_count);
                //broadcast(data); //Розсилаємо повідомолення усім клієнта, що є в чаті
                //Console.WriteLine(data);//на вервер виводим повідомлення клієнта

                broadcast(buffer);
            }
            lock (_lock) list_clients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        //Розсилка повідомлень усім клієнтам, хто є в чаті через broadcastt
        public static void broadcast(byte[] buffer) //Повідомлення
        {
            //byte[] buffer = Encoding.UTF8.GetBytes(data); //текст перетворюємо в байти
            lock(_lock) //блокуємо потік
            {
                foreach(TcpClient c in list_clients.Values) //розсилаємо усім хто є в чаті
                {
                    NetworkStream stream = c.GetStream(); //отримали силку на клієнта
                    stream.Write(buffer, 0, buffer.Length); //відпраивили клієнту
                }
            }
        }
    }
}

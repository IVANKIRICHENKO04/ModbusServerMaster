using System.Net.Sockets;
using System.Net;

namespace MdbusNServerMaster.Classes
{
    public class ModbusTCP
    {
        string RemoteIpAddress;     // адрес для передачи запроса
        int RemotePort;             // порт для передачи запроса 
        public TcpClient tcp_client;
        IPAddress remoteIPAddress;
        public NetworkStream stream;

        public string ErrorString;
        public int ErrorCode;

        /// <summary>
        /// Базовый конструктор
        /// </summary>
        public ModbusTCP() { }

        /// <summary>
        /// Конструктор с указанием адреса и порта
        /// </summary>
        /// <param name="remoteIPaddress">Адрес</param>
        /// <param name="remote_port">Порт</param>
        public ModbusTCP(string remoteIPaddress, int remote_port)
        {
            RemoteIpAddress = remoteIPaddress;
            RemotePort = remote_port;
        }

        /// <summary>
        /// Установка адреса и порта
        /// </summary>
        /// <param name="remoteIPaddress">Адрес</param>
        /// <param name="remote_port">Порт</param>
        public void SetRemouteAddress(string remoteIPaddress, int remote_port)
        {
            RemoteIpAddress = remoteIPaddress;
            RemotePort = remote_port;
        }

        /// <summary>
        /// Установление задержки
        /// </summary> 
        public void SetReceiveTimeout(int timeout)
        {
            if (tcp_client != null && tcp_client.Client != null)
            {
                tcp_client.Client.ReceiveTimeout = timeout;
                tcp_client.Client.SendTimeout = 5000;
            }
        }

        /// <summary>
        /// Открытие порта
        /// </summary>
        /// <returns>0 если открытие порта прошло успешно, -1 если возникли ошибки</returns>
        public int Open()
        {
            if (tcp_client != null && tcp_client.Client != null)
            {
                tcp_client.Close();
            }
            try
            {
                tcp_client = new TcpClient();
                Console.WriteLine("TCP-клиент - создан, IP-адрес = {0}/{1}", RemoteIpAddress, RemotePort);
            }
            catch (Exception e)
            {
                ErrorString = String.Format("TCP-клиент - ошибка создания, IP-адрес = {0}/{1}: {2}", RemoteIpAddress, RemotePort, e.Message);
                ErrorCode = (int)DevErrors.TCP_CREATE_ERROR;
                //LogMessage.Write(ErrorString);
                return -1;
            }
            try
            {
                remoteIPAddress = IPAddress.Parse(RemoteIpAddress);
            }
            catch (Exception e)
            {
                ErrorString = String.Format("нелегальная строка IP-адреса - {0}: {1}", RemoteIpAddress, e.Message);
                ErrorCode = (int)DevErrors.IP_FORMAT_ERROR;
                //LogMessage.Write(ErrorString);
                return -1;
            }

            // подключение к TCP-серверу
            try
            {
                tcp_client.Connect(remoteIPAddress, RemotePort);
                stream = tcp_client.GetStream();
            }
            catch (Exception e)
            {
                ErrorString = "TCP-клиент - ошибка подключения: " + e.Message;
                ErrorCode = (int)DevErrors.TCP_SERVER_CONNECT_ERROR;
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Закрытие порта
        /// </summary>
        public void Close()
        {
            if (tcp_client != null && tcp_client.Client != null)
            {
                tcp_client.Close();
                tcp_client = null;
            }
        }

        /// <summary>
        /// Закрытие сокета
        /// </summary>
        void CloseSocket()
        {
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
                stream = null;
            }
            if (tcp_client != null && tcp_client.Client != null)
            {
                tcp_client.Client.Close();
                tcp_client.Client = null;
                tcp_client.Close();
                tcp_client = null;
            }
        }

        /// <summary>
        /// Очситка приемного буфера
        /// </summary>
        public void DiscardInBuffer()
        {
            byte[] buf = new byte[1];
            if (stream != null)
                while (stream.DataAvailable)
                    stream.Read(buf, 0, buf.Length); // сброс приемного буфера
        }

        /// <summary>
        /// Метод получения данных
        /// </summary>
        /// <param name="buf">Буфер для приема данных</param>
        /// <param name="offset">Смещение записи в массиве</param>
        /// <param name="read_size">Нужное число регистров</param>
        /// <returns>Возвращает полученное число регистров</returns>
        public int Receive(ref byte[] buf, int offset, int read_size)
        {
            int rsize = 0;
            try
            {
                while (rsize < read_size)
                {
                    rsize += stream.Read(buf, rsize + offset, read_size - rsize);
                };
                return rsize;
            }
            catch (IOException e)
            {   // таймаут
                ErrorString = "устройство не отвечает";
                ErrorCode = (int)DevErrors.TCP_RECEIVE_ERROR;
                throw new TimeoutException(ErrorString);
            }
            catch (Exception e)
            {   // таймаут
                ErrorString = "ошибка приема: " + e.Message;
                ErrorCode = (int)DevErrors.TCP_RECEIVE_ERROR;
                CloseSocket();
                throw new TimeoutException(ErrorString);
            }
        }

        /// <summary>
        /// Отправка данных
        /// </summary>
        /// <param name="buf">Данные для отправки</param>
        /// <param name="size">Размер данных из массива для отправки</param>
        /// <returns>0 если отправка прошла успешно, -1 если возникли ошибки</returns>
        public int Send(byte[] buf)
        {
            try
            {
                tcp_client.Client.Send(buf, buf.Length, SocketFlags.None);
            }
            catch (IOException e)
            {   // таймаут передачи
                ErrorString = String.Format("ошибка передачи, IP-адрес = {0}, удаленный порт = {1}: {2}", RemoteIpAddress, RemotePort, e.Message);
                return -1;
            }
            catch (Exception e)
            {
                ErrorString = "подключение к серверу закрыто: " + e.Message;
                ErrorCode = (int)DevErrors.TCP_CONNECTION_CLOSED;
                //LogMessage.Write(ErrorString);
                CloseSocket();
                return -1;
            }
            return 0;
        }


    }
}

using System.Net;
using System.Net.Sockets;

namespace MdbusNServerMaster.Classes
{
    public class ModbusUDP
    {
        string RemoteIpAddress;     // адрес для передачи запроса
        int RemotePort;             // порт для передачи запроса 
        public UdpClient udp_client;
        IPEndPoint endPoint;

        public string ErrorString;
        public int ErrorCode;

        /// <summary>
        /// Базовый конструктор
        /// </summary>
        public ModbusUDP() { }

        /// <summary>
        /// Конструктор с адресом
        /// </summary>
        public ModbusUDP(string remoteIPaddress, ushort remote_port, ushort local_port)
        {
            RemoteIpAddress = remoteIPaddress;
            RemotePort = remote_port;
        }

        /// <summary>
        /// Установление адреса
        /// </summary>
        public void SetRemouteAddress(string remoteIPaddress, ushort remote_port)
        {
            RemoteIpAddress = remoteIPaddress;
            RemotePort = remote_port;
        }

        /// <summary>
        /// Установление задержки
        /// </summary>
        public void SetReceiveTimeout(int timeout)
        {
            if (udp_client != null && udp_client.Client != null)
                udp_client.Client.ReceiveTimeout = timeout;
        }

        /// <summary>
        /// Открытие клиента
        /// </summary>
        public int Open()
        {
            try
            {
                if (udp_client != null)
                {
                    udp_client.Close();
                    udp_client = null;
                }
                endPoint = new IPEndPoint(IPAddress.Parse(RemoteIpAddress), RemotePort);
                udp_client = new UdpClient();
                udp_client.Connect(endPoint);
            }
            catch (Exception e)
            {
                ErrorString = String.Format("Ошибка при открытии клиента: {0}", e.Message);
                ErrorCode = (int)DevErrors.UDP_CREATE_ERROR;
                return -1;
            }

            return 0;
        }


        /// <summary>
        /// Закрытие клиента
        /// </summary>
        public void Close()
        {
            if (udp_client != null && udp_client.Client != null)
            {
                udp_client.Close();
                udp_client = null;
            }
        }

        /// <summary>
        /// Чтение байтов
        /// </summary>
        /// <param name="buf">Буфер для сохранения прочитанных знаечний</param>
        /// <param name="offset">Сдвиг записи значений в буфере</param>
        /// <param name="read_size">Колличсетво байтов для чтения</param>
        /// <returns>Вохвращает прочитанное колличсество байтов</returns>
        public int ReadByte(ref byte[] buf, int offset, int read_size)
        {
            int rsize = 0;           // Общее количество прочитанных байтов
            int rcount = 0;          // Количество оставшихся для чтения байтов в текущем буфере
            byte[] rbuf = null;      // Буфер для принятых данных
            int rindex = 0;

            // Проверка на null для buf
            if (buf == null)
            {
                throw new ArgumentNullException("buf", "Буфер не может быть null.");
            }

            try
            {
                while (rsize < read_size)
                {
                    if (rcount == 0)  // Если в текущем буфере нет данных для чтения
                    {
                        rbuf = udp_client.Receive(ref endPoint); // Получение нового буфера данных
                        if (rbuf == null || rbuf.Length == 0)
                        {
                            // В случае пустого буфера пропускаем итерацию
                            continue;
                        }
                        rcount = rbuf.Length;  // Обновление количества байтов в буфере
                        rindex = 0;            // Сброс индекса для чтения из нового буфера
                    }
                    buf[rsize + offset] = rbuf[rindex]; // Запись байта из буфера rbuf в целевой буфер buf
                    rsize++; rindex++; rcount--;       // Увеличение счетчика прочитанных байтов и смещения в буфере
                }
                return rsize; // Возвращаем общее количество прочитанных байтов
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut)
                {
                    ErrorString = "устройство не отвечает";
                    ErrorCode = (int)DevErrors.UDP_RECEIVE_ERROR;
                    throw new TimeoutException(ErrorString);
                }
                else
                {
                    ErrorString = String.Format("ошибка приема {0}/{1}: {2}", RemoteIpAddress, RemotePort, e.Message);
                    ErrorCode = (int)DevErrors.UDP_RECEIVE_ERROR;
                    throw new Exception(ErrorString);
                }
            }
            catch (Exception e)
            {
                ErrorString = "ошибка приема: " + e.Message;
                ErrorCode = (int)DevErrors.UDP_RECEIVE_ERROR;
                throw new Exception(ErrorString);
            }
        }

        /// <summary>
        /// Отправка пакета по UDP
        /// </summary>
        /// <param name="buf">Пакет для отправки</param>
        /// <returns>0 если отправка прошла успешно, -1 если возникли ошибки</returns>
        public int Send(byte[] buf)
        {
            try
            {
                udp_client.Send(buf, buf.Length);
                return 0;
            }
            catch (SocketException ex)
            {
                ErrorString = String.Format("Ошибка передачи: {0}", ex.Message);
                ErrorCode = (int)DevErrors.UDP_SEND_ERROR;
                // Здесь можно добавить логирование
                return -1;
            }
        }

        /// <summary>
        /// Сброс приемного буфера UDP-соединения
        /// </summary>
        public void DiscardInBuffer()
        {
            try
            {
                // Проверяем, есть ли данные в приемном буфере
                while (udp_client.Available > 0)
                {
                    // Читаем данные из приемного буфера без обработки
                    byte[] discardedData = udp_client.Receive(ref endPoint);
                }
            }
            catch (Exception e)
            {
                // Обработка исключения, если необходимо
            }
        }
    }
}

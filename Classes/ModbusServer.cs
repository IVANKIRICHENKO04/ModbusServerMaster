using System.IO.Ports;

namespace MdbusNServerMaster.Classes
{
    public class ModbusServer
    {
        public LineConfig lineConfig;                                   // Экземпляр класса конфигурации линии из XML
        public List<SerialDevice> devList = new List<SerialDevice>();   // Список устройств на линии
        string PortName;
        int Baudrate;
        public Thread ServerThread;                                     // Поток работы севрера
        public ModbusRTU modbus;                      // Экземпляр класса для подключения к портам
        public List<string> Cmd = new List<string>();                   // Список команд для выполнения

        public int LineError = 0;                                       // Ошибка связи на линии, если 0, то ошибки нет
        public DateTime LastLineErrorTime;                              // время фиксации неисправности линии, мс
        public TimeSpan LineErrTimeout = new TimeSpan(0, 0, 0, 10, 0);  // таймаут для переоткрытия COM-порта
        public DateTime TimeNoConnect;                                  // время начала отсутствия связи со всеми устройствами | полного отказа линии
        public TimeSpan EthAdapterResetTimeout = new TimeSpan(0, 1, 0); // время задержки для переподключение адаптера eth

        /// <summary>
        /// Конструктор, получающий линию в параметрах
        /// </summary>
        public ModbusServer(LineConfig lineConfig)
        {
            modbus = new ModbusRTU();
            this.lineConfig = lineConfig;
            modbus.TransportMode = this.lineConfig.transportMode;
            foreach (var dev in lineConfig.Devices)
            {
                var bufdev = new SerialDevice(dev, this);
                devList.Add(bufdev);
            }
        }

        /// <summary>
        /// Метод для запуска работы сервера в отдельном потоке
        /// </summary>
        public void Run()
        {
            if (TransportOpen())
            {
                ServerThread = new Thread(() => { StartingSurvey(); });
                ServerThread.Start();
            }
            
        }

        public void StartingSurvey()
        {
            bool flag = true;
            while (flag)
            {
                if (Cmd.Count == 0)
                {
                    ReadDevices();
                }
                else
                {
                    string command = Cmd[0];
                    Cmd.RemoveAt(0);
                    flag = Communication(command);
                }
            }
        }

        /// <summary>
        /// Метод отработки команд, полученных извне
        /// </summary>
        /// <param name="command">Команда на выполнение</param>
        /// <returns>Возвращает флаг, можно ли продолжать цикл опроса или нет</returns>
        bool Communication(string command)
        {
            return true;
        }

        /// <summary>
        /// Метод опроса устойств на линии
        /// </summary>
        void ReadDevices()
        {
            if (LineErrorCheck() != false)
            {
                foreach (SerialDevice dev in devList)
                {
                    if (dev.NoConnectFlag == true)
                    {
                        // Проверка необходимости новой попытки опроса неотвечающего устройства
                        if (DateTime.Now.Subtract(dev.LastRequestTime) < lineConfig.TimeoutErrSec)
                        {
                            continue;
                        }
                    }
                    try
                    {
                        dev.ReadRegisters2(); // Чтение регистров устройства
                    }
                    catch (Exception e)
                    {
                        // Обработка исключения при чтении данных с устройства
                        Console.WriteLine($"** Исключение при чтении данных с устройства - {dev.deviceConfig.Name}, адрес={dev.deviceConfig.AddressOnLine}: " + e.Message + "\nStackTrace:\n" + e.StackTrace);
                        dev.Error(); // Установка состояния ошибки устройства
                    }
                }
            }
        }

        /// <summary>
        /// Метод проверяет текущее состояние линии, и разрешает либо запрещает опрос устройств
        /// </summary>
        bool LineErrorCheck()
        {
            if (LineError != 0) // Действия, если в момент запуска функции линия в ошибке
            {
                if (DateTime.Now.Subtract(LastLineErrorTime) >= LineErrTimeout) // Проверка времени с момента последней ошибки
                {
                    // Попытка восстановления связи с линией
                    if (TransportOpen() == true)
                    {
                        // Разрешить опрос всех устройств
                        foreach (SerialDevice sd in devList)
                            sd.NoConnectFlag = false;
                        return true; // Восстановление связи прошло успешно
                    }
                    else
                    {
                        // Попытка восстановления связи с линией через метод EthLineReset
                        bool restored = EthLineReset();
                        if (restored == true)
                        {
                            // Разрешить опрос всех устройств
                            foreach (SerialDevice sd in devList)
                                sd.NoConnectFlag = false;
                        }
                        else
                        {
                            LineError = 1;
                        }
                        return restored; // Возвращаем результат восстановления связи
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Метод проверки открытие портов
        /// </summary>
        public bool TransportOpen()
        {
            if (lineConfig.transportMode == TransportMode.RTU)
            {
                if (modbus.SerialPortOpen((byte)lineConfig.COMport, lineConfig.Baudrate) == true)
                {
                    Console.Write($"Modbus-сервер -- {lineConfig.COMport} COM порт открыт");
                    LineError = 0;
                }
                else
                {
                    LineError = modbus.ErrorCode;
                    Console.Write($"** Modbus-сервер: {lineConfig.transportMode} -- " + modbus.ErrorString);
                    LastLineErrorTime = DateTime.Now;
                    return false;
                }
            }
            else
            if (lineConfig.transportMode == TransportMode.UDP)
            {
                if (modbus.UdpPortOpen(lineConfig.IPAddress, lineConfig.IPport, lineConfig.IPport, modbus.GetPacketInterval(lineConfig.Baudrate)) == true)
                {
                    Console.Write($"Modbus-сервер -- {lineConfig.transportMode} порт открыт");
                    LineError = 0;
                }
                else
                {
                    LineError = modbus.udp_client.ErrorCode;
                    Console.Write($"** Modbus-сервер: {lineConfig.transportMode} -- " + modbus.udp_client.ErrorString);
                    LastLineErrorTime = DateTime.Now;
                    return false;
                }
            }
            else
            if (lineConfig.transportMode == TransportMode.TCP)
            {
                if (modbus.TcpPortOpen(lineConfig.IPAddress, lineConfig.IPport, modbus.GetPacketInterval(lineConfig.Baudrate)) == true)
                {
                    Console.WriteLine($"Modbus-сервер -- {lineConfig.transportMode}, TCP-клиент - соединение установлено");
                    LineError = 0;
                }
                else
                {
                    LineError = modbus.tcp_client.ErrorCode;
                    Console.Write($"** Modbus-сервер: {lineConfig.transportMode} -- " + modbus.tcp_client.ErrorString);
                    LastLineErrorTime = DateTime.Now;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Метод проверяет сколько устройств не отвечает и может перезапустить eth адаптер
        /// </summary>
        bool EthLineReset()
        {
            int no_connect = 0; // Переменная для подсчета неотвечающих устройств на линии
            for (int i = 0; i < devList.Count; i++)
            {
                if (devList[i].NoConnectFlag == true)
                    no_connect++;
            }
            if (devList.Count == no_connect)
            {   // Все устройства на линии не отвечают !
                if (TimeNoConnect == DateTime.MinValue)
                    TimeNoConnect = DateTime.Now;
            }
            else
            {
                // Сброс времени отсутствия связи и сброс ошибки адаптера
                TimeNoConnect = DateTime.MinValue;
                LineError = 0;
            }
            //if (TimeNoConnect != DateTime.MinValue && DateTime.Now.Subtract(TimeNoConnect) >= EthAdapterResetTimeout)
            //{
            //    // Попытка сброса адаптера при длительном отсутствии связи
            //    if (eth_adapter != null)
            //    {
            //        LineError = 1;
            //        // Если удалось восстановить связь, вернуть true
            //        if (TransportOpen())
            //            return true;
            //    }
            //}
            // Если не удалось восстановить связь, вернуть false
            return false;
        }
    }
}

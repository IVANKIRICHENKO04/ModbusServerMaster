using Microsoft.Win32;

namespace MdbusNServerMaster.Classes
{
    public  class SerialDevice
    {

        //========================================================= Переменные ======================================================================

        public DeviceConfig deviceConfig;       // Экземпляр класса конфигурации устройства
        List<RegistersBlock> registersConfig;   // Список блоков регистров для опроса
        public bool NoConnectFlag;              // признак отсутствия связи true если устройство не отвечает
        public DateTime LastRequestTime;        // время последнего успешного опроса
        public ModbusServer server;                // линия связи 
        public int LastError;                   // последняя ошибка
        public int TotalErrCnt;                 // непрерывный счетчик ошибок связи
        public int ErrCnt;                      // счетчик ошибок связи до отключения устройства
        public bool NewData;                    // true - выполнено чтение данных с устройства 
        public DateTime ReadTime;               // время чтения данных с устройства 
        public int RequestResult;               // результат выполнения запроса
        public ushort[] Registers = new ushort[100];              // блок регистров, прочитанных из устройства 

        //========================================================= Конструкторы ====================================================================

        /// <summary>
        /// Базовый конструктор
        /// </summary>
        public SerialDevice() { }

        /// <summary>
        /// Конструктор конфигурирующий устройство
        /// </summary>
        public SerialDevice(DeviceConfig config, ModbusServer server)
        {
            deviceConfig = config;
            registersConfig = new List<RegistersBlock>();
            foreach (var block in deviceConfig.redistersReads)
            {
                var bl = new RegistersBlock(block);
                bl.time_ms = DateTime.UtcNow;
                registersConfig.Add(bl);
            }
            this.server = server;
        }

        //========================================================= Методы ==========================================================================

        /// <summary>
        /// Чтение регистров устройства.
        /// </summary>
        /// <param name="err">Ссылка на переменную для сохранения кода ошибки (если есть).</param>
        /// <returns>Код запроса: 0, если был запрос; -1, если запрос не был выполнен.</returns>
        //int Device_ReadRegisters(ref int err)
        //{
        //    int request = -1;    // Переменная для хранения кода запроса, изначально установлена в -1 (запрос не был выполнен)
        //    bool result;         // Переменная для хранения результата выполнения операции чтения регистров

        //    // Устанавливаем таймаут для Modbus-сервера
        //    //server.modbus.SetTimeout(deviceConfig.TimeoutMS);

        //    // Проходим по всем блокам регистров для чтения
        //    for (int i = 0; i < registersConfig.Count; i++)
        //    {
        //        RegistersBlock rb = registersConfig[i]; // Получаем текущий блок регистров

        //        // Проверяем, прошло ли достаточное время с момента последнего опроса этого блока регистров
        //        double time_ms1 = new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds;
        //        if ((time_ms1 - rb.time_ms) < rb.Period)
        //            continue; // Если время не вышло, переходим к следующему блоку регистров

        //        // Чтение регистров из устройства
        //        result = server.modbus.ReadHoldingRegistersEx((byte)deviceConfig.AddressOnLine, rb.Start, (ushort)rb.Count, ref Registers, rb.Start);

        //        // Проверка результата операции чтения
        //        if (result == false)
        //            err = server.modbus.ErrorCode; // Если чтение не удалось, сохраняем код ошибки
        //        else
        //            err = 0; // Если операция чтения прошла успешно, код ошибки устанавливаем в 0

        //        request = 0; // Устанавливаем код запроса в 0, так как был выполнен запрос
        //        LastRequestTime = DateTime.Now; // Запоминаем время последнего запроса
        //        rb.time_ms = new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds; // Обновляем время последнего опроса блока регистров

        //        // Если произошла ошибка при чтении, прерываем дальнейшее чтение и возвращаем код запроса
        //        if (err != 0)
        //            return request;
        //    }

        //    if (request == 0)
        //    {   // Если был выполнен запрос
        //        ReadTime = DateTime.Now; // Запоминаем время чтения
        //        NewData = true; // Устанавливаем флаг новых данных
        //    }

        //    return request; // Возвращаем код запроса
        //}

        /// <summary>
        /// Метод для чтения всех регистров устройства
        /// </summary>
        /// <returns></returns>
        public int ReadRegisters()
        {
            Console.WriteLine("Reading Registers Starting...");
            int err = 0;

            // Выполняем чтение регистров
            //RequestResult = Device_ReadRegisters(ref err);


            // Если не было выполнено опроса, возвращаем 0
            if (RequestResult < 0)
            {
                Console.WriteLine("ошибка: " + err);
                Console.WriteLine("Registers Number: " + Registers.Length);
                return 0;
            }

            if (err == 0)
            {
                Console.Write("Registers: ");
                foreach (var reg in Registers)
                {
                    Console.Write(reg.ToString()+" ");
                }
                Console.WriteLine();
            }
            //// Если ошибок нет
            //if (err == 0)
            //{
            //    //server.LineError = 0; // Сбрасываем ошибку на линии

            //    // Проверяем идентификатор устройства, если он был прочитан
            //    if (RegAddress == DevType.DevIDAddress && ReadDevIDFlag == true)
            //    {
            //        TempDeviceID = Registers[DevType.DevIDAddress]; // Читаем идентификатор устройства

            //        // Проверяем соответствие идентификатора устройства с конфигурацией
            //        if (DevType.DevID == TempDeviceID)
            //        {
            //            ReadDeviceID = TempDeviceID;
            //            NoConnectFlag = false; // Сбрасываем флаг отсутствия связи
            //            server.CurrentDevice = null; // Сбрасываем текущее устройство
            //        }
            //        else
            //        {
            //            // Если идентификатор устройства не соответствует конфигурации, возвращаем ошибку
            //            RequestResult = (int)DevErrors.DEVICE_CONFIG_TYPE_ERR;
            //            LogMessage.Write($"** Modbus-сервер -- {server.cline.TransportString}, адрес={DeviceAddress}, " + DevType.Type + $": ID устройства={TempDeviceID} не соответствует конфигурации (DevID={DevType.DevID})");
            //            Error(); // Устанавливаем состояние ошибки устройства
            //            return RequestResult;
            //        }
            //    }
            //    else
            //        NoConnectFlag = false; // Сбрасываем флаг отсутствия связи

            //    // Если связь с устройством восстановлена, записываем соответствующее сообщение в лог
            //    if (Connect == false)
            //    {
            //        Connect = true;
            //        LogMessage.Write($"Modbus-сервер: {server.cline.TransportString}, адрес={DeviceAddress}, " + DevType.Type + ": Связь восстановлена");
            //        LogMessage.Writejournal("Modbus-сервер " + DevType.Name + ": адрес " + DeviceAddress + ", " + server.cline.TransportString + " -- связь восстановлена");
            //    }
            //    ErrCnt = 0; // Сбрасываем счетчик ошибок
            //}
            //else
            //    Error(); // Устанавливаем состояние ошибки устройства

            //return err; // Возвращаем код ошибки
            return 0;
        }

        public void ReadRegisters2()
        {
            foreach (var reg in registersConfig)
            {
                TimeSpan difference = DateTime.UtcNow - reg.time_ms; // Рассчитываем разницу с использованием UTC времени
                int delayMilliseconds = (int)difference.TotalMilliseconds;

                if (delayMilliseconds > reg.Period)
                {
                    reg.time_ms = DateTime.UtcNow; // Обновляем время использованием UTC времени

                    int[] Answ = server.modbus.ReadHoldingRegistersEx((byte)deviceConfig.AddressOnLine, reg.Start, reg.Count);
                    Console.WriteLine($"Device: {deviceConfig.Name}, Registers {reg.Start}-{reg.Start + reg.Count}: ");
                    foreach (int i in Answ)
                    {
                        Console.Write(i + " ");
                    }
                    Console.WriteLine();
                }
            }
        }


        /// <summary>
        /// Обработчик ошибки
        /// </summary>
        public void Error()
        {
            int err;
            if ((err = server.modbus.ErrorCode) != 0)
            {
                if (NoConnectFlag == false || err != LastError)
                {

                    Console.Write($"** Modbus-сервер  " + $": адрес={deviceConfig.AddressOnLine:D} -- " + server.modbus.ErrorString.ErrorString);
                    TotalErrCnt++;
                }
                if (err == (int)DevErrors.SERIAL_PORT_CLOSED || err == (int)DevErrors.SERIAL_PORT_NOT_OPEN ||
                    err == (int)DevErrors.UDP_CLIENT_CLOSED || err == (int)DevErrors.UDP_CLIENT_NOT_OPEN ||
                    err == (int)DevErrors.TCP_CONNECTION_CLOSED || err == (int)DevErrors.TCP_CLIENT_NOT_OPEN)
                {
                    server.LineError = server.modbus.ErrorCode;
                    server.LastLineErrorTime = DateTime.Now;
                    return;
                }
            }
            ErrCnt++;
            if (ErrCnt >= server.lineConfig.IOMaxErr)
            {
                if (NoConnectFlag == false)
                {
                    Console.Write("Modbus-сервер: адрес " + deviceConfig.AddressOnLine + ", " + server.lineConfig.transportMode + " -- нет связи");
                }
                ErrCnt = 0;
                NoConnectFlag = true;
                LastRequestTime = DateTime.Now;
            }
            LastError = server.modbus.ErrorCode;
        }
    }
}

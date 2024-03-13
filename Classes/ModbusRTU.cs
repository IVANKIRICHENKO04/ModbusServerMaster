using System.ComponentModel;
using System.IO.Ports;

namespace MdbusNServerMaster.Classes
{
    /// <summary>
    /// тест
    /// </summary>
    public class errrorString : INotifyPropertyChanged
    {
        private string errorString;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ErrorString
        {
            get { return errorString; }
            set
            {
                if (errorString != value)
                {
                    errorString = value;
                    OnPropertyChanged(nameof(ErrorString));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class ModbusRTU
    {

        //========================================================= Константы =======================================================================

        private const int DEFAULT_TIMEOUT = 1000;   // Мс
        private const int MIN_INTERVAL = 1;         // Минимальный интервал отправки пакетов
        private const int BUFFER_SIZE = 270;        // Длина буфера
        private enum Limits
        {
            MAX_READ_REGS = 125,
            MAX_WRITE_REGS = 60,
        }

        //========================================================= Переменные ======================================================================

        string COMport;                     // Com Port для чтения 
        int COMspeed;                       // Скорость опроса
        SerialPort Port;                    // COM-порт для обмена данными
        //int TimeOut;                        // Стандартный таймаут между опросами
        public TransportMode TransportMode;        // Режим протокола пережачи данных


        public byte[] Buffer;               // Буфер для передачи сообщений
        public int MessageLength;           // Длина отправляемого сообщения
        public int RepeatNumber = 1;        // Число повторений запроса при ошибках 
        public int WaitDataBytes = 0;       // Ожидаемое число байт ответа
        //public string ErrorString;          // Строка описания ошибки
        public int ErrorCode;               // Код ошибки
        public int PacketInterval;          // Рассчитанный интервал между пакетами
        //public short TransitAddr = -1;      // Адрес транзитного устройства, если оно транзитное
        public errrorString ErrorString = new errrorString();


        public ushort[] Values;             // Массив плученных значений

        //========================================================= Конструкторы ====================================================================

        /// <summary>
        /// Конструктор по умолчанию 
        /// </summary>
        public ModbusRTU()
        {
            TransportMode = TransportMode.COM_PORT;
            Buffer = new byte[BUFFER_SIZE];
            Values = new ushort[BUFFER_SIZE / 2];
            ErrorString.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "ErrorString")
                {
                    Console.WriteLine($"Значение ErrorString изменилось: {ErrorString.ErrorString}");
                }
            };
        }

        /// <summary>
        /// Конструктор для инициализации объекта ModbusRTU с указанием номера COM-порта и скорости передачи.
        /// </summary>
        /// <param name="ComNumber">Номер COM-порта.</param>
        /// <param name="ComSpeed">Скорость передачи данных.</param>
        public ModbusRTU(string ComNumber, int ComSpeed) : base()
        {
            COMport = ComNumber; // Устанавливаем номер COM-порта
            COMspeed = ComSpeed; // Устанавливаем скорость передачи данных

            try
            {
                Port = new SerialPort(ComNumber, ComSpeed, Parity.None, 8, StopBits.One); // Создаем новый объект SerialPort
                Port.Open(); // Открываем порт
            }
            catch (Exception e)
            {
                Port = null; // Если возникла ошибка, устанавливаем порт в null
                ErrorString.ErrorString = e.Message + "\nОшибка открытия COM-порта " + ComNumber; // Устанавливаем строку ошибки
                ErrorCode = (int)DevErrors.SERIAL_PORT_OPEN_ERROR; // Устанавливаем код ошибки
            }
            Port.ReadTimeout = DEFAULT_TIMEOUT; // Устанавливаем таймаут для чтения данных из порта
            PacketInterval = GetPacketInterval(ComSpeed); // Получаем интервал между пакетами
        }

        //========================================================= Методы ==========================================================================

        /// <summary>
        /// Определяет интервал между запросами пакетов на основе заданной скорости передачи данных.
        /// </summary>
        /// <param name="baudrate">Скорость передачи данных (бод).</param>
        /// <returns>Интервал между запросами пакетов в миллисекундах.</returns>
        public int GetPacketInterval(int baudrate)
        {
            int Interval = (4 * 1000 * 10 / baudrate); // Переменная для хранения интервала между запросами
            if (Interval < MIN_INTERVAL)
                Interval = MIN_INTERVAL; // Проверяем минимальное значение интервала
            return Interval; // Возвращаем вычисленный интервал
        }

        /// <summary>
        /// Открывает порт по его номеру.
        /// </summary>
        /// <param name="com_port_number">Номер COM-порта.</param>
        /// <param name="ComSpeed">Скорость передачи данных.</param>
        /// <returns>True, если порт успешно открыт, иначе False.</returns>
        public bool PortOpen(byte com_port_number, int ComSpeed)
        {
            string comport = "COM" + Convert.ToString(com_port_number); // Формируем имя COM-порта
            return NewPortOpen(comport, ComSpeed); // Вызываем метод открытия порта по имени
        }

        /// <summary>
        /// Открывает порт с указанным портом и скоростью передачи данных.
        /// </summary>
        /// <param name="ComPort">Имя COM-порта.</param>
        /// <param name="ComSpeed">Скорость передачи данных.</param>
        /// <returns>True, если порт успешно открыт, иначе False.</returns>
        public bool NewPortOpen(string ComPort, int ComSpeed)
        {
            COMspeed = ComSpeed; // Устанавливаем скорость передачи данных
            COMport = ComPort; // Устанавливаем имя COM-порта
            return CreaetNewPort(); // Вызываем метод открытия порта
        }

        /// <summary>
        /// Создает новый порт
        /// </summary>
        /// <returns>True, если порт успешно открыт, иначе False.</returns>
        public bool CreaetNewPort()
        {
            bool result = true; // Переменная для хранения результата открытия порта

            // Если порт уже открыт, закрываем его
            if (Port != null && Port.IsOpen == true)
            {
                Port.Close(); // Закрываем порт
                Port.Dispose(); // Освобождаем ресурсы порта
            }
            try
            {
                // Создаем новый объект SerialPort с указанными параметрами
                Port = new SerialPort(COMport, COMspeed, Parity.None, 8, StopBits.One);
                Port.Open(); // Открываем порт
            }
            catch (Exception e)
            {
                Port = null; // Если возникла ошибка, устанавливаем порт в null
                ErrorString.ErrorString = e.Message + " \nОшибка открытия порта " + COMport; // Устанавливаем строку ошибки
                ErrorCode = (int)DevErrors.SERIAL_PORT_OPEN_ERROR; // Устанавливаем код ошибки
                result = false; // Устанавливаем результат открытия порта в False
            }
            if (Port != null)
                Port.ReadTimeout = DEFAULT_TIMEOUT; // Устанавливаем таймаут для чтения данных из порта, если порт не равен null
            PacketInterval = GetPacketInterval(COMspeed); // Получаем интервал между пакетами
            return result; // Возвращаем результат открытия порта
        }

        /// <summary>
        /// Устанавливает таймаут для приема данных.
        /// </summary>
        /// <param name="timeout">Таймаут в миллисекундах.</param>
        //public void SetTimeout(int timeout)
        //{
        //    if (TransportMode == (ushort)TransportMode.COM_PORT)
        //    {
        //        if (Port != null)
        //            Port.ReadTimeout = timeout; // Устанавливаем таймаут для COM-порта
        //    }
        //}

        /// <summary>
        /// Закрывает соединение линии.
        /// </summary>
        public void TransportClose()
        {
            ErrorCode = 0; // Сбрасывает код ошибки

            // В зависимости от режима транспорта выполняется закрытие соединения
            if (TransportMode == (ushort)TransportMode.COM_PORT)
            {
                if (Port != null)
                {
                    Port.Close(); // Закрывает COM-порт
                    Port.Dispose(); // Освобождает ресурсы
                    Port = null; // Обнуляет ссылку на COM-порт
                }
            }
        }

        /// <summary>
        /// Проверяет наличие доступных данных на линии.
        /// </summary>
        /// <returns>True, если доступны данные, иначе False.</returns>
        //public bool TransportDataAvailable()
        //{
        //    bool result = false;

        //    try
        //    {
        //        if (TransportMode == (ushort)TransportMode.COM_PORT)
        //        {
        //            if (Port != null && Port.IsOpen)
        //            {
        //                result = Port.BytesToRead != 0; // Проверяем наличие байтов для чтения в COM-порте
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //    return result;
        //}

        /// <summary>
        /// Проверяет состояние линии связи.
        /// </summary>
        /// <returns>Возвращает true, если линия связи доступна, в противном случае - false.</returns>
        bool TransportCheck()
        {
            // Проверка для COM-порта
            if (TransportMode == (ushort)TransportMode.COM_PORT)
            {
                // Проверяем, что порт был создан
                if (Port == null)
                {
                    ErrorString.ErrorString = "Порт не открыт";
                    ErrorCode = (int)DevErrors.SERIAL_PORT_NOT_OPEN;
                    return false;
                }

                // Проверяем, что порт открыт
                if (!Port.IsOpen)
                {
                    ErrorString.ErrorString = "Порт неожиданно закрыт";
                    ErrorCode = (int)DevErrors.SERIAL_PORT_CLOSED;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Выполняет операцию записи данных в линию связи.
        /// </summary>
        /// <param name="WriteBuffer">Буфер с данными для записи.</param>
        /// <returns>True, если операция записи выполнена успешно, иначе False.</returns>
        bool TransportWrite(byte[] WriteBuffer)
        {
            try
            {
                // Если режим транспорта - COM порт
                if (TransportMode == (ushort)TransportMode.COM_PORT)
                {
                    Port.DiscardInBuffer(); // Очищаем входной буфер порта
                    Port.Write(WriteBuffer, 0, WriteBuffer.Length); // Записываем данные в порт
                }
            }
            catch (Exception e)
            {
                ErrorCode = (int)DevErrors.SERIAL_WRITE_ERROR; // Устанавливаем код ошибки для записи в последовательный порт
                ErrorString.ErrorString = e.Message; // Устанавливаем строку ошибки
                return false;
            }
            return true; // Возвращаем успешный результат, если операция выполнена без ошибок
        }

        /// <summary>
        /// Выполняет операцию чтения данных из линии связи.
        /// </summary>
        /// <param name="RBuffer">Буфер для чтения данных.</param>
        /// <param name="bytecount">Количество байт для чтения.</param>
        /// <returns>True, если операция чтения выполнена успешно, иначе False.</returns>
        bool TransportRead(ref byte[] RBuffer, int bytecount)
        {
            try
            {
                // Если режим транспорта - COM порт
                if (TransportMode == TransportMode.COM_PORT)
                {
                    // Читаем адрес пока не найдем его
                    for (int n = 0; n < 100; n++)
                    {
                        RBuffer[0] = (byte)Port.ReadByte();
                        if (RBuffer[0] != 0)
                            break;
                    }
                    RBuffer[1] = (byte)Port.ReadByte(); // Читаем функцию
                    int BytesRead = 2; // колличсетво прочитанных байтов

                    // Проверяем наличие ошибки в ответе Modbus-RTU
                    if ((RBuffer[1] & 0x80) == 0x80)
                    {
                        bytecount = 5; // Определяем количество байт для чтения
                    }
                    while (BytesRead < bytecount)
                        BytesRead += Port.Read(RBuffer, BytesRead, bytecount - BytesRead); // Читаем оставшиеся данные
                }
            }
            catch (TimeoutException ex)
            {
                ex.GetType(); // Получаем тип исключения
                ErrorString.ErrorString = "Устройство не отвечает"; // Устанавливаем строку ошибки
                ErrorCode = (int)DevErrors.DEVICE_RESPONSE_TIMEOUT_ERR; // Устанавливаем код ошибки
                return false;
            }
            catch (Exception ex)
            {
                ErrorString.ErrorString = "Сбой устройства: " + ex.Message; // Устанавливаем строку ошибки
                ErrorCode = (int)DevErrors.SERIAL_DEVICE_ERROR; // Устанавливаем код ошибки
                return false;
            }
            return true; // Возвращаем успешный результат, если операция чтения выполнена без ошибок
        }

        /// <summary>
        /// Отправляет пакет устройству
        /// </summary>
        /// <param name="Buffer">Пакет запроса без контрольной суммы</param>
        /// <param name="Answer">Массив байтов ответа</param>
        /// <param name="MessageLength">Длинна отправленного пакета</param>
        /// <param name="AnswerLenght">Длина ожидаемого ответа</param>
        /// <returns></returns>
        public bool ProcessQuery(byte[] Buffer, ref byte[] Answer, int MessageLength, int AnswerLenght)
        {
            bool result = true; // Результат выполнения операции

            // Проверяем подключение линии
            if (TransportCheck() == false)
                return false;

            // Добавляем контрольную сумму к сообщению
            ushort CRC = CalculateCRC(Buffer, MessageLength);
            Buffer[MessageLength++] = (byte)(CRC & 0xFF);
            Buffer[MessageLength++] = (byte)((CRC >> 8) & 0xFF);
            for (int i = 0; i < RepeatNumber; i++)
            {
                // Проверяем подключение линии
                if (TransportCheck() == false)
                    return false;

                // Отправляем сообщение
                if (TransportWrite(Buffer) == false)
                    return false;

                // Читаем ответ от устройства
                result = TransportRead(ref Answer, AnswerLenght);

                if (result == true)
                {
                    // Проверяем контрольную сумму сообщения
                    if (((Answer[AnswerLenght - 1] << 8) | Answer[AnswerLenght - 2]) != CalculateCRC(Answer, AnswerLenght - 2))
                    {
                        ErrorString.ErrorString = "Ошибка контрольной суммы";
                        ErrorCode = (int)DevErrors.DEVICE_CHECKSUMM_ERR;
                        result = false;
                    }

                    // Проверяем наличие ошибки в ответе
                    if (result == true && (Answer[1] & 0x80) == 0x80)
                    {
                        if (Answer[2] == 0xA5)
                        {
                            ErrorString.ErrorString = "ОЖИДАНИЕ ЗАГРУЗКИ ПРОШИВКИ";
                            ErrorCode = (int)DevErrors.DEVICE_IN_LOADER_MODE;
                        }
                        else
                        {
                            ErrorString.ErrorString = "Получен отрицательный ответ. Код: " + Answer[2].ToString();
                            ErrorCode = Answer[2];
                        }
                        result = false;
                    }
                }
                if (result == true)
                    break;
                else
                {
                    // Если операция неудачная, ждем некоторое время перед повторной попыткой
                    Thread.Sleep(PacketInterval);
                }
            }
            return result;
        }

        /// <summary>
        /// Читает версию прошивки устройства.
        /// </summary>
        /// <param name="plc_type">Тип PLC (Programmable Logic Controller - программируемый логический контроллер).</param>
        /// <param name="DeviceAddr">Адрес устройства</param>
        /// <returns>Строка, содержащая версию прошивки.</returns>
        public string ReadProgVersion(byte DeviceAddr)
        {
            string s; // Переменная для хранения версии прошивки

            int AnswerLenght = 2 + 80 + 2; // Ожидаемое количество байт в ответном сообщении
            byte[] bytes = new byte[4];
            bytes[0] = DeviceAddr; // Устанавливаем адрес устройства в буфере
            bytes[1] = (byte)ModbusCode.ReadProgramVersion; // Устанавливаем код Modbus для чтения версии прошивки

            byte[] Answer = new byte[AnswerLenght];
            // Выполняем запрос и обрабатываем его результат
            if (!ProcessQuery(bytes, ref Answer, 2, AnswerLenght)) // Если запрос не удался
                return null; // Возвращаем null

            // Преобразуем полученные байты в строку, используя кодировку 1251 (Windows-1251)
            s = System.Text.Encoding.GetEncoding(1251).GetString(Answer, 2, AnswerLenght - 4);
            return s; // Возвращаем версию прошивки в виде строки
        }

        /// <summary>
        /// Создает контрольную сумму для пакета данных.
        /// </summary>
        /// <param name="Message">Массив байтов, содержащий данные, для которых требуется создать контрольную сумму.</param>
        /// <param name="MessageLength">Длина сообщения.</param>
        /// <returns>Контрольная сумма пакета данных.</returns>
        public ushort CalculateCRC(byte[] data, int length)
        {
            ushort crc = 0xFFFF;

            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }

        /// <summary>
        /// Устанавливает один отдельный  (coil) в устройстве.
        /// </summary>
        /// <param name="DevAddr">Адрес устройства Modbus.</param>
        /// <param name="Address">Адрес  (coil) для установки.</param>
        /// <param name="value">Значение (coil), которое необходимо установить (0 или 1).</param>
        /// <returns>True, если операция установки катушки прошла успешно, иначе False.</returns>
        public bool SetSingleCoil(byte DevAddr, int Address, int value)
        {
            // Ожидаемый размер ответа
            int WaitDataBytes = 8;
            byte[] data = new byte[WaitDataBytes];
            byte[] Answer = new byte[WaitDataBytes];
            // Устанавливаем байты в буфере запроса
            data[0] = DevAddr;
            data[1] = (byte)ModbusCode.WriteSingleCoil;
            data[2] = (byte)(Address >> 8); // Старший байт адреса катушки
            data[3] = (byte)(Address & 0xFF); // Младший байт адреса катушки

            // Устанавливаем значение coil в зависимости от параметра value
            if (value != 0)
                data[4] = 0xFF; // Устанавливаем включенное состояние  (coil)
            else
                data[4] = 0; // Устанавливаем выключенное состояние  (coil)

            data[5] = 0; // Зарезервированный байт

            return ProcessQuery(data, ref Answer, 6, WaitDataBytes); // Возвращаем true, если операция установки катушки выполнена успешно
        }

        /// <summary>
        /// Читаем значения регистров 4x
        /// </summary>
        /// <param name="DevAddr">Адрес устройства</param>
        /// <param name="StartingAddress">Адрес начала чтения регистров</param>
        /// <param name="Count">Колличество регистров</param>
        /// <param name="Answer">Ответ</param>
        /// <returns></returns>
        public bool ReadHoldingRegisters(byte DevAddr, ushort StartingAddress, ushort Count, ref byte[] Answer)
        {
            byte[] Buffer = new byte[8];
            // Проверяем значение Count
            if (Count < 1 || Count > (ushort)Limits.MAX_READ_REGS)
            {
                ErrorString.ErrorString = "Число регистров должно быть в диапазоне от 1 до 125. Указано: " + Count;
                ErrorCode = (int)DevErrors.DEVICE_REGCOUNT_ERR;
                return false;
            }

            Buffer[0] = DevAddr;                                    // Аддрес устройства
            Buffer[1] = (byte)ModbusCode.ReadHoldingRegisters;      // Код функции чтения регистра
            Buffer[2] = (byte)(StartingAddress >> 8);               // Старший байт начального адреса регистра
            Buffer[3] = (byte)(StartingAddress & 0xFF);             // Младший байт начального адреса регистра
            Buffer[4] = (byte)(Count >> 8);                         // Старщий байт колличества регистров
            Buffer[5] = (byte)(Count & 0xFF);                       // Младший байт колличества регистров
            int answerlenght = 5 + Count * 2;

            return ProcessQuery(Buffer, ref Answer, 6, answerlenght);
        }

        /// <summary>
        /// Чтение регистров с расширенной логикой разделения запроса на блоки, если количество запрашиваемых регистров больше максимального значения.
        /// </summary>
        /// <param name="DevAddr">Адрес устройства Modbus.</param>
        /// <param name="StartingAddress">Начальный адрес регистров для чтения.</param>
        /// <param name="Count">Количество регистров для чтения.</param>
        /// <param name="Data">Массив для хранения прочитанных данных.</param>
        /// <param name="start_index">Индекс начала сохранения данных в массиве Data.</param>
        /// <returns>Возвращает полученый массив данных</returns>
        public int[] ReadHoldingRegistersEx(byte DevAddr, ushort StartingAddress, ushort Count)
        {
            ushort cnt = 0;
            ushort Address = StartingAddress;
            int[] FinalAnswer = new int[Count];

            // Проверяем, не превышает ли количество запрашиваемых регистров максимально допустимое значение
            if (Count > (ushort)Limits.MAX_READ_REGS)
            {
                int index = 0;
                // Разделяем запрос на блоки, если количество регистров больше максимального значения
                while (Count > 0)
                {
                    if (Count > (ushort)Limits.MAX_READ_REGS)
                    {
                        cnt = (ushort)Limits.MAX_READ_REGS;
                        Count -= (ushort)Limits.MAX_READ_REGS;
                    }
                    else
                    {
                        cnt = Count;
                        Count = 0;
                    }

                    byte[] Answer = new byte[(cnt * 2)+5]; // Предполагаем, что каждый регистр имеет размер 2 байта
                    if (!ReadHoldingRegisters(DevAddr, Address, cnt, ref Answer)) return new int[0];

                    // Преобразуем байты в int и сохраняем их в массиве FinalAnswer
                    for (int i = 0; i < cnt * 2; i += 2)
                    {
                        int valueIndex = i + 3; // Начинаем считывание значений с 6-го байта массива Answer
                        if (valueIndex + 1 < Answer.Length) // Проверяем, чтобы не выйти за границы массива Answer
                        {
                            int value = (Answer[valueIndex] << 8) | Answer[valueIndex + 1]; // Объединяем старший и младший байты
                            FinalAnswer[index++] = value;
                        }
                    }

                    Address += cnt;
                }
            }
            else
            {
                int index = 0;
                // Читаем регистры и сохраняем полученные данные в массив FinalAnswer
                byte[] Answer = new byte[(Count * 2)+5]; // Предполагаем, что каждый регистр имеет размер 2 байта
                if (!ReadHoldingRegisters(DevAddr, StartingAddress, Count, ref Answer)) return new int[0];
                //for(int i = 0; i < Answer.Length;i++)
                //{
                //    FinalAnswer[i] = Answer[i];
                //}
                //return FinalAnswer;
                // Преобразуем байты в int и сохраняем их в массиве FinalAnswer
                for (int i = 0; i < Count * 2; i += 2)
                {
                    int valueIndex = i + 3; // Начинаем считывание значений с 6-го байта массива Answer
                    if (valueIndex + 1 < Answer.Length) // Проверяем, чтобы не выйти за границы массива Answer
                    {
                        int value = (Answer[valueIndex] << 8) | Answer[valueIndex + 1]; // Объединяем старший и младший байты
                        FinalAnswer[index++] = value;
                    }
                }
            }

            return FinalAnswer;
        }

        /// <summary>
        /// Записываем единичный регистер 4х
        /// </summary>
        /// <param name="StartingAddress">Адрес регистра</param>
        /// <param name="Value">Новое значение регистра</param>
        public bool WriteSingleRegister(byte DevAddr, ushort StartingAddress, ushort Value)
        {
            int WaitDataBytes;
            // Подсчитываем размер ответа
            if (DevAddr == 0)
                WaitDataBytes = 0;
            else
                WaitDataBytes = 8;
            byte[] Buffer = new byte[8];
            byte[] Answer = new byte[8];
            Buffer[0] = DevAddr;
            Buffer[1] = (byte)ModbusCode.WriteSingleRegister;
            Buffer[2] = (byte)(StartingAddress >> 8); // С.Б. Адреса
            Buffer[3] = (byte)(StartingAddress & 0xFF);
            Buffer[4] = (byte)(Value >> 8); // С.Б. Кол-ва регистров
            Buffer[5] = (byte)(Value & 0xFF);

            if (!ProcessQuery(Buffer, ref Answer, 6, 8)) return false;
            if (DevAddr != 0)
            {
                // Проверяем результат
                if (((Buffer[4] << 8) | Buffer[5]) != Value)
                {
                    ErrorString.ErrorString = "Ошибка записи регистра:" + StartingAddress;
                    ErrorCode = (int)DevErrors.DEVICE_WRITE_REG_ERR;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Записывает значения сразу в несколько регистров.
        /// </summary>
        /// <param name="DevAddr">Адрес устройства.</param>
        /// <param name="StartingAddress">Начальный адрес.</param>
        /// <param name="Count">Количество регистров.</param>
        /// <param name="Data">Массив данных для записи.</param>
        /// <param name="data_start">Начальный индекс данных в массиве.</param>
        /// <returns>Возвращает true, если запись прошла успешно, в противном случае - false.</returns>
        public bool WriteMultipleRegisters(byte DevAddr, ushort StartingAddress, ushort Count, ushort[] Data, int data_start)
        {
            ushort register;

            // Проверяем значение Count
            if (Count < 1 || Count > (ushort)Limits.MAX_READ_REGS)
            {
                ErrorString.ErrorString = "Число регистров должно быть в диапазоне от 1 до 125. Указано: " + Count;
                ErrorCode = (int)DevErrors.DEVICE_REGCOUNT_ERR;
                return false;
            }
            int MessageLength = 7 + Count * 2;
            // Подсчитываем размер ответа
            if (DevAddr == 0)
                WaitDataBytes = 0;
            else
                WaitDataBytes = 8;
            byte[] Buffer = new byte[MessageLength + 2];
            byte[] Answer = new byte[8];
            Buffer[0] = DevAddr;
            Buffer[1] = (byte)ModbusCode.WriteMultipleRegisters;
            Buffer[2] = (byte)(StartingAddress >> 8); // Старший байт адреса
            Buffer[3] = (byte)(StartingAddress & 0xFF); // Младший байт адреса
            Buffer[4] = (byte)(Count >> 8); // Старший байт количества регистров
            Buffer[5] = (byte)(Count & 0xFF); // Младший байт количества регистров
            Buffer[6] = (byte)(Count * 2); // Количество байт данных
            int ByteCounter = 7;
            // Копируем данные в пакет
            for (int i = 0; i < Count; i++)
            {
                register = Data[data_start + i];
                Buffer[ByteCounter++] = (byte)(register >> 8); // Старший байт регистра
                Buffer[ByteCounter++] = (byte)register; // Младший байт регистра
            }
            if (!ProcessQuery(Buffer, ref Answer, MessageLength, Answer.Length)) return false;
            if (DevAddr != 0)
            {
                // Проверяем результат
                if (((Answer[4] << 8) | Answer[5]) != Count)
                {
                    ErrorString.ErrorString = "Ошибка записи регистров с регистра " + StartingAddress + "по регистр " + StartingAddress + Count;
                    ErrorCode = (int)DevErrors.DEVICE_WRITE_REG_ERR;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Записывает несколько регистров 4x с произвольным счётчиком.
        /// </summary>
        /// <param name="DevAddr">Адрес устройства</param>
        /// <param name="StartingAddress">Начальный адрес регистра</param>
        /// <param name="Count">Количество регистров</param>
        /// <param name="Data">Массив данных для записи</param>
        /// <param name="start_index">Индекс начала записи в массиве данных</param>
        /// <returns>Возвращает true, если запись прошла успешно, в противном случае - false.</returns>
        public bool WriteMultipleRegistersEx(byte DevAddr, ushort StartingAddress, ushort Count, ushort[] Data, int start_index)
        {
            ushort cnt, index = 0;
            ushort Address = StartingAddress;

            // Если количество регистров больше максимального значения для записи
            if (Count > (ushort)Limits.MAX_WRITE_REGS)
            {
                // Разбиваем запись на несколько пакетов
                while (Count > 0)
                {
                    if (Count > (ushort)Limits.MAX_WRITE_REGS)
                    {
                        cnt = (ushort)Limits.MAX_WRITE_REGS;
                        Count -= (ushort)Limits.MAX_WRITE_REGS;
                    }
                    else
                    {
                        cnt = Count;
                        Count = 0;
                    }
                    // Записываем очередной пакет регистров
                    if (!WriteMultipleRegisters(DevAddr, Address, cnt, Data, start_index + index)) return false;
                    Address += cnt;
                    index += cnt;
                }
            }
            else
            {
                // Записываем все регистры в одном пакете
                if (!WriteMultipleRegisters(DevAddr, StartingAddress, Count, Data, start_index)) return false;
            }
            return true;
        }
    }


    #region Коды ошибок связи
    public enum DevErrors
    {
        // ошибки устройства
        DEVICE_CHECKSUMM_ERR = -101,
        DEVICE_ANSWER_ADDRESS_ERR = -102,
        DEVICE_DATA_LENGTH_ERR = -103,
        DEVICE_TX_TIMEOUT_ERR = -104,
        DEVICE_RESPONSE_TIMEOUT_ERR = -105,
        DEVICE_CONFIG_TYPE_ERR = -106,
        DEVICE_SLAVE_ADDRESS_ERR = -107,
        DEVICE_REGCOUNT_ERR = -108,
        DEVICE_WRITE_REG_ERR = -109,
        // ошибки последовательного порта
        SERIAL_DEVICE_ERROR = -1,
        SERIAL_READ_ERROR = -2,
        SERIAL_WRITE_ERROR = -3,
        SERIAL_PORT_NOT_OPEN = -4,
        SERIAL_PORT_CLOSED = -5,
        SERIAL_PORT_OPEN_ERROR = -6,
        // ошибки конфигурирования
        UNDEFINED_DEVICE_TYPE = -10,
        // устройство в режиме загрузки прошивки
        DEVICE_IN_LOADER_MODE = -11,
        // UDP
        UDP_CLIENT_NOT_OPEN = -20,
        UDP_CLIENT_CLOSED = -21,
        UDP_CREATE_ERROR = -22,
        IP_FORMAT_ERROR = -23,
        UDP_RECEIVE_ERROR = -24,
        UDP_SEND_ERROR = -25,
        // TCP
        TCP_CREATE_ERROR = -26,
        TCP_SERVER_CONNECT_ERROR = -27,
        TCP_CONNECTION_CLOSED = -28,
        TCP_CLIENT_NOT_OPEN = -29,
        TCP_RECEIVE_ERROR = -30,
        TCP_SEND_ERROR = -31
    }
    #endregion

    #region Коды функций Modbus
    public enum ModbusCode
    {
        ReadDescreteInputs = 0x02,
        ReadCoils = 0x01,
        WriteSingleCoil = 0x05,
        WriteMultipleCoils = 0x0F,
        ReadInputRegister = 0x04,
        ReadHoldingRegisters = 0x03,
        WriteSingleRegister = 0x06,
        WriteMultipleRegisters = 0x10,
        ReadProgramVersion = 75,
        Transit = 65,   // Комманда с вложением пакета ()
    }
    #endregion
}

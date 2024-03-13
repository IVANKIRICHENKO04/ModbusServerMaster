namespace MdbusNServerMaster.Classes
{
    /// <summary>
    /// Коллекция протоколов соединения
    /// </summary>
    public enum TransportMode
    {   // режим транспорта данных
        COM_PORT,       // COM-порт
        UDP,            // UDP-клиент
        TCP_CLIENT      // TCP-клиент
    }

    /// <summary>
    /// Дескриптор линии связи
    /// </summary>
    public class LineConfig
    {

        public TransportMode transportMode;    // режим транспорта данных

        public ushort LineNumber;       // номер
        public ushort COMport;          // номер COM-порта
        public int Baudrate;            // скорость bod
        public ushort ScanIntervalMS;   // интервал опроса устройств
        public ushort IOMaxErr;         // максимальный непрерывный счетчик ошибок до отключения устройства
        public TimeSpan TimeoutErrSec;  // таймаут отключения устройства (попытка возобновить опрос)
        public string IPAddress;        // IP-адрес преобразователя Eth <=> RS485   
        public ushort IPport;           // номер порта для приема/передачи через преобразователь Eth <=> RS485   

        public List<DeviceConfig> Devices = new List<DeviceConfig>();
        public LineConfig() { }
    }

    /// <summary>
    /// Дескриптор девайса
    /// </summary>
    public class DeviceConfig
    {
        public ushort AddressOnLine;
        public string Name;
        public string Type;
        public List<RedistersConfig> redistersReads;
        public DeviceConfig() { }
    }

    /// <summary>
    /// Дескриптор блоков регистров 
    /// </summary>
    public class RedistersConfig
    {
        public ushort Start;
        public ushort Count;
        public ushort PeriodMS;
        public RedistersConfig() { }
    }

    /// <summary>
    /// Блок регистров для чтения
    /// </summary>
    public class RegistersBlock
    {
        public ushort Start;    // стартовый адрес регистра
        public ushort Count;    // число регистров
        public ushort Period;   // период опроса в мс
        public DateTime time_ms;  // время последнего чтения в мс
        public RegistersBlock(RedistersConfig config)
        {
            Start = config.Start;
            Count = config.Count;
            Period = config.PeriodMS;
            time_ms = DateTime.MinValue;
        }
    }

}

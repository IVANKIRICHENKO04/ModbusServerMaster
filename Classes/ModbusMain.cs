using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MdbusNServerMaster.Classes
{
    public static class ModbusMain
    {

        //====================================================== Статичные Переменные ===============================================================

        static List<LineConfig> lineConfigs = new List<LineConfig>();               //Список  линий, прочитанный из конфига
        static List<ModbusServer> modbusServers = new List<ModbusServer>();         //Список серверов опроса
        static Thread MainThread;                                                   //Поток для управления программой

        //========================================================= Статичные Методы ================================================================

        /// <summary>
        /// Метод для начала работы опросника, создает сервер для каждой линии
        /// </summary>
        /// <param name="path">путь к конфигу</param>
        public static void Start(string path)
        {
            if (ReadConfig(path) == true)
            {
                Console.WriteLine("Запуск сервера опроса");
                foreach (LineConfig cline in lineConfigs)
                {
                    ModbusServer server = new ModbusServer(cline);
                    modbusServers.Add(server);
                }
                Console.WriteLine($"Было создано {modbusServers.Count} линий опроса");

                MainThread = new Thread(CommunicationThread);
                MainThread.Start();
            }
        }

        /// <summary>
        /// Чтение конфига из файла xml
        /// </summary>
        /// <param name="path">путь к конфигу</param>
        /// <returns>True если конфиг прочитан, False если чтение не удалось</returns>
        public static bool ReadConfig(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Отсутствует файл конфигурирования линий связи");
                return false;
            }
            List<LineConfig> connect_line = XmlHelper.DeserializeFromXml<List<LineConfig>>(path);// конфигурация линий связи
            foreach(LineConfig line in connect_line)
                lineConfigs.Add(line);
            return true;
        }

        /// <summary>
        /// Останавливает работу всех серверов Modbus.
        /// </summary>
        public static void Stop()
        {
            // Перебираем все серверы Modbus
            foreach (ModbusServer ms in modbusServers)
            {
                // Закрываем линию связи
                ms.modbus.TransportClose();
                // Прерываем поток обмена данными
                ms.ServerThread.Abort();
                // Делаем небольшую задержку
                Thread.Sleep(20);
                // Выводим сообщение об остановке сервера
                Console.WriteLine("Сервер опроса остановлен");
            }
        }

        /// <summary>
        /// Возвращает объект сервера Modbus по номеру линии.
        /// </summary>
        /// <param name="line">Номер линии</param>
        /// <returns>Объект сервера Modbus или null, если сервер не найден.</returns>
        public static ModbusServer GetModbusServer(ushort line)
        {
            // Перебираем все серверы Modbus
            foreach (ModbusServer ms in modbusServers)
            {
                // Проверяем номер линии
                if (ms.lineConfig.LineNumber == line)
                    return ms;
            }
            // Если сервер не найден, возвращаем null
            return null;
        }

        /// <summary>
        /// Возвращает объект устройства по его типу, адресу и номеру линии.
        /// </summary>
        /// <param name="type">Тип устройства</param>
        /// <param name="address">Адрес устройства</param>
        /// <param name="line">Номер линии</param>
        /// <returns>Объект устройства или null, если устройство не найдено.</returns>
        public static SerialDevice GetDevice(string type, ushort address, ushort line)
        {
            ModbusServer server = GetModbusServer(line);
            if (server != null)
            {
                // Перебираем все устройства на указанной линии
                foreach (SerialDevice sd in server.devList)
                {
                    if (sd.deviceConfig.AddressOnLine == address)
                        if (sd.deviceConfig.Type == type)
                            return sd;

                }
                Console.WriteLine($"На линии {line} не обнаружено устройства типа {type} с адресом {address}");
                return null;
            }
            else
            {
                Console.WriteLine($"Для линии {line} не запущено Modbus сервера");
                return null;
            }

        }


        /// <summary>
        /// TECT Метод реализующий меню приложения
        /// </summary>
        static void CommunicationThread()
        {
            foreach (ModbusServer server in modbusServers)
            {
                server.Run();
            }
            while (true)
            {
                // Читаем команду из консоли
                string command = Console.ReadLine();

                // Обрабатываем команду
                switch (command)
                {
                    case "ServersCount":
                        {
                            Console.WriteLine(modbusServers.Count);
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("incorrect comand");
                            break;
                        }
                }
            }
        }

    }
}

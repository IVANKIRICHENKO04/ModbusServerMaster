namespace MdbusNServerMaster.Classes
{
    class Program
    {
        static void Main(string[] args)
        {

            string path = "D:\\Avalonia\\HelpingPrograms\\ModbusServerMaster\\Configs\\LineConfigTestUDP.xml";
            ModbusMain.Start(path);
        }
    }
}

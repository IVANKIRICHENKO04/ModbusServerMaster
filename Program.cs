using MdbusNServerMaster.Classes;
using System;
using System.IO.Ports;

namespace ModbusRTUMaster
{
    class Program
    {
        static void Main(string[] args)
        {

            string path = "D:\\Avalonia\\HelpingPrograms\\ModbusServerMaster\\LineConfigTestTCP.xml";
            ModbusMain.Start(path);
        }
    }
}

namespace MdbusNServerMaster.Classes
{
    /// <summary>
    /// Коллекция кодов функций Modbus
    /// </summary>
    public enum ModbusFunctionCode
    {
        ReadCoils = 0x01,
        ReadDiscreteInputs = 0x02,
        ReadHoldingRegisters = 0x03,
        ReadInputRegisters = 0x04,
        WriteSingleCoil = 0x05,
        WriteSingleRegister = 0x06,
        WriteMultipleCoils = 0x0F,
        WriteMultipleRegisters = 0x10
    }

    public static class ModbusPackets
    {
        public static byte[] PacketReadHoldingRegister(TransportMode transportMode, byte DevAddr, int StartingAddress, int Count)
        {
            byte[] Packet = new byte[0];
            if (transportMode == TransportMode.COM_PORT)
                Packet = new byte[8];
            else if(transportMode== TransportMode.UDP||transportMode==TransportMode.TCP_CLIENT)
                Packet = new byte[12];
            byte[] Buffer = new byte[6];
            Buffer[0] = 0; //DevAddr;                                    // Аддрес устройства
            Buffer[1] = (byte)ModbusCode.ReadHoldingRegisters;      // Код функции чтения регистра
            Buffer[2] = (byte)(StartingAddress >> 8);               // Старший байт начального адреса регистра
            Buffer[3] = (byte)(StartingAddress & 0xFF);             // Младший байт начального адреса регистра
            Buffer[4] = (byte)(Count >> 8);                         // Старщий байт колличества регистров
            Buffer[5] = (byte)(Count & 0xFF);                       // Младший байт колличества регистров
            if (transportMode == TransportMode.COM_PORT)
            {
                Packet = ConnectArrays(Packet, Buffer, 0);
                ushort CRC = CalculateCRC(Packet, 6);
                Packet[6] = (byte)(CRC & 0xFF);
                Packet[7] = (byte)((CRC >> 8) & 0xFF);
            }
            else
            {
                Packet = ConnectArrays(Packet, Buffer, 6);
                ushort transactionId = (ushort)(DateTime.UtcNow.Ticks % ushort.MaxValue);
                Packet[0] = (byte)(transactionId >> 8); // Старший байт идентификатора транзакции
                Packet[1] = (byte)(transactionId & 0xFF); // Младший байт идентификатора транзакции
                Packet[2] = 0; // Идентификатор протокола Modbus (всегда 0 в Modbus TCP)
                Packet[3] = 0; // Идентификатор протокола Modbus (продолжение)
                Packet[4] = 0; // Длина последующих байт (старший байт)
                Packet[5] = 6; // Длина последующих байт (младший байт) - 6 байт для функции чтения регистров
            }
            return Packet;
        }






        static byte[] ConnectArrays(byte[] FirstArray, byte[] SecondArray, int Shearing)
        {
            for (int i = 0; i < SecondArray.Length; i++)
            {
                FirstArray[i+Shearing] = SecondArray[i];

            }
            return FirstArray;
        }

        /// <summary>
        /// Создает контрольную сумму для пакета данных.
        /// </summary>
        /// <param name="data">Массив байтов, содержащий данные, для которых требуется создать контрольную сумму.</param>
        /// <param name="length">Длина сообщения.</param>
        /// <returns>Контрольная сумма пакета данных.</returns>
        static ushort CalculateCRC(byte[] data, int length)
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
    }
}

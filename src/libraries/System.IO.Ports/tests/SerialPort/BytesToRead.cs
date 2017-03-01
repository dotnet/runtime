// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class BytesToRead_Property : PortsTest
    {
        private const int DEFAULT_NUM_RND_BYTES = 8;

        private delegate void ReadMethodDelegate(SerialPort com, int bufferSize);

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BytesToRead_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default BytesToRead");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_RcvRndNumBytes()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying BytesToRead after receiving a random number of bytes");
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com2.Open();

                com2.Write(new byte[DEFAULT_NUM_RND_BYTES], 0, DEFAULT_NUM_RND_BYTES);

                serPortProp.SetProperty("BytesToRead", DEFAULT_NUM_RND_BYTES);
                Thread.Sleep(100); //Wait for com1 to get all of the bytes

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying BytesToRead with Read(byte[] buffer, int offset, int count)");
            VerifyBytesToRead(Read_byte_int_int, DEFAULT_NUM_RND_BYTES, false);
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_Read_char_int_int()
        {
            Debug.WriteLine("Verifying BytesToRead with Read(char[] buffer, int offset, int count)");
            VerifyBytesToRead(Read_char_int_int, DEFAULT_NUM_RND_BYTES, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_ReadByte()
        {
            Debug.WriteLine("Verifying BytesToRead with ReadByte()");
            VerifyBytesToRead(ReadByte, 1, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_ReadChar()
        {
            Debug.WriteLine("Verifying BytesToRead with ReadChar()");
            VerifyBytesToRead(ReadChar, 1, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_ReadLine()
        {
            Debug.WriteLine("Verifying BytesToRead with ReadLine()");
            VerifyBytesToRead(ReadLine, DEFAULT_NUM_RND_BYTES, true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_ReadTo()
        {
            Debug.WriteLine("Verifying BytesToRead with ReadTo(string value)");
            VerifyBytesToRead(ReadTo, DEFAULT_NUM_RND_BYTES, true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToRead_ReadExisting()
        {
            Debug.WriteLine("Verifying BytesToRead with ReadExisting()");
            VerifyBytesToRead(ReadExisting, DEFAULT_NUM_RND_BYTES, false);
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyBytesToRead(ReadMethodDelegate readMethod, int bufferSize, bool sendNewLine)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();
                int numNewLineBytes = 0;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com2.Open();

                com2.Write(new byte[bufferSize], 0, bufferSize);

                if (sendNewLine)
                {
                    com2.Write(com2.NewLine);
                    numNewLineBytes = com2.Encoding.GetByteCount(com2.NewLine.ToCharArray());
                }

                while (bufferSize + numNewLineBytes > com1.BytesToRead)
                    Thread.Sleep(10);

                readMethod(com1, bufferSize + numNewLineBytes);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        private void Read_byte_int_int(SerialPort com, int bufferSize)
        {
            com.Read(new byte[bufferSize], 0, bufferSize);
        }


        private void Read_char_int_int(SerialPort com, int bufferSize)
        {
            com.Read(new char[bufferSize], 0, bufferSize);
        }


        private void ReadByte(SerialPort com, int bufferSize)
        {
            com.ReadByte();
        }


        private void ReadChar(SerialPort com, int bufferSize)
        {
            com.ReadChar();
        }


        private void ReadLine(SerialPort com, int bufferSize)
        {
            com.ReadLine();
        }


        private void ReadTo(SerialPort com, int bufferSize)
        {
            com.ReadTo(com.NewLine);
        }


        private void ReadExisting(SerialPort com, int bufferSize)
        {
            com.ReadExisting();
        }
        #endregion
    }
}

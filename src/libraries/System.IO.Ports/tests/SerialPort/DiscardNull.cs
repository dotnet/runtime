// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class DiscardNull_Property : PortsTest
    {
        //The default number of bytes to read/write to verify the DiscardNull
        private const int DEFAULT_NUM_CHARS_TO_WRITE = 8;

        //The default number of null characters to be inserted into the characters written
        private const int DEFUALT_NUM_NULL_CHAR = 1;

        //The default number of chars to write with when testing timeout with Read(char[], int, int)
        private const int DEFAULT_READ_CHAR_ARRAY_SIZE = 8;

        //The default number of bytes to write with when testing timeout with Read(byte[], int, int)
        private const int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;

        private delegate char[] ReadMethodDelegate(SerialPort com);

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_Default_Read_byte_int_int()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();


                Debug.WriteLine("Verifying default DiscardNull with Read_byte_int_int");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, Read_byte_int_int, false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_Default_Read_char_int_int()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DiscardNull with Read_char_int_int");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, Read_char_int_int, false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_Default_ReadByte()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DiscardNull with ReadByte");
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, ReadByte, false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_Default_ReadChar()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();


                Debug.WriteLine("Verifying default DiscardNull with ReadChar");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, ReadChar, false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_Default_ReadLine()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DiscardNull with ReadLine");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, ReadLine, true);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_Default_ReadTo()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DiscardNull with ReadTo");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, ReadTo, true);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_Read_byte_int_int_Before()
        {
            Debug.WriteLine("Verifying true DiscardNull with Read_byte_int_int before open");
            VerifyDiscardNullBeforeOpen(true, Read_byte_int_int, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_Read_char_int_int_After()
        {
            Debug.WriteLine("Verifying true DiscardNull with Read_char_int_int after open");
            VerifyDiscardNullAfterOpen(true, Read_char_int_int, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_ReadByte_Before()
        {
            Debug.WriteLine("Verifying true DiscardNull with ReadByte before open");
            VerifyDiscardNullBeforeOpen(true, ReadByte, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_ReadChar_After()
        {
            Debug.WriteLine("Verifying true DiscardNull with ReadChar after open");
            VerifyDiscardNullAfterOpen(true, ReadChar, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_ReadLine_Before()
        {
            Debug.WriteLine("Verifying true DiscardNull with ReadLine before open");
            VerifyDiscardNullBeforeOpen(true, ReadLine, true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_ReadTo_After()
        {
            Debug.WriteLine("Verifying true DiscardNull with ReadTo after open");
            VerifyDiscardNullAfterOpen(true, ReadTo, true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_false_Read_byte_int_int_Before()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DiscardNull with Read_byte_int_int");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                com1.DiscardNull = true;
                com1.DiscardNull = false;

                serPortProp.SetProperty("DiscardNull", false);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, Read_byte_int_int, false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_true_true_Read_char_int_int_After()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DiscardNull with Read_char_int_int");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                com1.DiscardNull = true;
                com1.DiscardNull = true;

                serPortProp.SetProperty("DiscardNull", true);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, Read_char_int_int, false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DiscardNull_false_flase_Default_ReadByte()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DiscardNull with ReadByte");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                com1.DiscardNull = false;
                com1.DiscardNull = false;

                serPortProp.SetProperty("DiscardNull", false);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, ReadByte, false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyDiscardNullBeforeOpen(bool discardNull, ReadMethodDelegate readMethod, bool sendNewLine)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.DiscardNull = discardNull;
                com1.Open();
                serPortProp.SetProperty("DiscardNull", discardNull);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, readMethod, sendNewLine);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        private void VerifyDiscardNullAfterOpen(bool discardNull, ReadMethodDelegate readMethod, bool sendNewLine)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.DiscardNull = discardNull;
                serPortProp.SetProperty("DiscardNull", discardNull);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDiscardNull(com1, readMethod, sendNewLine);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        private void VerifyDiscardNull(SerialPort com1, ReadMethodDelegate readMethod, bool sendNewLine)
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                char[] expectedChars;
                char[] rcvChars;
                int origReadTimeout;
                char[] xmitChars = new char[DEFAULT_NUM_CHARS_TO_WRITE];
                byte[] xmitBytes;
                byte[] expectedBytes;
                Random rndGen = new Random();
                int numNulls = 0;
                int expectedIndex;
                origReadTimeout = com1.ReadTimeout;

                //Generate some random chars to transfer
                for (int i = 0; i < xmitChars.Length; i++)
                {
                    //xmitChars[i] = (char)rndGen.Next(1, UInt16.MaxValue);
                    xmitChars[i] = (char)rndGen.Next(60, 80);
                }

                //Inject the null char randomly
                for (int i = 0; i < DEFUALT_NUM_NULL_CHAR; i++)
                {
                    int nullIndex = rndGen.Next(0, xmitChars.Length);

                    if ('\0' != xmitChars[nullIndex])
                    {
                        numNulls++;
                    }

                    xmitChars[nullIndex] = '\0';
                }

                xmitBytes = com1.Encoding.GetBytes(xmitChars);

                if (com1.DiscardNull)
                {
                    expectedIndex = 0;
                    expectedChars = new char[xmitChars.Length - numNulls];
                    for (int i = 0; i < xmitChars.Length; i++)
                    {
                        if ('\0' != xmitChars[i])
                        {
                            expectedChars[expectedIndex] = xmitChars[i];
                            expectedIndex++;
                        }
                    }
                }
                else
                {
                    expectedChars = new char[xmitChars.Length];
                    Array.Copy(xmitChars, expectedChars, expectedChars.Length);
                }

                expectedBytes = com1.Encoding.GetBytes(expectedChars);
                expectedChars = com1.Encoding.GetChars(expectedBytes);

                com2.Open();
                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, expectedBytes.Length);

                if (sendNewLine)
                    com2.WriteLine("");

                com1.ReadTimeout = 250;

                rcvChars = readMethod(com1);

                Assert.Equal(0, com1.BytesToRead);
                Assert.Equal(expectedChars, rcvChars);

                com1.ReadTimeout = origReadTimeout;
            }
        }

        private char[] Read_byte_int_int(SerialPort com)
        {
            var receivedBytes = new List<byte>();
            var buffer = new byte[DEFAULT_READ_BYTE_ARRAY_SIZE];
            int totalBytesRead = 0;
            int numBytes;

            while (true)
            {
                try
                {
                    numBytes = com.Read(buffer, 0, buffer.Length);
                }
                catch (TimeoutException)
                {
                    break;
                }

                receivedBytes.InsertRange(totalBytesRead, buffer);
                totalBytesRead += numBytes;
            }

            if (totalBytesRead < receivedBytes.Count)
                receivedBytes.RemoveRange(totalBytesRead, receivedBytes.Count - totalBytesRead);

            return com.Encoding.GetChars(receivedBytes.ToArray());
        }


        private char[] Read_char_int_int(SerialPort com)
        {
            var receivedChars = new List<char>();
            var buffer = new char[DEFAULT_READ_CHAR_ARRAY_SIZE];
            int totalCharsRead = 0;
            int numChars;

            while (true)
            {
                try
                {
                    numChars = com.Read(buffer, 0, buffer.Length);
                }
                catch (TimeoutException)
                {
                    break;
                }

                receivedChars.InsertRange(totalCharsRead, buffer);
                totalCharsRead += numChars;
            }

            if (totalCharsRead < receivedChars.Count)
                receivedChars.RemoveRange(totalCharsRead, receivedChars.Count - totalCharsRead);

            return receivedChars.ToArray();
        }


        private char[] ReadByte(SerialPort com)
        {
            var receivedBytes = new List<byte>();
            int rcvByte;

            while (true)
            {
                try
                {
                    rcvByte = com.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }

                receivedBytes.Add((byte)rcvByte);
            }

            return com.Encoding.GetChars(receivedBytes.ToArray());
        }


        private char[] ReadChar(SerialPort com)
        {
            var receivedChars = new List<char>();
            int rcvChar;

            while (true)
            {
                try
                {
                    rcvChar = com.ReadChar();
                }
                catch (TimeoutException)
                {
                    break;
                }
                receivedChars.Add((char)rcvChar);
            }

            return receivedChars.ToArray();
        }


        private char[] ReadLine(SerialPort com)
        {
            var rcvStringBuilder = new StringBuilder();
            string rcvString;

            while (true)
            {
                try
                {
                    rcvString = com.ReadLine();
                }
                catch (TimeoutException)
                {
                    break;
                }

                rcvStringBuilder.Append(rcvString);
            }

            return rcvStringBuilder.ToString().ToCharArray();
        }


        private char[] ReadTo(SerialPort com)
        {
            var rcvStringBuilder = new StringBuilder();
            string rcvString;

            while (true)
            {
                try
                {
                    rcvString = com.ReadTo(com.NewLine);
                }
                catch (TimeoutException)
                {
                    break;
                }

                rcvStringBuilder.Append(rcvString);
            }

            return rcvStringBuilder.ToString().ToCharArray();
        }
        #endregion
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class ParityReplace_Property : PortsTest
    {
        //The default number of chars to write with when testing timeout with Read(char[], int, int)
        private const int DEFAULT_READ_CHAR_ARRAY_SIZE = 8;

        //The default number of bytes to write with when testing timeout with Read(byte[], int, int)
        private const int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;
        private const int s_numRndBytesPairty = 8;

        private delegate char[] ReadMethodDelegate(SerialPort com);

        #region Test Cases
        [Fact]
        public void ParityReplace_Default_BeforeOpen()
        {
            using (SerialPort com1 = new SerialPort())
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default ParityReplace before Open");

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ParityReplace_Default_AfterOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default ParityReplace after Open");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void Read_byte_int_int_RNDParityReplace()
        {
            Debug.WriteLine("Verifying random ParityReplace with Read(byte[], int, int)");
            VerifyParityReplaceByte(Read_byte_int_int, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Read_char_int_int_RNDParityReplace()
        {
            Debug.WriteLine("Verifying random ParityReplace with Read(char[], int, int)");
            VerifyParityReplaceByte(Read_char_int_int, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadByte_RNDParityReplace()
        {
            Debug.WriteLine("Verifying random ParityReplace with ReadByte()");
            VerifyParityReplaceByte(ReadByte, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadChar_RNDParityReplace()
        {
            Debug.WriteLine("Verifying random ParityReplace with ReadChar()");
            VerifyParityReplaceByte(ReadChar, false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadLine_RNDParityReplace()
        {
            Debug.WriteLine("Verifying random ParityReplace with ReadLine()");
            VerifyParityReplaceByte(17, ReadLine, true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadTo_str_RNDParityReplace()
        {
            Debug.WriteLine("Verifying random ParityReplace with ReadTo(string)");
            VerifyParityReplaceByte(ReadTo, true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ParityReplace_After_Parity()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying setting ParityReplace after Parity has been set");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com2.Open();

                com1.Parity = Parity.Even;
                com1.ParityReplace = 1;

                serPortProp.SetProperty("Parity", Parity.Even);
                serPortProp.SetProperty("ParityReplace", (byte)1);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParityReplaceByte(com1, com2, Read_byte_int_int, false);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ParityReplace_After_ParityReplace()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying setting ParityReplace after ParityReplace has aready been set");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com2.Open();

                com1.ParityReplace = 1;
                com1.ParityReplace = 2;
                com1.Parity = Parity.Odd;

                serPortProp.SetProperty("Parity", Parity.Odd);
                serPortProp.SetProperty("ParityReplace", (byte)2);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParityReplaceByte(com1, com2, Read_byte_int_int, false);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ParityReplace_After_ParityReplaceAndParity()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying setting ParityReplace after ParityReplace and Parity have aready been set");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com2.Open();

                com1.Parity = Parity.Mark;
                com1.ParityReplace = 1;
                com1.ParityReplace = 2;

                serPortProp.SetProperty("Parity", Parity.Mark);
                serPortProp.SetProperty("ParityReplace", (byte)2);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParityReplaceByte(com1, com2, Read_byte_int_int, false);
            }
        }

        #endregion

        #region Verification for Test Cases

        private void VerifyParityReplaceByte(ReadMethodDelegate readMethod, bool newLine)
        {
            Random rndGen = new Random();

            VerifyParityReplaceByte(rndGen.Next(1, 128), readMethod, newLine);
        }


        private void VerifyParityReplaceByte(int parityReplace, ReadMethodDelegate readMethod, bool newLine)
        {
            VerifyParityReplaceByteBeforeOpen(parityReplace, readMethod, newLine);
            VerifyParityReplaceByteAfterOpen(parityReplace, readMethod, newLine);
        }


        private void VerifyParityReplaceByteBeforeOpen(int parityReplace, ReadMethodDelegate readMethod, bool newLine)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                com1.ParityReplace = (byte)parityReplace;
                com1.Open();
                com2.Open();

                VerifyParityReplaceByte(com1, com2, readMethod, newLine);
            }
        }

        private void VerifyParityReplaceByteAfterOpen(int parityReplace, ReadMethodDelegate readMethod, bool newLine)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                com1.Open();
                com2.Open();

                com1.ParityReplace = (byte)parityReplace;
                VerifyParityReplaceByte(com1, com2, readMethod, newLine);
            }
        }

        private void VerifyParityReplaceByte(SerialPort com1, SerialPort com2, ReadMethodDelegate readMethod, bool newLine)
        {
            byte[] bytesToWrite = new byte[s_numRndBytesPairty];
            char[] expectedChars = new char[s_numRndBytesPairty];
            Random rndGen = new Random();
            int parityErrorIndex = rndGen.Next(0, s_numRndBytesPairty - 1);
            byte newLineByte = (byte)com1.NewLine[0];

            com1.Parity = Parity.Space;
            com1.DataBits = 7;
            com1.ReadTimeout = 500;

            //Genrate random characters without an parity error
            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                byte randByte;

                do
                {
                    randByte = (byte)rndGen.Next(0, 128);
                } while (randByte == newLineByte);

                bytesToWrite[i] = randByte;
                expectedChars[i] = (char)randByte;
            }

            bytesToWrite[parityErrorIndex] |= (byte)0x80;
            expectedChars[parityErrorIndex] = (char)com1.ParityReplace;

            VerifyRead(com1, com2, bytesToWrite, expectedChars, readMethod, newLine);
        }


        private void VerifyRead(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars, ReadMethodDelegate readMethod, bool newLine)
        {
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);

            if (newLine)
            {
                com2.Write(com1.NewLine);
                while (bytesToWrite.Length + com1.NewLine.Length > com1.BytesToRead)
                {
                }
            }
            else
            {
                TCSupport.WaitForReadBufferToLoad(com1, bytesToWrite.Length);
            }

            char[] actualChars = readMethod(com1);

            //Compare the chars that were written with the ones we expected to read
            for (int i = 0; i < expectedChars.Length; i++)
            {
                if (actualChars.Length <= i)
                {
                    Fail("ERROR!!!: Expected more characters then were actually read");
                }
                else if (expectedChars[i] != actualChars[i])
                {
                    Fail("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", (int)expectedChars[i], (int)actualChars[i], i);
                }
            }

            if (actualChars.Length > expectedChars.Length)
            {
                Fail("ERROR!!!: Read in more characters then expected");
            }
        }


        private char[] Read_byte_int_int(SerialPort com)
        {
            ArrayList receivedBytes = new ArrayList();
            byte[] buffer = new byte[DEFAULT_READ_BYTE_ARRAY_SIZE];
            int totalBytesRead = 0;

            while (true)
            {
                int numBytes;
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

            return com.Encoding.GetChars((byte[])receivedBytes.ToArray(typeof(byte)));
        }


        private char[] Read_char_int_int(SerialPort com)
        {
            ArrayList receivedChars = new ArrayList();
            char[] buffer = new char[DEFAULT_READ_CHAR_ARRAY_SIZE];
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

            return (char[])receivedChars.ToArray(typeof(char));
        }


        private char[] ReadByte(SerialPort com)
        {
            ArrayList receivedBytes = new ArrayList();
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

            return com.Encoding.GetChars((byte[])receivedBytes.ToArray(typeof(byte)));
        }


        private char[] ReadChar(SerialPort com)
        {
            ArrayList receivedChars = new ArrayList();
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

            return (char[])receivedChars.ToArray(typeof(char));
        }


        private char[] ReadLine(SerialPort com)
        {
            StringBuilder rcvStringBuilder = new StringBuilder();
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
            StringBuilder rcvStringBuilder = new StringBuilder();
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

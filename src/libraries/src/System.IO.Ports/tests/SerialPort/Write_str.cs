// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Write_str : PortsTest
    {
        // The string size used when verifying encoding 
        private const int ENCODING_STRING_SIZE = 4;

        // The string size used for large string testing
        // This has been reduced from 2048 to 2000 because the associated byte buffer size (i.e. 4096 bytes) is too large
        // to allow single read/write transactions on a FTDI USB-Serial device
        private const int LARGE_STRING_SIZE = 2000;

        //The default number of times the write method is called when verifying write
        private const int DEFAULT_NUM_WRITES = 3;

        #region Test Cases

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ASCIIEncoding()
        {
            Debug.WriteLine("Verifying write method with ASCIIEncoding");
            VerifyWrite(new ASCIIEncoding(), ENCODING_STRING_SIZE);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding()
        {
            Debug.WriteLine("Verifying write method with UTF8Encoding");
            VerifyWrite(new UTF8Encoding(), ENCODING_STRING_SIZE);
        }


        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF32Encoding()
        {
            Debug.WriteLine("Verifying write method with UTF32Encoding");
            VerifyWrite(new UTF32Encoding(), ENCODING_STRING_SIZE);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UnicodeEncoding()
        {
            Debug.WriteLine("Verifying write method with UnicodeEncoding");
            VerifyWrite(new UnicodeEncoding(), ENCODING_STRING_SIZE);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void NullString()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying Write with a null string");
                com.Open();

                try
                {
                    com.Write(null);
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void EmptyString()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying Write with an empty string");

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                VerifyWriteStr(com1, com2, "");
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void String_Null_Char()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying Write with an string containing only the null character");

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                VerifyWriteStr(com1, com2, "\0");
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void LargeString()
        {
            Debug.WriteLine("Verifying write method with a large string size");
            VerifyWrite(new UnicodeEncoding(), LARGE_STRING_SIZE, 1);
        }
        #endregion

        #region Verification for Test Cases

        public void VerifyWrite(Encoding encoding, int strSize)
        {
            VerifyWrite(encoding, strSize, DEFAULT_NUM_WRITES);
        }

        public void VerifyWrite(Encoding encoding, int strSize, int numWrites)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                string stringToWrite = TCSupport.GetRandomString(strSize, TCSupport.CharacterOptions.Surrogates);

                com1.Encoding = encoding;
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                VerifyWriteStr(com1, com2, stringToWrite, numWrites);
            }
        }

        public void VerifyWriteStr(SerialPort com1, SerialPort com2, string stringToWrite)
        {
            VerifyWriteStr(com1, com2, stringToWrite, DEFAULT_NUM_WRITES);
        }

        public void VerifyWriteStr(SerialPort com1, SerialPort com2, string stringToWrite, int numWrites)
        {
            char[] actualChars;
            byte[] expectedBytes, actualBytes;
            int byteRead;
            int index = 0;

            expectedBytes = com1.Encoding.GetBytes(stringToWrite.ToCharArray());
            char[] expectedChars = com1.Encoding.GetChars(expectedBytes);
            actualBytes = new byte[expectedBytes.Length * numWrites];

            for (int i = 0; i < numWrites; i++)
            {
                com1.Write(stringToWrite);
            }

            com2.ReadTimeout = 500;

            //com2.Encoding = com1.Encoding;
            Thread.Sleep((int)(((expectedBytes.Length * 10.0) / com1.BaudRate) * 1000) + 250);

            while (true)
            {
                try
                {
                    byteRead = com2.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }

                if (actualBytes.Length <= index)
                {
                    //If we have read in more bytes then we expect
                    Fail("ERROR!!!: We have received more bytes then were sent");
                }

                actualBytes[index] = (byte)byteRead;
                index++;

                Assert.Equal(actualBytes.Length - index, com2.BytesToRead);
            }

            actualChars = com1.Encoding.GetChars(actualBytes);

            if (actualChars.Length != expectedChars.Length * numWrites)
            {
                Fail("ERROR!!!: Expected to read {0} chars actually read {1}", expectedChars.Length * numWrites,
                    actualChars.Length);
            }
            else
            {
                //Compare the chars that were read with the ones we expected to read
                for (int j = 0; j < numWrites; j++)
                {
                    for (int i = 0; i < expectedChars.Length; i++)
                    {
                        if (expectedChars[i] != actualChars[i + expectedChars.Length * j])
                        {
                            Fail("ERROR!!!: Expected to read {0}  actual read {1} at {2}", (int)expectedChars[i],
                                (int)actualChars[i + expectedChars.Length * j], i);
                        }
                    }
                }
            }

            //Compare the bytes that were read with the ones we expected to read
            for (int j = 0; j < numWrites; j++)
            {
                for (int i = 0; i < expectedBytes.Length; i++)
                {
                    if (expectedBytes[i] != actualBytes[i + expectedBytes.Length * j])
                    {
                        Fail("ERROR!!!: Expected to read byte {0}  actual read {1} at {2}", (int)expectedBytes[i],
                            (int)actualBytes[i + expectedBytes.Length * j], i);
                    }
                }
            }
        }
        #endregion
    }
}

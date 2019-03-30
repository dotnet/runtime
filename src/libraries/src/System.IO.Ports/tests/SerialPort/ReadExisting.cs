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
    public class ReadExisting : PortsTest
    {
        //The number of random bytes to receive for read method testing
        private const int numRndBytesToRead = 8;

        //The number of random bytes to receive for large input buffer testing
        private const int largeNumRndBytesToRead = 2048;

        private enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

        #region Test Cases

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ASCIIEncoding()
        {
            VerifyRead(new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding()
        {
            VerifyRead(new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF32Encoding()
        {
            VerifyRead(new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedData()
        {
            int numberOfBytesToRead = 32;
            VerifyRead(Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedData()
        {
            int numberOfBytesToRead = 32;
            VerifyRead(Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedAndNonBufferedData()
        {
            int numberOfBytesToRead = 64;

            VerifyRead(Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedAndNonBufferedData()
        {
            int numberOfBytesToRead = 3;

            VerifyRead(Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void GreedyRead()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(128, true);
                byte[] byteXmitBuffer = new byte[1024];
                char utf32Char = TCSupport.GenerateRandomCharNonSurrogate();
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });
                int numBytes;

                Debug.WriteLine("Verifying that ReadExisting() will read everything from internal buffer and drivers buffer");

                //Put the first byte of the utf32 encoder char in the last byte of this buffer
                //when we read this later the buffer will have to be resized
                byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                TCSupport.WaitForReadBufferToLoad(com1, byteXmitBuffer.Length);

                //Read Every Byte except the last one. The last bye should be left in the last position of SerialPort's
                //internal buffer. When we try to read this char as UTF32 the buffer should have to be resized so 
                //the other 3 bytes of the ut32 encoded char can be in the buffer
                com1.Read(new char[1023], 0, 1023);

                Assert.Equal(1, com1.BytesToRead);

                com1.Encoding = Encoding.UTF32;
                com2.Encoding = Encoding.UTF32;

                com2.Write(utf32CharBytes, 1, 3);
                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);

                numBytes = Encoding.UTF32.GetByteCount(charXmitBuffer);

                byte[] byteBuffer = Encoding.UTF32.GetBytes(charXmitBuffer);

                var expectedChars = new char[1 + Encoding.UTF32.GetCharCount(byteBuffer)];
                expectedChars[0] = utf32Char;

                Encoding.UTF32.GetChars(byteBuffer, 0, byteBuffer.Length, expectedChars, 1);

                TCSupport.WaitForReadBufferToLoad(com1, 4 + numBytes);

                string rcvString = com1.ReadExisting();

                Assert.NotNull(rcvString);

                char[] actualChars = rcvString.ToCharArray();

                Assert.Equal(expectedChars, actualChars);

                Assert.Equal(0, com1.BytesToRead);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void LargeInputBuffer()
        {
            VerifyRead(largeNumRndBytesToRead);
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyRead()
        {
            VerifyRead(new ASCIIEncoding(), numRndBytesToRead);
        }

        private void VerifyRead(int numberOfBytesToRead)
        {
            VerifyRead(new ASCIIEncoding(), numberOfBytesToRead);
        }

        private void VerifyRead(Encoding encoding)
        {
            VerifyRead(encoding, numRndBytesToRead);
        }

        private void VerifyRead(Encoding encoding, int numberOfBytesToRead)
        {
            VerifyRead(encoding, numberOfBytesToRead, ReadDataFromEnum.NonBuffered);
        }

        private void VerifyRead(Encoding encoding, int numberOfBytesToRead, ReadDataFromEnum readDataFrom)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random(-55);
                char[] charsToWrite = new char[numberOfBytesToRead];
                byte[] bytesToWrite = new byte[numberOfBytesToRead];

                //Genrate random chars to send
                for (int i = 0; i < bytesToWrite.Length; i++)
                {
                    char randChar = (char)rndGen.Next(0, ushort.MaxValue);

                    charsToWrite[i] = randChar;
                }

                Debug.WriteLine("Verifying read method endocing={0} with {1} random chars", encoding.EncodingName,
                    bytesToWrite.Length);

                com1.ReadTimeout = 500;
                com1.Encoding = encoding;

                TCSupport.SetHighSpeed(com1, com2);

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                bytesToWrite = com1.Encoding.GetBytes(charsToWrite, 0, charsToWrite.Length);

                switch (readDataFrom)
                {
                    case ReadDataFromEnum.NonBuffered:
                        VerifyReadNonBuffered(com1, com2, bytesToWrite);
                        break;
                    case ReadDataFromEnum.Buffered:
                        VerifyReadBuffered(com1, com2, bytesToWrite);
                        break;
                    case ReadDataFromEnum.BufferedAndNonBuffered:
                        VerifyReadBufferedAndNonBuffered(com1, com2, bytesToWrite);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(readDataFrom), readDataFrom, null);
                }
            }
        }

        private void VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars);
        }

        private void VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

            BufferData(com1, com2, bytesToWrite);

            PerformReadOnCom1FromCom2(com1, com2, expectedChars);
        }


        private void VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            char[] expectedChars = new char[com1.Encoding.GetCharCount(bytesToWrite, 0, bytesToWrite.Length) * 2];
            char[] encodedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

            Array.Copy(encodedChars, 0, expectedChars, 0, bytesToWrite.Length);
            Array.Copy(encodedChars, 0, expectedChars, encodedChars.Length, encodedChars.Length);

            BufferData(com1, com2, bytesToWrite);

            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars);
        }

        private void BufferData(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            com2.Write(bytesToWrite, 0, 1); // Write one byte at the begining because we are going to read this to buffer the rest of the data
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);

            TCSupport.WaitForReadBufferToLoad(com1, bytesToWrite.Length);

            com1.Read(new char[1], 0, 1); // This should put the rest of the bytes in SerialPorts own internal buffer

            Assert.Equal(bytesToWrite.Length, com1.BytesToRead);
        }

        private void VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars)
        {
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);
            com1.ReadTimeout = 500;
            Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);
            PerformReadOnCom1FromCom2(com1, com2, expectedChars);
        }

        private void PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, char[] expectedChars)
        {
            string rcvString = com1.ReadExisting();
            char[] rcvBuffer = rcvString.ToCharArray();

            //Compare the chars that were written with the ones we expected to read
            Assert.Equal(expectedChars, rcvBuffer);

            Assert.Equal(0, com1.BytesToRead);
        }
        #endregion
    }
}

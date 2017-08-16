// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class ReadChar : PortsTest
    {
        //The number of random bytes to receive for large input buffer testing
        // This was 4096, but the largest buffer setting on FTDI USB-Serial devices is "4096", which actually only allows a read of 4094 or 4095 bytes
        // The test code assumes that we will be able to do this transfer as a single read, so 4000 is safer and would seem to be about 
        // as rigourous a test
        private const int largeNumRndBytesToRead = 4000;

        //The number of random characters to receive
        private const int numRndChar = 8;

        private enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

        #region Test Cases

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ASCIIEncoding()
        {
            Debug.WriteLine("Verifying read with bytes encoded with ASCIIEncoding");
            VerifyRead(new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding()
        {
            Debug.WriteLine("Verifying read with bytes encoded with UTF8Encoding");
            VerifyRead(new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF32Encoding()
        {
            Debug.WriteLine("Verifying read with bytes encoded with UTF32Encoding");
            VerifyRead(new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedData()
        {
            VerifyRead(Encoding.ASCII, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedData()
        {
            VerifyRead(Encoding.ASCII, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedAndNonBufferedData()
        {
            VerifyRead(Encoding.ASCII, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedAndNonBufferedData()
        {
            VerifyRead(Encoding.ASCII, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Zero_ResizeBuffer()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                byte[] byteXmitBuffer = new byte[1024];
                char utf32Char = 'A';
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });
                char[] charXmitBuffer = TCSupport.GetRandomChars(16, false);
                char[] expectedChars = new char[charXmitBuffer.Length + 1];

                Debug.WriteLine("Verifying Read method with zero timeout that resizes SerialPort's buffer");

                expectedChars[0] = utf32Char;
                Array.Copy(charXmitBuffer, 0, expectedChars, 1, charXmitBuffer.Length);

                //Put the first byte of the utf32 encoder char in the last byte of this buffer
                //when we read this later the buffer will have to be resized
                byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];
                com1.ReadTimeout = 0;

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
                byteXmitBuffer = Encoding.UTF32.GetBytes(charXmitBuffer);

                Assert.Throws<TimeoutException>(() => com1.ReadChar());

                Assert.Equal(1, com1.BytesToRead);

                com2.Write(utf32CharBytes, 1, 3);
                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                TCSupport.WaitForReadBufferToLoad(com1, 4 + byteXmitBuffer.Length);

                PerformReadOnCom1FromCom2(com1, com2, expectedChars);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_NonZero_ResizeBuffer()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                byte[] byteXmitBuffer = new byte[1024];
                char utf32Char = 'A';
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });
                char[] charXmitBuffer = TCSupport.GetRandomChars(16, false);
                char[] expectedChars = new char[charXmitBuffer.Length + 1];

                Debug.WriteLine("Verifying Read method with non zero timeout that resizes SerialPort's buffer");

                expectedChars[0] = utf32Char;
                Array.Copy(charXmitBuffer, 0, expectedChars, 1, charXmitBuffer.Length);

                //Put the first byte of the utf32 encoder char in the last byte of this buffer
                //when we read this later the buffer will have to be resized
                byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];

                com1.ReadTimeout = 500;
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
                byteXmitBuffer = Encoding.UTF32.GetBytes(charXmitBuffer);

                Assert.Throws<TimeoutException>(() => com1.ReadChar());

                Assert.Equal(1, com1.BytesToRead);

                com2.Write(utf32CharBytes, 1, 3);
                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                TCSupport.WaitForReadBufferToLoad(com1, 4 + byteXmitBuffer.Length);

                PerformReadOnCom1FromCom2(com1, com2, expectedChars);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void GreedyRead()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                byte[] byteXmitBuffer = new byte[1024];
                char utf32Char = TCSupport.GenerateRandomCharNonSurrogate();
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });
                int charRead;

                Debug.WriteLine("Verifying that ReadChar() will read everything from internal buffer and drivers buffer");

                //Put the first byte of the utf32 encoder char in the last byte of this buffer
                //when we read this later the buffer will have to be resized
                byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];

                TCSupport.SetHighSpeed(com1, com2);

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
                com2.Write(utf32CharBytes, 1, 3);

                TCSupport.WaitForReadBufferToLoad(com1, 4);

                if (utf32Char != (charRead = com1.ReadChar()))
                {
                    Fail("Err_6481sfadw ReadChar() returned={0} expected={1}", charRead, utf32Char);
                }

                Assert.Equal(0, com1.BytesToRead);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void LargeInputBuffer()
        {
            Debug.WriteLine("Verifying read with large input buffer");
            VerifyRead(Encoding.ASCII, largeNumRndBytesToRead);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Zero_Bytes()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char utf32Char = (char)0x254b; //Box drawing char
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });

                int readChar;

                Debug.WriteLine("Verifying Read method with zero timeout that resizes SerialPort's buffer");

                com1.Encoding = Encoding.UTF32;
                com1.ReadTimeout = 0;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                //[] Try ReadChar with no bytes available
                Assert.Throws<TimeoutException>(() => com1.ReadChar());

                Assert.Equal(0, com1.BytesToRead);

                //[] Try ReadChar with 1 byte available
                com2.Write(utf32CharBytes, 0, 1);

                TCSupport.WaitForPredicate(() => com1.BytesToRead == 1, 2000,
                    "Err_28292aheid Expected BytesToRead to be 1");

                Assert.Throws<TimeoutException>(() => com1.ReadChar());

                Assert.Equal(1, com1.BytesToRead);

                //[] Try ReadChar with the bytes in the buffer and in available in the SerialPort
                com2.Write(utf32CharBytes, 1, 3);

                TCSupport.WaitForPredicate(() => com1.BytesToRead == 4, 2000,
                    "Err_415568haikpas Expected BytesToRead to be 4");

                readChar = com1.ReadChar();

                Assert.Equal(utf32Char, readChar);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_DataReceivedBeforeTimeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
                char[] charRcvBuffer = new char[charXmitBuffer.Length];
                ASyncRead asyncRead = new ASyncRead(com1);
                var asyncReadTask = new Task(asyncRead.Read);


                Debug.WriteLine(
                    "Verifying that ReadChar will read characters that have been received after the call to Read was made");

                com1.Encoding = Encoding.UTF8;
                com2.Encoding = Encoding.UTF8;
                com1.ReadTimeout = 20000; // 20 seconds

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                asyncReadTask.Start();
                asyncRead.ReadStartedEvent.WaitOne();
                //This only tells us that the thread has started to execute code in the method
                Thread.Sleep(2000); //We need to wait to guarentee that we are executing code in SerialPort
                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);

                asyncRead.ReadCompletedEvent.WaitOne();

                Assert.Null(asyncRead.Exception);

                if (asyncRead.Result != charXmitBuffer[0])
                {
                    Fail("Err_0158ahei Expected ReadChar to read {0}({0:X}) actual {1}({1:X})", charXmitBuffer[0], asyncRead.Result);
                }
                else
                {
                    charRcvBuffer[0] = (char)asyncRead.Result;

                    int receivedLength = 1;
                    while (receivedLength < charXmitBuffer.Length)
                    {
                        receivedLength += com1.Read(charRcvBuffer, receivedLength, charRcvBuffer.Length - receivedLength);
                    }

                    Assert.Equal(receivedLength, charXmitBuffer.Length);
                    Assert.Equal(charXmitBuffer, charRcvBuffer);
                }

                TCSupport.WaitForTaskCompletion(asyncReadTask);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_Timeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
                byte[] byteXmitBuffer = new UTF32Encoding().GetBytes(charXmitBuffer);
                char[] charRcvBuffer = new char[charXmitBuffer.Length];

                int result;

                Debug.WriteLine(
                    "Verifying that Read(char[], int, int) works appropriately after TimeoutException has been thrown");

                com1.Encoding = new UTF32Encoding();
                com2.Encoding = new UTF32Encoding();
                com1.ReadTimeout = 500; // 20 seconds

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                //Write the first 3 bytes of a character
                com2.Write(byteXmitBuffer, 0, 3);

                Assert.Throws<TimeoutException>(() => com1.ReadChar());

                Assert.Equal(3, com1.BytesToRead);

                com2.Write(byteXmitBuffer, 3, byteXmitBuffer.Length - 3);

                TCSupport.WaitForExpected(() => com1.BytesToRead, byteXmitBuffer.Length,
                    5000, "Err_91818aheid BytesToRead");

                result = com1.ReadChar();

                if (result != charXmitBuffer[0])
                {
                    Fail("Err_0158ahei Expected ReadChar to read {0}({0:X}) actual {1}({1:X})", charXmitBuffer[0], result);
                }
                else
                {
                    charRcvBuffer[0] = (char)result;
                    int readResult = com1.Read(charRcvBuffer, 1, charRcvBuffer.Length - 1);

                    if (readResult + 1 != charXmitBuffer.Length)
                    {
                        Fail("Err_051884ajoedo Expected Read to read {0} characters actually read {1}", charXmitBuffer.Length - 1, readResult);
                    }
                    else
                    {
                        for (int i = 0; i < charXmitBuffer.Length; ++i)
                        {
                            if (charRcvBuffer[i] != charXmitBuffer[i])
                            {
                                Fail("Err_05188ahed Characters differ at {0} expected:{1}({1:X}) actual:{2}({2:X})", i, charXmitBuffer[i], charRcvBuffer[i]);
                            }
                        }
                    }
                }

                VerifyBytesReadOnCom1FromCom2(com1, com2, byteXmitBuffer, charXmitBuffer);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_Surrogate()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] surrogateChars = { (char)0xDB26, (char)0xDC49 };
                char[] additionalChars = TCSupport.GetRandomChars(32, TCSupport.CharacterOptions.None);
                char[] charRcvBuffer = new char[2];

                Debug.WriteLine("Verifying that ReadChar works correctly when trying to read surrogate characters");

                com1.Encoding = new UTF32Encoding();
                com2.Encoding = new UTF32Encoding();
                com1.ReadTimeout = 500; // 20 seconds

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                int numBytes = com2.Encoding.GetByteCount(surrogateChars);
                numBytes += com2.Encoding.GetByteCount(additionalChars);

                com2.Write(surrogateChars, 0, 2);
                com2.Write(additionalChars, 0, additionalChars.Length);

                TCSupport.WaitForExpected(() => com1.BytesToRead, numBytes,
                    5000, "Err_91818aheid BytesToRead");

                // We expect this to fail, because it can't read a surrogate
                AssertExtensions.Throws<ArgumentException>(null, () => com1.ReadChar());

                int result = com1.Read(charRcvBuffer, 0, 2);

                Assert.Equal(2, result);

                if (charRcvBuffer[0] != surrogateChars[0])
                {
                    Fail("Err_12929anied Expected first char read={0}({1:X}) actually read={2}({3:X})",
                        surrogateChars[0], (int)surrogateChars[0], charRcvBuffer[0], (int)charRcvBuffer[0]);
                }
                else if (charRcvBuffer[1] != surrogateChars[1])
                {
                    Fail("Err_12929anied Expected second char read={0}({1:X}) actually read={2}({3:X})",
                        surrogateChars[1], (int)surrogateChars[1], charRcvBuffer[1], (int)charRcvBuffer[1]);
                }

                PerformReadOnCom1FromCom2(com1, com2, additionalChars);
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyRead(Encoding encoding)
        {
            VerifyRead(encoding, ReadDataFromEnum.NonBuffered);
        }

        private void VerifyRead(Encoding encoding, int bufferSize)
        {
            VerifyRead(encoding, ReadDataFromEnum.NonBuffered, bufferSize);
        }

        private void VerifyRead(Encoding encoding, ReadDataFromEnum readDataFrom)
        {
            VerifyRead(encoding, readDataFrom, numRndChar);
        }

        private void VerifyRead(Encoding encoding, ReadDataFromEnum readDataFrom, int bufferSize)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(bufferSize, false);
                byte[] byteXmitBuffer = encoding.GetBytes(charXmitBuffer);

                com1.ReadTimeout = 500;
                com1.Encoding = encoding;

                TCSupport.SetHighSpeed(com1, com2);

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                switch (readDataFrom)
                {
                    case ReadDataFromEnum.NonBuffered:
                        VerifyReadNonBuffered(com1, com2, byteXmitBuffer);
                        break;
                    case ReadDataFromEnum.Buffered:
                        VerifyReadBuffered(com1, com2, byteXmitBuffer);
                        break;
                    case ReadDataFromEnum.BufferedAndNonBuffered:
                        VerifyReadBufferedAndNonBuffered(com1, com2, byteXmitBuffer);
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
            int bytesToRead = com1.Encoding.GetByteCount(expectedChars);
            char[] charRcvBuffer = new char[expectedChars.Length];
            int rcvBufferSize = 0;
            int i;

            i = 0;
            while (true)
            {
                int readInt;
                try
                {
                    readInt = com1.ReadChar();
                }
                catch (TimeoutException)
                {
                    Assert.Equal(expectedChars.Length, i);
                    break;
                }

                //While there are more characters to be read
                if (expectedChars.Length <= i)
                {
                    //If we have read in more characters then were actually sent
                    Fail("ERROR!!!: We have received more characters then were sent");
                }

                charRcvBuffer[i] = (char)readInt;
                rcvBufferSize += com1.Encoding.GetByteCount(charRcvBuffer, i, 1);

                int com1ToRead = com1.BytesToRead;

                if (bytesToRead - rcvBufferSize != com1ToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1} at {2}", bytesToRead - rcvBufferSize, com1ToRead, i);
                }

                if (readInt != expectedChars[i])
                {
                    //If the character read is not the expected character
                    Fail("ERROR!!!: Expected to read {0}  actual read char {1} at {2}", (int)expectedChars[i], readInt, i);
                }

                i++;
            }

            Assert.Equal(0, com1.BytesToRead);
        }

        public class ASyncRead
        {
            private readonly SerialPort _com;
            private int _result;

            private readonly AutoResetEvent _readCompletedEvent;
            private readonly AutoResetEvent _readStartedEvent;

            private Exception _exception;

            public ASyncRead(SerialPort com)
            {
                _com = com;
                _result = int.MinValue;

                _readCompletedEvent = new AutoResetEvent(false);
                _readStartedEvent = new AutoResetEvent(false);

                _exception = null;
            }

            public void Read()
            {
                try
                {
                    _readStartedEvent.Set();
                    _result = _com.ReadChar();
                }
                catch (Exception e)
                {
                    _exception = e;
                }
                finally
                {
                    _readCompletedEvent.Set();
                }
            }

            public AutoResetEvent ReadStartedEvent => _readStartedEvent;

            public AutoResetEvent ReadCompletedEvent => _readCompletedEvent;

            public int Result => _result;

            public Exception Exception => _exception;
        }
        #endregion
    }
}

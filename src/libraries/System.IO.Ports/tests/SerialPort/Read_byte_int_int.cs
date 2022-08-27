// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Read_byte_int_int : PortsTest
    {
        //The number of random bytes to receive for read method testing
        private const int numRndBytesToRead = 16;

        //The number of random bytes to receive for large input buffer testing
        private const int largeNumRndBytesToRead = 2048;

        //When we test Read and do not care about actually reading anything we must still
        //create an byte array to pass into the method the following is the size of the
        //byte array used in this situation
        private const int defaultByteArraySize = 1;
        private const int defaultByteOffset = 0;
        private const int defaultByteCount = 1;

        //The maximum buffer size when an exception occurs
        private const int maxBufferSizeForException = 255;

        //The maximum buffer size when an exception is not expected
        private const int maxBufferSize = 8;

        private enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

        #region Test Cases
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Buffer_Null()
        {
            VerifyReadException(null, 0, 1, typeof(ArgumentNullException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEG1()
        {
            VerifyReadException(new byte[defaultByteArraySize], -1, defaultByteCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEGRND()
        {
            Random rndGen = new Random();

            VerifyReadException(new byte[defaultByteArraySize], rndGen.Next(int.MinValue, 0), defaultByteCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_MinInt()
        {
            VerifyReadException(new byte[defaultByteArraySize], int.MinValue, defaultByteCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEG1()
        {
            VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, -1, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEGRND()
        {
            Random rndGen = new Random();

            VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, rndGen.Next(int.MinValue, 0), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_MinInt()
        {
            VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void OffsetCount_EQ_Length_Plus_1()
        {
            Random rndGen = new Random();
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = bufferLength + 1 - offset;
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void OffsetCount_GT_Length()
        {
            Random rndGen = new Random();
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = rndGen.Next(bufferLength + 1 - offset, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Offset_GT_Length()
        {
            Random rndGen = new Random();
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(bufferLength, int.MaxValue);
            int count = defaultByteCount;
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Count_GT_Length()
        {
            Random rndGen = new Random();
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = defaultByteOffset;
            int count = rndGen.Next(bufferLength + 1, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void OffsetCount_EQ_Length()
        {
            Random rndGen = new Random();
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = bufferLength - offset;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Offset_EQ_Length_Minus_1()
        {
            Random rndGen = new Random();
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = bufferLength - 1;
            int count = 1;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Count_EQ_Length()
        {
            Random rndGen = new Random();
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = 0;
            int count = bufferLength;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedData()
        {
            int bufferLength = 32 + 8;
            int offset = 3;
            int count = 32;

            VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedData()
        {
            int bufferLength = 8;
            int offset = 3;
            int count = 3;

            VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedAndNonBufferedData()
        {
            int bufferLength = 64 + 8;
            int offset = 3;
            int count = 64;

            VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedAndNonBufferedData()
        {
            int bufferLength = 8;
            int offset = 3;
            int count = 3;

            VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void GreedyRead()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random();
                byte[] byteXmitBuffer = new byte[1024];
                byte[] expectedBytes = new byte[byteXmitBuffer.Length + 4];
                byte[] byteRcvBuffer;
                char utf32Char = (char)8169;
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });
                int numBytesRead;

                Debug.WriteLine(
                    "Verifying that Read(byte[], int, int) will read everything from internal buffer and drivers buffer");
                for (int i = 0; i < utf32CharBytes.Length; i++)
                {
                    expectedBytes[i] = utf32CharBytes[i];
                }

                for (int i = 0; i < byteXmitBuffer.Length; i++)
                {
                    byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
                    expectedBytes[i + 4] = byteXmitBuffer[i];
                }

                //Put the first byte of the utf32 encoder char in the last byte of this buffer
                //when we read this later the buffer will have to be resized
                byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];
                expectedBytes[byteXmitBuffer.Length + 4 - 1] = utf32CharBytes[0];

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                TCSupport.WaitForReadBufferToLoad(com1, byteXmitBuffer.Length);

                //Read Every Byte except the last one. The last bye should be left in the last position of SerialPort's
                //internal buffer. When we try to read this char as UTF32 the buffer should have to be resized so
                //the other 3 bytes of the ut32 encoded char can be in the buffer
                com1.Read(new char[1023], 0, 1023);

                if (1 != com1.BytesToRead)
                {
                    Fail("Err_9416sapz ExpectedByteToRead={0} actual={1}", 1, com1.BytesToRead);
                }

                com2.Write(utf32CharBytes, 1, 3);
                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                byteRcvBuffer = new byte[(int)(expectedBytes.Length * 1.5)];

                TCSupport.WaitForReadBufferToLoad(com1, 4 + byteXmitBuffer.Length);

                if (expectedBytes.Length != (numBytesRead = com1.Read(byteRcvBuffer, 0, byteRcvBuffer.Length)))
                {
                    Fail("Err_6481sfadw Expected read to read {0} chars actually read {1}", expectedBytes.Length, numBytesRead);
                }

                for (int i = 0; i < expectedBytes.Length; i++)
                {
                    if (expectedBytes[i] != byteRcvBuffer[i])
                    {
                        Fail("Err_70782apzh Expected to read {0} actually read {1} at {2}", (int)expectedBytes[i], (int)byteRcvBuffer[i], i);
                    }
                }

                if (0 != com1.BytesToRead)
                {
                    Fail("Err_78028asdf ExpectedByteToRead={0} actual={1}", 0, com1.BytesToRead);
                }
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void LargeInputBuffer()
        {
            int bufferLength = largeNumRndBytesToRead;
            int offset = 0;
            int count = bufferLength;

            VerifyRead(new byte[bufferLength], offset, count, largeNumRndBytesToRead);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_DataReceivedBeforeTimeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                byte[] byteXmitBuffer = TCSupport.GetRandomBytes(512);
                byte[] byteRcvBuffer = new byte[byteXmitBuffer.Length];
                ASyncRead asyncRead = new ASyncRead(com1, byteRcvBuffer, 0, byteRcvBuffer.Length);
                var asyncReadTask = new Task(asyncRead.Read);


                Debug.WriteLine(
                    "Verifying that Read(byte[], int, int) will read characters that have been received after the call to Read was made");

                com1.Encoding = Encoding.UTF8;
                com2.Encoding = Encoding.UTF8;
                com1.ReadTimeout = 20000; // 20 seconds

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                asyncReadTask.Start();
                asyncRead.ReadStartedEvent.WaitOne();
                //This only tells us that the thread has started to execute code in the method
                Thread.Sleep(2000); //We need to wait to guarantee that we are executing code in SerialPort
                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                asyncRead.ReadCompletedEvent.WaitOne();

                if (null != asyncRead.Exception)
                {
                    Fail("Err_04448ajhied Unexpected exception thrown from async read:\n{0}", asyncRead.Exception);
                }
                else if (asyncRead.Result < 1)
                {
                    Fail("Err_0158ahei Expected Read to read at least one character {0}", asyncRead.Result);
                }
                else
                {
                    Thread.Sleep(1000); //We need to wait for all of the bytes to be received
                    int readResult = com1.Read(byteRcvBuffer, asyncRead.Result, byteRcvBuffer.Length - asyncRead.Result);

                    if (asyncRead.Result + readResult != byteXmitBuffer.Length)
                    {
                        Fail("Err_051884ajoedo Expected Read to read {0} characters actually read {1}",
                            byteXmitBuffer.Length - asyncRead.Result, readResult);
                    }
                    else
                    {
                        for (int i = 0; i < byteXmitBuffer.Length; ++i)
                        {
                            if (byteRcvBuffer[i] != byteXmitBuffer[i])
                            {
                                Fail(
                                    "Err_05188ahed Characters differ at {0} expected:{1}({1:X}) actual:{2}({2:X}) asyncRead.Result={3}",
                                    i, byteXmitBuffer[i], byteRcvBuffer[i], asyncRead.Result);
                            }
                        }
                    }
                }

                TCSupport.WaitForTaskCompletion(asyncReadTask);
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyReadException(byte[] buffer, int offset, int count, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int bufferLength = null == buffer ? 0 : buffer.Length;

                Debug.WriteLine("Verifying read method throws {0} buffer.Length={1}, offset={2}, count={3}", expectedException, bufferLength, offset, count);
                com.Open();

                Assert.Throws(expectedException, () => com.Read(buffer, offset, count));
            }
        }

        private void VerifyRead(byte[] buffer, int offset, int count)
        {
            VerifyRead(buffer, offset, count, numRndBytesToRead);
        }

        private void VerifyRead(byte[] buffer, int offset, int count, int numberOfBytesToRead)
        {
            VerifyRead(buffer, offset, count, numberOfBytesToRead, ReadDataFromEnum.NonBuffered);
        }

        private void VerifyRead(byte[] buffer, int offset, int count, int numberOfBytesToRead, ReadDataFromEnum readDataFrom)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random();
                byte[] bytesToWrite = new byte[numberOfBytesToRead];

                // Generate random bytes
                for (int i = 0; i < bytesToWrite.Length; i++)
                {
                    byte randByte = (byte)rndGen.Next(0, 256);

                    bytesToWrite[i] = randByte;
                }

                // Generate some random bytes in the buffer
                for (int i = 0; i < buffer.Length; i++)
                {
                    byte randByte = (byte)rndGen.Next(0, 256);

                    buffer[i] = randByte;
                }

                Debug.WriteLine("Verifying read method buffer.Length={0}, offset={1}, count={2} with {3} random chars",
                    buffer.Length, offset, count, bytesToWrite.Length);

                com1.ReadTimeout = 500;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                switch (readDataFrom)
                {
                    case ReadDataFromEnum.NonBuffered:
                        VerifyReadNonBuffered(com1, com2, bytesToWrite, buffer, offset, count);
                        break;

                    case ReadDataFromEnum.Buffered:
                        VerifyReadBuffered(com1, com2, bytesToWrite, buffer, offset, count);
                        break;

                    case ReadDataFromEnum.BufferedAndNonBuffered:
                        VerifyReadBufferedAndNonBuffered(com1, com2, bytesToWrite, buffer, offset, count);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(readDataFrom), readDataFrom, null);
                }
            }
        }

        private void VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
        {
            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, rcvBuffer, offset, count);
        }

        private void VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
        {
            BufferData(com1, com2, bytesToWrite);
            PerformReadOnCom1FromCom2(com1, com2, bytesToWrite, rcvBuffer, offset, count);
        }

        private void VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
        {
            byte[] expectedBytes = new byte[(2 * bytesToWrite.Length)];

            BufferData(com1, com2, bytesToWrite);

            Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, 0, bytesToWrite.Length);
            Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, bytesToWrite.Length, bytesToWrite.Length);

            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedBytes, rcvBuffer, offset, count);
        }

        private void BufferData(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            com2.Write(bytesToWrite, 0, 1); // Write one byte at the beginning because we are going to read this to buffer the rest of the data
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);

            while (com1.BytesToRead < bytesToWrite.Length)
            {
                Thread.Sleep(50);
            }

            com1.Read(new char[1], 0, 1); // This should put the rest of the bytes in SerialPorts own internal buffer

            Assert.Equal(bytesToWrite.Length, com1.BytesToRead);
        }

        private void VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
        {
            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, bytesToWrite, rcvBuffer, offset, count);
        }


        private void VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] expectedBytes, byte[] rcvBuffer, int offset, int count)
        {
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);

            com1.ReadTimeout = 500;

            Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

            PerformReadOnCom1FromCom2(com1, com2, expectedBytes, rcvBuffer, offset, count);
        }

        private void PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] expectedBytes, byte[] rcvBuffer, int offset, int count)
        {
            byte[] buffer = new byte[expectedBytes.Length];
            int bytesRead, totalBytesRead;
            int bytesToRead;
            byte[] oldRcvBuffer = (byte[])rcvBuffer.Clone();

            totalBytesRead = 0;
            bytesToRead = com1.BytesToRead;

            while (true)
            {
                try
                {
                    bytesRead = com1.Read(rcvBuffer, offset, count);
                }
                catch (TimeoutException)
                {
                    break;
                }

                //While their are more characters to be read
                if ((bytesToRead > bytesRead && count != bytesRead) || (bytesToRead <= bytesRead && bytesRead != bytesToRead))
                {
                    //If we have not read all of the characters that we should have
                    Fail("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                }

                if (expectedBytes.Length < totalBytesRead + bytesRead)
                {
                    //If we have read in more characters then we expect
                    Fail("ERROR!!!: We have received more characters then were sent {0}", totalBytesRead + bytesRead);
                }

                VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, bytesRead);

                Array.Copy(rcvBuffer, offset, buffer, totalBytesRead, bytesRead);
                totalBytesRead += bytesRead;

                if (expectedBytes.Length - totalBytesRead != com1.BytesToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1}", expectedBytes.Length - totalBytesRead, com1.BytesToRead);
                }

                oldRcvBuffer = (byte[])rcvBuffer.Clone();
                bytesToRead = com1.BytesToRead;
            }

            VerifyBuffer(rcvBuffer, oldRcvBuffer, 0, rcvBuffer.Length);

            //Compare the bytes that were written with the ones we read
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (expectedBytes[i] != buffer[i])
                {
                    Fail("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", expectedBytes[i], buffer[i], i);
                }
            }

            if (0 != com1.BytesToRead)
            {
                Fail("ERROR!!!: Expected BytesToRead=0  actual BytesToRead={0}", com1.BytesToRead);
            }

            if (com1.IsOpen)
                com1.Close();

            if (com2.IsOpen)
                com2.Close();
        }


        private void VerifyBuffer(byte[] actualBuffer, byte[] expectedBuffer, int offset, int count)
        {
            //Verify all character before the offset
            for (int i = 0; i < offset; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("Err_2038apzn!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }

            //Verify all character after the offset + count
            for (int i = offset + count; i < actualBuffer.Length; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("Err_7025nbht!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }
        }

        private class ASyncRead
        {
            private readonly SerialPort _com;
            private readonly byte[] _buffer;
            private readonly int _offset;
            private readonly int _count;
            private int _result;

            private readonly AutoResetEvent _readCompletedEvent;
            private readonly AutoResetEvent _readStartedEvent;

            private Exception _exception;

            public ASyncRead(SerialPort com, byte[] buffer, int offset, int count)
            {
                _com = com;
                _buffer = buffer;
                _offset = offset;
                _count = count;

                _result = -1;

                _readCompletedEvent = new AutoResetEvent(false);
                _readStartedEvent = new AutoResetEvent(false);

                _exception = null;
            }

            public void Read()
            {
                try
                {
                    _readStartedEvent.Set();
                    _result = _com.Read(_buffer, _offset, _count);
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

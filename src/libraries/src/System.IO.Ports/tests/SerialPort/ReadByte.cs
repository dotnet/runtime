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
    public class ReadByte : PortsTest
    {
        //The number of random bytes to receive
        private const int numRndByte = 8;

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
        public void Read_DataReceivedBeforeTimeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                byte[] byteXmitBuffer = TCSupport.GetRandomBytes(512);
                byte[] byteRcvBuffer = new byte[byteXmitBuffer.Length];
                ASyncRead asyncRead = new ASyncRead(com1);
                var asyncReadTask = new Task(asyncRead.Read);

                Debug.WriteLine(
                    "Verifying that ReadByte() will read bytes that have been received after the call to Read was made");

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
                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                asyncRead.ReadCompletedEvent.WaitOne();

                if (null != asyncRead.Exception)
                {
                    Fail("Err_04448ajhied Unexpected exception thrown from async read:\n{0}", asyncRead.Exception);
                }
                else if (asyncRead.Result != byteXmitBuffer[0])
                {
                    Fail("Err_0158ahei Expected ReadChar to read {0}({0:X}) actual {1}({1:X})", byteXmitBuffer[0], asyncRead.Result);
                }
                else
                {
                    Thread.Sleep(1000); //We need to wait for all of the bytes to be received
                    byteRcvBuffer[0] = (byte)asyncRead.Result;
                    int readResult = com1.Read(byteRcvBuffer, 1, byteRcvBuffer.Length - 1);

                    if (1 + readResult != byteXmitBuffer.Length)
                    {
                        Fail("Err_051884ajoedo Expected Read to read {0} bytes actually read {1}",
                            byteXmitBuffer.Length - 1, readResult);
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
        private void VerifyRead(Encoding encoding)
        {
            VerifyRead(encoding, ReadDataFromEnum.NonBuffered);
        }


        private void VerifyRead(Encoding encoding, ReadDataFromEnum readDataFrom)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random(-55);
                int bufferSize = numRndByte;
                byte[] byteXmitBuffer = new byte[bufferSize];

                //Genrate random bytes
                for (int i = 0; i < byteXmitBuffer.Length; i++)
                {
                    byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
                }

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
                        throw new ArgumentOutOfRangeException(nameof(readDataFrom));
                }
            }
        }


        private void VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, bytesToWrite);
        }


        private void VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            BufferData(com1, com2, bytesToWrite);
            PerformReadOnCom1FromCom2(com1, com2, bytesToWrite);
        }


        private void VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            byte[] expectedBytes = new byte[(2 * bytesToWrite.Length)];

            BufferData(com1, com2, bytesToWrite);
            Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, 0, bytesToWrite.Length);
            Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, bytesToWrite.Length, bytesToWrite.Length);

            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedBytes);
        }


        private void BufferData(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            com2.Write(bytesToWrite, 0, 1); // Write one byte at the begining because we are going to read this to buffer the rest of the data
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);

            TCSupport.WaitForReadBufferToLoad(com1, bytesToWrite.Length);

            com1.Read(new char[1], 0, 1); // This should put the rest of the bytes in SerialPorts own internal buffer

            if (com1.BytesToRead != bytesToWrite.Length)
            {
                Fail("Err_7083zaz Expected com1.BytesToRead={0} actual={1}", bytesToWrite.Length, com1.BytesToRead);
            }
        }

        private void VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] expectedBytes)
        {
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);
            com1.ReadTimeout = 500;

            Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

            PerformReadOnCom1FromCom2(com1, com2, expectedBytes);
        }

        private void PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] expectedBytes)
        {
            byte[] byteRcvBuffer = new byte[expectedBytes.Length];
            int readInt;
            int i;

            i = 0;
            while (true)
            {
                try
                {
                    readInt = com1.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }

                //While their are more bytes to be read
                if (expectedBytes.Length <= i)
                {
                    //If we have read in more bytes then were actually sent
                    Fail("ERROR!!!: We have received more bytes then were sent");
                    break;
                }

                byteRcvBuffer[i] = (byte)readInt;

                if (readInt != expectedBytes[i])
                {
                    //If the byte read is not the expected byte
                    Fail("ERROR!!!: Expected to read {0}  actual read byte {1}", (int)expectedBytes[i], readInt);
                }

                i++;

                if (expectedBytes.Length - i != com1.BytesToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1}", expectedBytes.Length - i, com1.BytesToRead);
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
                    _result = _com.ReadByte();
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

            public AutoResetEvent ReadStartedEvent
            {
                get
                {
                    return _readStartedEvent;
                }
            }

            public AutoResetEvent ReadCompletedEvent
            {
                get
                {
                    return _readCompletedEvent;
                }
            }

            public int Result
            {
                get
                {
                    return _result;
                }
            }

            public Exception Exception
            {
                get
                {
                    return _exception;
                }
            }
        }
        #endregion
    }
}

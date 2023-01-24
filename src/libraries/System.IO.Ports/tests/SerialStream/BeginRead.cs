// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Linq;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_BeginRead : PortsTest
    {
        // The number of random bytes to receive for read method testing
        private const int numRndBytesToRead = 16;

        // The number of random bytes to receive for large input buffer testing
        private const int largeNumRndBytesToRead = 2048;

        // When we test Read and do not care about actually reading anything we must still
        // create an byte array to pass into the method the following is the size of the
        // byte array used in this situation
        private const int defaultByteArraySize = 1;
        private const int defaultByteOffset = 0;
        private const int defaultByteCount = 1;

        // The maximum buffer size when an exception occurs
        private const int maxBufferSizeForException = 255;

        // The maximum buffer size when an exception is not expected
        private const int maxBufferSize = 8;

        // Maximum time to wait for processing the read command to complete
        private const int MAX_WAIT_READ_COMPLETE = 1000;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Buffer_Null()
        {
            VerifyReadException<ArgumentNullException>(null, 0, 1);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEG1()
        {
            VerifyReadException<ArgumentOutOfRangeException>(new byte[defaultByteArraySize], -1, defaultByteCount);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEGRND()
        {
            var rndGen = new Random(-55);
            VerifyReadException<ArgumentOutOfRangeException>(new byte[defaultByteArraySize], rndGen.Next(int.MinValue, 0), defaultByteCount);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_MinInt()
        {
            VerifyReadException<ArgumentOutOfRangeException>(new byte[defaultByteArraySize], int.MinValue, defaultByteCount);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEG1()
        {
            VerifyReadException<ArgumentOutOfRangeException>(new byte[defaultByteArraySize], defaultByteOffset, -1);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEGRND()
        {
            var rndGen = new Random(-55);
            VerifyReadException<ArgumentOutOfRangeException>(new byte[defaultByteArraySize], defaultByteOffset, rndGen.Next(int.MinValue, 0));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_MinInt()
        {
            VerifyReadException<ArgumentOutOfRangeException>(new byte[defaultByteArraySize], defaultByteOffset, int.MinValue);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_EQ_Length_Plus_1()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = bufferLength + 1 - offset;

            VerifyReadException<ArgumentException>(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_GT_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = rndGen.Next(bufferLength + 1 - offset, int.MaxValue);
            VerifyReadException<ArgumentException>(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_GT_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(bufferLength, int.MaxValue);
            int count = defaultByteCount;

            VerifyReadException<ArgumentException>(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_GT_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = defaultByteOffset;
            int count = rndGen.Next(bufferLength + 1, int.MaxValue);

            VerifyReadException<ArgumentException>(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void OffsetCount_EQ_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = bufferLength - offset;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Offset_EQ_Length_Minus_1()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = bufferLength - 1;
            var count = 1;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Count_EQ_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            var offset = 0;
            int count = bufferLength;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void LargeInputBuffer()
        {
            int bufferLength = largeNumRndBytesToRead;
            var offset = 0;
            int count = bufferLength;

            VerifyRead(new byte[bufferLength], offset, count, largeNumRndBytesToRead);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Callback()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var callbackHandler = new CallbackHandler();

                int elapsedTime;

                Debug.WriteLine("Verifying BeginRead with a callback specified");

                com1.Open();
                com2.Open();

                IAsyncResult readAsyncResult = com1.BaseStream.BeginRead(new byte[numRndBytesToRead], 0, numRndBytesToRead,
                    callbackHandler.Callback, null);
                callbackHandler.BeginReadAsyncResult = readAsyncResult;

                Assert.Null(readAsyncResult.AsyncState);
                Assert.False(readAsyncResult.CompletedSynchronously, "Should not have completed sync (read)");
                Assert.False(readAsyncResult.IsCompleted, "Should not have completed yet");

                com2.Write(new byte[numRndBytesToRead], 0, numRndBytesToRead);

                // callbackHandler.ReadAsyncResult  guarantees that the callback has been calledhowever it does not gauarentee that
                // the code calling the callback has finished it's processing
                IAsyncResult callbackReadAsyncResult = callbackHandler.ReadAsyncResult;

                // No we have to wait for the callbackHandler to complete
                elapsedTime = 0;
                while (!callbackReadAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_READ_COMPLETE)
                {
                    Thread.Sleep(10);
                    elapsedTime += 10;
                }

                Assert.Null(callbackReadAsyncResult.AsyncState);
                Assert.False(callbackReadAsyncResult.CompletedSynchronously, "Should not have completed sync (cback)");
                Assert.True(callbackReadAsyncResult.IsCompleted, "Should have completed (cback)");
                Assert.Null(readAsyncResult.AsyncState);
                Assert.False(readAsyncResult.CompletedSynchronously, "Should not have completed sync (read)");
                Assert.True(readAsyncResult.IsCompleted, "Should have completed (read)");
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Callback_EndReadonCallback()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var callbackHandler = new CallbackHandler(com1);

                int elapsedTime;

                Debug.WriteLine("Verifying BeginRead with a callback that calls EndRead");

                com1.Open();
                com2.Open();

                IAsyncResult readAsyncResult = com1.BaseStream.BeginRead(new byte[numRndBytesToRead], 0, numRndBytesToRead,
                    callbackHandler.Callback, null);
                callbackHandler.BeginReadAsyncResult = readAsyncResult;

                Assert.Null(readAsyncResult.AsyncState);
                Assert.False(readAsyncResult.CompletedSynchronously);
                Assert.False(readAsyncResult.IsCompleted);

                com2.Write(new byte[numRndBytesToRead], 0, numRndBytesToRead);

                // callbackHandler.ReadAsyncResult  guarantees that the callback has been calledhowever it does not gauarentee that
                // the code calling the callback has finished it's processing
                IAsyncResult callbackReadAsyncResult = callbackHandler.ReadAsyncResult;

                // No we have to wait for the callbackHandler to complete
                elapsedTime = 0;
                while (!callbackReadAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_READ_COMPLETE)
                {
                    Thread.Sleep(10);
                    elapsedTime += 10;
                }

                Assert.Null(callbackReadAsyncResult.AsyncState);
                Assert.False(callbackReadAsyncResult.CompletedSynchronously, "Should not have completed sync (cback)");
                Assert.True(callbackReadAsyncResult.IsCompleted, "Should have completed (cback)");
                Assert.Null(readAsyncResult.AsyncState);
                Assert.False(readAsyncResult.CompletedSynchronously, "Should not have completed sync (read)");
                Assert.True(readAsyncResult.IsCompleted, "Should have completed (read)");
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void Callback_State()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var callbackHandler = new CallbackHandler();

                int elapsedTime;

                Debug.WriteLine("Verifying BeginRead with a callback and state specified");

                com1.Open();
                com2.Open();

                IAsyncResult readAsyncResult = com1.BaseStream.BeginRead(new byte[numRndBytesToRead], 0, numRndBytesToRead,
                    callbackHandler.Callback, this);
                callbackHandler.BeginReadAsyncResult = readAsyncResult;
                Assert.Equal(this, readAsyncResult.AsyncState);
                Assert.False(readAsyncResult.CompletedSynchronously);
                Assert.False(readAsyncResult.IsCompleted);

                com2.Write(new byte[numRndBytesToRead], 0, numRndBytesToRead);

                // callbackHandler.ReadAsyncResult  guarantees that the callback has been calledhowever it does not gauarentee that
                // the code calling the callback has finished it's processing
                IAsyncResult callbackReadAsyncResult = callbackHandler.ReadAsyncResult;

                // No we have to wait for the callbackHandler to complete
                elapsedTime = 0;
                while (!callbackReadAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_READ_COMPLETE)
                {
                    Thread.Sleep(10);
                    elapsedTime += 10;
                }

                Assert.Equal(this, callbackReadAsyncResult.AsyncState);
                Assert.False(callbackReadAsyncResult.CompletedSynchronously);
                Assert.True(callbackReadAsyncResult.IsCompleted);
                Assert.Equal(this, readAsyncResult.AsyncState);
                Assert.False(readAsyncResult.CompletedSynchronously);
                Assert.True(readAsyncResult.IsCompleted);
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyReadException<T>(byte[] buffer, int offset, int count) where T : Exception
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int bufferLength = null == buffer ? 0 : buffer.Length;

                Debug.WriteLine("Verifying read method throws {0} buffer.Length={1}, offset={2}, count={3}",
                                typeof(T), bufferLength, offset, count);

                com.Open();

                Action a = () => com.BaseStream.BeginRead(buffer, offset, count, null, null);
                Assert.Throws<T>(a);
            }
        }

        private void VerifyRead(byte[] buffer, int offset, int count)
        {
            VerifyRead(buffer, offset, count, numRndBytesToRead);
        }

        private void VerifyRead(byte[] buffer, int offset, int count, int numberOfBytesToRead)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var rndGen = new Random(-55);
                var bytesToWrite = new byte[numberOfBytesToRead];

                // Generate random bytes
                for (var i = 0; i < bytesToWrite.Length; i++)
                {
                    var randByte = (byte)rndGen.Next(0, 256);

                    bytesToWrite[i] = randByte;
                }

                // Generate some random bytes in the buffer
                rndGen.NextBytes(buffer);

                Debug.WriteLine("Verifying read method buffer.Length={0}, offset={1}, count={2} with {3} random chars", buffer.Length, offset, count, bytesToWrite.Length);

                com1.ReadTimeout = 500;

                com1.Open();
                com2.Open();

                VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, buffer, offset, count);
            }
        }

        private void VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
        {
            var buffer = new byte[bytesToWrite.Length];
            int totalBytesRead;
            int bytesToRead;
            var oldRcvBuffer = (byte[])rcvBuffer.Clone();
            var callbackHandler = new CallbackHandler();

            com2.Write(bytesToWrite, 0, bytesToWrite.Length);
            com1.ReadTimeout = 500;
            Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);
            totalBytesRead = 0;
            bytesToRead = com1.BytesToRead;

            do
            {
                IAsyncResult readAsyncResult = com1.BaseStream.BeginRead(rcvBuffer, offset, count,
                    callbackHandler.Callback, this);
                readAsyncResult.AsyncWaitHandle.WaitOne();
                callbackHandler.BeginReadAsyncResult = readAsyncResult;

                int bytesRead = com1.BaseStream.EndRead(readAsyncResult);
                IAsyncResult asyncResult = callbackHandler.ReadAsyncResult;
                Assert.Equal(this, asyncResult.AsyncState);
                Assert.False(asyncResult.CompletedSynchronously);
                Assert.True(asyncResult.IsCompleted);
                Assert.Equal(this, readAsyncResult.AsyncState);
                Assert.False(readAsyncResult.CompletedSynchronously);
                Assert.True(readAsyncResult.IsCompleted);

                if ((bytesToRead > bytesRead && count != bytesRead) ||
                    (bytesToRead <= bytesRead && bytesRead != bytesToRead))
                {
                    // If we have not read all of the characters that we should have
                    Fail("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                }

                if (bytesToWrite.Length < totalBytesRead + bytesRead)
                {
                    // If we have read in more characters then we expect
                    Fail("ERROR!!!: We have received more characters then were sent");
                }

                VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, bytesRead);

                Array.Copy(rcvBuffer, offset, buffer, totalBytesRead, bytesRead);
                totalBytesRead += bytesRead;

                if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1}", bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                }

                oldRcvBuffer = (byte[])rcvBuffer.Clone();
                bytesToRead = com1.BytesToRead;
            } while (0 != com1.BytesToRead); // While there are more bytes to read

            // Compare the bytes that were written with the ones we read
            Assert.Equal(bytesToWrite, buffer.Take(bytesToWrite.Length).ToArray());
        }

        private void VerifyBuffer(byte[] actualBuffer, byte[] expectedBuffer, int offset, int count)
        {
            // Verify all character before the offset
            for (var i = 0; i < offset; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }

            // Verify all character after the offset + count
            for (int i = offset + count; i < actualBuffer.Length; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }
        }

        private class CallbackHandler
        {
            private IAsyncResult _readAsyncResult;
            private IAsyncResult _beginReadAsyncResult;
            private readonly SerialPort _com;

            public CallbackHandler() : this(null) { }

            public CallbackHandler(SerialPort com)
            {
                _com = com;
            }

            public void Callback(IAsyncResult readAsyncResult)
            {
                lock (this)
                {
                    _readAsyncResult = readAsyncResult;

                    Assert.True(readAsyncResult.IsCompleted, "IAsyncResult passed into callback is not completed");

                    while (null == _beginReadAsyncResult)
                    {
                        Monitor.Wait(this);
                    }

                    if (null != _beginReadAsyncResult && !_beginReadAsyncResult.IsCompleted)
                    {
                        Fail("Err_7907azpu Expected IAsyncResult returned from begin read to not be completed");
                    }

                    if (null != _com)
                    {
                        _com.BaseStream.EndRead(_beginReadAsyncResult);
                        if (!_beginReadAsyncResult.IsCompleted)
                        {
                            Fail("Err_6498afead Expected IAsyncResult returned from begin read to not be completed");
                        }

                        if (!readAsyncResult.IsCompleted)
                        {
                            Fail("Err_1398ehpo Expected IAsyncResult passed into callback to not be completed");
                        }
                    }

                    Monitor.Pulse(this);
                }
            }


            public IAsyncResult ReadAsyncResult
            {
                get
                {
                    lock (this)
                    {
                        while (null == _readAsyncResult)
                        {
                            Monitor.Wait(this);
                        }

                        return _readAsyncResult;
                    }
                }
            }

            public IAsyncResult BeginReadAsyncResult
            {
                get
                {
                    return _beginReadAsyncResult;
                }
                set
                {
                    lock (this)
                    {
                        _beginReadAsyncResult = value;
                        Monitor.Pulse(this);
                    }
                }
            }
        }

        #endregion
    }
}

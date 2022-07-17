// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_BeginWrite : PortsTest
    {
        // The string size used for large byte array testing
        private const int LARGE_BUFFER_SIZE = 2048;

        // When we test Write and do not care about actually writing anything we must still
        // create an byte array to pass into the method the following is the size of the
        // byte array used in this situation
        private const int DEFAULT_BUFFER_SIZE = 1;
        private const int DEFAULT_BUFFER_OFFSET = 0;
        private const int DEFAULT_BUFFER_COUNT = 1;

        // The maximum buffer size when an exception occurs
        private const int MAX_BUFFER_SIZE_FOR_EXCEPTION = 255;

        // The maximum buffer size when an exception is not expected
        private const int MAX_BUFFER_SIZE = 8;

        // The default number of times the write method is called when verifying write
        private const int DEFAULT_NUM_WRITES = 3;

        // The default number of bytes to write
        private const int DEFAULT_NUM_BYTES_TO_WRITE = 128;

        // Maximum time to wait for processing the read command to complete
        private const int MAX_WAIT_WRITE_COMPLETE = 1000;

        #region Test Cases
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Buffer_Null()
        {
            VerifyWriteException(null, 0, 1, typeof(ArgumentNullException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEG1()
        {
            VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], -1, DEFAULT_BUFFER_COUNT, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEGRND()
        {
            Random rndGen = new Random(-55);

            VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], rndGen.Next(int.MinValue, 0), DEFAULT_BUFFER_COUNT, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_MinInt()
        {
            VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], int.MinValue, DEFAULT_BUFFER_COUNT, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEG1()
        {
            VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], DEFAULT_BUFFER_OFFSET, -1, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEGRND()
        {
            Random rndGen = new Random(-55);

            VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], DEFAULT_BUFFER_OFFSET, rndGen.Next(int.MinValue, 0), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_MinInt()
        {
            VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], DEFAULT_BUFFER_OFFSET, int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_EQ_Length_Plus_1()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = rndGen.Next(0, bufferLength);
            int count = bufferLength + 1 - offset;
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = rndGen.Next(0, bufferLength);
            int count = rndGen.Next(bufferLength + 1 - offset, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = rndGen.Next(bufferLength, int.MaxValue);
            int count = DEFAULT_BUFFER_COUNT;
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = DEFAULT_BUFFER_OFFSET;
            int count = rndGen.Next(bufferLength + 1, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void OffsetCount_EQ_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = bufferLength - offset;

            VerifyWrite(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Offset_EQ_Length_Minus_1()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = bufferLength - 1;
            int count = 1;

            VerifyWrite(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Count_EQ_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = 0;
            int count = bufferLength;

            VerifyWrite(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ASCIIEncoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new byte[bufferLength], offset, count, new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UTF8Encoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new byte[bufferLength], offset, count, new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UTF32Encoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new byte[bufferLength], offset, count, new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UnicodeEncoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new byte[bufferLength], offset, count, new UnicodeEncoding());
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void LargeBuffer()
        {
            int bufferLength = LARGE_BUFFER_SIZE;
            int offset = 0;
            int count = bufferLength;

            VerifyWrite(new byte[bufferLength], offset, count, 1);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Callback()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                CallbackHandler callbackHandler = new CallbackHandler();

                Debug.WriteLine("Verifying BeginWrite with a callback specified");

                com1.Handshake = Handshake.RequestToSend;
                com2.ReadTimeout = 300;
                com1.Open();
                com2.Open();

                // RTS allows us to control when driver sends the data but does not
                // guarantee that data will not be consumed by driver
                // we can check if data was received on the other side though
                Action read = () => {
                    com2.BaseStream.Read(new byte[DEFAULT_NUM_BYTES_TO_WRITE], 0, DEFAULT_NUM_BYTES_TO_WRITE);
                };

                IAsyncResult writeAsyncResult = com1.BaseStream.BeginWrite(new byte[DEFAULT_NUM_BYTES_TO_WRITE], 0,
                    DEFAULT_NUM_BYTES_TO_WRITE, callbackHandler.Callback, this);
                callbackHandler.BeginWriteAsyncResult = writeAsyncResult;

                Assert.Equal(this, writeAsyncResult.AsyncState);

                Thread.Sleep(100);
                Assert.Throws<TimeoutException>(read);
                com2.RtsEnable = true;
                read();

                // callbackHandler.WriteAsyncResult guarantees that the callback has been called however it does not gauarentee that
                // the code calling the callback has finished it's processing
                IAsyncResult callbackWriteAsyncResult = callbackHandler.WriteAsyncResult;

                // No we have to wait for the callbackHandler to complete
                int elapsedTime = 0;
                while (!callbackWriteAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_WRITE_COMPLETE)
                {
                    Thread.Sleep(10);
                    elapsedTime += 10;
                }

                Assert.Equal(this, callbackWriteAsyncResult.AsyncState);
                Assert.False(callbackWriteAsyncResult.CompletedSynchronously, "Should not have completed sync (cback)");
                Assert.True(callbackWriteAsyncResult.IsCompleted, "Should have completed (cback)");
                Assert.Equal(this, writeAsyncResult.AsyncState);
                Assert.False(writeAsyncResult.CompletedSynchronously, "Should not have completed sync (write)");
                Assert.True(writeAsyncResult.IsCompleted, "Should have completed (write)");
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Callback_State()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                CallbackHandler callbackHandler = new CallbackHandler();

                Debug.WriteLine("Verifying BeginWrite with a callback and state specified");
                com1.Open();
                com2.Open();

                Action read = () => {
                    com2.BaseStream.Read(new byte[DEFAULT_NUM_BYTES_TO_WRITE], 0, DEFAULT_NUM_BYTES_TO_WRITE);
                };

                IAsyncResult writeAsyncResult = com1.BaseStream.BeginWrite(new byte[DEFAULT_NUM_BYTES_TO_WRITE], 0, DEFAULT_NUM_BYTES_TO_WRITE, callbackHandler.Callback, this);
                callbackHandler.BeginWriteAsyncResult = writeAsyncResult;

                Assert.Throws<TimeoutException>(read);
                com2.RtsEnable = true;
                read();

                // callbackHandler.WriteAsyncResult guarantees that the callback has been called however it does not gauarentee that
                // the code calling the callback has finished it's processing
                IAsyncResult callbackWriteAsyncResult = callbackHandler.WriteAsyncResult;

                // No we have to wait for the callbackHandler to complete
                int elapsedTime = 0;
                while (!callbackWriteAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_WRITE_COMPLETE)
                {
                    Thread.Sleep(10);
                    elapsedTime += 10;
                }

                Assert.Equal(this, callbackWriteAsyncResult.AsyncState);
                Assert.False(writeAsyncResult.CompletedSynchronously, "Should not have completed sync (cback)");
                Assert.True(callbackWriteAsyncResult.IsCompleted, "Should have completed (cback)");
                Assert.Equal(this, writeAsyncResult.AsyncState);
                Assert.False(writeAsyncResult.CompletedSynchronously, "Should not have completed sync (write)");
                Assert.True(writeAsyncResult.IsCompleted, "Should have completed (write)");
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void InBreak()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying BeginWrite throws InvalidOperationException while in a Break");
                com1.Open();
                com1.BreakState = true;

                Assert.Throws<InvalidOperationException>(() => com1.BaseStream.BeginWrite(new byte[8], 0, 8, null, null));
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyWriteException(byte[] buffer, int offset, int count, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int bufferLength = null == buffer ? 0 : buffer.Length;

                Debug.WriteLine("Verifying write method throws {0} buffer.Length={1}, offset={2}, count={3}",
                    expectedException, bufferLength, offset, count);
                com.Open();

                Assert.Throws(expectedException, () => com.BaseStream.BeginWrite(buffer, offset, count, null, null));
            }
        }

        private void VerifyWrite(byte[] buffer, int offset, int count)
        {
            VerifyWrite(buffer, offset, count, new ASCIIEncoding());
        }

        private void VerifyWrite(byte[] buffer, int offset, int count, int numWrites)
        {
            VerifyWrite(buffer, offset, count, new ASCIIEncoding(), numWrites);
        }

        private void VerifyWrite(byte[] buffer, int offset, int count, Encoding encoding)
        {
            VerifyWrite(buffer, offset, count, encoding, DEFAULT_NUM_WRITES);
        }

        private void VerifyWrite(byte[] buffer, int offset, int count, Encoding encoding, int numWrites)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Random rndGen = new Random(-55);

                Debug.WriteLine("Verifying write method buffer.Length={0}, offset={1}, count={2}, endocing={3}",
                    buffer.Length, offset, count, encoding.EncodingName);

                com1.Encoding = encoding;
                com2.Encoding = encoding;

                com1.Open();
                com2.Open();

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)rndGen.Next(0, 256);
                }

                VerifyWriteByteArray(buffer, offset, count, com1, com2, numWrites);
            }
        }

        private void VerifyWriteByteArray(byte[] buffer, int offset, int count, SerialPort com1, SerialPort com2, int numWrites)
        {
            int index = 0;
            CallbackHandler callbackHandler = new CallbackHandler();

            var oldBuffer = (byte[])buffer.Clone();
            var expectedBytes = new byte[count];
            var actualBytes = new byte[expectedBytes.Length * numWrites];

            for (int i = 0; i < count; i++)
            {
                expectedBytes[i] = buffer[i + offset];
            }

            for (int i = 0; i < numWrites; i++)
            {
                IAsyncResult writeAsyncResult = com1.BaseStream.BeginWrite(buffer, offset, count, callbackHandler.Callback, this);
                com1.BaseStream.EndWrite(writeAsyncResult);
                callbackHandler.BeginWriteAsyncResult = writeAsyncResult;

                IAsyncResult callbackWriteAsyncResult = callbackHandler.WriteAsyncResult;
                Assert.Equal(this, callbackWriteAsyncResult.AsyncState);
                Assert.False(callbackWriteAsyncResult.CompletedSynchronously, "Should not have completed sync (cback)");
                Assert.True(callbackWriteAsyncResult.IsCompleted, "Should have completed (cback)");
                Assert.Equal(this, writeAsyncResult.AsyncState);
                Assert.False(writeAsyncResult.CompletedSynchronously, "Should not have completed sync (write)");
                Assert.True(writeAsyncResult.IsCompleted, "Should have completed (write)");
            }

            com2.ReadTimeout = 500;
            Thread.Sleep((int)(((expectedBytes.Length * numWrites * 10.0) / com1.BaudRate) * 1000) + 250);

            // Make sure buffer was not altered during the write call
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != oldBuffer[i])
                {
                    Fail("ERROR!!!: The contents of the buffer were changed from {0} to {1} at {2}", oldBuffer[i], buffer[i], i);
                }
            }

            while (true)
            {
                int byteRead;
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
                    // If we have read in more bytes then we expect
                    Fail("ERROR!!!: We have received more bytes then were sent");
                    break;
                }

                actualBytes[index] = (byte)byteRead;
                index++;
                if (actualBytes.Length - index != com2.BytesToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1}", actualBytes.Length - index, com2.BytesToRead);
                }
            }

            // Compare the bytes that were read with the ones we expected to read
            for (int j = 0; j < numWrites; j++)
            {
                for (int i = 0; i < expectedBytes.Length; i++)
                {
                    if (expectedBytes[i] != actualBytes[i + expectedBytes.Length * j])
                    {
                        Fail("ERROR!!!: Expected to read byte {0}  actual read {1} at {2}", (int)expectedBytes[i], (int)actualBytes[i + expectedBytes.Length * j], i);
                    }
                }
            }
        }

        private class CallbackHandler
        {
            private IAsyncResult _writeAsyncResult;
            private IAsyncResult _beginWriteAsyncResult;
            private readonly SerialPort _com;

            public CallbackHandler() : this(null) { }

            private CallbackHandler(SerialPort com)
            {
                _com = com;
            }

            public void Callback(IAsyncResult writeAsyncResult)
            {
                Debug.WriteLine("About to enter callback lock (already entered {0})", Monitor.IsEntered(this));
                lock (this)
                {
                    Debug.WriteLine("Inside callback lock");
                    _writeAsyncResult = writeAsyncResult;

                    if (!writeAsyncResult.IsCompleted)
                    {
                        throw new Exception("Err_23984afaea Expected IAsyncResult passed into callback to not be completed");
                    }

                    while (null == _beginWriteAsyncResult)
                    {
                        Assert.True(Monitor.Wait(this, 5000), "Monitor.Wait in Callback");
                    }

                    if (null != _beginWriteAsyncResult && !_beginWriteAsyncResult.IsCompleted)
                    {
                        throw new Exception("Err_7907azpu Expected IAsyncResult returned from begin write to not be completed");
                    }

                    if (null != _com)
                    {
                        _com.BaseStream.EndWrite(_beginWriteAsyncResult);
                        if (!_beginWriteAsyncResult.IsCompleted)
                        {
                            throw new Exception("Err_6498afead Expected IAsyncResult returned from begin write to not be completed");
                        }

                        if (!writeAsyncResult.IsCompleted)
                        {
                            throw new Exception("Err_1398ehpo Expected IAsyncResult passed into callback to not be completed");
                        }
                    }

                    Monitor.Pulse(this);
                }
            }


            public IAsyncResult WriteAsyncResult
            {
                get
                {
                    lock (this)
                    {
                        while (null == _writeAsyncResult)
                        {
                            Monitor.Wait(this);
                        }

                        return _writeAsyncResult;
                    }
                }
            }

            public IAsyncResult BeginWriteAsyncResult
            {
                get
                {
                    return _beginWriteAsyncResult;
                }
                set
                {
                    lock (this)
                    {
                        _beginWriteAsyncResult = value;
                        Monitor.Pulse(this);
                    }
                }
            }
        }

        #endregion
    }
}

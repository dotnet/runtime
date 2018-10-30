// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.IO.Ports.Tests
{
    public class SerialStream_WriteTimeout_Property : PortsTest
    {
        // The default number of bytes to write with when testing timeout with Write(byte[], int, int)
        private static readonly int s_DEFAULT_WRITE_BYTE_ARRAY_SIZE = TCSupport.MinimumBlockingByteCount;

        // The large number of bytes to write with when testing timeout with Write(byte[], int, int)
        // This needs to be large enough for Write timeout
        private const int DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE = 1024 * 100;

        // The BaudRate to use to make Write timeout when writing DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE bytes
        private const int LARGEWRITE_BAUDRATE = 1200;

        // The timeout to use to make Write timeout when writing DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE
        private const int LARGEWRITE_TIMEOUT = 750;

        // The default byte to call with WriteByte
        private const byte DEFAULT_WRITE_BYTE = 33;

        // The amount of time to wait when expecting an long timeout
        private const int DEFAULT_WAIT_LONG_TIMEOUT = 250;

        // The maximum acceptable time allowed when a write method should timeout immediately
        private const int MAX_ACCEPTABLE_ZERO_TIMEOUT = 100;

        // The maximum acceptable time allowed when a write method should timeout immediately when it is called for the first time
        private const int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 1000;

        // The maximum acceptable percentage difference allowed when a write method is called for the first time
        private const double MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE = .5;

        // The maximum acceptable percentage difference allowed
        private const double MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE = .15;

        private const int SUCCESSIVE_WriteTimeout_SOMEDATA = 950;

        private const int NUM_TRYS = 5;

        private delegate void WriteMethodDelegate(Stream stream);

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteTimeout_DefaultValue()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream stream = com.BaseStream;

                Debug.WriteLine("Verifying the default value of WriteTimeout");

                Assert.True(stream.WriteTimeout == -1,
                    string.Format(
                        "Err_1707azhpbn Verifying the default value of WriteTimeout Expected={0} Actual={1} FAILED", -1,
                        stream.WriteTimeout));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteTimeout_AfterClose()
        {
            Debug.WriteLine("Verifying setting WriteTimeout after the SerialPort was closed");

            VerifyException(2048, null, typeof(ObjectDisposedException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteTimeout_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue WriteTimeout");

            VerifyException(int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteTimeout_NEG2()
        {
            Debug.WriteLine("Verifying -2 WriteTimeout");

            VerifyException(-2, typeof(ArgumentOutOfRangeException));
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteTimeout_ZERO()
        {
            Debug.WriteLine("Verifying 0 WriteTimeout");

            VerifyException(0, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Default_Write_byte_int_int()
        {
            Debug.WriteLine("Verifying default WriteTimeout with Write(byte[] buffer, int offset, int count)");

            VerifyDefaultTimeout(Write_byte_int_int);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasSingleByteTransmitBlocking))]
        public void WriteTimeout_Default_WriteByte()
        {
            Debug.WriteLine("Verifying default WriteTimeout with WriteByte()");

            VerifyDefaultTimeout(WriteByte);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Infinite_Write_byte_int_int()
        {
            Debug.WriteLine("Verifying infinite WriteTimeout with Write(byte[] buffer, int offset, int count)");

            VerifyLongTimeout(Write_byte_int_int, -1);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasSingleByteTransmitBlocking))]
        public void WriteTimeout_Infinite_WriteByte()
        {
            Debug.WriteLine("Verifying infinite WriteTimeout with WriteByte()");

            VerifyLongTimeout(WriteByte, -1);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Int32MaxValue_Write_byte_int_int()
        {
            Debug.WriteLine("Verifying Int32.MaxValue WriteTimeout with Write(byte[] buffer, int offset, int count)");

            VerifyLongTimeout(Write_byte_int_int, int.MaxValue - 1);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasSingleByteTransmitBlocking))]
        public void WriteTimeout_Int32MaxValue_WriteByte()
        {
            Debug.WriteLine("Verifying Int32.MaxValue WriteTimeout with WriteByte()");

            VerifyLongTimeout(WriteByte, int.MaxValue - 1);
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_750_Write_byte_int_int()
        {
            Debug.WriteLine("Verifying 750 WriteTimeout with Write(byte[] buffer, int offset, int count)");

            VerifyTimeout(Write_byte_int_int, 750);
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasSingleByteTransmitBlocking))]
        public void WriteTimeout_750_WriteByte()
        {
            Debug.WriteLine("Verifying 750 WriteTimeout with WriteByte()");

            VerifyTimeout(WriteByte, 750);
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void SuccessiveWriteTimeoutNoData_Write_byte_int_int()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                com.Handshake = Handshake.RequestToSend;
                Stream stream = com.BaseStream;
                stream.WriteTimeout = 850;

                Debug.WriteLine("Verifying WriteTimeout={0} with successive call to Write(byte[], int, int) and no data", stream.WriteTimeout);

                Assert.Throws<TimeoutException>(() => stream.Write(new byte[s_DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, s_DEFAULT_WRITE_BYTE_ARRAY_SIZE));

                VerifyTimeout(Write_byte_int_int, stream);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void SuccessiveWriteTimeoutSomeData_Write_byte_int_int()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var asyncEnableRts = new AsyncEnableRts();
                var t = new Task(asyncEnableRts.EnableRTS);

                com1.Open();
                com1.Handshake = Handshake.RequestToSend;
                Stream stream = com1.BaseStream;
                stream.WriteTimeout = SUCCESSIVE_WriteTimeout_SOMEDATA;

                Debug.WriteLine(
                    "Verifying WriteTimeout={0} with successive call to Write(byte[], int, int) and some data being received in the first call",
                    stream.WriteTimeout);

                // Call EnableRTS asynchronously this will enable RTS in the middle of the following write call allowing it to succeed
                // before the timeout is reached
                t.Start();
                TCSupport.WaitForTaskToStart(t);
                try
                {
                    stream.Write(new byte[s_DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, s_DEFAULT_WRITE_BYTE_ARRAY_SIZE);
                }
                catch (TimeoutException)
                {
                }

                asyncEnableRts.Stop();

                TCSupport.WaitForTaskCompletion(t);

                // Make sure there is no bytes in the buffer so the next call to write will timeout
                com1.DiscardInBuffer();

                VerifyTimeout(Write_byte_int_int, stream);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasSingleByteTransmitBlocking))]
        public void SuccessiveWriteTimeoutNoData_WriteByte()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                com.Handshake = Handshake.RequestToSend;
                Stream stream = com.BaseStream;
                stream.WriteTimeout = 850;

                Debug.WriteLine("Verifying WriteTimeout={0} with successive call to WriteByte() and no data",
                    stream.WriteTimeout);

                Assert.Throws<TimeoutException>(() => stream.WriteByte(DEFAULT_WRITE_BYTE));

                VerifyTimeout(WriteByte, stream);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void SuccessiveWriteTimeoutSomeData_WriteByte()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var asyncEnableRts = new AsyncEnableRts();
                var t = new Task(asyncEnableRts.EnableRTS);

                com1.Open();
                com1.Handshake = Handshake.RequestToSend;
                Stream stream = com1.BaseStream;
                stream.WriteTimeout = SUCCESSIVE_WriteTimeout_SOMEDATA;

                Debug.WriteLine(
                    "Verifying WriteTimeout={0} with successive call to WriteByte() and some data being received in the first call",
                    stream.WriteTimeout);

                // Call EnableRTS asynchronously this will enable RTS in the middle of the following write call allowing it to succeed
                // before the timeout is reached
                t.Start();
                TCSupport.WaitForTaskToStart(t);
                try
                {
                    stream.WriteByte(DEFAULT_WRITE_BYTE);
                }
                catch (TimeoutException)
                {
                }

                asyncEnableRts.Stop();

                TCSupport.WaitForTaskCompletion(t);

                // Make sure there is no bytes in the buffer so the next call to write will timeout
                com1.DiscardInBuffer();

                VerifyTimeout(WriteByte, stream);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_0_Write_byte_int_int()
        {
            Debug.WriteLine("Verifying 0 WriteTimeout with Write(byte[] buffer, int offset, int count)");
            Verify0Timeout(Write_byte_int_int);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_0_WriteByte()
        {
            Debug.WriteLine("Verifying 0 WriteTimeout with WriteByte()");
            Verify0Timeout(WriteByte);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_LargeWrite()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                com.BaudRate = LARGEWRITE_BAUDRATE;
                com.Handshake = Handshake.RequestToSend;
                Stream stream = com.BaseStream;
                stream.WriteTimeout = LARGEWRITE_TIMEOUT;

                Debug.WriteLine("Verifying {0} WriteTimeout with Write(byte[] , int, int) and writing {1} bytes", LARGEWRITE_TIMEOUT, DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE);

                VerifyTimeout(Write_byte_int_int_Large, stream);
            }
        }

        private class AsyncEnableRts
        {
            private bool _stop;

            public void EnableRTS()
            {
                lock (this)
                {
                    using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
                    {
                        int sleepPeriod = SUCCESSIVE_WriteTimeout_SOMEDATA / 2;

                        // Sleep some random period with of a maximum duration of half the largest possible timeout value for a write method on COM1
                        Thread.Sleep(sleepPeriod);

                        com2.Open();
                        com2.RtsEnable = true;

                        while (!_stop)
                            Monitor.Wait(this);

                        com2.RtsEnable = false;
                    }
                }
            }

            public void Stop()
            {
                lock (this)
                {
                    _stop = true;
                    Monitor.Pulse(this);
                }
            }
        }

        private void VerifyDefaultTimeout(WriteMethodDelegate writeMethod)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            {
                Debug.WriteLine("Serial port being used : " + com1.PortName);


                com1.Open();
                com1.Handshake = Handshake.RequestToSend;
                com1.BaseStream.ReadTimeout = 1;

                Assert.Equal(-1, com1.BaseStream.WriteTimeout);

                VerifyLongTimeout(writeMethod, com1);
            }
        }

        private void VerifyLongTimeout(WriteMethodDelegate writeMethod, int writeTimeout)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            {
                com1.Open();
                com1.Handshake = Handshake.RequestToSend;
                com1.BaseStream.ReadTimeout = 1;

                com1.BaseStream.WriteTimeout = writeTimeout;
                Assert.Equal(writeTimeout, com1.BaseStream.WriteTimeout);

                VerifyLongTimeout(writeMethod, com1);
            }
        }

        private void VerifyLongTimeout(WriteMethodDelegate writeMethod, SerialPort com1)
        {
            var t = new Task(() => { writeMethod(com1.BaseStream); });

            t.Start();
            Thread.Sleep(DEFAULT_WAIT_LONG_TIMEOUT);
            Assert.False(t.IsCompleted,
                string.Format("Err_17071ahpa!!! {0} terminated with a long timeout of {1}ms", writeMethod.Method.Name, com1.BaseStream.WriteTimeout));
            com1.Handshake = Handshake.None;
            TCSupport.WaitForTaskCompletion(t);
        }

        private void VerifyTimeout(WriteMethodDelegate writeMethod, int WriteTimeout)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))

            {
                com1.Open();
                com1.Handshake = Handshake.RequestToSend;
                com1.BaseStream.ReadTimeout = 1;
                com1.BaseStream.WriteTimeout = 1;

                com1.BaseStream.WriteTimeout = WriteTimeout;

                Assert.Equal(WriteTimeout, com1.BaseStream.WriteTimeout);

                VerifyTimeout(writeMethod, com1.BaseStream);
            }
        }

        private void VerifyTimeout(WriteMethodDelegate writeMethod, Stream stream)
        {
            var timer = new Stopwatch();
            int expectedTime = stream.WriteTimeout;
            int actualTime;
            double percentageDifference;


            // Warmup the write method. When called for the first time the write method seems to take much longer then subsequent calls
            timer.Start();
            try
            {
                writeMethod(stream);
            }
            catch (TimeoutException) { }
            timer.Stop();
            actualTime = (int)timer.ElapsedMilliseconds;
            percentageDifference = Math.Abs((expectedTime - actualTime) / (double)expectedTime);

            // Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
            Assert.True(percentageDifference <= MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE,
                string.Format("Err_88558amuph!!!: The write method timedout in {0} expected {1} percentage difference: {2} when called for the first time",
                    actualTime, expectedTime, percentageDifference));

            actualTime = 0;
            timer.Reset();

            // Perform the actual test verifying that the write method times out in approximately WriteTimeout milliseconds
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (var i = 0; i < NUM_TRYS; i++)
            {
                timer.Start();
                try { writeMethod(stream); }
                catch (TimeoutException) { }
                timer.Stop();

                actualTime += (int)timer.ElapsedMilliseconds;
                timer.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;
            percentageDifference = Math.Abs((expectedTime - actualTime) / (double)expectedTime);

            // Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
            Assert.True(percentageDifference <= MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE,
                string.Format("Err_56485ahpbz!!!: The write method timedout in {0} expected {1} percentage difference: {2}", actualTime, expectedTime, percentageDifference));
        }

        private void Verify0Timeout(WriteMethodDelegate writeMethod)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))

            {
                com1.Open();
                com1.Handshake = Handshake.RequestToSend;
                com1.BaseStream.ReadTimeout = 1;
                com1.BaseStream.WriteTimeout = 1;

                com1.BaseStream.WriteTimeout = 1;

                Assert.Equal(1, com1.BaseStream.WriteTimeout);

                Verify0Timeout(writeMethod, com1.BaseStream);
            }
        }

        private void Verify0Timeout(WriteMethodDelegate writeMethod, Stream stream)
        {
            var timer = new Stopwatch();
            int actualTime;

            // Warmup the write method. When called for the first time the write method seems to take much longer then subsequent calls
            timer.Start();
            try
            {
                writeMethod(stream);
            }
            catch (TimeoutException) { }
            timer.Stop();
            actualTime = (int)timer.ElapsedMilliseconds;

            // Verify that the time the method took to timeout is less then the maximum acceptable time
            Assert.True(actualTime <= MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT,
                string.Format("Err_277a0ahpsb!!!: With a timeout of 0 the write method timedout in {0} expected something less then {1} when called for the first time",
                    actualTime, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT));

            actualTime = 0;
            timer.Reset();

            // Perform the actual test verifying that the write method times out in approximately WriteTimeout milliseconds
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (var i = 0; i < NUM_TRYS; i++)
            {
                timer.Start();
                try { writeMethod(stream); }
                catch (TimeoutException) { }
                timer.Stop();

                actualTime += (int)timer.ElapsedMilliseconds;
                timer.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;

            // Verify that the time the method took to timeout is less then the maximum acceptable time
            Assert.True(actualTime <= MAX_ACCEPTABLE_ZERO_TIMEOUT,
                string.Format("Err_112389ahbp!!!: With a timeout of 0 the write method timedout in {0} expected something less then {1}",
                    actualTime, MAX_ACCEPTABLE_ZERO_TIMEOUT));
        }

        private void VerifyException(int readTimeout, Type expectedException)
        {
            VerifyException(readTimeout, expectedException, expectedException);
        }

        private void VerifyException(int readTimeout, Type expectedExceptionAfterOpen, Type expectedExceptionAfterClose)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream stream = com.BaseStream;

                VerifyException(stream, readTimeout, expectedExceptionAfterOpen);

                com.Close();

                VerifyException(stream, readTimeout, expectedExceptionAfterClose);
            }
        }

        private void VerifyException(Stream stream, int WriteTimeout, Type expectedException)
        {
            int origWriteTimeout = stream.WriteTimeout;

            if (expectedException == null)
            {
                stream.WriteTimeout = WriteTimeout;
                Assert.Equal(WriteTimeout, stream.WriteTimeout);
            }
            else
            {
                Assert.Throws(expectedException, () => stream.WriteTimeout = WriteTimeout);
                Assert.Equal(origWriteTimeout, stream.WriteTimeout);
            }
        }

        private void Write_byte_int_int(Stream stream)
        {
            stream.Write(new byte[s_DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, s_DEFAULT_WRITE_BYTE_ARRAY_SIZE);
        }

        private void Write_byte_int_int_Large(Stream stream)
        {
            stream.Write(new byte[DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE);
        }

        private void WriteByte(Stream stream)
        {
            stream.WriteByte(DEFAULT_WRITE_BYTE);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.IO.Ports.Tests
{
    public class SerialStream_ReadTimeout_Property : PortsTest
    {
        // The default number of bytes to write with when testing timeout with Read(byte[], int, int)
        private const int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;

        // The amount of time to wait when expecting an long timeout
        private const int DEFAULT_WAIT_LONG_TIMEOUT = 250;

        // The maximum acceptable time allowed when a read method should timeout immediately when it is called for the first time
        private const int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 1000;

        // The maximum acceptable percentage difference allowed when a read method is called for the first time
        private const double MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE = .5;

        // The maximum acceptable percentage difference allowed
        private const double MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE = .15;

        private const int SUCCESSIVE_READTIMEOUT_SOMEDATA = 950;

        private const int NUM_TRYS = 5;

        private delegate void ReadMethodDelegate(Stream stream);

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_DefaultValue()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream stream = com.BaseStream;

                Debug.WriteLine("Verifying the default value of ReadTimeout");

                Assert.Equal(-1, stream.ReadTimeout);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_AfterClose()
        {
            Debug.WriteLine("Verifying setting ReadTimeout after the SerialPort was closed");
            VerifyException(2048, null, typeof(ObjectDisposedException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue ReadTimeout");
            VerifyException(int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_NEG2()
        {
            Debug.WriteLine("Verifying -2 ReadTimeout");
            VerifyException(-2, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Default_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying default ReadTimeout with Read(byte[] buffer, int offset, int count)");
            VerifyDefaultTimeout(Read_byte_int_int);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Default_ReadByte()
        {
            Debug.WriteLine("Verifying default ReadTimeout with ReadByte()");
            VerifyDefaultTimeout(ReadByte);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Infinite_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying infinite ReadTimeout with Read(byte[] buffer, int offset, int count)");

            VerifyLongTimeout(Read_byte_int_int, -1);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Infinite_ReadByte()
        {
            Debug.WriteLine("Verifying infinite ReadTimeout with ReadByte()");
            VerifyLongTimeout(ReadByte, -1);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Int32MaxValue_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying Int32.MaxValue ReadTimeout with Read(byte[] buffer, int offset, int count)");

            VerifyLongTimeout(Read_byte_int_int, int.MaxValue - 1);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Int32MaxValue_ReadByte()
        {
            Debug.WriteLine("Verifying Int32.MaxValue ReadTimeout with ReadByte()");
            VerifyLongTimeout(ReadByte, int.MaxValue - 1);
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_750_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying 750 ReadTimeout with Read(byte[] buffer, int offset, int count)");
            VerifyTimeout(Read_byte_int_int, 750);
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_750_ReadByte()
        {
            Debug.WriteLine("Verifying 750 ReadTimeout with ReadByte()");
            VerifyTimeout(ReadByte, 750);
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void SuccessiveReadTimeoutNoData_Read_byte_int_int()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream stream = com.BaseStream;
                stream.ReadTimeout = 850;

                Debug.WriteLine(
                    "Verifying ReadTimeout={0} with successive call to Read(byte[], int, int) and no data",
                    stream.ReadTimeout);

                try
                {
                    stream.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
                    Assert.Fail("Err_1707ahbap!!!: Read did not throw TimeouException when it timed out");
                }
                catch (TimeoutException)
                {
                }

                VerifyTimeout(Read_byte_int_int, stream);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasNullModem))]
        public void SuccessiveReadTimeoutSomeData_Read_byte_int_int()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var t = new Task(WriteToCom1);

                com1.Open();
                Stream stream = com1.BaseStream;
                stream.ReadTimeout = SUCCESSIVE_READTIMEOUT_SOMEDATA;

                Debug.WriteLine(
                    "Verifying ReadTimeout={0} with successive call to Read(byte[], int, int) and some data being received in the first call",
                    stream.ReadTimeout);

                // Call WriteToCom1 asynchronously this will write to com1 some time before the following call
                // to a read method times out
                t.Start();

                try
                {
                    stream.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
                }
                catch (TimeoutException)
                {
                }

                TCSupport.WaitForTaskCompletion(t);

                // Make sure there is no bytes in the buffer so the next call to read will timeout
                com1.DiscardInBuffer();

                VerifyTimeout(Read_byte_int_int, stream);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void SuccessiveReadTimeoutNoData_ReadByte()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream stream = com.BaseStream;
                stream.ReadTimeout = 850;

                Debug.WriteLine("Verifying ReadTimeout={0} with successive call to ReadByte() and no data", stream.ReadTimeout);

                Assert.Throws<TimeoutException>(() => stream.ReadByte());

                VerifyTimeout(ReadByte, stream);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void SuccessiveReadTimeoutSomeData_ReadByte()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var t = new Task(WriteToCom1);

                com1.Open();
                Stream stream = com1.BaseStream;
                stream.ReadTimeout = SUCCESSIVE_READTIMEOUT_SOMEDATA;

                Debug.WriteLine(
                    "Verifying ReadTimeout={0} with successive call to ReadByte() and some data being received in the first call",
                    stream.ReadTimeout);

                // Call WriteToCom1 asynchronously this will write to com1 some time before the following call
                // to a read method times out
                t.Start();

                try
                {
                    stream.ReadByte();
                }
                catch (TimeoutException)
                {
                }

                TCSupport.WaitForTaskCompletion(t);

                // Make sure there is no bytes in the buffer so the next call to read will timeout
                com1.DiscardInBuffer();

                VerifyTimeout(ReadByte, stream);
            }
        }


        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying 0 ReadTimeout with Read(byte[] buffer, int offset, int count)");

            Verify0Timeout(Read_byte_int_int);
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_ReadByte()
        {
            Debug.WriteLine("Verifying 0 ReadTimeout with ReadByte()");
            Verify0Timeout(ReadByte);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadTimeout_0_1ByteAvailable_Read_byte_int_int()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var rcvBytes = new byte[128];
                int bytesRead;

                Debug.WriteLine(
                    "Verifying 0 ReadTimeout with Read(byte[] buffer, int offset, int count) and one byte available");

                com1.Open();
                com2.Open();
                Stream stream = com1.BaseStream;
                stream.ReadTimeout = 0;

                com2.Write(new byte[] { 50 }, 0, 1);

                TCSupport.WaitForReadBufferToLoad(com1, 1);

                Assert.True(1 == (bytesRead = com1.Read(rcvBytes, 0, rcvBytes.Length)),
                    string.Format("Err_31597ahpba, Expected to Read to return 1 actual={0}", bytesRead));

                Assert.True(50 == rcvBytes[0],
                    string.Format("Err_778946ahba, Expected to read 50 actual={0}", rcvBytes[0]));
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadTimeout_0_1ByteAvailable_ReadByte()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                int byteRead;

                Debug.WriteLine("Verifying 0 ReadTimeout with ReadByte() and one byte available");

                com1.Open();
                com2.Open();
                Stream stream = com1.BaseStream;
                stream.ReadTimeout = 0;

                com2.Write(new byte[] { 50 }, 0, 1);

                TCSupport.WaitForReadBufferToLoad(com1, 1);

                Assert.True(50 == (byteRead = com1.ReadByte()),
                    string.Format("Err_05949aypa, Expected to Read to return 50 actual={0}", byteRead));
            }
        }

        private void WriteToCom1()
        {
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var xmitBuffer = new byte[1];
                int sleepPeriod = SUCCESSIVE_READTIMEOUT_SOMEDATA / 2;

                // Sleep some random period with of a maximum duration of half the largest possible timeout value for a read method on COM1
                Thread.Sleep(sleepPeriod);

                com2.Open();
                com2.Write(xmitBuffer, 0, xmitBuffer.Length);
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyDefaultTimeout(ReadMethodDelegate readMethod)
        {
            using (var com1 = TCSupport.InitFirstSerialPort())
            using (var com2 = TCSupport.InitSecondSerialPort(com1))
            {
                com1.Open();

                if (!com2.IsOpen)
                    com2.Open();

                com1.BaseStream.WriteTimeout = 1;

                Assert.Equal(-1, com1.BaseStream.ReadTimeout);

                VerifyLongTimeout(readMethod, com1, com2);
            }
        }

        private void VerifyLongTimeout(ReadMethodDelegate readMethod, int readTimeout)
        {
            using (var com1 = TCSupport.InitFirstSerialPort())
            using (var com2 = TCSupport.InitSecondSerialPort(com1))
            {
                com1.Open();

                if (!com2.IsOpen)
                    com2.Open();

                com1.BaseStream.WriteTimeout = 1;
                com1.BaseStream.ReadTimeout = 1;

                com1.BaseStream.ReadTimeout = readTimeout;

                Assert.True(readTimeout == com1.BaseStream.ReadTimeout,
                    string.Format("Err_7071ahpsb!!! Expected ReadTimeout to be {0} actual {1}", readTimeout,
                        com1.BaseStream.ReadTimeout));

                VerifyLongTimeout(readMethod, com1, com2);
            }
        }

        private void VerifyLongTimeout(ReadMethodDelegate readMethod, SerialPort com1, SerialPort com2)
        {
            var readThread = new ReadDelegateThread(com1.BaseStream, readMethod);
            var t = new Task(readThread.CallRead);


            t.Start();
            Thread.Sleep(DEFAULT_WAIT_LONG_TIMEOUT);

            Assert.False(t.IsCompleted,
                string.Format("Err_17071ahpa!!! {0} terminated with a long timeout of {1}ms", readMethod.Method.Name, com1.BaseStream.ReadTimeout));

            com2.Write(new byte[8], 0, 8);

            TCSupport.WaitForTaskCompletion(t);
        }

        private void VerifyTimeout(ReadMethodDelegate readMethod, int readTimeout)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com1.Open();
                com1.BaseStream.WriteTimeout = 1;
                com1.BaseStream.ReadTimeout = 1;

                com1.BaseStream.ReadTimeout = readTimeout;

                Assert.True(readTimeout == com1.BaseStream.ReadTimeout,
                    string.Format("Err_236897ahpbm!!! Expected ReadTimeout to be {0} actual {1}", readTimeout,
                        com1.BaseStream.ReadTimeout));

                VerifyTimeout(readMethod, com1.BaseStream);
            }
        }

        private void VerifyTimeout(ReadMethodDelegate readMethod, Stream stream)
        {
            var timer = new Stopwatch();
            int expectedTime = stream.ReadTimeout;
            int actualTime;
            double percentageDifference;


            // Warmup the read method. When called for the first time the read method seems to take much longer then subsequent calls
            timer.Start();
            try
            {
                readMethod(stream);
            }
            catch (TimeoutException) { }
            timer.Stop();
            actualTime = (int)timer.ElapsedMilliseconds;
            percentageDifference = Math.Abs((expectedTime - actualTime) / (double)expectedTime);

            // Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
            Assert.True(percentageDifference <= MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE,
                string.Format("Err_88558amuph!!!: The read method timedout in {0} expected {1} percentage difference: {2} when called for the first time",
                    actualTime, expectedTime, percentageDifference));

            actualTime = 0;
            timer.Reset();

            // Perform the actual test verifying that the read method times out in approximately ReadTime milliseconds
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (var i = 0; i < NUM_TRYS; i++)
            {
                timer.Start();
                try { readMethod(stream); }
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
                string.Format("Err_56485ahpbz!!!: The read method timedout in {0} expected {1} percentage difference: {2}", actualTime, expectedTime, percentageDifference));
        }

        private void Verify0Timeout(ReadMethodDelegate readMethod)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))

            {
                com1.Open();
                com1.BaseStream.WriteTimeout = 1;
                com1.BaseStream.ReadTimeout = 1;

                com1.BaseStream.ReadTimeout = 0;

                Assert.True(0 == com1.BaseStream.ReadTimeout,
                    string.Format("Err_72072ahps!!! Expected ReadTimeout to be {0} actual {1}", 0,
                        com1.BaseStream.ReadTimeout));

                Verify0Timeout(readMethod, com1.BaseStream);
            }
        }

        private void Verify0Timeout(ReadMethodDelegate readMethod, Stream stream)
        {
            var timer = new Stopwatch();
            int actualTime;

            // Warmup the read method. When called for the first time the read method seems to take much longer then subsequent calls
            timer.Start();
            try
            {
                readMethod(stream);
            }
            catch (TimeoutException) { }
            timer.Stop();
            actualTime = (int)timer.ElapsedMilliseconds;

            // Verify that the time the method took to timeout is less then the maximum acceptable time
            Assert.True(actualTime <= MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT,
                string.Format("Err_277a0ahpsb!!!: With a timeout of 0 the read method timedout in {0} expected something less then {1} when called for the first time",
                    actualTime, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT));

            actualTime = 0;
            timer.Reset();

            // Perform the actual test verifying that the read method times out in approximately ReadTime milliseconds
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (var i = 0; i < NUM_TRYS; i++)
            {
                timer.Start();
                try { readMethod(stream); }
                catch (TimeoutException) { }
                timer.Stop();

                actualTime += (int)timer.ElapsedMilliseconds;
                timer.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;

            // Verify that the time the method took to timeout is less then the maximum acceptable time
            Assert.True(actualTime <= MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT,
                string.Format("Err_112389ahbp!!!: With a timeout of 0 the read method timedout in {0} expected something less then {1}",
                    actualTime, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT));
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

        private void VerifyException(Stream stream, int readTimeout, Type expectedException)
        {
            int origReadTimeout = stream.ReadTimeout;

            if (expectedException != null)
            {
                Assert.Throws(expectedException, () => stream.ReadTimeout = readTimeout);
                Assert.Equal(origReadTimeout, stream.ReadTimeout);
            }
            else
            {
                stream.ReadTimeout = readTimeout;
                Assert.Equal(readTimeout, stream.ReadTimeout);
            }
        }

        private void Read_byte_int_int(Stream stream)
        {
            stream.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
        }

        private void ReadByte(Stream stream)
        {
            stream.ReadByte();
        }

        private class ReadDelegateThread
        {
            public ReadDelegateThread(Stream stream, ReadMethodDelegate readMethod)
            {
                _stream = stream;
                _readMethod = readMethod;
            }

            public void CallRead()
            {
                _readMethod(_stream);
            }

            private readonly ReadMethodDelegate _readMethod;
            private readonly Stream _stream;
        }

        #endregion
    }
}

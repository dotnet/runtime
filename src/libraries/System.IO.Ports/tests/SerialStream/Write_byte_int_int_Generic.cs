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
using Xunit.NetCore.Extensions;

namespace System.IO.Ports.Tests
{
    public class SerialStream_Write_byte_int_int_Generic : PortsTest
    {
        // Set bounds fore random timeout values.
        // If the min is to low write will not timeout accurately and the testcase will fail
        private const int minRandomTimeout = 250;

        // If the max is to large then the testcase will take forever to run
        private const int maxRandomTimeout = 2000;

        // If the percentage difference between the expected timeout and the actual timeout
        // found through Stopwatch is greater then 10% then the timeout value was not correctly
        // to the write method and the testcase fails.
        private const double maxPercentageDifference = .15;

        // The byte size used when veryifying exceptions that write will throw 
        private const int BYTE_SIZE_EXCEPTION = 4;

        // The byte size used when veryifying timeout 
        private static readonly int s_BYTE_SIZE_TIMEOUT = TCSupport.MinimumBlockingByteCount;

        // The byte size used when veryifying BytesToWrite 
        private static readonly int s_BYTE_SIZE_BYTES_TO_WRITE = TCSupport.MinimumBlockingByteCount;

        // The bytes size used when veryifying Handshake 
        private static readonly int s_BYTE_SIZE_HANDSHAKE = TCSupport.MinimumBlockingByteCount;

        private const int NUM_TRYS = 5;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteAfterClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying write method throws exception after a call to Cloes()");

                com.Open();
                Stream serialStream = com.BaseStream;
                com.Close();

                VerifyWriteException(serialStream, typeof(ObjectDisposedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteAfterBaseStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying write method throws exception after a call to BaseStream.Close()");

                com.Open();
                Stream serialStream = com.BaseStream;
                com.BaseStream.Close();

                VerifyWriteException(serialStream, typeof(ObjectDisposedException));
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Timeout()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var rndGen = new Random(-55);
                var XOffBuffer = new byte[1];

                XOffBuffer[0] = 19;

                com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
                com1.Handshake = Handshake.XOnXOff;

                Debug.WriteLine("Verifying WriteTimeout={0}", com1.WriteTimeout);

                com1.Open();
                com2.Open();

                com2.BaseStream.Write(XOffBuffer, 0, 1);
                Thread.Sleep(250);
                com2.Close();

                VerifyTimeout(com1);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void SuccessiveWriteTimeout()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var rndGen = new Random(-55);

                com.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
                com.Handshake = Handshake.RequestToSendXOnXOff;
                // 		com.Encoding = new System.Text.UTF7Encoding();
                com.Encoding = Encoding.Unicode;

                Debug.WriteLine("Verifying WriteTimeout={0} with successive call to write method", com.WriteTimeout);
                com.Open();

                try
                {
                    com.BaseStream.Write(new byte[s_BYTE_SIZE_TIMEOUT], 0, s_BYTE_SIZE_TIMEOUT);
                }
                catch (TimeoutException)
                {
                }

                VerifyTimeout(com);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void SuccessiveWriteTimeoutWithWriteSucceeding()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var rndGen = new Random(-55);
                var asyncEnableRts = new AsyncEnableRts();
                var t = new Task(asyncEnableRts.EnableRTS);

                com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
                com1.Handshake = Handshake.RequestToSend;
                com1.Encoding = new UTF8Encoding();

                Debug.WriteLine(
                    "Verifying WriteTimeout={0} with successive call to write method with the write succeeding sometime before it's timeout",
                    com1.WriteTimeout);
                com1.Open();

                // Call EnableRTS asynchronously this will enable RTS in the middle of the following write call allowing it to succeed 
                // before the timeout is reached
                t.Start();
                TCSupport.WaitForTaskToStart(t);

                try
                {
                    com1.BaseStream.Write(new byte[s_BYTE_SIZE_TIMEOUT], 0, s_BYTE_SIZE_TIMEOUT);
                }
                catch (TimeoutException)
                {
                }

                asyncEnableRts.Stop();

                TCSupport.WaitForTaskCompletion(t);
                VerifyTimeout(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void BytesToWrite()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var asyncWriteRndByteArray = new AsyncWriteRndByteArray(com, s_BYTE_SIZE_BYTES_TO_WRITE);
                var t = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                Debug.WriteLine("Verifying BytesToWrite with one call to Write");

                com.Handshake = Handshake.RequestToSend;
                com.Open();
                com.WriteTimeout = 500;

                // Write a random byte[] asynchronously so we can verify some things while the write call is blocking
                t.Start();
                TCSupport.WaitForTaskToStart(t);

                TCSupport.WaitForWriteBufferToLoad(com, s_BYTE_SIZE_BYTES_TO_WRITE);

                TCSupport.WaitForTaskCompletion(t);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void BytesToWriteSuccessive()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var asyncWriteRndByteArray = new AsyncWriteRndByteArray(com, s_BYTE_SIZE_BYTES_TO_WRITE);
                var t1 = new Task(asyncWriteRndByteArray.WriteRndByteArray);
                var t2 = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                Debug.WriteLine("Verifying BytesToWrite with successive calls to Write");

                com.Handshake = Handshake.RequestToSend;
                com.Open();
                com.WriteTimeout = 4000;

                // Write a random byte[] asynchronously so we can verify some things while the write call is blocking
                t1.Start();
                TCSupport.WaitForTaskToStart(t1);
                TCSupport.WaitForExactWriteBufferLoad(com, s_BYTE_SIZE_BYTES_TO_WRITE);

                // Write a random byte[] asynchronously so we can verify some things while the write call is blocking
                t2.Start();
                TCSupport.WaitForTaskToStart(t2);
                TCSupport.WaitForExactWriteBufferLoad(com, s_BYTE_SIZE_BYTES_TO_WRITE * 2);

                // Wait for both write methods to timeout
                var aggregatedException = Assert.Throws<AggregateException>(() => TCSupport.WaitForTaskCompletion(t2));
                Assert.IsType<IOException>(aggregatedException.InnerException);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Handshake_None()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var asyncWriteRndByteArray = new AsyncWriteRndByteArray(com, s_BYTE_SIZE_HANDSHAKE);
                var t = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                // Write a random byte[] asynchronously so we can verify some things while the write call is blocking
                Debug.WriteLine("Verifying Handshake=None");

                com.Open();
                t.Start();
                TCSupport.WaitForTaskCompletion(t);

                Assert.Equal(0, com.BytesToWrite);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSend()
        {
            Verify_Handshake(Handshake.RequestToSend);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_XOnXOff()
        {
            Verify_Handshake(Handshake.XOnXOff);
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSendXOnXOff()
        {
            Verify_Handshake(Handshake.RequestToSendXOnXOff);
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
                        var rndGen = new Random(-55);
                        int sleepPeriod = rndGen.Next(minRandomTimeout, maxRandomTimeout / 2);

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

        private class AsyncWriteRndByteArray
        {
            private readonly SerialPort _com;
            private readonly int _byteLength;


            public AsyncWriteRndByteArray(SerialPort com, int byteLength)
            {
                _com = com;
                _byteLength = byteLength;
            }


            public void WriteRndByteArray()
            {
                var buffer = new byte[_byteLength];
                var rndGen = new Random(-55);

                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)rndGen.Next(0, 256);
                }

                try
                {
                    _com.BaseStream.Write(buffer, 0, buffer.Length);
                }
                catch (TimeoutException)
                {
                }
            }
        }

        #endregion

        #region Verification for Test Cases

        private static void VerifyWriteException(Stream serialStream, Type expectedException)
        {
            Assert.Throws(expectedException,
                () => serialStream.Write(new byte[BYTE_SIZE_EXCEPTION], 0, BYTE_SIZE_EXCEPTION));
        }

        private void VerifyTimeout(SerialPort com)
        {
            var timer = new Stopwatch();
            int expectedTime = com.WriteTimeout;
            var actualTime = 0;


            try
            {
                com.BaseStream.Write(new byte[s_BYTE_SIZE_TIMEOUT], 0, s_BYTE_SIZE_TIMEOUT); // Warm up write method
            }
            catch (TimeoutException)
            {
            }

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (var i = 0; i < NUM_TRYS; i++)
            {
                timer.Start();
                try
                {
                    com.BaseStream.Write(new byte[s_BYTE_SIZE_TIMEOUT], 0, s_BYTE_SIZE_TIMEOUT);
                }
                catch (TimeoutException)
                {
                }

                timer.Stop();
                actualTime += (int)timer.ElapsedMilliseconds;
                timer.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;
            double percentageDifference = Math.Abs((expectedTime - actualTime) / (double)expectedTime);

            // Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
            if (maxPercentageDifference < percentageDifference)
            {
                Fail("ERROR!!!: The write method timedout in {0} expected {1} percentage difference: {2}", actualTime,
                    expectedTime, percentageDifference);
            }
        }


        private void Verify_Handshake(Handshake handshake)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, s_BYTE_SIZE_HANDSHAKE);
                var t = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                var XOffBuffer = new byte[1];
                var XOnBuffer = new byte[1];

                XOffBuffer[0] = 19;
                XOnBuffer[0] = 17;

                Debug.WriteLine("Verifying Handshake={0}", handshake);
                com1.Handshake = handshake;

                com1.Open();
                com2.Open();

                // Setup to ensure write will bock with type of handshake method being used
                if (Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.RtsEnable = false;
                }

                if (Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.BaseStream.Write(XOffBuffer, 0, 1);
                    Thread.Sleep(250);
                }

                // Write a random byte asynchronously so we can verify some things while the write call is blocking
                t.Start();
                TCSupport.WaitForTaskToStart(t);
                TCSupport.WaitForWriteBufferToLoad(com1, s_BYTE_SIZE_HANDSHAKE);

                // Verify that CtsHolding is false if the RequestToSend or RequestToSendXOnXOff handshake method is used
                if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) &&
                    com1.CtsHolding)
                {
                    Fail("ERROR!!! Expected CtsHolding={0} actual {1}", false, com1.CtsHolding);
                }

                // Setup to ensure write will succeed
                if (Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.RtsEnable = true;
                }

                if (Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.BaseStream.Write(XOnBuffer, 0, 1);
                }

                TCSupport.WaitForTaskCompletion(t);

                // Verify that the correct number of bytes are in the buffer
                Assert.Equal(0, com1.BytesToWrite);

                // Verify that CtsHolding is true if the RequestToSend or RequestToSendXOnXOff handshake method is used
                if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) &&
                    !com1.CtsHolding)
                {
                    Fail("ERROR!!! Expected CtsHolding={0} actual {1}", true, com1.CtsHolding);
                }
            }

            #endregion
        }
    }
}

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
using Microsoft.DotNet.XUnitExtensions;

namespace System.IO.Ports.Tests
{
    public class Write_str_Generic : PortsTest
    {
        //Set bounds fore random timeout values.
        //If the min is to low write will not timeout accurately and the testcase will fail
        private const int minRandomTimeout = 250;

        //If the max is to large then the testcase will take forever to run
        private const int maxRandomTimeout = 2000;

        //If the percentage difference between the expected timeout and the actual timeout
        //found through Stopwatch is greater then 10% then the timeout value was not correctly
        //to the write method and the testcase fails.
        private const double maxPercentageDifference = .15;

        //The string used when we expect the ReadCall to throw an exception for something other
        //then the contents of the string itself
        private const string DEFAULT_STRING = "DEFAULT_STRING";

        //The string size used when verifying BytesToWrite
        private static readonly int s_STRING_SIZE_BYTES_TO_WRITE = 2 * TCSupport.MinimumBlockingByteCount;

        //The string size used when verifying Handshake
        private const int STRING_SIZE_HANDSHAKE = 8;
        private const int NUM_TRYS = 5;

        #region Test Cases

        [Fact]
        public void WriteWithoutOpen()
        {
            using (SerialPort com = new SerialPort())
            {
                Debug.WriteLine("Verifying write method throws exception without a call to Open()");

                VerifyWriteException(com, typeof(InvalidOperationException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteAfterFailedOpen()
        {
            using (SerialPort com = new SerialPort("BAD_PORT_NAME"))
            {
                Debug.WriteLine("Verifying write method throws exception with a failed call to Open()");

                //Since the PortName is set to a bad port name Open will thrown an exception
                //however we don't care what it is since we are verifying a write method
                Assert.ThrowsAny<Exception>(() => com.Open());

                VerifyWriteException(com, typeof(InvalidOperationException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteAfterClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying write method throws exception after a call to Cloes()");
                com.Open();
                com.Close();

                VerifyWriteException(com, typeof(InvalidOperationException));
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void Timeout()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Random rndGen = new Random(-55);
                byte[] XOffBuffer = new byte[1];

                XOffBuffer[0] = 19;

                com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
                com1.Handshake = Handshake.XOnXOff;

                Debug.WriteLine("Verifying WriteTimeout={0}", com1.WriteTimeout);

                com1.Open();
                com2.Open();

                com2.Write(XOffBuffer, 0, 1);
                Thread.Sleep(250);

                com2.Close();

                VerifyTimeout(com1);
            }
        }

        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)]  // Timing-sensitive
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void SuccessiveReadTimeout()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Random rndGen = new Random(-55);

                com.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
                com.Handshake = Handshake.RequestToSendXOnXOff;
                //		com.Encoding = new System.Text.UTF7Encoding();
                com.Encoding = Encoding.Unicode;

                Debug.WriteLine("Verifying WriteTimeout={0} with successive call to write method", com.WriteTimeout);
                com.Open();

                try
                {
                    com.Write(DEFAULT_STRING);
                }
                catch (TimeoutException)
                {
                }

                VerifyTimeout(com);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void SuccessiveReadTimeoutWithWriteSucceeding()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Random rndGen = new Random(-55);
                AsyncEnableRts asyncEnableRts = new AsyncEnableRts();
                var t = new Task(asyncEnableRts.EnableRTS);

                com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
                com1.Handshake = Handshake.RequestToSend;
                com1.Encoding = new UTF8Encoding();

                Debug.WriteLine(
                    "Verifying WriteTimeout={0} with successive call to write method with the write succeeding sometime before its timeout",
                    com1.WriteTimeout);
                com1.Open();

                //Call EnableRTS asynchronously this will enable RTS in the middle of the following write call allowing it to succeed
                //before the timeout is reached
                t.Start();
                TCSupport.WaitForTaskToStart(t);

                try
                {
                    com1.Write(DEFAULT_STRING);
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
                AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com, s_STRING_SIZE_BYTES_TO_WRITE);
                var t = new Task(asyncWriteRndStr.WriteRndStr);

                Debug.WriteLine("Verifying BytesToWrite with one call to Write");
                com.Handshake = Handshake.RequestToSend;
                com.Open();
                com.WriteTimeout = 500;

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                t.Start();
                TCSupport.WaitForTaskToStart(t);
                TCSupport.WaitForWriteBufferToLoad(com, s_STRING_SIZE_BYTES_TO_WRITE);
                TCSupport.WaitForTaskCompletion(t);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void BytesToWriteSuccessive()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com, s_STRING_SIZE_BYTES_TO_WRITE);
                var t1 = new Task(asyncWriteRndStr.WriteRndStr);
                var t2 = new Task(asyncWriteRndStr.WriteRndStr);

                Debug.WriteLine("Verifying BytesToWrite with successive calls to Write");
                com.Handshake = Handshake.RequestToSend;
                com.Open();
                com.WriteTimeout = 1000;

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                t1.Start();
                TCSupport.WaitForTaskToStart(t1);
                TCSupport.WaitForWriteBufferToLoad(com, s_STRING_SIZE_BYTES_TO_WRITE);

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                t2.Start();
                TCSupport.WaitForTaskToStart(t2);
                TCSupport.WaitForWriteBufferToLoad(com, s_STRING_SIZE_BYTES_TO_WRITE * 2);

                //Wait for both write methods to timeout
                TCSupport.WaitForTaskCompletion(t1);
                var aggregatedException = Assert.Throws<AggregateException>(() => TCSupport.WaitForTaskCompletion(t2));
                Assert.IsType<IOException>(aggregatedException.InnerException);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Handshake_None()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com, STRING_SIZE_HANDSHAKE);
                var t = new Task(asyncWriteRndStr.WriteRndStr);

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                Debug.WriteLine("Verifying Handshake=None");
                com.Open();

                t.Start();
                TCSupport.WaitForTaskCompletion(t);
                Assert.Equal(0, com.BytesToWrite);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_RequestToSend()
        {
            Verify_Handshake(Handshake.RequestToSend);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_XOnXOff()
        {
            Verify_Handshake(Handshake.XOnXOff);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
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
                    SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
                    Random rndGen = new Random(-55);
                    int sleepPeriod = rndGen.Next(minRandomTimeout, maxRandomTimeout / 2);

                    //Sleep some random period with of a maximum duration of half the largest possible timeout value for a write method on COM1
                    Thread.Sleep(sleepPeriod);

                    com2.Open();
                    com2.RtsEnable = true;

                    while (!_stop)
                        Monitor.Wait(this);

                    com2.RtsEnable = false;

                    if (com2.IsOpen)
                        com2.Close();
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



        public class AsyncWriteRndStr
        {
            private readonly SerialPort _com;
            private readonly int _strSize;


            public AsyncWriteRndStr(SerialPort com, int strSize)
            {
                _com = com;
                _strSize = strSize;
            }


            public void WriteRndStr()
            {
                string stringToWrite = TCSupport.GetRandomString(_strSize, TCSupport.CharacterOptions.Surrogates);

                try
                {
                    _com.Write(stringToWrite);
                }
                catch (TimeoutException)
                {
                }
            }
        }
        #endregion

        #region Verification for Test Cases
        public static void VerifyWriteException(SerialPort com, Type expectedException)
        {
            Assert.Throws(expectedException, () => com.Write(DEFAULT_STRING));
        }

        private void VerifyTimeout(SerialPort com)
        {
            Stopwatch timer = new Stopwatch();
            int expectedTime = com.WriteTimeout;
            int actualTime = 0;
            double percentageDifference;

            try
            {
                com.Write(DEFAULT_STRING); //Warm up write method
            }
            catch (TimeoutException) { }

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (int i = 0; i < NUM_TRYS; i++)
            {
                timer.Start();
                try
                {
                    com.Write(DEFAULT_STRING);
                }
                catch (TimeoutException) { }

                timer.Stop();

                actualTime += (int)timer.ElapsedMilliseconds;
                timer.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;
            percentageDifference = Math.Abs((expectedTime - actualTime) / (double)expectedTime);

            //Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
            if (maxPercentageDifference < percentageDifference)
            {
                Fail("ERROR!!!: The write method timedout in {0} expected {1} percentage difference: {2}", actualTime, expectedTime, percentageDifference);
            }
        }

        private void Verify_Handshake(Handshake handshake)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                bool rts = Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake;
                bool xonxoff = Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake;

                byte[] XOffBuffer = new byte[1];
                byte[] XOnBuffer = new byte[1];

                XOffBuffer[0] = 19;
                XOnBuffer[0] = 17;

                Debug.WriteLine("Verifying Handshake={0}", handshake);

                com1.Handshake = handshake;
                com1.Open();
                com2.Open();

                //Setup to ensure write will block with type of handshake method being used
                if (rts)
                {
                    com2.RtsEnable = false;
                }

                if (xonxoff)
                {
                    com2.Write(XOffBuffer, 0, 1);
                    Thread.Sleep(250);
                }

                Task writeTask = Task.Run(() => com1.Write("ABC"));

                CancellationTokenSource cts = new CancellationTokenSource();
                Task<int> readTask = com2.BaseStream.ReadAsync(new byte[3], 0, 3, cts.Token);

                // Give it some time to make sure transmission doesn't happen
                Thread.Sleep(200);
                Assert.False(readTask.IsCompleted);

                cts.CancelAfter(500);

                if (rts)
                {
                    Assert.False(com1.CtsHolding);
                    com2.RtsEnable = true;
                }

                if (xonxoff)
                {
                    com2.Write(XOnBuffer, 0, 1);
                }

                writeTask.Wait();
                Assert.True(readTask.Result > 0);

                //Verify that CtsHolding is true if the RequestToSend or RequestToSendXOnXOff handshake method is used
                if (rts)
                {
                    Assert.True(com1.CtsHolding);
                }
            }
        }

        #endregion
    }
}

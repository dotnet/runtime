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
    public class WriteLine_Generic : PortsTest
    {
        //Set bounds for random timeout values.
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

        //The string size used when veryifying BytesToWrite 
        private static readonly int s_STRING_SIZE_BYTES_TO_WRITE = TCSupport.MinimumBlockingByteCount;

        //The string size used when veryifying Handshake 
        private static readonly int s_STRING_SIZE_HANDSHAKE = TCSupport.MinimumBlockingByteCount;
        private const int NUM_TRYS = 5;

        #region Test Cases

        [Fact]
        public void WriteWithoutOpen()
        {
            using (SerialPort com = new SerialPort())
            {
                Debug.WriteLine("Case WriteWithoutOpen : Verifying write method throws System.InvalidOperationException without a call to Open()");

                VerifyWriteException(com, typeof(InvalidOperationException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteAfterFailedOpen()
        {
            using (SerialPort com = new SerialPort("BAD_PORT_NAME"))
            {
                Debug.WriteLine("Case WriteAfterFailedOpen : Verifying write method throws exception with a failed call to Open()");

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
                Debug.WriteLine("Case WriteAfterClose : Verifying write method throws exception after a call to Close()");
                com.Open();
                com.Close();

                VerifyWriteException(com, typeof(InvalidOperationException));
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void SimpleTimeout()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Random rndGen = new Random(-55);
                byte[] XOffBuffer = new byte[1];

                XOffBuffer[0] = 19;

                com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
                com1.Handshake = Handshake.XOnXOff;

                Debug.WriteLine("Case SimpleTimeout : Verifying WriteTimeout={0}", com1.WriteTimeout);

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
                com.Encoding = Encoding.Unicode;

                Debug.WriteLine("Case SuccessiveReadTimeout : Verifying WriteTimeout={0} with successive call to write method", com.WriteTimeout);
                com.Open();

                try
                {
                    com.WriteLine(DEFAULT_STRING);
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

                Debug.WriteLine("Case SuccessiveReadTimeoutWithWriteSucceeding : Verifying WriteTimeout={0} with successive call to write method with the write succeeding sometime before its timeout", com1.WriteTimeout);
                com1.Open();

                //Call EnableRTS asynchronously this will enable RTS in the middle of the following write call allowing it to succeed 
                //before the timeout is reached
                t.Start();
                TCSupport.WaitForTaskToStart(t);

                try
                {
                    com1.WriteLine(DEFAULT_STRING);
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

                int numNewLineBytes;

                Debug.WriteLine("Case BytesToWrite : Verifying BytesToWrite with one call to Write");

                com.Handshake = Handshake.RequestToSend;
                com.Open();
                com.WriteTimeout = 500;

                numNewLineBytes = com.Encoding.GetByteCount(com.NewLine.ToCharArray());

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                t.Start();

                TCSupport.WaitForTaskToStart(t);

                TCSupport.WaitForWriteBufferToLoad(com, s_STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes);

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

                int numNewLineBytes;

                Debug.WriteLine("Case BytesToWriteSuccessive : Verifying BytesToWrite with successive calls to Write");

                com.Handshake = Handshake.RequestToSend;
                com.Open();
                com.WriteTimeout = 1000;
                numNewLineBytes = com.Encoding.GetByteCount(com.NewLine.ToCharArray());

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                t1.Start();
                TCSupport.WaitForTaskToStart(t1);
                TCSupport.WaitForWriteBufferToLoad(com, s_STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes);

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                t2.Start();
                TCSupport.WaitForTaskToStart(t2);
                TCSupport.WaitForWriteBufferToLoad(com, (s_STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes) * 2);

                //Wait for both write methods to timeout
                TCSupport.WaitForTaskCompletion(t1);
                var aggregatedException = Assert.Throws<AggregateException>(() => TCSupport.WaitForTaskCompletion(t2));
                Assert.IsType<IOException>(aggregatedException.InnerException);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_None()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com, s_STRING_SIZE_HANDSHAKE);
                var t = new Task(asyncWriteRndStr.WriteRndStr);

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                Debug.WriteLine("Case Handshake_None : Verifying Handshake=None");

                com.Open();
                t.Start();
                TCSupport.WaitForTaskCompletion(t);
                Assert.Equal(0, com.BytesToWrite);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSend()
        {
            Debug.WriteLine("Case Handshake_RequestToSend : Verifying Handshake=RequestToSend");
            Verify_Handshake(Handshake.RequestToSend);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_XOnXOff()
        {
            Debug.WriteLine("Case Handshake_XOnXOff : Verifying Handshake=XOnXOff");
            Verify_Handshake(Handshake.XOnXOff);
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSendXOnXOff()
        {
            Debug.WriteLine("Case Handshake_RequestToSendXOnXOff : Verifying Handshake=RequestToSendXOnXOff");
            Verify_Handshake(Handshake.RequestToSendXOnXOff);
        }

        private class AsyncEnableRts
        {
            private bool _stop;

            public void EnableRTS()
            {
                lock (this)
                {
                    using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
                    {
                        Random rndGen = new Random(-55);
                        int sleepPeriod = rndGen.Next(minRandomTimeout, maxRandomTimeout / 2);

                        //Sleep some random period with of a maximum duration of half the largest possible timeout value for a write method on COM1
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

        private class AsyncWriteRndStr
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
                    _com.WriteLine(stringToWrite);
                }
                catch (TimeoutException)
                {
                }
            }
        }
        #endregion

        #region Verification for Test Cases
        private static void VerifyWriteException(SerialPort com, Type expectedException)
        {
            Assert.Throws(expectedException, () => com.WriteLine(DEFAULT_STRING));
        }

        private void VerifyTimeout(SerialPort com)
        {
            Stopwatch timer = new Stopwatch();
            int expectedTime = com.WriteTimeout;
            int actualTime = 0;
            double percentageDifference;

            try
            {
                com.WriteLine(DEFAULT_STRING); //Warm up write method
            }
            catch (TimeoutException) { }

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            for (int i = 0; i < NUM_TRYS; i++)
            {
                timer.Start();

                try
                {
                    com.WriteLine(DEFAULT_STRING);
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
                byte[] XOffBuffer = new byte[1];
                byte[] XOnBuffer = new byte[1];

                XOffBuffer[0] = 19;
                XOnBuffer[0] = 17;

                int numNewLineBytes;

                Debug.WriteLine("Verifying Handshake={0}", handshake);

                com1.WriteTimeout = 3000;
                com1.Handshake = handshake;
                com1.Open();
                com2.Open();

                numNewLineBytes = com1.Encoding.GetByteCount(com1.NewLine.ToCharArray());

                //Setup to ensure write will block with type of handshake method being used
                if (Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.RtsEnable = false;
                }

                if (Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.Write(XOffBuffer, 0, 1);
                    Thread.Sleep(250);
                }

                //Write a random string asynchronously so we can verify some things while the write call is blocking
                string randomLine = TCSupport.GetRandomString(s_STRING_SIZE_HANDSHAKE, TCSupport.CharacterOptions.Surrogates);
                byte[] randomLineBytes = com1.Encoding.GetBytes(randomLine);
                Task task = Task.Run(() => com1.WriteLine(randomLine));

                TCSupport.WaitForTaskToStart(task);

                TCSupport.WaitForWriteBufferToLoad(com1, randomLineBytes.Length + numNewLineBytes);

                //Verify that CtsHolding is false if the RequestToSend or RequestToSendXOnXOff handshake method is used
                if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) && com1.CtsHolding)
                {
                    Fail("ERROR!!! Expcted CtsHolding={0} actual {1}", false, com1.CtsHolding);
                }

                //Setup to ensure write will succeed
                if (Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.RtsEnable = true;
                }

                if (Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake)
                {
                    com2.Write(XOnBuffer, 0, 1);
                }

                //Wait till write finishes
                TCSupport.WaitForTaskCompletion(task);

                Assert.Equal(0, com1.BytesToWrite);

                //Verify that CtsHolding is true if the RequestToSend or RequestToSendXOnXOff handshake method is used
                if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) &&
                    !com1.CtsHolding)
                {
                    Fail("ERROR!!! Expected CtsHolding={0} actual {1}", true, com1.CtsHolding);
                }
            }
        }

        #endregion
    }
}

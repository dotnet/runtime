// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_BeginWrite_Generic : PortsTest
    {
        // Set bounds fore random timeout values.
        // If the min is to low write will not timeout accurately and the testcase will fail
        private const int minRandomTimeout = 250;

        // If the max is to large then the testcase will take forever to run
        private const int maxRandomTimeout = 2000;

        // If the percentage difference between the expected timeout and the actual timeout
        // found through Stopwatch is greater then 10% then the timeout value was not correctly
        // to the write method and the testcase fails.
        public static double maxPercentageDifference = .15;

        // The byte size used when veryifying exceptions that write will throw 
        private const int BYTE_SIZE_EXCEPTION = 4;

        // The byte size used when veryifying timeout 
        private const int BYTE_SIZE_TIMEOUT = 4;

        // The byte size used when veryifying BytesToWrite 
        private const int BYTE_SIZE_BYTES_TO_WRITE = 4;

        // The bytes size used when veryifying Handshake 
        private const int BYTE_SIZE_HANDSHAKE = 8;
        private const int MAX_WAIT = 250;
        private const int ITERATION_WAIT = 50;
        private const int NUM_TRYS = 5;

        private const int MAX_WAIT_THREAD = 1000;

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
        public void WriteAfterSerialStreamClose()
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
            var rndGen = new Random(-55);
            int writeTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);

            Debug.WriteLine("Verifying WriteTimeout={0}", writeTimeout);

            VerifyTimeout(writeTimeout);
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void BytesToWrite()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var elapsedTime = 0;

                Debug.WriteLine("Verifying BytesToWrite with one call to Write");

                com1.Handshake = Handshake.RequestToSend;
                com1.Open();
                com2.Open();
                com1.WriteTimeout = MAX_WAIT;

                IAsyncResult writeAsyncResult = WriteRndByteArray(com1, BYTE_SIZE_BYTES_TO_WRITE);
                Thread.Sleep(100);

                while (elapsedTime < MAX_WAIT && BYTE_SIZE_BYTES_TO_WRITE != com1.BytesToWrite)
                {
                    elapsedTime += ITERATION_WAIT;
                    Thread.Sleep(ITERATION_WAIT);
                }

                if (elapsedTime >= MAX_WAIT)
                {
                    Fail("Err_2257asap!!! Expcted BytesToWrite={0} actual {1} after first write",
                                            BYTE_SIZE_BYTES_TO_WRITE, com1.BytesToWrite);
                }

                com2.RtsEnable = true;

                // Wait for write method to complete
                elapsedTime = 0;
                while (!writeAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_THREAD)
                {
                    Thread.Sleep(50);
                    elapsedTime += 50;
                }

                if (MAX_WAIT_THREAD <= elapsedTime)
                {
                    Fail("Err_40888ajhied!!!: Expected write method to complete");
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BytesToWriteSuccessive()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var elapsedTime = 0;

                Debug.WriteLine("Verifying BytesToWrite with successive calls to Write");

                com1.Handshake = Handshake.RequestToSend;
                com1.Open();
                com2.Open();
                com1.WriteTimeout = 2000;

                // Write a random byte[] asynchronously so we can verify some things while the write call is blocking
                IAsyncResult writeAsyncResult1 = WriteRndByteArray(com1, BYTE_SIZE_BYTES_TO_WRITE);
                while (elapsedTime < 1000 && BYTE_SIZE_BYTES_TO_WRITE != com1.BytesToWrite)
                {
                    elapsedTime += ITERATION_WAIT;
                    Thread.Sleep(ITERATION_WAIT);
                }

                if (elapsedTime >= 1000)
                {
                    Fail("Err_5201afwp!!! Expcted BytesToWrite={0} actual {1} after first write",
                        BYTE_SIZE_BYTES_TO_WRITE, com1.BytesToWrite);
                }

                // Write a random byte[] asynchronously so we can verify some things while the write call is blocking
                IAsyncResult writeAsyncResult2 = WriteRndByteArray(com1, BYTE_SIZE_BYTES_TO_WRITE);
                elapsedTime = 0;

                while (elapsedTime < 1000 && BYTE_SIZE_BYTES_TO_WRITE * 2 != com1.BytesToWrite)
                {
                    elapsedTime += ITERATION_WAIT;
                    Thread.Sleep(ITERATION_WAIT);
                }

                if (elapsedTime >= 1000)
                {
                    Fail("Err_2541afpsduz!!! Expcted BytesToWrite={0} actual {1} after first write",
                        BYTE_SIZE_BYTES_TO_WRITE, com1.BytesToWrite);
                }

                com2.RtsEnable = true;

                // Wait for write method to complete
                elapsedTime = 0;
                while ((!writeAsyncResult1.IsCompleted || !writeAsyncResult2.IsCompleted) &&
                       elapsedTime < MAX_WAIT_THREAD)
                {
                    Thread.Sleep(50);
                    elapsedTime += 50;
                }

                if (MAX_WAIT_THREAD <= elapsedTime)
                {
                    Fail("Err_65825215ahue!!!: Expected write method to complete");
                }
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Handshake_None()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                // Write a random byte[] asynchronously so we can verify some things while the write call is blocking
                Debug.WriteLine("Verifying Handshake=None");

                com.Open();
                IAsyncResult writeAsyncResult = WriteRndByteArray(com, BYTE_SIZE_BYTES_TO_WRITE);

                // Wait for both write methods to timeout
                while (!writeAsyncResult.IsCompleted)
                    Thread.Sleep(100);

                if (0 != com.BytesToWrite)
                {
                    Fail("ERROR!!! Expcted BytesToWrite=0 actual {0}", com.BytesToWrite);
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_RequestToSend()
        {
            Verify_Handshake(Handshake.RequestToSend);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_XOnXOff()
        {
            Verify_Handshake(Handshake.XOnXOff);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_RequestToSendXOnXOff()
        {
            Verify_Handshake(Handshake.RequestToSendXOnXOff);
        }

        #endregion

        #region Verification for Test Cases

        private IAsyncResult WriteRndByteArray(SerialPort com, int byteLength)
        {
            var buffer = new byte[byteLength];
            var rndGen = new Random(-55);

            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)rndGen.Next(0, 256);
            }

            return com.BaseStream.BeginWrite(buffer, 0, buffer.Length, null, null);
        }

        private static void VerifyWriteException(Stream serialStream, Type expectedException)
        {
            Assert.Throws(expectedException, () =>
            {
                IAsyncResult writeAsyncResult = serialStream.BeginWrite(new byte[BYTE_SIZE_EXCEPTION], 0, BYTE_SIZE_EXCEPTION, null, null);
                writeAsyncResult.AsyncWaitHandle.WaitOne();
            });
        }

        private void VerifyTimeout(int writeTimeout)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var asyncRead = new AsyncWrite(com1);
                var asyncEndWrite = new Task(asyncRead.EndWrite);
                var asyncCallbackCalled = false;

                com1.Open();
                com2.Open();
                com1.Handshake = Handshake.RequestToSend;
                com1.WriteTimeout = writeTimeout;

                IAsyncResult writeAsyncResult = com1.BaseStream.BeginWrite(new byte[8], 0, 8, ar => asyncCallbackCalled = true, null);
                asyncRead.WriteAsyncResult = writeAsyncResult;

                Thread.Sleep(100 > com1.WriteTimeout ? 2 * com1.WriteTimeout : 200);
                // Sleep for 200ms or 2 times the WriteTimeout

                if (writeAsyncResult.IsCompleted)
                {
                    // Verify the IAsyncResult has not completed
                    Fail("Err_565088aueiud!!!: Expected read to not have completed");
                }

                asyncEndWrite.Start();
                TCSupport.WaitForTaskToStart(asyncEndWrite);
                Thread.Sleep(100 < com1.WriteTimeout ? 2 * com1.WriteTimeout : 200);
                // Sleep for 200ms or 2 times the WriteTimeout

                if (asyncEndWrite.IsCompleted)
                {
                    // Verify EndRead is blocking and is still alive
                    Fail("Err_4085858aiehe!!!: Expected read to not have completed");
                }

                if (asyncCallbackCalled)
                {
                    Fail("Err_750551aiuehd!!!: Expected AsyncCallback not to be called");
                }

                com2.RtsEnable = true;

                TCSupport.WaitForTaskCompletion(asyncEndWrite);
                var waitTime = 0;
                while (!asyncCallbackCalled && waitTime < 5000)
                {
                    Thread.Sleep(50);
                    waitTime += 50;
                }

                if (!asyncCallbackCalled)
                {
                    Fail(
                        "Err_21208aheide!!!: Expected AsyncCallback to be called after some data was written to the port");
                }
            }
        }

        private void Verify_Handshake(Handshake handshake)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var XOffBuffer = new byte[1];
                var XOnBuffer = new byte[1];
                var waitTime = 0;

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
                IAsyncResult writeAsyncResult = WriteRndByteArray(com1, BYTE_SIZE_HANDSHAKE);
                while (BYTE_SIZE_HANDSHAKE > com1.BytesToWrite && waitTime < 500)
                {
                    Thread.Sleep(50);
                    waitTime += 50;
                }

                // Verify that the correct number of bytes are in the buffer
                if (BYTE_SIZE_HANDSHAKE != com1.BytesToWrite)
                {
                    Fail("ERROR!!! Expcted BytesToWrite={0} actual {1}", BYTE_SIZE_HANDSHAKE,
                        com1.BytesToWrite);
                }

                // Verify that CtsHolding is false if the RequestToSend or RequestToSendXOnXOff handshake method is used
                if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) &&
                    com1.CtsHolding)
                {
                    Fail("ERROR!!! Expcted CtsHolding={0} actual {1}", false, com1.CtsHolding);
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

                // Wait till write finishes
                while (!writeAsyncResult.IsCompleted)
                    Thread.Sleep(100);

                // Verify that the correct number of bytes are in the buffer
                if (0 != com1.BytesToWrite)
                {
                    Fail("ERROR!!! Expcted BytesToWrite=0 actual {0}", com1.BytesToWrite);
                }

                // Verify that CtsHolding is true if the RequestToSend or RequestToSendXOnXOff handshake method is used
                if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) &&
                    !com1.CtsHolding)
                {
                    Fail("ERROR!!! Expcted CtsHolding={0} actual {1}", true, com1.CtsHolding);
                }
            }
        }

        private class AsyncWrite
        {
            private readonly SerialPort _com;
            private IAsyncResult _writeAsyncResult;

            public AsyncWrite(SerialPort com)
            {
                _com = com;
            }

            public IAsyncResult WriteAsyncResult
            {
                get
                {
                    return _writeAsyncResult;
                }
                set
                {
                    _writeAsyncResult = value;
                }
            }

            public void EndWrite()
            {
                _com.BaseStream.EndWrite(_writeAsyncResult);
            }
        }

        #endregion
    }
}

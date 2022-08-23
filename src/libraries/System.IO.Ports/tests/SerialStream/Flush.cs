// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_Flush : PortsTest
    {
        // The string used with Write(str) to fill the input buffer
        private const int DEFAULT_BUFFER_SIZE = 32;
        private const int MAX_WAIT_TIME = 500;

        // The buffer length used whe filling the output buffer
        private const int DEFAULT_BUFFER_LENGTH = 8;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Flush_Open_Close()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.Close();

                Debug.WriteLine("Verifying Flush throws exception After Open() then Close()");

                VerifyException(serialStream, typeof(ObjectDisposedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Flush_Open_BaseStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.BaseStream.Close();

                Debug.WriteLine("Verifying Flush throws exception After Open() then BaseStream.Close()");

                VerifyException(serialStream, typeof(ObjectDisposedException));
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void InBufferFilled_Flush_Once()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

                Debug.WriteLine("Verifying Flush method after input buffer has been filled");
                com1.Open();
                com2.Open();

                for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                VerifyFlush(com1);
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void InBufferFilled_Flush_Multiple()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

                Debug.WriteLine("Verifying call Flush method several times after input buffer has been filled");
                com1.Open();
                com2.Open();

                for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                VerifyFlush(com1);
                VerifyFlush(com1);
                VerifyFlush(com1);
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void InBufferFilled_Flush_Cycle()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

                Debug.WriteLine(
                    "Verifying call Flush method after input buffer has been filled discarded and filled again");

                com1.Open();
                com2.Open();

                for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                VerifyFlush(com1);

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                VerifyFlush(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void OutBufferFilled_Flush_Once()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
                var t = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                Debug.WriteLine("Verifying Flush method after output buffer has been filled");

                com1.Open();
                com1.WriteTimeout = 500;
                com1.Handshake = Handshake.RequestToSend;

                t.Start();

                TCSupport.WaitForWriteBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void OutBufferFilled_Flush_Multiple()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
                var t = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                Debug.WriteLine("Verifying call Flush method several times after output buffer has been filled");

                com1.Open();
                com1.WriteTimeout = 500;
                com1.Handshake = Handshake.RequestToSend;

                t.Start();

                TCSupport.WaitForWriteBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

                VerifyFlush(com1);
                VerifyFlush(com1);
                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void OutBufferFilled_Flush_Cycle()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
                var t1 = new Task(asyncWriteRndByteArray.WriteRndByteArray);
                var t2 = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                int elapsedTime;

                Debug.WriteLine(
                    "Verifying call Flush method after output buffer has been filled discarded and filled again");

                com1.Open();
                com1.WriteTimeout = 500;
                com1.Handshake = Handshake.RequestToSend;

                t1.Start();
                elapsedTime = 0;

                while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
                {
                    Thread.Sleep(50);
                    elapsedTime += 50;
                }

                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t1);

                t2.Start();

                TCSupport.WaitForWriteBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t2);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void InOutBufferFilled_Flush_Once()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
                var t = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

                Debug.WriteLine("Verifying Flush method after input and output buffer has been filled");

                com1.Open();
                com2.Open();
                com1.WriteTimeout = 500;
                com1.Handshake = Handshake.RequestToSend;

                for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                t.Start();

                TCSupport.WaitForWriteBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void InOutBufferFilled_Flush_Multiple()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
                var t = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                int elapsedTime = 0;
                byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

                Debug.WriteLine(
                    "Verifying call Flush method several times after input and output buffer has been filled");

                com1.Open();
                com2.Open();
                com1.WriteTimeout = 500;
                com1.Handshake = Handshake.RequestToSend;

                for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                t.Start();
                elapsedTime = 0;

                while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
                {
                    Thread.Sleep(50);
                    elapsedTime += 50;
                }

                VerifyFlush(com1);
                VerifyFlush(com1);
                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void InOutBufferFilled_Flush_Cycle()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
                var t1 = new Task(asyncWriteRndByteArray.WriteRndByteArray);
                var t2 = new Task(asyncWriteRndByteArray.WriteRndByteArray);

                byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

                Debug.WriteLine(
                    "Verifying call Flush method after input and output buffer has been filled discarded and filled again");

                com1.Open();
                com2.Open();
                com1.WriteTimeout = 500;
                com1.Handshake = Handshake.RequestToSend;

                for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                t1.Start();

                TCSupport.WaitForWriteBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t1);

                t2.Start();

                TCSupport.WaitForWriteBufferToLoad(com1, DEFAULT_BUFFER_LENGTH);

                com2.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_BUFFER_SIZE);

                VerifyFlush(com1);

                TCSupport.WaitForTaskCompletion(t2);
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
                byte[] buffer = new byte[_byteLength];
                Random rndGen = new Random(-55);

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)rndGen.Next(0, 256);
                }

                try
                {
                    _com.Write(buffer, 0, buffer.Length);
                }
                catch (TimeoutException)
                {
                }
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyException(Stream serialStream, Type expectedException)
        {
            Assert.Throws(expectedException, () =>
            {
                serialStream.Flush();
            });
        }

        private void VerifyFlush(SerialPort com)
        {
            int origBytesToRead = com.BytesToRead;
            int i = 0;

            com.BaseStream.Flush();

            if (origBytesToRead != com.BytesToRead)
            {
                Fail("ERROR!!! Expected BytesToRead={0} Actual BytesToRead={1}", origBytesToRead, com.BytesToRead);
            }

            if (0 != com.BytesToWrite)
            {
                Fail("ERROR!!! Expected BytesToWrite=0 Actual BytesToWrite={0}", com.BytesToWrite);
            }

            com.ReadTimeout = 0;

            if (origBytesToRead != 0)
            {
                while (true)
                {
                    int byteRead;
                    try
                    {
                        byteRead = com.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }

                    if (i != byteRead)
                    {
                        Fail("Err_7083apnh Expecte to read={0} actual={1}", i, byteRead);
                    }

                    i++;
                }

                if (i != DEFAULT_BUFFER_SIZE)
                {
                    Fail("Err_09778asdh Expected to read {0} bytes actually read {1}", DEFAULT_BUFFER_SIZE, i);
                }
            }
        }
        #endregion
    }
}

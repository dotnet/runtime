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
    public class DiscardInBuffer : PortsTest
    {
        //The string used with Write(str) to fill the input buffer
        private const string DEFAULT_STRING = "Hello World";

        //The buffer lenght used whe filling the ouput buffer
        private const int DEFAULT_BUFFER_LENGTH = 8;

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void InBufferFilled_Discard_Once()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Debug.WriteLine("Verifying Discard method after input buffer has been filled");
                com1.Open();
                com2.Open();
                com2.Write(DEFAULT_STRING);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_STRING.Length);

                VerifyDiscard(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void InBufferFilled_Discard_Multiple()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Debug.WriteLine("Verifying call Discard method several times after input buffer has been filled");
                com1.Open();
                com2.Open();
                com2.Write(DEFAULT_STRING);
                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_STRING.Length);

                VerifyDiscard(com1);
                VerifyDiscard(com1);
                VerifyDiscard(com1);
                /*if (!retValue)
            {
                Debug.WriteLine(
                    "Err_002!!! Verifying call Discard method several times after input buffer has been filled FAILED");
            }*/
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void InBufferFilled_Discard_Cycle()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Debug.WriteLine(
                    "Verifying call Discard method after input buffer has been filled discarded and filled again");
                com1.Open();
                com2.Open();
                com2.Write(DEFAULT_STRING);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_STRING.Length);

                VerifyDiscard(com1);
                com2.Write(DEFAULT_STRING);

                TCSupport.WaitForReadBufferToLoad(com1, DEFAULT_STRING.Length);

                VerifyDiscard(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void InAndOutBufferFilled_Discard()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                int origBytesToWrite;

                Debug.WriteLine("Verifying Discard method after input buffer has been filled");
                com1.Open();
                com2.Open();
                com1.WriteTimeout = 500;
                var task = Task.Run(() => WriteRndByteArray(com1, DEFAULT_BUFFER_LENGTH));
                Thread.Sleep(100);
                origBytesToWrite = com1.BytesToWrite;
                VerifyDiscard(com1);
                Assert.Equal(com1.BytesToWrite, origBytesToWrite);

                //Wait for write method to timeout
                TCSupport.WaitForTaskCompletion(task);
            }
        }


        private void WriteRndByteArray(SerialPort com, int byteLength)
        {
            byte[] buffer = new byte[byteLength];
            Random rndGen = new Random();

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)rndGen.Next(0, 256);
            }

            try
            {
                com.Write(buffer, 0, buffer.Length);
            }
            catch (TimeoutException)
            {
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyDiscard(SerialPort com)
        {
            com.DiscardInBuffer();
            Assert.Equal(0, com.BytesToRead);

            com.ReadTimeout = 0;

            Assert.Throws<TimeoutException>(() => com.ReadByte());
        }
        #endregion
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace Legacy.SerialStream
{
    public class EndWrite : PortsTest
    {
        #region Test Cases

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void EndWriteAfterClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying EndWrite method throws exception after a call to Close()");

                com.Open();
                Stream serialStream = com.BaseStream;
                IAsyncResult asyncResult = com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);

                com.Close();

                VerifyEndWriteException(serialStream, asyncResult, null);
            }
        }

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void EndWriteAfterSerialStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying EndWrite method throws exception after a call to BaseStream.Close()");

                com.Open();
                Stream serialStream = com.BaseStream;
                IAsyncResult asyncResult = com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);

                serialStream.Close();

                VerifyEndWriteException(serialStream, asyncResult, null);

                com.Close();
                Thread.Sleep(200);

                // Give the port time to finish closing since we have an unclosed BeginWrite - see  - see Dev10 #591344
            }
        }

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void AsyncResult_Null()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying EndWrite with null asyncResult");

                com.Open();
                VerifyEndWriteException(com.BaseStream, null, typeof(ArgumentNullException));
            }
        }

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void AsyncResult_ReadResult()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying EndWrite with asyncResult returned from read");

                com.Open();

                com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);
                com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);

                IAsyncResult writeAsyncResult = com.BaseStream.BeginRead(new byte[8], 0, 8, null, null);
                VerifyEndWriteException(com.BaseStream, writeAsyncResult, typeof(ArgumentException));

                // Not needed if this scenario is run last.
                // System.Threading.Thread.Sleep(200);  // Give the port time to finish closing since we have an unclosed BeginWrite
            }
        }

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void AsyncResult_MultipleSameResult()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                var numBytesToWrite = 8;

                Debug.WriteLine("Verifying calling EndWrite twice with the same asyncResult");

                com.Open();

                IAsyncResult writeAsyncResult = com.BaseStream.BeginWrite(new byte[numBytesToWrite], 0, numBytesToWrite, null, null);
                com.BaseStream.EndWrite(writeAsyncResult);

                VerifyEndWriteException(com.BaseStream, writeAsyncResult, typeof(ArgumentException));
            }
        }

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void AsyncResult_MultipleInOrder()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int numBytesToWrite1 = 8, numBytesToWrite2 = 16, numBytesToWrite3 = 10;

                Debug.WriteLine("Verifying EndWrite with multiple calls to BeginRead");

                com.Open();

                IAsyncResult readAsyncResult1 = com.BaseStream.BeginWrite(new byte[numBytesToWrite1], 0, numBytesToWrite1, null, null);
                IAsyncResult readAsyncResult2 = com.BaseStream.BeginWrite(new byte[numBytesToWrite2], 0, numBytesToWrite2, null, null);
                IAsyncResult readAsyncResult3 = com.BaseStream.BeginWrite(new byte[numBytesToWrite3], 0, numBytesToWrite3, null, null);

                com.BaseStream.EndWrite(readAsyncResult1);
                com.BaseStream.EndWrite(readAsyncResult2);
                com.BaseStream.EndWrite(readAsyncResult3);
            }
        }

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void AsyncResult_MultipleOutOfOrder()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int numBytesToWrite1 = 8, numBytesToWrite2 = 16, numBytesToWrite3 = 10;

                Debug.WriteLine(
                    "Verifying calling EndWrite with different asyncResults out of order returned from BeginRead");

                com.Open();

                IAsyncResult readAsyncResult1 = com.BaseStream.BeginWrite(new byte[numBytesToWrite1], 0, numBytesToWrite1, null, null);
                IAsyncResult readAsyncResult2 = com.BaseStream.BeginWrite(new byte[numBytesToWrite2], 0, numBytesToWrite2, null, null);
                IAsyncResult readAsyncResult3 = com.BaseStream.BeginWrite(new byte[numBytesToWrite3], 0, numBytesToWrite3, null, null);

                com.BaseStream.EndWrite(readAsyncResult2);
                com.BaseStream.EndWrite(readAsyncResult3);
                com.BaseStream.EndWrite(readAsyncResult1);
            }
        }

        [ActiveIssue(16055)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void InBreak()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying EndWrite throws InvalidOperationException while in a Break");
                com1.WriteTimeout = 5000;
                com1.Open();

                IAsyncResult readAsyncResult = com1.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);
                com1.BreakState = true;

                Assert.Throws<InvalidOperationException>(() => com1.BaseStream.EndWrite(readAsyncResult));

                com1.DiscardOutBuffer();
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyEndWriteException(Stream serialStream, IAsyncResult asyncResult, Type expectedException)
        {
            if (expectedException == null)
            {
                serialStream.EndWrite(asyncResult);
            }
            else
            {
                Assert.Throws(expectedException, () => serialStream.EndWrite(asyncResult));
            }
        }
        #endregion
    }
}

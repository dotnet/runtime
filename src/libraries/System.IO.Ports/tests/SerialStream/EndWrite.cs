// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_EndWrite : PortsTest
    {
        #region Test Cases
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

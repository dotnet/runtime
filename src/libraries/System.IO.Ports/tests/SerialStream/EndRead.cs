// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_EndRead : PortsTest
    {
        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void EndReadAfterClose()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Debug.WriteLine("Verifying EndRead method throws exception after a call to Close()");

                com1.Open();
                com2.Open();

                Stream serialStream = com1.BaseStream;
                IAsyncResult asyncResult = com1.BaseStream.BeginRead(new byte[8], 0, 8, null, null);

                com2.Write(new byte[16], 0, 16);

                TCSupport.WaitForReadBufferToLoad(com1, 1);

                com1.Close();

                VerifyEndReadException(serialStream, asyncResult, null);
            }
            // Give the port time to finish closing since we potentially have an unclosed BeginRead/BeginWrite
            Thread.Sleep(200);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void EndReadAfterSerialStreamClose()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Debug.WriteLine("Verifying EndRead method throws exception after a call to BaseStream.Close()");

                com1.Open();
                com2.Open();

                Stream serialStream = com1.BaseStream;
                IAsyncResult asyncResult = com1.BaseStream.BeginRead(new byte[8], 0, 8, null, null);

                com2.Write(new byte[16], 0, 16);

                TCSupport.WaitForReadBufferToLoad(com1, 1);

                com1.BaseStream.Close();

                VerifyEndReadException(serialStream, asyncResult, null);
            }
            // Give the port time to finish closing since we potentially have an unclosed BeginRead/BeginWrite
            Thread.Sleep(200);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void AsyncResult_Null()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying EndRead with null asyncResult");

                com.Open();
                VerifyEndReadException(com.BaseStream, null, typeof(ArgumentNullException));
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void AsyncResult_MultipleInOrder()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                int endReadReturnValue;
                int numBytesToRead1 = 8, numBytesToRead2 = 16, numBytesToRead3 = 10;
                int totalBytesToRead = numBytesToRead1 + numBytesToRead2 + numBytesToRead3;

                Debug.WriteLine("Verifying EndRead with multiple calls to BeginRead");

                com1.Open();
                com2.Open();

                com2.Write(new byte[totalBytesToRead], 0, totalBytesToRead);

                TCSupport.WaitForReadBufferToLoad(com1, totalBytesToRead);

                IAsyncResult readAsyncResult1 = com1.BaseStream.BeginRead(new byte[numBytesToRead1], 0, numBytesToRead1, null, null);
                IAsyncResult readAsyncResult2 = com1.BaseStream.BeginRead(new byte[numBytesToRead2], 0, numBytesToRead2, null, null);
                IAsyncResult readAsyncResult3 = com1.BaseStream.BeginRead(new byte[numBytesToRead3], 0, numBytesToRead3, null, null);

                if (numBytesToRead1 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult1)))
                {
                    Fail("ERROR!!! Expected EndRead to return={0} actual={1} for first read", numBytesToRead1,
                        endReadReturnValue);
                }

                if (numBytesToRead2 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult2)))
                {
                    Fail("ERROR!!! Expected EndRead to return={0} actual={1} for second read", numBytesToRead2,
                        endReadReturnValue);
                }

                if (numBytesToRead3 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult3)))
                {
                    Fail("ERROR!!! Expected EndRead to return={0} actual={1} for third read", numBytesToRead3,
                        endReadReturnValue);
                }
            }
            // Give the port time to finish closing since we potentially have an unclosed BeginRead/BeginWrite
            Thread.Sleep(200);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void AsyncResult_MultipleOutOfOrder()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                int endReadReturnValue;
                int numBytesToRead1 = 8, numBytesToRead2 = 16, numBytesToRead3 = 10;
                int totalBytesToRead = numBytesToRead1 + numBytesToRead2 + numBytesToRead3;

                Debug.WriteLine(
                    "Verifying calling EndRead with different asyncResults out of order returned from BeginRead");

                com1.Open();
                com2.Open();

                com2.Write(new byte[totalBytesToRead], 0, totalBytesToRead);

                TCSupport.WaitForReadBufferToLoad(com1, totalBytesToRead);

                IAsyncResult readAsyncResult1 = com1.BaseStream.BeginRead(new byte[numBytesToRead1], 0, numBytesToRead1, null, null);
                IAsyncResult readAsyncResult2 = com1.BaseStream.BeginRead(new byte[numBytesToRead2], 0, numBytesToRead2, null, null);
                IAsyncResult readAsyncResult3 = com1.BaseStream.BeginRead(new byte[numBytesToRead3], 0, numBytesToRead3, null, null);

                if (numBytesToRead2 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult2)))
                {
                    Fail("ERROR!!! Expected EndRead to return={0} actual={1} for second read", numBytesToRead2,
                        endReadReturnValue);
                }

                if (numBytesToRead3 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult3)))
                {
                    Fail("ERROR!!! Expected EndRead to return={0} actual={1} for third read", numBytesToRead3,
                        endReadReturnValue);
                }

                if (numBytesToRead1 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult1)))
                {
                    Fail("ERROR!!! Expected EndRead to return={0} actual={1} for first read", numBytesToRead1,
                        endReadReturnValue);
                }
            }
            // Give the port time to finish closing since we potentially have an unclosed BeginRead/BeginWrite
            Thread.Sleep(200);
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyEndReadException(Stream serialStream, IAsyncResult asyncResult, Type expectedException)
        {
            if (expectedException == null)
            {
                serialStream.EndRead(asyncResult);
            }
            else
            {
                Assert.Throws(expectedException, () => serialStream.EndRead(asyncResult));
            }
        }
        #endregion
    }
}

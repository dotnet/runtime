// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_ReadByte : PortsTest
    {
        // The number of random bytes to receive
        private const int numRndByte = 8;

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void ASCIIEncoding()
        {
            Debug.WriteLine("Verifying read with bytes encoded with ASCIIEncoding");
            VerifyRead(new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UTF8Encoding()
        {
            Debug.WriteLine("Verifying read with bytes encoded with UTF8Encoding");
            VerifyRead(new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UTF32Encoding()
        {
            Debug.WriteLine("Verifying read with bytes encoded with UTF32Encoding");
            VerifyRead(new UTF32Encoding());
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyRead(Encoding encoding)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var rndGen = new Random(-55);

                int bufferSize = numRndByte;
                var byteXmitBuffer = new byte[bufferSize];

                // Generate random bytes
                for (int i = 0; i < byteXmitBuffer.Length; i++)
                {
                    byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
                }

                com1.ReadTimeout = 100;
                com1.Encoding = encoding;

                com1.Open();
                com2.Open();

                com2.Write(byteXmitBuffer, 0, bufferSize);

                for (int i = 0; i < bufferSize; i++)
                {
                    Assert.Equal(byteXmitBuffer[i], com1.BaseStream.ReadByte());
                }

                // did we receive more bytes than sent?
                Assert.Throws<TimeoutException>(() => com1.BaseStream.ReadByte());
            }
        }
        #endregion
    }
}

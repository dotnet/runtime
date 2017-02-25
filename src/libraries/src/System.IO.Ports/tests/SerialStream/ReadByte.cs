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
                var byteRcvBuffer = new byte[bufferSize];
                int i;

                // Generate random bytes
                for (i = 0; i < byteXmitBuffer.Length; i++)
                {
                    byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
                }

                com1.ReadTimeout = 500;
                com1.Encoding = encoding;

                com1.Open();
                com2.Open();

                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);
                Thread.Sleep((int)(((byteXmitBuffer.Length * 10.0) / com1.BaudRate) * 1000));

                i = 0;

                while (true)
                {
                    int readInt;
                    try
                    {
                        readInt = com1.BaseStream.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }

                    // While their are more bytes to be read
                    if (byteXmitBuffer.Length <= i)
                    {
                        // If we have read in more bytes then were actually sent
                        Fail("ERROR!!!: We have received more bytes then were sent");
                        break;
                    }

                    byteRcvBuffer[i] = (byte)readInt;
                    if (readInt != byteXmitBuffer[i])
                    {
                        // If the byte read is not the expected byte
                        Fail("ERROR!!!: Expected to read {0}  actual read byte {1}", (int)byteXmitBuffer[i], readInt);
                    }

                    i++;

                    Assert.Equal(byteXmitBuffer.Length - i, com1.BytesToRead);
                }
            }
        }
        #endregion
    }
}

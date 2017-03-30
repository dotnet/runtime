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
    public class SerialStream_WriteByte : PortsTest
    {
        // The large number of times the write method is called when verifying write
        private const int LARGE_NUM_WRITES = 2048;

        // The default number of times the write method is called when verifying write
        private const int DEFAULT_NUM_WRITES = 8;

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void ASCIIEncoding()
        {
            VerifyWrite(new ASCIIEncoding(), DEFAULT_NUM_WRITES);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UTF8Encoding()
        {
            VerifyWrite(new UTF8Encoding(), DEFAULT_NUM_WRITES);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UTF32Encoding()
        {
            VerifyWrite(new UTF32Encoding(), DEFAULT_NUM_WRITES);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void UnicodeEncoding()
        {
            VerifyWrite(new UnicodeEncoding(), DEFAULT_NUM_WRITES);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void LargeBuffer()
        {
            VerifyWrite(new ASCIIEncoding(), LARGE_NUM_WRITES);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void InBreak()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying WriteByte throws InvalidOperationException while in a Break");
                com1.Open();
                com1.BreakState = true;

                Assert.Throws<InvalidOperationException>(() => com1.BaseStream.WriteByte(1));
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyWrite(Encoding encoding, int numWrites)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var rndGen = new Random(-55);
                var buffer = new byte[numWrites];

                Debug.WriteLine("Verifying calling write method {0} timees with endocing={1}", numWrites,
                    encoding.EncodingName);

                com1.Encoding = encoding;
                com2.Encoding = encoding;

                com1.Open();
                com2.Open();

                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)rndGen.Next(0, 256);
                }

                VerifyWriteByteArray(com1, com2, buffer);
            }
        }

        private void VerifyWriteByteArray(SerialPort com1, SerialPort com2, byte[] buffer)
        {
            var index = 0;
            var actualBytes = new byte[buffer.Length];

            foreach (byte dataByte in buffer)
            {
                com1.BaseStream.WriteByte(dataByte);
            }

            com2.ReadTimeout = 500;
            Thread.Sleep((int)(((buffer.Length * 10.0) / com1.BaudRate) * 1000) + 250);

            while (true)
            {
                int byteRead;
                try
                {
                    byteRead = com2.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }

                if (buffer.Length <= index)
                {
                    // If we have read in more bytes then we expect
                    Fail("ERROR!!!: We have received more bytes then were sent");
                    break;
                }

                actualBytes[index] = (byte)byteRead;
                index++;
                if (buffer.Length - index != com2.BytesToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1}", buffer.Length - index, com2.BytesToRead);
                }
            }

            // Compare the bytes that were read with the ones we expected to read
            Assert.Equal(buffer, actualBytes);
        }
        #endregion
    }
}

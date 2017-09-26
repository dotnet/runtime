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
    public class SerialStream_Read_byte_int_int : PortsTest
    {
        // The number of random bytes to receive for read method testing
        private const int numRndBytesToRead = 16;

        // The number of random bytes to receive for large input buffer testing
        private const int largeNumRndBytesToRead = 2048;

        // When we test Read and do not care about actually reading anything we must still
        // create an byte array to pass into the method the following is the size of the 
        // byte array used in this situation
        private const int defaultByteArraySize = 1;
        private const int defaultByteOffset = 0;
        private const int defaultByteCount = 1;

        // The maximum buffer size when an exception occurs
        private const int maxBufferSizeForException = 255;

        // The maximum buffer size when an exception is not expected
        private const int maxBufferSize = 8;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Buffer_Null()
        {
            VerifyReadException(null, 0, 1, typeof(ArgumentNullException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEG1()
        {
            VerifyReadException(new byte[defaultByteArraySize], -1, defaultByteCount, typeof(ArgumentOutOfRangeException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEGRND()
        {
            var rndGen = new Random(-55);

            VerifyReadException(new byte[defaultByteArraySize], rndGen.Next(int.MinValue, 0), defaultByteCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_MinInt()
        {
            VerifyReadException(new byte[defaultByteArraySize], int.MinValue, defaultByteCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEG1()
        {
            VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, -1, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEGRND()
        {
            var rndGen = new Random(-55);

            VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, rndGen.Next(int.MinValue, 0), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_MinInt()
        {
            VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_EQ_Length_Plus_1()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = bufferLength + 1 - offset;
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_GT_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = rndGen.Next(bufferLength + 1 - offset, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_GT_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(bufferLength, int.MaxValue);
            int count = defaultByteCount;
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_GT_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = defaultByteOffset;
            int count = rndGen.Next(bufferLength + 1, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new byte[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void OffsetCount_EQ_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = bufferLength - offset;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Offset_EQ_Length_Minus_1()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = bufferLength - 1;
            var count = 1;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Count_EQ_Length()
        {
            var rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            var offset = 0;
            int count = bufferLength;

            VerifyRead(new byte[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void LargeInputBuffer()
        {
            int bufferLength = largeNumRndBytesToRead;
            var offset = 0;
            int count = bufferLength;

            VerifyRead(new byte[bufferLength], offset, count, largeNumRndBytesToRead);
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyReadException(byte[] buffer, int offset, int count, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int bufferLength = null == buffer ? 0 : buffer.Length;

                Debug.WriteLine("Verifying read method throws {0} buffer.Lenght={1}, offset={2}, count={3}",
                    expectedException, bufferLength, offset, count);
                com.Open();

                Assert.Throws(expectedException, () => com.BaseStream.Read(buffer, offset, count));
            }
        }

        private void VerifyRead(byte[] buffer, int offset, int count)
        {
            VerifyRead(buffer, offset, count, numRndBytesToRead);
        }

        private void VerifyRead(byte[] buffer, int offset, int count, int numberOfBytesToRead)
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                var rndGen = new Random(-55);
                var bytesToWrite = new byte[numberOfBytesToRead];

                // Generate random bytes
                for (var i = 0; i < bytesToWrite.Length; i++)
                {
                    var randByte = (byte)rndGen.Next(0, 256);

                    bytesToWrite[i] = randByte;
                }

                // Generate some random bytes in the buffer
                for (var i = 0; i < buffer.Length; i++)
                {
                    var randByte = (byte)rndGen.Next(0, 256);

                    buffer[i] = randByte;
                }

                Debug.WriteLine(
                    "Verifying read method buffer.Lenght={0}, offset={1}, count={2} with {3} random chars",
                    buffer.Length, offset, count, bytesToWrite.Length);

                com1.ReadTimeout = 500;
                com1.Open();
                com2.Open();

                VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, buffer, offset, count);
            }
        }

        private void VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
        {
            var buffer = new byte[bytesToWrite.Length];
            int totalBytesRead;
            int bytesToRead;
            var oldRcvBuffer = (byte[])rcvBuffer.Clone();

            com2.Write(bytesToWrite, 0, bytesToWrite.Length);
            com1.ReadTimeout = 500;
            Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

            totalBytesRead = 0;
            bytesToRead = com1.BytesToRead;

            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = com1.BaseStream.Read(rcvBuffer, offset, count);
                }
                catch (TimeoutException)
                {
                    break;
                }

                // While their are more characters to be read
                if ((bytesToRead > bytesRead && count != bytesRead) || (bytesToRead <= bytesRead && bytesRead != bytesToRead))
                {
                    // If we have not read all of the characters that we should have
                    Fail("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                }

                if (bytesToWrite.Length < totalBytesRead + bytesRead)
                {
                    // If we have read in more characters then we expect
                    Fail("ERROR!!!: We have received more characters then were sent");
                }

                VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, bytesRead);

                Array.Copy(rcvBuffer, offset, buffer, totalBytesRead, bytesRead);
                totalBytesRead += bytesRead;

                if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1}", bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                }

                oldRcvBuffer = (byte[])rcvBuffer.Clone();
                bytesToRead = com1.BytesToRead;
            }

            // Compare the bytes that were written with the ones we read
            Assert.Equal(bytesToWrite, buffer);
        }

        private void VerifyBuffer(byte[] actualBuffer, byte[] expectedBuffer, int offset, int count)
        {
            // Verify all character before the offset
            for (var i = 0; i < offset; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }

            // Verify all character after the offset + count
            for (int i = offset + count; i < actualBuffer.Length; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }
        }
        #endregion
    }
}

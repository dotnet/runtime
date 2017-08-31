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
    public class Write_char_int_int : PortsTest
    {
        //The string size used for large char array testing
        private const int LARGE_BUFFER_SIZE = 2048;

        //When we test Write and do not care about actually writing anything we must still
        //create an cahr array to pass into the method the following is the size of the 
        //char array used in this situation
        private const int DEFAULT_BUFFER_SIZE = 1;
        private const int DEFAULT_CHAR_OFFSET = 0;
        private const int DEFAULT_CHAR_COUNT = 1;

        //The maximum buffer size when an exception occurs
        private const int MAX_BUFFER_SIZE_FOR_EXCEPTION = 255;

        //The maximum buffer size when an exception is not expected
        private const int MAX_BUFFER_SIZE = 8;

        //The default number of times the write method is called when verifying write
        private const int DEFAULT_NUM_WRITES = 3;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Buffer_Null()
        {
            VerifyWriteException(null, 0, 1, typeof(ArgumentNullException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEG1()
        {
            VerifyWriteException(new char[DEFAULT_BUFFER_SIZE], -1, DEFAULT_CHAR_COUNT, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEGRND()
        {
            Random rndGen = new Random(-55);

            VerifyWriteException(new char[DEFAULT_BUFFER_SIZE], rndGen.Next(int.MinValue, 0), DEFAULT_CHAR_COUNT, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_MinInt()
        {
            VerifyWriteException(new char[DEFAULT_BUFFER_SIZE], int.MinValue, DEFAULT_CHAR_COUNT, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEG1()
        {
            VerifyWriteException(new char[DEFAULT_BUFFER_SIZE], DEFAULT_CHAR_OFFSET, -1, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEGRND()
        {
            Random rndGen = new Random(-55);

            VerifyWriteException(new char[DEFAULT_BUFFER_SIZE], DEFAULT_CHAR_OFFSET, rndGen.Next(int.MinValue, 0), typeof(ArgumentOutOfRangeException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_MinInt()
        {
            VerifyWriteException(new char[DEFAULT_BUFFER_SIZE], DEFAULT_CHAR_OFFSET, int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_EQ_Length_Plus_1()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = rndGen.Next(0, bufferLength);
            int count = bufferLength + 1 - offset;
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new char[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = rndGen.Next(0, bufferLength);
            int count = rndGen.Next(bufferLength + 1 - offset, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new char[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = rndGen.Next(bufferLength, int.MaxValue);
            int count = DEFAULT_CHAR_COUNT;
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new char[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
            int offset = DEFAULT_CHAR_OFFSET;
            int count = rndGen.Next(bufferLength + 1, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyWriteException(new char[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void OffsetCount_EQ_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = bufferLength - offset;

            VerifyWrite(new char[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Offset_EQ_Length_Minus_1()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = bufferLength - 1;
            int count = 1;

            VerifyWrite(new char[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Count_EQ_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = 0;
            int count = bufferLength;

            VerifyWrite(new char[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Count_EQ_Zero()
        {
            int bufferLength = 0;
            int offset = 0;
            int count = bufferLength;

            VerifyWrite(new char[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ASCIIEncoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new char[bufferLength], offset, count, new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new char[bufferLength], offset, count, new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF32Encoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new char[bufferLength], offset, count, new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UnicodeEncoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyWrite(new char[bufferLength], offset, count, new UnicodeEncoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void LargeBuffer()
        {
            int bufferLength = LARGE_BUFFER_SIZE;
            int offset = 0;
            int count = bufferLength;

            VerifyWrite(new char[bufferLength], offset, count, 1);
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyWriteException(char[] buffer, int offset, int count, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int bufferLength = null == buffer ? 0 : buffer.Length;

                Debug.WriteLine("Verifying write method throws {0} buffer.Lenght={1}, offset={2}, count={3}", expectedException, bufferLength, offset, count);
                com.Open();

                try
                {
                    com.Write(buffer, offset, count);

                    Fail("ERROR!!!: No Excpetion was thrown");
                }
                catch (Exception e)
                {
                    if (e.GetType() != expectedException)
                    {
                        Fail("ERROR!!!: {0} exception was thrown expected {1}", e.GetType(), expectedException);
                    }
                }
                if (com.IsOpen)
                    com.Close();
            }
        }

        private void VerifyWrite(char[] buffer, int offset, int count)
        {
            VerifyWrite(buffer, offset, count, new ASCIIEncoding());
        }

        private void VerifyWrite(char[] buffer, int offset, int count, int numWrites)
        {
            VerifyWrite(buffer, offset, count, new ASCIIEncoding(), numWrites);
        }

        private void VerifyWrite(char[] buffer, int offset, int count, Encoding encoding)
        {
            VerifyWrite(buffer, offset, count, encoding, DEFAULT_NUM_WRITES);
        }

        private void VerifyWrite(char[] buffer, int offset, int count, Encoding encoding, int numWrites)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying write method buffer.Lenght={0}, offset={1}, count={2}, endocing={3}",
                    buffer.Length, offset, count, encoding.EncodingName);

                com1.Encoding = encoding;
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                TCSupport.GetRandomChars(buffer, 0, buffer.Length, TCSupport.CharacterOptions.Surrogates);

                VerifyWriteCharArray(buffer, offset, count, com1, com2, numWrites);
            }
        }

        private void VerifyWriteCharArray(char[] buffer, int offset, int count, SerialPort com1, SerialPort com2)
        {
            VerifyWriteCharArray(buffer, offset, count, com1, com2, DEFAULT_NUM_WRITES);
        }

        private void VerifyWriteCharArray(char[] buffer, int offset, int count, SerialPort com1, SerialPort com2, int numWrites)
        {
            char[] oldBuffer, expectedChars, actualChars;
            byte[] expectedBytes, actualBytes;
            int byteRead;
            int index = 0;

            oldBuffer = (char[])buffer.Clone();

            expectedBytes = com1.Encoding.GetBytes(buffer, offset, count);
            expectedChars = com1.Encoding.GetChars(expectedBytes);
            actualBytes = new byte[expectedBytes.Length * numWrites];

            for (int i = 0; i < numWrites; i++)
            {
                com1.Write(buffer, offset, count);
            }

            com2.ReadTimeout = 500;
            Thread.Sleep((int)(((expectedBytes.Length * numWrites * 10.0) / com1.BaudRate) * 1000) + 250);

            Assert.Equal(oldBuffer, buffer);

            while (true)
            {
                try
                {
                    byteRead = com2.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }

                if (actualBytes.Length <= index)
                {
                    //If we have read in more bytes then we expect
                    Fail("ERROR!!!: We have received more bytes then were sent");
                }

                actualBytes[index] = (byte)byteRead;
                index++;

                if (actualBytes.Length - index != com2.BytesToRead)
                {
                    Fail("ERROR!!!: Expected BytesToRead={0} actual={1}", actualBytes.Length - index, com2.BytesToRead);
                }
            }

            actualChars = com1.Encoding.GetChars(actualBytes);

            if (actualChars.Length != expectedChars.Length * numWrites)
            {
                Fail("ERROR!!!: Expected to read {0} chars actually read {1}", expectedChars.Length * numWrites, actualChars.Length);
            }
            else
            {
                //Compare the chars that were read with the ones we expected to read
                for (int j = 0; j < numWrites; j++)
                {
                    for (int i = 0; i < expectedChars.Length; i++)
                    {
                        if (expectedChars[i] != actualChars[i + expectedChars.Length * j])
                        {
                            Fail("ERROR!!!: Expected to read {0}  actual read {1} at {2}", (int)expectedChars[i], (int)actualChars[i + expectedChars.Length * j], i);
                        }
                    }
                }
            }

            //Compare the bytes that were read with the ones we expected to read
            for (int j = 0; j < numWrites; j++)
            {
                for (int i = 0; i < expectedBytes.Length; i++)
                {
                    if (expectedBytes[i] != actualBytes[i + expectedBytes.Length * j])
                    {
                        Fail("ERROR!!!: Expected to read byte {0}  actual read {1} at {2}", (int)expectedBytes[i], (int)actualBytes[i + expectedBytes.Length * j], i);
                    }
                }
            }
        }

        #endregion
    }
}

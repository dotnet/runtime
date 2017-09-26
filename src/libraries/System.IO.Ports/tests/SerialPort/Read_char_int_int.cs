// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Read_char_int_int : PortsTest
    {
        //The number of random bytes to receive for read method testing
        private const int numRndBytesToRead = 8;

        //The number of random bytes to receive for large input buffer testing
        private const int largeNumRndCharsToRead = 2048;

        //When we test Read and do not care about actually reading anything we must still
        //create an byte array to pass into the method the following is the size of the 
        //byte array used in this situation
        private const int defaultCharArraySize = 1;
        private const int defaultCharOffset = 0;
        private const int defaultCharCount = 1;

        //The maximum buffer size when an exception occurs
        private const int maxBufferSizeForException = 255;

        //The maximum buffer size when an exception is not expected
        private const int maxBufferSize = 8;

        public enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

        #region Test Cases
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Buffer_Null()
        {
            VerifyReadException(null, 0, 1, typeof(ArgumentNullException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEG1()
        {
            VerifyReadException(new char[defaultCharArraySize], -1, defaultCharCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_NEGRND()
        {
            Random rndGen = new Random(-55);

            VerifyReadException(new char[defaultCharArraySize], rndGen.Next(int.MinValue, 0), defaultCharCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_MinInt()
        {
            VerifyReadException(new char[defaultCharArraySize], int.MinValue, defaultCharCount, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEG1()
        {
            VerifyReadException(new char[defaultCharArraySize], defaultCharOffset, -1, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_NEGRND()
        {
            Random rndGen = new Random(-55);

            VerifyReadException(new char[defaultCharArraySize], defaultCharOffset, rndGen.Next(int.MinValue, 0), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_MinInt()
        {
            VerifyReadException(new char[defaultCharArraySize], defaultCharOffset, int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_EQ_Length_Plus_1()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = bufferLength + 1 - offset;
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new char[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OffsetCount_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(0, bufferLength);
            int count = rndGen.Next(bufferLength + 1 - offset, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new char[bufferLength], offset, count, expectedException);
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Offset_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = rndGen.Next(bufferLength, int.MaxValue);
            int count = defaultCharCount;
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new char[bufferLength], offset, count, expectedException);
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_GT_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSizeForException);
            int offset = defaultCharOffset;
            int count = rndGen.Next(bufferLength + 1, int.MaxValue);
            Type expectedException = typeof(ArgumentException);

            VerifyReadException(new char[bufferLength], offset, count, expectedException);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void OffsetCount_EQ_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = bufferLength - offset;

            VerifyRead(new char[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Offset_EQ_Length_Minus_1()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = bufferLength - 1;
            int count = 1;

            VerifyRead(new char[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Count_EQ_Length()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = 0;
            int count = bufferLength;

            VerifyRead(new char[bufferLength], offset, count);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Count_EQ_Zero()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Random rndGen = new Random(-55);
                int bufferLength = rndGen.Next(1, maxBufferSize);
                int offset = 0;
                int count = 0;

                com.Open();
                Assert.Equal(0, com.Read(new char[bufferLength], offset, count));
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ASCIIEncoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyRead(new char[bufferLength], offset, count, new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyRead(new char[bufferLength], offset, count, new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF32Encoding()
        {
            Random rndGen = new Random(-55);
            int bufferLength = rndGen.Next(1, maxBufferSize);
            int offset = rndGen.Next(0, bufferLength - 1);
            int count = rndGen.Next(1, bufferLength - offset);

            VerifyRead(new char[bufferLength], offset, count, new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ASCIIEncoding_1Char()
        {
            VerifyRead(new char[1], 0, 1, new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding_1Char()
        {
            VerifyRead(new char[1], 0, 1, new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF32Encoding_1Char()
        {
            VerifyRead(new char[1], 0, 1, new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void LargeInputBuffer()
        {
            int bufferLength = largeNumRndCharsToRead;
            int offset = 0;
            int count = bufferLength;

            VerifyRead(new char[bufferLength], offset, count, largeNumRndCharsToRead);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void BytesFollowedByCharsASCII()
        {
            VerifyBytesFollowedByChars(new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void BytesFollowedByCharsUTF8()
        {
            VerifyBytesFollowedByChars(new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void BytesFollowedByCharsUTF32()
        {
            VerifyBytesFollowedByChars(new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedData()
        {
            int bufferLength = 32 + 8;
            int offset = 3;
            int count = 32;

            VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedData()
        {
            int bufferLength = 8;
            int offset = 3;
            int count = 3;

            VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedAndNonBufferedData()
        {
            int bufferLength = 64 + 8;
            int offset = 3;
            int count = 64;

            VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedAndNonBufferedData()
        {
            int bufferLength = 8;
            int offset = 3;
            int count = 3;

            VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void GreedyRead()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random(-55);
                char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.Surrogates);
                byte[] byteXmitBuffer = new byte[1024];
                char utf32Char = (char)8169;
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });
                int numCharsRead;

                Debug.WriteLine(
                    "Verifying that Read(char[], int, int) will read everything from internal buffer and drivers buffer");

                for (int i = 0; i < byteXmitBuffer.Length; i++)
                {
                    byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
                }

                //Put the first byte of the utf32 encoder char in the last byte of this buffer
                //when we read this later the buffer will have to be resized
                byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];

                TCSupport.SetHighSpeed(com1, com2);

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

                TCSupport.WaitForReadBufferToLoad(com1, byteXmitBuffer.Length);

                //Read Every Byte except the last one. The last bye should be left in the last position of SerialPort's
                //internal buffer. When we try to read this char as UTF32 the buffer should have to be resized so 
                //the other 3 bytes of the ut32 encoded char can be in the buffer
                com1.Read(new char[1023], 0, 1023);
                Assert.Equal(1, com1.BytesToRead);

                com1.Encoding = Encoding.UTF32;
                com2.Encoding = Encoding.UTF32;

                com2.Write(utf32CharBytes, 1, 3);
                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);

                int numBytes = Encoding.UTF32.GetByteCount(charXmitBuffer);

                byte[] byteBuffer = Encoding.UTF32.GetBytes(charXmitBuffer);

                var expectedChars = new char[1 + Encoding.UTF32.GetCharCount(byteBuffer)];
                expectedChars[0] = utf32Char;

                Encoding.UTF32.GetChars(byteBuffer, 0, byteBuffer.Length, expectedChars, 1);
                var charRcvBuffer = new char[(int)(expectedChars.Length * 1.5)];

                TCSupport.WaitForReadBufferToLoad(com1, 4 + numBytes);

                if (expectedChars.Length != (numCharsRead = com1.Read(charRcvBuffer, 0, charRcvBuffer.Length)))
                {
                    Fail("Err_6481sfadw Expected read to read {0} chars actually read {1}", expectedChars.Length,
                        numCharsRead);
                }

                Assert.Equal(expectedChars, charRcvBuffer.Take(expectedChars.Length).ToArray());

                Assert.Equal(0, com1.BytesToRead);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_DataReceivedBeforeTimeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
                char[] charRcvBuffer = new char[charXmitBuffer.Length];
                ASyncRead asyncRead = new ASyncRead(com1, charRcvBuffer, 0, charRcvBuffer.Length);
                var asyncReadTask = new Task(asyncRead.Read);


                Debug.WriteLine(
                    "Verifying that Read(char[], int, int) will read characters that have been received after the call to Read was made");

                com1.Encoding = Encoding.UTF8;
                com2.Encoding = Encoding.UTF8;
                com1.ReadTimeout = 20000; // 20 seconds

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                asyncReadTask.Start();
                asyncRead.ReadStartedEvent.WaitOne();
                // The WaitOne only tells us that the thread has started to execute code in the method
                Thread.Sleep(2000); // We need to wait to guarantee that we are executing code in SerialPort
                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);

                asyncRead.ReadCompletedEvent.WaitOne();

                Assert.Null(asyncRead.Exception);

                if (asyncRead.Result < 1)
                {
                    Fail("Err_0158ahei Expected Read to read at least one character {0}", asyncRead.Result);
                }
                else
                {
                    int receivedLength = asyncRead.Result;
                    while (receivedLength < charRcvBuffer.Length)
                    {
                        receivedLength += com1.Read(charRcvBuffer, receivedLength, charRcvBuffer.Length - receivedLength);
                    }
                    Assert.Equal(charXmitBuffer.Length, receivedLength);
                    Assert.Equal(charXmitBuffer, charRcvBuffer);
                }

                TCSupport.WaitForTaskCompletion(asyncReadTask);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_ResizeBuffer()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(1023, TCSupport.CharacterOptions.ASCII);
                int readResult;

                Debug.WriteLine("Verifying that Read(char[], int, int) will compact data in the buffer");

                com1.Encoding = Encoding.ASCII;
                com2.Encoding = Encoding.ASCII;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();


                //[] Fill the buffer up then read in all but one of the chars
                var expectedChars = new char[charXmitBuffer.Length - 1];
                var charRcvBuffer = new char[charXmitBuffer.Length - 1];
                Array.Copy(charXmitBuffer, 0, expectedChars, 0, charXmitBuffer.Length - 1);

                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
                TCSupport.WaitForPredicate(() => com1.BytesToRead == charXmitBuffer.Length, 2000,
                    "Err_29829haie Expected to received {0} bytes actual={1}", charXmitBuffer.Length, com1.BytesToRead);

                if (charXmitBuffer.Length - 1 != (readResult = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length - 1)))
                {
                    Fail("Err_55084aheid Expected to read {0} chars actual {1}", charXmitBuffer.Length - 1,
                        readResult);
                }

                TCSupport.VerifyArray(expectedChars, charRcvBuffer);

                //[] Write 16 more cahrs and read in 16 chars
                expectedChars = new char[16];
                charRcvBuffer = new char[16];
                expectedChars[0] = charXmitBuffer[charXmitBuffer.Length - 1];
                Array.Copy(charXmitBuffer, 0, expectedChars, 1, 15);

                com2.Write(charXmitBuffer, 0, 16);
                TCSupport.WaitForPredicate(() => com1.BytesToRead == 17, 2000,
                    "Err_0516848aied Expected to received {0} bytes actual={1}", 17, com1.BytesToRead);

                if (16 != (readResult = com1.Read(charRcvBuffer, 0, 16)))
                {
                    Fail("Err_650848ahide Expected to read {0} chars actual {1}", 16, readResult);
                }

                Assert.Equal(expectedChars, charRcvBuffer);

                //[] Write more chars and read in all of the chars
                expectedChars = new char[charXmitBuffer.Length + 1];
                charRcvBuffer = new char[charXmitBuffer.Length + 1];
                expectedChars[0] = charXmitBuffer[15];
                Array.Copy(charXmitBuffer, 0, expectedChars, 1, charXmitBuffer.Length);

                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
                TCSupport.WaitForPredicate(() => com1.BytesToRead == charXmitBuffer.Length + 1, 2000,
                    "Err_41515684 Expected to received {0} bytes actual={1}", charXmitBuffer.Length + 2, com1.BytesToRead);

                if (charXmitBuffer.Length + 1 != (readResult = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length + 1)))
                {
                    Fail("Err_460574ajied Expected to read {0} chars actual {1}", charXmitBuffer.Length + 1, readResult);
                }
                Assert.Equal(expectedChars, charRcvBuffer);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_Timeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
                byte[] byteXmitBuffer = new UTF32Encoding().GetBytes(charXmitBuffer);
                char[] charRcvBuffer = new char[charXmitBuffer.Length];

                int result;

                Debug.WriteLine(
                    "Verifying that Read(char[], int, int) works appropriately after TimeoutException has been thrown");

                com1.Encoding = new UTF32Encoding();
                com2.Encoding = new UTF32Encoding();
                com1.ReadTimeout = 500; // 20 seconds

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                //Write the first 3 bytes of a character
                com2.Write(byteXmitBuffer, 0, 3);

                try
                {
                    com1.Read(charXmitBuffer, 0, charXmitBuffer.Length);
                    Fail("Err_29299aize Expected ReadTo to throw TimeoutException");
                }
                catch (TimeoutException)
                {
                } //Expected

                Assert.Equal(3, com1.BytesToRead);

                com2.Write(byteXmitBuffer, 3, byteXmitBuffer.Length - 3);

                //		retValue &= TCSupport.WaitForPredicate(delegate() {return com1.BytesToRead == byteXmitBuffer.Length; }, 
                //			5000, "Err_91818aheid Expected BytesToRead={0} actual={1}", byteXmitBuffer.Length, com1.BytesToRead);

                TCSupport.WaitForExpected(() => com1.BytesToRead, byteXmitBuffer.Length,
                    5000, "Err_91818aheid BytesToRead");
                result = com1.Read(charRcvBuffer, 0, charRcvBuffer.Length);

                Assert.Equal(charXmitBuffer.Length, result);
                Assert.Equal(charXmitBuffer, charRcvBuffer);

                VerifyBytesReadOnCom1FromCom2(com1, com2, byteXmitBuffer, charXmitBuffer, charRcvBuffer, 0,
                    charRcvBuffer.Length);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_Partial()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char utf32Char = (char)0x254b; //Box drawing char
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new[] { utf32Char });
                char[] charRcvBuffer = new char[3];

                int result;

                Debug.WriteLine("Verifying that Read(char[], int, int) works when reading partial characters");

                com1.Encoding = new UTF32Encoding();
                com2.Encoding = new UTF32Encoding();
                com1.ReadTimeout = 500;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                //Write the first 3 bytes of a character
                com2.Write(utf32CharBytes, 0, 4);
                com2.Write(utf32CharBytes, 0, 3);

                TCSupport.WaitForExpected(() => com1.BytesToRead, 7,
                    5000, "Err_018158ajid BytesToRead");
                result = com1.Read(charRcvBuffer, 0, charRcvBuffer.Length);

                Assert.Equal(1, result);
                com2.Write(utf32CharBytes, 3, 1);

                result = com1.Read(charRcvBuffer, 1, charRcvBuffer.Length - 1);
                Assert.Equal(1, result);

                Assert.Equal(utf32Char, charRcvBuffer[0]);
                Assert.Equal(utf32Char, charRcvBuffer[1]);

                VerifyBytesReadOnCom1FromCom2(com1, com2, utf32CharBytes, new[] { utf32Char }, charRcvBuffer, 0,
                    charRcvBuffer.Length);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_SurrogateBoundary()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = new char[32];

                int result;

                Debug.WriteLine("Verifying that Read(char[], int, int) works with reading surrogate characters");

                TCSupport.GetRandomChars(charXmitBuffer, 0, charXmitBuffer.Length - 2, TCSupport.CharacterOptions.Surrogates);
                charXmitBuffer[charXmitBuffer.Length - 2] = TCSupport.GenerateRandomHighSurrogate();
                charXmitBuffer[charXmitBuffer.Length - 1] = TCSupport.GenerateRandomLowSurrogate();

                com1.Encoding = Encoding.Unicode;
                com2.Encoding = Encoding.Unicode;
                com1.ReadTimeout = 500;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                //[] First lets try with buffer size that is larger then what we are asking for
                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
                TCSupport.WaitForExpected(() => com1.BytesToRead, charXmitBuffer.Length * 2,
                    5000, "Err_018158ajid BytesToRead");

                var charRcvBuffer = new char[charXmitBuffer.Length];

                result = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length - 1);

                Assert.Equal(charXmitBuffer.Length - 2, result);

                char[] actualChars = new char[charXmitBuffer.Length];

                Array.Copy(charRcvBuffer, 0, actualChars, 0, result);
                result = com1.Read(actualChars, actualChars.Length - 2, 2);

                Assert.Equal(2, result);
                Assert.Equal(charXmitBuffer, actualChars);

                //[] Next lets try with buffer size that is the same size as what we are asking for
                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
                TCSupport.WaitForExpected(() => com1.BytesToRead, charXmitBuffer.Length * 2,
                    5000, "Err_018158ajid BytesToRead");

                charRcvBuffer = new char[charXmitBuffer.Length - 1];

                result = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length - 1);

                Assert.Equal(charXmitBuffer.Length - 2, result);

                actualChars = new char[charXmitBuffer.Length];

                Array.Copy(charRcvBuffer, 0, actualChars, 0, result);
                result = com1.Read(actualChars, actualChars.Length - 2, 2);

                Assert.Equal(2, result);
                Assert.Equal(charXmitBuffer, actualChars);
            }
        }

        #endregion

        #region Verification for Test Cases
        private void VerifyReadException(char[] buffer, int offset, int count, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int bufferLength = null == buffer ? 0 : buffer.Length;

                Debug.WriteLine("Verifying read method throws {0} buffer.Lenght={1}, offset={2}, count={3}", expectedException, bufferLength, offset, count);
                com.Open();

                Assert.Throws(expectedException, () => com.Read(buffer, offset, count));
            }
        }

        private void VerifyRead(char[] buffer, int offset, int count)
        {
            VerifyRead(buffer, offset, count, new ASCIIEncoding(), numRndBytesToRead, ReadDataFromEnum.NonBuffered);
        }


        private void VerifyRead(char[] buffer, int offset, int count, int numberOfBytesToRead)
        {
            VerifyRead(buffer, offset, count, new ASCIIEncoding(), numberOfBytesToRead, ReadDataFromEnum.NonBuffered);
        }


        private void VerifyRead(char[] buffer, int offset, int count, int numberOfBytesToRead, ReadDataFromEnum readDataFrom)
        {
            VerifyRead(buffer, offset, count, new ASCIIEncoding(), numberOfBytesToRead, readDataFrom);
        }


        private void VerifyRead(char[] buffer, int offset, int count, Encoding encoding)
        {
            VerifyRead(buffer, offset, count, encoding, numRndBytesToRead, ReadDataFromEnum.NonBuffered);
        }


        private void VerifyRead(char[] buffer, int offset, int count, Encoding encoding, int numberOfBytesToRead,
            ReadDataFromEnum readDataFrom)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random(-55);
                char[] charsToWrite;
                char[] expectedChars = new char[numberOfBytesToRead];
                byte[] bytesToWrite = new byte[numberOfBytesToRead];

                if (1 < count)
                {
                    charsToWrite = TCSupport.GetRandomChars(numberOfBytesToRead, TCSupport.CharacterOptions.Surrogates);
                }
                else
                {
                    charsToWrite = TCSupport.GetRandomChars(numberOfBytesToRead, TCSupport.CharacterOptions.None);
                }

                //Genrate some random chars in the buffer
                for (int i = 0; i < buffer.Length; i++)
                {
                    char randChar = (char)rndGen.Next(0, ushort.MaxValue);

                    buffer[i] = randChar;
                }

                TCSupport.SetHighSpeed(com1, com2);

                Debug.WriteLine(
                    "Verifying read method buffer.Length={0}, offset={1}, count={2}, endocing={3} with {4} random chars",
                    buffer.Length, offset, count, encoding.EncodingName, bytesToWrite.Length);
                com1.ReadTimeout = 500;
                com1.Encoding = encoding;
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                bytesToWrite = com1.Encoding.GetBytes(charsToWrite, 0, charsToWrite.Length);
                expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

                switch (readDataFrom)
                {
                    case ReadDataFromEnum.NonBuffered:
                        VerifyReadNonBuffered(com1, com2, bytesToWrite, buffer, offset, count);
                        break;
                    case ReadDataFromEnum.Buffered:
                        VerifyReadBuffered(com1, com2, bytesToWrite, buffer, offset, count);
                        break;
                    case ReadDataFromEnum.BufferedAndNonBuffered:
                        VerifyReadBufferedAndNonBuffered(com1, com2, bytesToWrite, buffer, offset, count);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(readDataFrom), readDataFrom, null);
                }
            }
        }

        private void VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] rcvBuffer, int offset, int count)
        {
            char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);
            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars, rcvBuffer, offset, count);
        }


        private void VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] rcvBuffer, int offset, int count)
        {
            char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

            BufferData(com1, com2, bytesToWrite);
            PerformReadOnCom1FromCom2(com1, com2, expectedChars, rcvBuffer, offset, count);
        }


        private void VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] rcvBuffer, int offset, int count)
        {
            char[] expectedChars = new char[com1.Encoding.GetCharCount(bytesToWrite, 0, bytesToWrite.Length) * 2];
            char[] encodedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

            Array.Copy(encodedChars, 0, expectedChars, 0, bytesToWrite.Length);
            Array.Copy(encodedChars, 0, expectedChars, encodedChars.Length, encodedChars.Length);

            BufferData(com1, com2, bytesToWrite);

            VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars, rcvBuffer, offset, count);
        }


        private void BufferData(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
        {
            char c = TCSupport.GenerateRandomCharNonSurrogate();
            byte[] bytesForSingleChar = com1.Encoding.GetBytes(new char[] { c }, 0, 1);

            com2.Write(bytesForSingleChar, 0, bytesForSingleChar.Length); // Write one byte at the begining because we are going to read this to buffer the rest of the data
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);

            TCSupport.WaitForReadBufferToLoad(com1, bytesToWrite.Length);

            com1.Read(new char[1], 0, 1); // This should put the rest of the bytes in SerialPorts own internal buffer

            Assert.Equal(bytesToWrite.Length, com1.BytesToRead);
        }

        private void VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars, char[] rcvBuffer, int offset, int count)
        {
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);
            com1.ReadTimeout = 500;

            //This is pretty lame but we will have to live with if for now becuase we can not 
            //gaurentee the number of bytes Write will add
            Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

            PerformReadOnCom1FromCom2(com1, com2, expectedChars, rcvBuffer, offset, count);
        }

        private void PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, char[] expectedChars, char[] rcvBuffer, int offset, int count)
        {
            char[] buffer = new char[expectedChars.Length];
            char[] oldRcvBuffer = (char[])rcvBuffer.Clone();
            int numBytesWritten = com1.Encoding.GetByteCount(expectedChars);

            int totalBytesRead = 0;
            int totalCharsRead = 0;

            while (true)
            {
                int charsRead;
                try
                {
                    charsRead = com1.Read(rcvBuffer, offset, count);
                }
                catch (TimeoutException)
                {
                    break;
                }

                // While there are more characters to be read
                int bytesRead = com1.Encoding.GetByteCount(rcvBuffer, offset, charsRead);
                totalBytesRead += bytesRead;

                if (expectedChars.Length < totalCharsRead + charsRead)
                {
                    //If we have read in more characters than we expect

                    //1<DEBUG>
                    Debug.WriteLine("count={0}, charsRead={1} expectedChars.Length={2}, totalCharsRead={3}", count, charsRead, expectedChars.Length, totalCharsRead);

                    Debug.WriteLine("rcvBuffer");
                    TCSupport.PrintChars(rcvBuffer);

                    Debug.WriteLine("\nexpectedChars");
                    TCSupport.PrintChars(expectedChars);
                    //1</DEBUG>

                    Fail("ERROR!!!: We have received more characters then were sent");
                }

                if (count != charsRead &&
                    (count < charsRead ||
                     ((expectedChars.Length - totalCharsRead) != charsRead &&
                      !TCSupport.IsSurrogate(expectedChars[totalCharsRead + charsRead]))))
                {
                    //If we have not read all of the characters that we should have

                    //1<DEBUG>
                    Debug.WriteLine("count={0}, charsRead={1} expectedChars.Length={2}, totalCharsRead={3}", count, charsRead, expectedChars.Length, totalCharsRead);

                    Debug.WriteLine("rcvBuffer");
                    TCSupport.PrintChars(rcvBuffer);

                    Debug.WriteLine("\nexpectedChars");
                    TCSupport.PrintChars(expectedChars);
                    //1</DEBUG>

                    Fail("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                }

                VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, charsRead);

                Array.Copy(rcvBuffer, offset, buffer, totalCharsRead, charsRead);

                totalCharsRead += charsRead;

                Assert.Equal(numBytesWritten - totalBytesRead, com1.BytesToRead);

                oldRcvBuffer = (char[])rcvBuffer.Clone();
            }

            VerifyBuffer(rcvBuffer, oldRcvBuffer, 0, rcvBuffer.Length);

            //Compare the chars that were written with the ones we expected to read
            for (int i = 0; i < expectedChars.Length; i++)
            {
                if (expectedChars[i] != buffer[i])
                {
                    Fail("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", (int)expectedChars[i], (int)buffer[i], i);
                }
            }

            Assert.Equal(0, com1.BytesToRead);
        }

        private void VerifyBytesFollowedByChars(Encoding encoding)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] xmitCharBuffer = TCSupport.GetRandomChars(numRndBytesToRead, TCSupport.CharacterOptions.Surrogates);
                char[] rcvCharBuffer = new char[xmitCharBuffer.Length];
                byte[] xmitByteBuffer = new byte[numRndBytesToRead];
                byte[] rcvByteBuffer = new byte[xmitByteBuffer.Length];
                Random rndGen = new Random(-55);

                int numRead;

                Debug.WriteLine("Verifying read method does not alter stream of bytes after chars have been read with {0}",
                    encoding.GetType());

                for (int i = 0; i < xmitByteBuffer.Length; i++)
                {
                    xmitByteBuffer[i] = (byte)rndGen.Next(0, 256);
                }

                com1.Encoding = encoding;
                com2.Encoding = encoding;
                com1.ReadTimeout = 500;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                com2.Write(xmitCharBuffer, 0, xmitCharBuffer.Length);
                com2.Write(xmitByteBuffer, 0, xmitByteBuffer.Length);

                Thread.Sleep(
                    (int)
                    (((xmitByteBuffer.Length + com2.Encoding.GetByteCount(xmitCharBuffer) * 10.0) / com1.BaudRate) * 1000) +
                    500);
                Thread.Sleep(500);

                if (xmitCharBuffer.Length != (numRead = com1.Read(rcvCharBuffer, 0, rcvCharBuffer.Length)))
                {
                    Fail("ERROR!!!: Expected to read {0} chars actually read {1}", xmitCharBuffer.Length, numRead);
                }

                if (encoding.EncodingName == Encoding.UTF7.EncodingName)
                {
                    //If UTF7Encoding is being used we we might leave a - in the stream
                    if (com1.BytesToRead == xmitByteBuffer.Length + 1)
                    {
                        int byteRead;

                        if ('-' != (char)(byteRead = com1.ReadByte()))
                        {
                            Fail("Err_29282naie Expected '-' to be left in the stream with UTF7Encoding and read {0}", byteRead);
                        }
                    }
                }

                if (xmitByteBuffer.Length != (numRead = com1.Read(rcvByteBuffer, 0, rcvByteBuffer.Length)))
                {
                    Fail("ERROR!!!: Expected to read {0} bytes actually read {1}", xmitByteBuffer.Length, numRead);
                }

                for (int i = 0; i < xmitByteBuffer.Length; i++)
                {
                    if (xmitByteBuffer[i] != rcvByteBuffer[i])
                    {
                        Fail("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", (int)xmitByteBuffer[i], (int)rcvByteBuffer[i], i);
                    }
                }

                Assert.Equal(0, com1.BytesToRead);

                /*DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG
            if(!retValue) {
                for(int i=0; i<xmitCharBuffer.Length; ++i) {
                    Debug.WriteLine("(char){0}, ", (int)xmitCharBuffer[i]);
                }
    
                for(int i=0; i<xmitCharBuffer.Length; ++i) {
                    Debug.WriteLine("{0}, ", (int)xmitByteBuffer[i]);
                }			
            }*/
            }
        }

        private void VerifyBuffer(char[] actualBuffer, char[] expectedBuffer, int offset, int count)
        {
            //Verify all character before the offset
            for (int i = 0; i < offset; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }

            //Verify all character after the offset + count
            for (int i = offset + count; i < actualBuffer.Length; i++)
            {
                if (actualBuffer[i] != expectedBuffer[i])
                {
                    Fail("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                }
            }
        }

        private class ASyncRead
        {
            private readonly SerialPort _com;
            private readonly char[] _buffer;
            private readonly int _offset;
            private readonly int _count;
            private int _result;

            private readonly AutoResetEvent _readCompletedEvent;
            private readonly AutoResetEvent _readStartedEvent;

            private Exception _exception;

            public ASyncRead(SerialPort com, char[] buffer, int offset, int count)
            {
                _com = com;
                _buffer = buffer;
                _offset = offset;
                _count = count;

                _result = -1;

                _readCompletedEvent = new AutoResetEvent(false);
                _readStartedEvent = new AutoResetEvent(false);

                _exception = null;
            }

            public void Read()
            {
                try
                {
                    _readStartedEvent.Set();
                    _result = _com.Read(_buffer, _offset, _count);
                }
                catch (Exception e)
                {
                    _exception = e;
                }
                finally
                {
                    _readCompletedEvent.Set();
                }
            }

            public AutoResetEvent ReadStartedEvent => _readStartedEvent;

            public AutoResetEvent ReadCompletedEvent => _readCompletedEvent;

            public int Result => _result;

            public Exception Exception => _exception;
        }
        #endregion
    }
}

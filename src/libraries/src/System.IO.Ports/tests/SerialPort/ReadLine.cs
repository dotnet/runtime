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
    public class ReadLine : PortsTest
    {
        //The number of random bytes to receive for read method testing
        private const int DEFAULT_NUM_CHARS_TO_READ = 8;

        //The number of new lines to insert into the string not including the one at the end
        private const int DEFAULT_NUMBER_NEW_LINES = 2;

        //The number of random bytes to receive for large input buffer testing
        private const int LARGE_NUM_CHARS_TO_READ = 2048;
        private const int MIN_NUM_NEWLINE_CHARS = 1;
        private const int MAX_NUM_NEWLINE_CHARS = 5;

        private enum ReadDataFromEnum
        {
            NonBuffered,
            Buffered,
            BufferedAndNonBuffered
        };

        #region Test Cases

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_Contains_nullChar()
        {
            VerifyRead(new ASCIIEncoding(), "\0");
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_CR()
        {
            VerifyRead(new ASCIIEncoding(), "\r");
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_LF()
        {
            VerifyRead(new ASCIIEncoding(), "\n");
        }


        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_CRLF_RndStr()
        {
            VerifyRead(new ASCIIEncoding(), "\r\n");
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_CRLF_CRStr()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying read method with \\r\\n NewLine and a string containing just \\r");

                com1.Open();
                if (!com2.IsOpen)
                {
                    com2.Open();
                }
                com1.NewLine = "\r\n";

                VerifyReadLine(com1, com2, "TEST\r");
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_CRLF_LFStr()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying read method with \\r\\n NewLine and a string containing just \\n");

                com1.Open();
                if (!com2.IsOpen)
                {
                    com2.Open();
                }
                com1.NewLine = "\r\n";

                VerifyReadLine(com1, com2, "TEST\n");
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_END()
        {
            VerifyRead(new ASCIIEncoding(), "END");
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLine_Surrogate()
        {
            VerifyRead(Encoding.UTF8, "\uD800\uDC00");
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ASCIIEncoding()
        {
            VerifyRead(new ASCIIEncoding(), GenRandomNewLine(true));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding()
        {
            VerifyRead(new UTF8Encoding(), GenRandomNewLine(false));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF32Encoding()
        {
            VerifyRead(new UTF32Encoding(), GenRandomNewLine(false));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UnicodeEncoding()
        {
            VerifyRead(new UnicodeEncoding(), GenRandomNewLine(false));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void LargeInputBuffer()
        {
            VerifyRead(LARGE_NUM_CHARS_TO_READ);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadLineWriteLine_ASCII()
        {
            VerifyReadLineWithWriteLine(new ASCIIEncoding(), GenRandomNewLine(true));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadLineWriteLine_UTF8()
        {
            VerifyReadLineWithWriteLine(new UTF8Encoding(), GenRandomNewLine(true));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadLineWriteLine_UTF32()
        {
            VerifyReadLineWithWriteLine(new UTF32Encoding(), GenRandomNewLine(true));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadLineWriteLine_Unicode()
        {
            VerifyReadLineWithWriteLine(new UnicodeEncoding(), GenRandomNewLine(true));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedData()
        {
            var numBytesToRead = 32;
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                var rndGen = new Random(-55);
                var strBldrToWrite = new StringBuilder();

                //Genrate random characters
                for (var i = 0; i < numBytesToRead; i++)
                {
                    strBldrToWrite.Append((char)rndGen.Next(40, 60));
                }

                int newLineIndex;

                while (-1 != (newLineIndex = strBldrToWrite.ToString().IndexOf(com1.NewLine)))
                {
                    strBldrToWrite[newLineIndex] = (char)rndGen.Next(40, 60);
                }

                com1.ReadTimeout = 500;
                com1.Open();


                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                strBldrToWrite.Append(com1.NewLine);

                BufferData(com1, com2, strBldrToWrite.ToString());
                PerformReadOnCom1FromCom2(com1, com2, strBldrToWrite.ToString(), com1.NewLine);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedData()
        {
            var numBytesToRead = 32;

            VerifyRead(Encoding.ASCII, GenRandomNewLine(true), numBytesToRead, 1, ReadDataFromEnum.Buffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_ReadBufferedAndNonBufferedData()
        {
            var numBytesToRead = 32;
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                var rndGen = new Random(-55);
                var strBldrToWrite = new StringBuilder();
                var strBldrExpected = new StringBuilder();


                //Genrate random characters
                for (var i = 0; i < numBytesToRead; i++)
                {
                    strBldrToWrite.Append((char)rndGen.Next(0, 256));
                }

                int newLineIndex;

                while (-1 != (newLineIndex = strBldrToWrite.ToString().IndexOf(com1.NewLine)))
                {
                    strBldrToWrite[newLineIndex] = (char)rndGen.Next(0, 256);
                }

                com1.ReadTimeout = 500;
                com1.Open();


                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                BufferData(com1, com2, strBldrToWrite.ToString());

                strBldrExpected.Append(strBldrToWrite);
                strBldrToWrite.Append(com1.NewLine);
                strBldrExpected.Append(strBldrToWrite);

                VerifyReadLine(com1, com2, strBldrToWrite.ToString(), strBldrExpected.ToString(), com1.NewLine);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void SerialPort_IterativeReadBufferedAndNonBufferedData()
        {
            var numBytesToRead = 3;

            VerifyRead(Encoding.ASCII, GenRandomNewLine(true), numBytesToRead, 1, ReadDataFromEnum.BufferedAndNonBuffered);
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void GreedyRead()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(128, TCSupport.CharacterOptions.Surrogates);
                var byteXmitBuffer = new byte[1024];
                char[] expectedChars;
                string rcvString;
                char[] actualChars;
                char utf32Char = TCSupport.GenerateRandomCharNonSurrogate();
                byte[] utf32CharBytes = Encoding.UTF32.GetBytes(new char[] { utf32Char });
                int numBytes;


                Debug.WriteLine("Verifying that ReadLine() will read everything from internal buffer and drivers buffer");

                //Put the first byte of the utf32 encoder char in the last byte of this buffer
                //when we read this later the buffer will have to be resized
                byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];

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
                com2.WriteLine(string.Empty);

                numBytes = Encoding.UTF32.GetByteCount(charXmitBuffer);

                byte[] byteBuffer = Encoding.UTF32.GetBytes(charXmitBuffer);

                expectedChars = new char[1 + Encoding.UTF32.GetCharCount(byteBuffer)];
                expectedChars[0] = utf32Char;

                Encoding.UTF32.GetChars(byteBuffer, 0, byteBuffer.Length, expectedChars, 1);

                TCSupport.WaitForReadBufferToLoad(com1, 4 + numBytes);

                rcvString = com1.ReadLine();

                Assert.NotNull(rcvString);

                actualChars = rcvString.ToCharArray();
                Assert.Equal(expectedChars, actualChars);

                Assert.Equal(0, com1.BytesToRead);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void NewLineSubstring()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying read method with sub strings of the new line appearing in the string being read");
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                com1.NewLine = "asfg";

                string newLineSubStrings = "a" + "as" + "asf" + "sfg" + "fg" + "g"; //All the substrings of newLine
                string testStr = newLineSubStrings + "asfg" + newLineSubStrings;

                VerifyReadLine(com1, com2, testStr);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_DataReceivedBeforeTimeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
                var asyncRead = new ASyncRead(com1);
                var asyncReadTask = new Task(asyncRead.Read);

                char endLineChar = com1.NewLine[0];
                char notEndLineChar = TCSupport.GetRandomOtherChar(endLineChar, TCSupport.CharacterOptions.None);

                Debug.WriteLine(
                    "Verifying that Read(char[], int, int) will read characters that have been received after the call to Read was made");

                //Ensure the new line is not in charXmitBuffer
                for (var i = 0; i < charXmitBuffer.Length; ++i)
                {
                    //Se any appearances of a character in the new line string to some other char
                    if (endLineChar == charXmitBuffer[i])
                    {
                        charXmitBuffer[i] = notEndLineChar;
                    }
                }

                com1.Encoding = Encoding.UTF8;
                com2.Encoding = Encoding.UTF8;
                com1.ReadTimeout = 20000; // 20 seconds

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                asyncReadTask.Start();
                asyncRead.ReadStartedEvent.WaitOne();
                //This only tells us that the thread has started to execute code in the method
                Thread.Sleep(2000); //We need to wait to guarentee that we are executing code in SerialPort
                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
                com2.WriteLine(string.Empty);

                asyncRead.ReadCompletedEvent.WaitOne();

                Assert.Null(asyncRead.Exception);

                if (null == asyncRead.Result || 0 == asyncRead.Result.Length)
                {
                    Fail("Err_0158ahei Expected Read to read at least one character");
                }
                else
                {
                    Assert.Equal(charXmitBuffer, asyncRead.Result.ToCharArray());
                }

                VerifyReadLine(com1, com2, new string(charXmitBuffer));
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_Timeout()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);

                char endChar = com1.NewLine[0];
                char notEndChar = TCSupport.GetRandomOtherChar(endChar, TCSupport.CharacterOptions.None);

                Debug.WriteLine("Verifying that ReadLine(string) works appropriately after TimeoutException has been thrown");

                //Ensure the new line is not in charXmitBuffer
                for (var i = 0; i < charXmitBuffer.Length; ++i)
                {
                    // Set any appearances of a character in the new line string to some other char
                    if (endChar == charXmitBuffer[i])
                    {
                        charXmitBuffer[i] = notEndChar;
                    }
                }

                com1.Encoding = Encoding.Unicode;
                com2.Encoding = Encoding.Unicode;
                com1.ReadTimeout = 1500;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);

                Assert.Throws<TimeoutException>(() => com1.ReadLine());

                Assert.Equal(2 * charXmitBuffer.Length, com1.BytesToRead);

                com2.WriteLine(string.Empty);
                string result = com1.ReadLine();
                char[] charRcvBuffer = result.ToCharArray();

                Assert.Equal(charXmitBuffer, charRcvBuffer);

                VerifyReadLine(com1, com2, new string(charXmitBuffer));
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_LargeBuffer()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                int numberOfCharsToWrite = 2048;

                char[] charactersToWrite = new char[numberOfCharsToWrite];
                for (int i = 0; i < numberOfCharsToWrite; i++)
                {
                    charactersToWrite[i] = (char)('A' + (i % ('Z' - 'A' + 1)));
                }

                var writeToCom2Task = new Task(() =>
                {
                    com1.Write(charactersToWrite, 0, numberOfCharsToWrite);
                });

                com1.BaudRate = 115200;
                com2.BaudRate = 115200;

                com1.Encoding = Encoding.Unicode;
                com2.Encoding = Encoding.Unicode;

                com2.ReadTimeout = 10000;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                writeToCom2Task.Start();

                Assert.Throws<TimeoutException>(() => com2.ReadLine());
                writeToCom2Task.Wait();

                com1.Write(com1.NewLine);

                string receivedLine = com2.ReadLine();
                Assert.Equal(charactersToWrite, receivedLine);
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void Read_SurrogateCharacter()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying read method with surrogate pair in the input and a surrogate pair for the newline");
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                var surrogatePair = "\uD800\uDC00";
                var newLine = "\uD801\uDC01";
                string input = TCSupport.GetRandomString(256, TCSupport.CharacterOptions.None) + surrogatePair + newLine;

                com1.NewLine = newLine;

                VerifyReadLine(com1, com2, input, newLine);
            }
        }

        #endregion

        #region Verification for Test Cases

        private void VerifyRead(int numberOfBytesToRead)
        {
            VerifyRead(new ASCIIEncoding(), "\n", numberOfBytesToRead, DEFAULT_NUMBER_NEW_LINES,
                ReadDataFromEnum.NonBuffered);
        }

        private void VerifyRead(Encoding encoding)
        {
            VerifyRead(encoding, "\n", DEFAULT_NUM_CHARS_TO_READ, DEFAULT_NUMBER_NEW_LINES,
                ReadDataFromEnum.NonBuffered);
        }

        private void VerifyRead(Encoding encoding, string newLine)
        {
            VerifyRead(encoding, newLine, DEFAULT_NUM_CHARS_TO_READ, DEFAULT_NUMBER_NEW_LINES,
                ReadDataFromEnum.NonBuffered);
        }

        private void VerifyRead(Encoding encoding, string newLine, int numBytesRead, int numNewLines, ReadDataFromEnum readDataFrom)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                var rndGen = new Random(-55);
                StringBuilder strBldrToWrite;
                int numNewLineChars = newLine.ToCharArray().Length;
                int minLength = (1 + numNewLineChars) * numNewLines;

                if (minLength < numBytesRead)
                    strBldrToWrite = TCSupport.GetRandomStringBuilder(numBytesRead, TCSupport.CharacterOptions.None);
                else
                    strBldrToWrite = TCSupport.GetRandomStringBuilder(rndGen.Next(minLength, minLength * 2),
                        TCSupport.CharacterOptions.None);

                //We need place the newLine so that they do not write over eachother
                int divisionLength = strBldrToWrite.Length / numNewLines;
                int range = divisionLength - numNewLineChars;

                for (var i = 0; i < numNewLines; i++)
                {
                    int newLineIndex = rndGen.Next(0, range + 1);

                    strBldrToWrite.Insert(newLineIndex + (i * divisionLength) + (i * numNewLineChars), newLine);
                }

                Debug.WriteLine("Verifying ReadLine encoding={0}, newLine={1}, numBytesRead={2}, numNewLines={3}", encoding,
                    newLine, numBytesRead, numNewLines);

                com1.ReadTimeout = 500;
                com1.Encoding = encoding;
                com1.NewLine = newLine;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                switch (readDataFrom)
                {
                    case ReadDataFromEnum.NonBuffered:
                        VerifyReadNonBuffered(com1, com2, strBldrToWrite.ToString(), newLine);
                        break;
                    case ReadDataFromEnum.Buffered:
                        VerifyReadBuffered(com1, com2, strBldrToWrite.ToString(), newLine);
                        break;
                    case ReadDataFromEnum.BufferedAndNonBuffered:
                        VerifyReadBufferedAndNonBuffered(com1, com2, strBldrToWrite.ToString(), newLine);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(readDataFrom), readDataFrom, null);
                }
            }
        }

        private void VerifyReadNonBuffered(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
        {
            VerifyReadLine(com1, com2, strToWrite, newLine);
        }


        private void VerifyReadBuffered(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
        {
            BufferData(com1, com2, strToWrite);
            PerformReadOnCom1FromCom2(com1, com2, strToWrite, newLine);
        }


        private void VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
        {
            BufferData(com1, com2, strToWrite);
            VerifyReadLine(com1, com2, strToWrite, strToWrite + strToWrite, newLine);
        }


        private void BufferData(SerialPort com1, SerialPort com2, string strToWrite)
        {
            char[] charsToWrite = strToWrite.ToCharArray();
            byte[] bytesToWrite = com1.Encoding.GetBytes(charsToWrite);

            com2.Write(bytesToWrite, 0, 1);
            // Write one byte at the begining because we are going to read this to buffer the rest of the data
            com2.Write(bytesToWrite, 0, bytesToWrite.Length);

            TCSupport.WaitForReadBufferToLoad(com1, bytesToWrite.Length + 1);

            com1.Read(new char[1], 0, 1); // This should put the rest of the bytes in SerialPorts own internal buffer

            Assert.Equal(bytesToWrite.Length, com1.BytesToRead);
        }

        private void VerifyReadLine(SerialPort com1, SerialPort com2, string strToWrite)
        {
            VerifyReadLine(com1, com2, strToWrite, strToWrite, com1.NewLine);
        }


        private void VerifyReadLine(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
        {
            VerifyReadLine(com1, com2, strToWrite, strToWrite, newLine);
        }


        private void VerifyReadLine(SerialPort com1, SerialPort com2, string strToWrite, string expectedString, string newLine)
        {
            char[] charsToWrite = strToWrite.ToCharArray();
            byte[] bytesToWrite = com1.Encoding.GetBytes(charsToWrite);

            com2.Write(bytesToWrite, 0, bytesToWrite.Length);
            com1.ReadTimeout = 500;
            Thread.Sleep((int)((((bytesToWrite.Length + 1) * 10.0) / com1.BaudRate) * 1000) + 250);

            PerformReadOnCom1FromCom2(com1, com2, expectedString, newLine);
        }


        private void PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
        {
            var strBldrRead = new StringBuilder();
            int newLineStringLength = newLine.Length;
            int numNewLineChars = newLine.ToCharArray().Length;
            int numNewLineBytes = com1.Encoding.GetByteCount(newLine.ToCharArray());
            int bytesRead, totalBytesRead, charsRead, totalCharsRead;
            int indexOfNewLine, lastIndexOfNewLine = -newLineStringLength;

            char[] charsToWrite = strToWrite.ToCharArray();
            byte[] bytesToWrite = com1.Encoding.GetBytes(charsToWrite);
            var expectedString = new string(com1.Encoding.GetChars(bytesToWrite));

            totalBytesRead = 0;
            totalCharsRead = 0;

            while (true)
            {
                string rcvString;
                try
                {
                    rcvString = com1.ReadLine();
                }
                catch (TimeoutException)
                {
                    break;
                }

                //While their are more characters to be read
                char[] rcvCharBuffer = rcvString.ToCharArray();
                charsRead = rcvCharBuffer.Length;

                bytesRead = com1.Encoding.GetByteCount(rcvCharBuffer, 0, charsRead);
                totalBytesRead += bytesRead + numNewLineBytes;

                //			indexOfNewLine = strToWrite.IndexOf(com1.NewLine, lastIndexOfNewLine + newLineStringLength);
                indexOfNewLine = TCSupport.OrdinalIndexOf(expectedString, lastIndexOfNewLine + newLineStringLength, newLine);
                //SerialPort does a Ordinal comparison

                if ((indexOfNewLine - (lastIndexOfNewLine + newLineStringLength)) != charsRead)
                {
                    //If we have not read all of the characters that we should have
                    Debug.WriteLine(
                        "indexOfNewLine={0} lastIndexOfNewLine={1} charsRead={2} numNewLineChars={3} newLineStringLength={4} strToWrite.Length={5}",
                        indexOfNewLine, lastIndexOfNewLine, charsRead, numNewLineChars, newLineStringLength,
                        strToWrite.Length);
                    Debug.WriteLine(strToWrite);
                    Fail("Err_1707ahsp!!!: Read did not return all of the characters that were in SerialPort buffer");
                }

                if (charsToWrite.Length < totalCharsRead + charsRead)
                {
                    //If we have read in more characters then we expect
                    Fail("Err_21707adad!!!: We have received more characters then were sent");
                }

                strBldrRead.Append(rcvString);
                strBldrRead.Append(newLine);

                totalCharsRead += charsRead + numNewLineChars;

                lastIndexOfNewLine = indexOfNewLine;

                if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
                {
                    Fail("Err_99087ahpbx!!!: Expected BytesToRead={0} actual={1}",
                        bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                }
            } //End while there are more characters to read

            if (0 != com1.BytesToRead)
            {
                //If there are more bytes to read but there must not be a new line char at the end

                while (true)
                {
                    try
                    {
                        int charRead = com1.ReadChar();
                        strBldrRead.Append((char)charRead);
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }
            }

            Assert.Equal(0, com1.BytesToRead);

            Assert.Equal(expectedString, strBldrRead.ToString());
        }

        private void VerifyReadLineWithWriteLine(Encoding encoding, string newLine)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                var rndGen = new Random(-55);
                StringBuilder strBldrToWrite;

                Debug.WriteLine("Verifying ReadLine with WriteLine encoding={0}, newLine={1}", encoding, newLine);

                com1.ReadTimeout = 500;
                com1.NewLine = newLine;
                com2.NewLine = newLine;
                com1.Encoding = encoding;
                com2.Encoding = encoding;

                //Genrate random characters
                do
                {
                    strBldrToWrite = new StringBuilder();
                    for (var i = 0; i < DEFAULT_NUM_CHARS_TO_READ; i++)
                    {
                        strBldrToWrite.Append((char)rndGen.Next(0, 128));
                    }
                } while (-1 != TCSupport.OrdinalIndexOf(strBldrToWrite.ToString(), newLine));
                //SerialPort does a Ordinal comparison

                com1.Open();
                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                com2.WriteLine(strBldrToWrite.ToString());
                string strRead = com1.ReadLine();

                Assert.Equal(strBldrToWrite.ToString(), strRead);

                Assert.Equal(0, com1.BytesToRead);
            }
        }


        private string GenRandomNewLine(bool validAscii)
        {
            var rndGen = new Random(-55);
            int newLineLength = rndGen.Next(MIN_NUM_NEWLINE_CHARS, MAX_NUM_NEWLINE_CHARS);

            if (validAscii)
                return new string(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.ASCII));
            else
                return new string(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.Surrogates));
        }

        private class ASyncRead
        {
            private readonly SerialPort _com;
            private string _result;

            private readonly AutoResetEvent _readCompletedEvent;
            private readonly AutoResetEvent _readStartedEvent;

            private Exception _exception;

            public ASyncRead(SerialPort com)
            {
                _com = com;

                _result = null;

                _readCompletedEvent = new AutoResetEvent(false);
                _readStartedEvent = new AutoResetEvent(false);

                _exception = null;
            }

            public void Read()
            {
                try
                {
                    _readStartedEvent.Set();
                    _result = _com.ReadLine();
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

            public string Result => _result;

            public Exception Exception => _exception;
        }

        #endregion
    }
}

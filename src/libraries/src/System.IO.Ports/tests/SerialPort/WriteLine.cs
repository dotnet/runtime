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
    public class WriteLine : PortsTest
    {
        //The string size used when verifying NewLine
        private const int NEWLINE_TESTING_STRING_SIZE = 4;

        //The string size used when veryifying encoding 
        private const int ENCODING_STRING_SIZE = 4;

        //The string size used for large string testing
        private const int LARGE_STRING_SIZE = 2048;

        //The default number of times the write method is called when verifying write
        private const int DEFAULT_NUM_WRITES = 3;
        private const string DEFAULT_NEW_LINE = "\n";
        private const int MIN_NUM_NEWLINE_CHARS = 1;
        private const int MAX_NUM_NEWLINE_CHARS = 5;

        #region Test Cases
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void ASCIIEncoding()
        {
            Debug.WriteLine("Verifying write method with ASCIIEncoding");
            VerifyWrite(new ASCIIEncoding(), ENCODING_STRING_SIZE, GenRandomNewLine(true));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void UTF8Encoding()
        {
            Debug.WriteLine("Verifying write method with UTF8Encoding");
            VerifyWrite(new UTF8Encoding(), ENCODING_STRING_SIZE, GenRandomNewLine(false));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void UTF32Encoding()
        {
            Debug.WriteLine("Verifying write method with UTF32Encoding");
            VerifyWrite(new UTF32Encoding(), ENCODING_STRING_SIZE, GenRandomNewLine(false));
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void UnicodeEncoding()
        {
            Debug.WriteLine("Verifying write method with UnicodeEncoding");
            VerifyWrite(new UnicodeEncoding(), ENCODING_STRING_SIZE, GenRandomNewLine(false));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        private void NullString()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying Write with a null string");
                com.Open();

                try
                {
                    com.WriteLine(null);
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void EmptyString()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying Write with an empty string");

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                VerifyWriteLine(com1, com2, "");
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void String_Null_Char()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Debug.WriteLine("Verifying Write with an string containing only the null character");

                com1.Open();
                com1.NewLine = GenRandomNewLine(true);

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                VerifyWriteLine(com1, com2, "\0");
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void LargeString()
        {
            Debug.WriteLine("Verifying write method with a large string size");
            VerifyWrite(new UnicodeEncoding(), LARGE_STRING_SIZE, DEFAULT_NEW_LINE, 1);
        }


        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void StrContains_NewLine_RND()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random(-55);
                StringBuilder strBldrToWrite = TCSupport.GetRandomStringBuilder(NEWLINE_TESTING_STRING_SIZE,
                    TCSupport.CharacterOptions.None);

                string newLine = GenRandomNewLine(true);

                Debug.WriteLine(
                    "Verifying write method with a random NewLine string and writing a string that contains the NewLine");

                com1.NewLine = newLine;
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                strBldrToWrite.Insert(rndGen.Next(0, NEWLINE_TESTING_STRING_SIZE), newLine);

                VerifyWriteLine(com1, com2, strBldrToWrite.ToString());
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void StrContains_NewLine_CRLF()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random(-55);
                StringBuilder strBldrToWrite = TCSupport.GetRandomStringBuilder(NEWLINE_TESTING_STRING_SIZE,
                    TCSupport.CharacterOptions.None);

                string newLine = "\r\n";

                Debug.WriteLine(
                    "Verifying write method with a NewLine=\\r\\n string and writing a string that contains the NewLine");

                com1.NewLine = newLine;
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                strBldrToWrite.Insert(rndGen.Next(0, NEWLINE_TESTING_STRING_SIZE), newLine);

                VerifyWriteLine(com1, com2, strBldrToWrite.ToString());
            }
        }

        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        private void StrContains_NewLine_null()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                Random rndGen = new Random(-55);
                StringBuilder strBldrToWrite = TCSupport.GetRandomStringBuilder(NEWLINE_TESTING_STRING_SIZE,
                    TCSupport.CharacterOptions.None);

                string newLine = "\0";

                Debug.WriteLine(
                    "Verifying write method with a NewLine=\\0 string and writing a string that contains the NewLine");

                com1.NewLine = newLine;
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                strBldrToWrite.Insert(rndGen.Next(0, NEWLINE_TESTING_STRING_SIZE), newLine);

                VerifyWriteLine(com1, com2, strBldrToWrite.ToString());
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyWrite(Encoding encoding, int strSize, string newLine)
        {
            VerifyWrite(encoding, strSize, newLine, DEFAULT_NUM_WRITES);
        }

        private void VerifyWrite(Encoding encoding, int strSize, string newLine, int numWrites)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                string stringToWrite = TCSupport.GetRandomString(NEWLINE_TESTING_STRING_SIZE, TCSupport.CharacterOptions.None);

                TCSupport.SetHighSpeed(com1, com2);

                com1.Encoding = encoding;
                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                VerifyWriteLine(com1, com2, stringToWrite, numWrites);
            }
        }


        private void VerifyWriteLine(SerialPort com1, SerialPort com2, string stringToWrite)
        {
            VerifyWriteLine(com1, com2, stringToWrite, DEFAULT_NUM_WRITES);
        }


        private void VerifyWriteLine(SerialPort com1, SerialPort com2, string stringToWrite, int numWrites)
        {
            char[] expectedChars, actualChars;
            byte[] expectedBytes, actualBytes;
            int byteRead;
            int index = 0;
            int numNewLineBytes;
            char[] newLineChars = com1.NewLine.ToCharArray();
            StringBuilder expectedStrBldr = new StringBuilder();
            string expectedString;

            expectedBytes = com1.Encoding.GetBytes(stringToWrite.ToCharArray());
            expectedChars = com1.Encoding.GetChars(expectedBytes);

            numNewLineBytes = com1.Encoding.GetByteCount(com1.NewLine.ToCharArray());
            actualBytes = new byte[(expectedBytes.Length + numNewLineBytes) * numWrites];

            for (int i = 0; i < numWrites; i++)
            {
                com1.WriteLine(stringToWrite);
                expectedStrBldr.Append(expectedChars);
                expectedStrBldr.Append(com1.NewLine);
            }

            expectedString = expectedStrBldr.ToString();

            com2.ReadTimeout = 500;

            Thread.Sleep((int)(((expectedBytes.Length * 10.0) / com1.BaudRate) * 1000) + 250);

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
            var actualString = new string(actualChars);

            Assert.Equal(expectedString, actualString);
        }

        private string GenRandomNewLine(bool validAscii)
        {
            Random rndGen = new Random(-55);
            int newLineLength = rndGen.Next(MIN_NUM_NEWLINE_CHARS, MAX_NUM_NEWLINE_CHARS);

            if (validAscii)
                return new string(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.ASCII));
            else
                return new string(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.Surrogates));
        }
        #endregion
    }
}

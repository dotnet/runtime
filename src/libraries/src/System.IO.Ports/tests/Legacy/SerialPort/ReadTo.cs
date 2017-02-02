// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class ReadTo
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/19 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadTo(str)";
    public static readonly String s_strTFName = "ReadTo.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The number of random bytes to receive for read method testing
    public static readonly int DEFAULT_NUM_CHARS_TO_READ = 8;

    //The number of new lines to insert into the string not including the one at the end
    public static readonly int DEFAULT_NUMBER_NEW_LINES = 2;

    //The number of random bytes to receive for large input buffer testing
    public static readonly int LARGE_NUM_CHARS_TO_READ = 2048;
    public static readonly int MIN_NUM_NEWLINE_CHARS = 1;
    public static readonly int MAX_NUM_NEWLINE_CHARS = 5;

    public enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadTo objTest = new ReadTo();
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(objTest.AppDomainUnhandledException_EventHandler);

        Console.WriteLine(s_strTFPath + " " + s_strTFName + " , for " + s_strClassMethod + " , Source ver : " + s_strDtTmVer);

        try
        {
            objTest.RunTest();
        }
        catch (Exception e)
        {
            Console.WriteLine(s_strTFAbbrev + " : FAIL The following exception was thorwn in RunTest(): \n" + e.ToString());
            objTest._numErrors++;
            objTest._exitValue = TCSupport.FailExitCode;
        }

        ////	Finish Diagnostics
        if (objTest._numErrors == 0)
        {
            Console.WriteLine("PASS.	 " + s_strTFPath + " " + s_strTFName + " ,numTestcases==" + objTest._numTestcases);
        }
        else
        {
            Console.WriteLine("FAIL!	 " + s_strTFPath + " " + s_strTFName + " ,numErrors==" + objTest._numErrors);

            if (TCSupport.PassExitCode == objTest._exitValue)
                objTest._exitValue = TCSupport.FailExitCode;
        }

        Environment.ExitCode = objTest._exitValue;
    }

    private void AppDomainUnhandledException_EventHandler(Object sender, UnhandledExceptionEventArgs e)
    {
        _numErrors++;
        Console.WriteLine("\nAn unhandled exception was thrown and not caught in the app domain: \n{0}", e.ExceptionObject);
        Console.WriteLine("Test FAILED!!!\n");

        Environment.ExitCode = 101;
    }

    public bool RunTest()
    {
        bool retValue = true;
        TCSupport tcSupport = new TCSupport();

        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLine_Contains_nullChar), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLine_CR), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLine_LF), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLine_CRLF_RndStr), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLine_CRLF_CRStr), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLine_CRLF_LFStr), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLine_END), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UnicodeEncoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeInputBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadToWriteLine_ASCII), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadToWriteLine_UTF7), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadToWriteLine_UTF8), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadToWriteLine_UTF32), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadToWriteLine_Unicode), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(GreedyRead), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(NullNewLine), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(EmptyNewLine), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(NewLineSubstring), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_DataReceivedBeforeTimeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_Timeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_LargeBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_SurrogateCharacter), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool NewLine_Contains_nullChar()
    {
        if (!VerifyRead(new System.Text.ASCIIEncoding(), "\0"))
        {
            Console.WriteLine("Err_001!!! Verifying read method with NewLine containing just the null character");
            return false;
        }

        return true;
    }


    public bool NewLine_CR()
    {
        if (!VerifyRead(new System.Text.ASCIIEncoding(), "\r"))
        {
            Console.WriteLine("Err_002!!! Verifying read method with \\r NewLine");
            return false;
        }

        return true;
    }


    public bool NewLine_LF()
    {
        if (!VerifyRead(new System.Text.ASCIIEncoding(), "\n"))
        {
            Console.WriteLine("Err_003!!! Verifying read method with \\n NewLine");
            return false;
        }

        return true;
    }


    public bool NewLine_CRLF_RndStr()
    {
        if (!VerifyRead(new System.Text.ASCIIEncoding(), "\r\n"))
        {
            Console.WriteLine("Err_004!!! Verifying read method with \\r\\n NewLine and a random string");
            return false;
        }

        return true;
    }


    public bool NewLine_CRLF_CRStr()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);

        Console.WriteLine("Verifying read method with \\r\\n NewLine and a string containing just \\r");
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        if (!VerifyReadTo(com1, com2, "TEST\r", "\r\n"))
        {
            Console.WriteLine("Err_005!!! Verifying read method with \\r\\n NewLine and a string containing just \\r");
            return false;
        }

        return true;
    }


    public bool NewLine_CRLF_LFStr()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);

        Console.WriteLine("Verifying read method with \\r\\n NewLine and a string containing just \\n");
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        if (!VerifyReadTo(com1, com2, "TEST\n", "\r\n"))
        {
            Console.WriteLine("Err_006!!! Verifying read method with \\r\\n NewLine and a string containing just \\n");
            return false;
        }

        return true;
    }


    public bool NewLine_END()
    {
        if (!VerifyRead(new System.Text.ASCIIEncoding(), "END"))
        {
            Console.WriteLine("Err_007!!! Verifying read method with END NewLine");
            return false;
        }

        return true;
    }


    public bool ASCIIEncoding()
    {
        if (!VerifyRead(new System.Text.ASCIIEncoding(), GenRandomNewLine(true)))
        {
            Console.WriteLine("Err_08!!! Verifying read method with ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        if (!VerifyRead(new System.Text.UTF7Encoding(), GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_09!!! Verifying read method with UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        if (!VerifyRead(new System.Text.UTF8Encoding(), GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_010!!! Verifying read method with UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        if (!VerifyRead(new System.Text.UTF32Encoding(), GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_011!!! Verifying read method with UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool UnicodeEncoding()
    {
        if (!VerifyRead(new System.Text.UnicodeEncoding(), GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_012!!! Verifying read method with Unicode");
            return false;
        }

        return true;
    }


    public bool LargeInputBuffer()
    {
        if (!VerifyRead(LARGE_NUM_CHARS_TO_READ))
        {
            Console.WriteLine("Err_013!!! Verifying read method with large input buffer");
            return false;
        }

        return true;
    }


    public bool ReadToWriteLine_ASCII()
    {
        if (!VerifyReadToWithWriteLine(new System.Text.ASCIIEncoding(), GenRandomNewLine(true)))
        {
            Console.WriteLine("Err_014!!! Verifying ReadTo with WriteLine and ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool ReadToWriteLine_UTF7()
    {
        if (!VerifyReadToWithWriteLine(new System.Text.UTF7Encoding(), GenRandomNewLine(true)))
        {
            Console.WriteLine("Err_015!!! Verifying ReadTo with WriteLine and UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool ReadToWriteLine_UTF8()
    {
        if (!VerifyReadToWithWriteLine(new System.Text.UTF8Encoding(), GenRandomNewLine(true)))
        {
            Console.WriteLine("Err_016!!! Verifying ReadTo with WriteLine and UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool ReadToWriteLine_UTF32()
    {
        if (!VerifyReadToWithWriteLine(new System.Text.UTF32Encoding(), GenRandomNewLine(true)))
        {
            Console.WriteLine("Err_017!!! Verifying ReadTo with WriteLine and UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool ReadToWriteLine_Unicode()
    {
        if (!VerifyReadToWithWriteLine(new System.Text.UnicodeEncoding(), GenRandomNewLine(true)))
        {
            Console.WriteLine("Err_018!!! Verifying ReadTo with WriteLine and UnicodeEncoding");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedData()
    {
        int numBytesToRead = 32;
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        System.Text.StringBuilder strBldrToWrite = new System.Text.StringBuilder();
        bool retValue = true;

        //Genrate random characters
        for (int i = 0; i < numBytesToRead; i++)
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

        retValue &= BufferData(com1, com2, strBldrToWrite.ToString());
        retValue &= PerformReadOnCom1FromCom2(com1, com2, strBldrToWrite.ToString(), com1.NewLine);

        if (!retValue)
        {
            Console.WriteLine("Err_2507ajlsp!!! Verifying read method with reading all of the buffered data in one call");
            return false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool SerialPort_IterativeReadBufferedData()
    {
        int numBytesToRead = 32;

        if (!VerifyRead(System.Text.Encoding.ASCII, GenRandomNewLine(true), numBytesToRead, 1, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_1659akl!!! Verifying read method with reading the buffered data in several calls");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedAndNonBufferedData()
    {
        int numBytesToRead = 32;
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        System.Text.StringBuilder strBldrToWrite = new System.Text.StringBuilder();
        System.Text.StringBuilder strBldrExpected = new System.Text.StringBuilder();
        bool retValue = true;

        //Genrate random characters
        for (int i = 0; i < numBytesToRead; i++)
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

        retValue &= BufferData(com1, com2, strBldrToWrite.ToString());

        strBldrExpected.Append(strBldrToWrite);
        strBldrToWrite.Append(com1.NewLine);
        strBldrExpected.Append(strBldrToWrite);

        retValue &= VerifyReadTo(com1, com2, strBldrToWrite.ToString(), strBldrExpected.ToString(), com1.NewLine);

        if (!retValue)
        {
            Console.WriteLine("Err_2082aspzh!!! Verifying read method with reading all of the buffered an non buffered data in one call");
            return false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool SerialPort_IterativeReadBufferedAndNonBufferedData()
    {
        int numBytesToRead = 3;

        if (!VerifyRead(System.Text.Encoding.ASCII, GenRandomNewLine(true), numBytesToRead, 1, ReadDataFromEnum.BufferedAndNonBuffered))
        {
            Console.WriteLine("Err_5687nhnhl!!! Verifying read method with reading the buffered and non buffereddata in several calls");
            return false;
        }

        return true;
    }


    public bool GreedyRead()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(128, TCSupport.CharacterOptions.Surrogates);
        byte[] byteXmitBuffer = new byte[1024];
        char[] expectedChars;
        string rcvString;
        char[] actualChars;
        char utf32Char = TCSupport.GenerateRandomCharNonSurrogate();
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        int numBytes;
        bool retValue = true;

        Console.WriteLine("Verifying that ReadTo() will read everything from internal buffer and drivers buffer");

        //Put the first byte of the utf32 encoder char in the last byte of this buffer
        //when we read this later the buffer will have to be resized
        byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

        while (com1.BytesToRead < byteXmitBuffer.Length)
            System.Threading.Thread.Sleep(50);

        //Read Every Byte except the last one. The last bye should be left in the last position of SerialPort's
        //internal buffer. When we try to read this char as UTF32 the buffer should have to be resized so 
        //the other 3 bytes of the ut32 encoded char can be in the buffer
        com1.Read(new char[1023], 0, 1023);

        if (1 != com1.BytesToRead)
        {
            Console.WriteLine("Err_9416sapz ExpectedByteToRead={0} actual={1}", 1, com1.BytesToRead);
            retValue = false;
        }

        com1.Encoding = System.Text.Encoding.UTF32;
        com2.Encoding = System.Text.Encoding.UTF32;

        com2.Write(utf32CharBytes, 1, 3);
        com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
        com2.WriteLine(String.Empty);

        numBytes = System.Text.Encoding.UTF32.GetByteCount(charXmitBuffer);

        byte[] byteBuffer = System.Text.Encoding.UTF32.GetBytes(charXmitBuffer);

        expectedChars = new char[1 + System.Text.Encoding.UTF32.GetCharCount(byteBuffer)];
        expectedChars[0] = utf32Char;

        System.Text.Encoding.UTF32.GetChars(byteBuffer, 0, byteBuffer.Length, expectedChars, 1);

        while (com1.BytesToRead < 4 + numBytes)
        {
            System.Threading.Thread.Sleep(50);
        }

        if (null == (rcvString = com1.ReadTo(com2.NewLine)))
        {
            Console.WriteLine("Err_6481sfadw ReadTo returned null");
            retValue = false;
        }
        else
        {
            actualChars = rcvString.ToCharArray();

            if (actualChars.Length != expectedChars.Length)
            {
                Console.WriteLine("Err_0872watr Expected to read {0} chars actually read {1} chars", expectedChars.Length, actualChars.Length);
                retValue = false;
            }
            else
            {
                for (int i = 0; i < expectedChars.Length; i++)
                {
                    if (expectedChars[i] != actualChars[i])
                    {
                        Console.WriteLine("Err_70782apzh Expected to read {0} actually read {1}", (int)expectedChars[i], (int)actualChars[i]);
                        retValue = false;
                        break;
                    }
                }
            }
        }

        if (0 != com1.BytesToRead)
        {
            Console.WriteLine("Err_78028asdf ExpectedByteToRead={0} actual={1}", 0, com1.BytesToRead);
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_1389 Verifying that ReadTo() will read everything from internal buffer and drivers buffer failed");

        com1.Close();
        com2.Close();

        return retValue;
    }


    public bool NullNewLine()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying read method thows ArgumentExcpetion with a null NewLine string");
        com.Open();

        if (!VerifyReadException(com, null, typeof(System.ArgumentNullException)))
        {
            Console.WriteLine("Err_019!!! Verifying read method with a null NewLine string");
            return false;
        }

        if (com.IsOpen)
            com.Close();

        return true;
    }


    public bool EmptyNewLine()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying read method thows ArgumentExcpetion with a empty NewLine string");
        com.Open();

        if (!VerifyReadException(com, "", typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_020!!! Verifying read method with a empty NewLine string");
            return false;
        }

        if (com.IsOpen)
            com.Close();

        return true;
    }


    public bool NewLineSubstring()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);

        Console.WriteLine("Verifying read method with sub strings of the new line appearing in the string being read");
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        string newLine = "asfg";
        string newLineSubStrings = "a" + "as" + "asf" + "sfg" + "fg" + "g"; //All the substrings of newLine
        string testStr = newLineSubStrings + "asfg" + newLineSubStrings;

        if (!VerifyReadTo(com1, com2, testStr, newLine))
        {
            Console.WriteLine("Err_019!!! Verifying read method with sub strings of the new line appearing in the string being read FAILED");
            return false;
        }

        return true;
    }

    public bool Read_DataReceivedBeforeTimeout()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
        char[] charRcvBuffer;
        string endString = "END";
        ASyncRead asyncRead = new ASyncRead(com1, endString);
        System.Threading.Thread asyncReadThread = new System.Threading.Thread(new System.Threading.ThreadStart(asyncRead.Read));
        bool retValue = true;
        char endChar = endString[0];
        char notEndChar = TCSupport.GetRandomOtherChar(endChar, TCSupport.CharacterOptions.None);

        Console.WriteLine("Verifying that ReadTo(string) will read characters that have been received after the call to Read was made");

        //Ensure the new line is not in charXmitBuffer
        for (int i = 0; i < charXmitBuffer.Length; ++i)
        {//Se any appearances of a character in the new line string to some other char
            if (endChar == charXmitBuffer[i])
            {
                charXmitBuffer[i] = notEndChar;
            }
        }

        com1.Encoding = System.Text.Encoding.UTF8;
        com2.Encoding = System.Text.Encoding.UTF8;
        com1.ReadTimeout = 20000; // 20 seconds

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        asyncReadThread.Start();
        asyncRead.ReadStartedEvent.WaitOne(); //This only tells us that the thread has started to execute code in the method
        System.Threading.Thread.Sleep(2000); //We need to wait to guarentee that we are executing code in SerialPort
        com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
        com2.Write(endString);

        asyncRead.ReadCompletedEvent.WaitOne();

        if (null != asyncRead.Exception)
        {
            retValue = false;
            Console.WriteLine("Err_04448ajhied Unexpected exception thrown from async read:\n{0}", asyncRead.Exception);
        }
        else if (null == asyncRead.Result || 0 == asyncRead.Result.Length)
        {
            retValue = false;
            Console.WriteLine("Err_0158ahei Expected Read to read at least one character");
        }
        else
        {
            charRcvBuffer = asyncRead.Result.ToCharArray();

            if (charRcvBuffer.Length != charXmitBuffer.Length)
            {
                retValue = false;
                Console.WriteLine("Err_051884ajoedo Expected Read to read {0} cahracters actually read {1}",
                    charXmitBuffer.Length, charRcvBuffer.Length);
            }
            else
            {
                for (int i = 0; i < charXmitBuffer.Length; ++i)
                {
                    if (charRcvBuffer[i] != charXmitBuffer[i])
                    {
                        retValue = false;
                        Console.WriteLine("Err_0518895akiezp Characters differ at {0} expected:{1}({2:X}) actual:{3}({4:X})",
                            i, charXmitBuffer[i], (int)charXmitBuffer[i], charRcvBuffer[i], (int)charRcvBuffer[i]);
                    }
                }
            }
        }

        if (!VerifyReadTo(com1, com2, new string(charXmitBuffer), endString))
        {
            Console.WriteLine("Err_05188ajied Verify ReadTo after read failed");
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_018068ajkid Verifying that ReadTo(string) will read characters that have been received after the call to Read was made failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_Timeout()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
        char[] charRcvBuffer;
        string endString = "END";
        bool retValue = true;
        char endChar = endString[0];
        char notEndChar = TCSupport.GetRandomOtherChar(endChar, TCSupport.CharacterOptions.None);
        string result;

        Console.WriteLine("Verifying that ReadTo(string) works appropriately after TimeoutException has been thrown");

        //Ensure the new line is not in charXmitBuffer
        for (int i = 0; i < charXmitBuffer.Length; ++i)
        {//Se any appearances of a character in the new line string to some other char
            if (endChar == charXmitBuffer[i])
            {
                charXmitBuffer[i] = notEndChar;
            }
        }

        com1.Encoding = System.Text.Encoding.Unicode;
        com2.Encoding = System.Text.Encoding.Unicode;
        com1.ReadTimeout = 500; // 20 seconds

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);

        try
        {
            com1.ReadTo(endString);
            Console.WriteLine("Err_29299aize Expected ReadTo to throw TimeoutException");
            retValue = false;
        }
        catch (TimeoutException) { }//Expected

        if (2 * charXmitBuffer.Length != com1.BytesToRead)
        {
            Console.WriteLine("Err_0585haieidp Expected BytesToRead: {0} actual: {1}", 2 * charXmitBuffer.Length, com1.BytesToRead);
            retValue = false;
        }

        com2.Write(endString);
        result = com1.ReadTo(endString);
        charRcvBuffer = result.ToCharArray();

        if (charRcvBuffer.Length != charXmitBuffer.Length)
        {
            retValue = false;
            Console.WriteLine("Err_051884ajoedo Expected Read to read {0} cahracters actually read {1}",
                charXmitBuffer.Length, charRcvBuffer.Length);
        }
        else
        {
            for (int i = 0; i < charXmitBuffer.Length; ++i)
            {
                if (charRcvBuffer[i] != charXmitBuffer[i])
                {
                    retValue = false;
                    Console.WriteLine("Err_8988auzobn Characters differ at {0} expected:{1}({2:X}) actual:{3}({4:X})",
                        i, charXmitBuffer[i], (int)charXmitBuffer[i], charRcvBuffer[i], (int)charRcvBuffer[i]);
                }
            }
        }

        if (!VerifyReadTo(com1, com2, new string(charXmitBuffer), endString))
        {
            Console.WriteLine("Err_05188ajied Verify ReadTo after read failed");
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_05498352aiiueid Verifying that ReadTo(string) works appropriately after TimeoutException has been thrown failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_LargeBuffer()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
        string stringRcvBuffer;
        bool retValue = true;

        bool continueRunning = true;
        int numberOfIterations = 0;
        System.Threading.Thread writeToCom2Thread = new System.Threading.Thread(delegate ()
        {
            while (continueRunning)
            {
                com1.Write(charXmitBuffer, 0, charXmitBuffer.Length);
                ++numberOfIterations;
            }
        });

        char endChar = TCSupport.GenerateRandomCharNonSurrogate();
        char notEndChar = TCSupport.GetRandomOtherChar(endChar, TCSupport.CharacterOptions.None);

        //Ensure the new line is not in charXmitBuffer
        for (int i = 0; i < charXmitBuffer.Length; ++i)
        {//Se any appearances of a character in the new line string to some other char
            if (endChar == charXmitBuffer[i])
            {
                charXmitBuffer[i] = notEndChar;
            }
        }

        com1.BaudRate = 115200;
        com2.BaudRate = 115200;

        com1.Encoding = System.Text.Encoding.Unicode;
        com2.Encoding = System.Text.Encoding.Unicode;

        com2.ReadTimeout = 10000;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        writeToCom2Thread.Start();

        try
        {
            com2.ReadTo(new String(endChar, 1));
            retValue = false;
            Console.WriteLine("Err_2928aneieud Expected ReadLine() to throw timeoutException()");
        }
        catch (TimeoutException) { };

        continueRunning = false;
        writeToCom2Thread.Join();

        com1.Write(new string(endChar, 1));

        stringRcvBuffer = com2.ReadTo(new String(endChar, 1));

        if (charXmitBuffer.Length * numberOfIterations == stringRcvBuffer.Length)
        {
            for (int i = 0; i < charXmitBuffer.Length * numberOfIterations; ++i)
            {
                if (stringRcvBuffer[i] != charXmitBuffer[i % charXmitBuffer.Length])
                {
                    retValue = false;
                    Console.WriteLine("Err_292aneid Expected to read {0} actually read {1}", charXmitBuffer[i % charXmitBuffer.Length], stringRcvBuffer[i]);
                    break;
                }
            }
        }
        else
        {
            retValue = false;
            Console.WriteLine("Err_292haie Expected to read {0} characters actually read {1}", charXmitBuffer.Length * numberOfIterations, stringRcvBuffer.Length);
        }

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_SurrogateCharacter()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);

        Console.WriteLine("Verifying read method with surrogate pair in the input and a surrogate pair for the newline");
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        string surrogatePair = "\uD800\uDC00";
        string newLine = "\uD801\uDC01";
        string input = TCSupport.GetRandomString(256, TCSupport.CharacterOptions.None) + surrogatePair + newLine;

        com1.NewLine = newLine;

        if (!VerifyReadTo(com1, com2, input, newLine))
        {
            Console.WriteLine("Err_342882haue!!! Verifying read method with surrogate pair in the input and a surrogate pair for the newline FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyReadException(SerialPort com, string newLine, Type expectedException)
    {
        bool retValue = true;

        try
        {
            com.ReadTo(newLine);
            Console.WriteLine("ERROR!!!: No Excpetion was thrown");
            retValue = false;
        }
        catch (System.Exception e)
        {
            if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!!: {0} exception was thrown expected {1}", e.GetType(), expectedException);
                retValue = false;
            }
        }
        return retValue;
    }


    public bool VerifyRead(int numberOfBytesToRead)
    {
        return VerifyRead(new System.Text.ASCIIEncoding(), "\n", numberOfBytesToRead, DEFAULT_NUMBER_NEW_LINES, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(System.Text.Encoding encoding)
    {
        return VerifyRead(encoding, "\n", DEFAULT_NUM_CHARS_TO_READ, DEFAULT_NUMBER_NEW_LINES, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(System.Text.Encoding encoding, string newLine)
    {
        return VerifyRead(encoding, newLine, DEFAULT_NUM_CHARS_TO_READ, DEFAULT_NUMBER_NEW_LINES, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(System.Text.Encoding encoding, string newLine, int numBytesRead, int numNewLines, ReadDataFromEnum readDataFrom)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        System.Text.StringBuilder strBldrToWrite;
        int numNewLineChars = newLine.ToCharArray().Length;
        int minLength = (1 + numNewLineChars) * numNewLines;

        if (minLength < numBytesRead)
            strBldrToWrite = TCSupport.GetRandomStringBuilder(numBytesRead, TCSupport.CharacterOptions.None);
        else
            strBldrToWrite = TCSupport.GetRandomStringBuilder(rndGen.Next(minLength, minLength * 2), TCSupport.CharacterOptions.None);

        //We need place the newLine so that they do not write over eachother
        int divisionLength = strBldrToWrite.Length / numNewLines;
        int range = divisionLength - numNewLineChars;

        for (int i = 0; i < numNewLines; i++)
        {
            int newLineIndex = rndGen.Next(0, range + 1);

            strBldrToWrite.Insert(newLineIndex + (i * divisionLength) + (i * numNewLineChars), newLine);
        }

        Console.WriteLine("Verifying ReadTo encoding={0}, newLine={1}, numBytesRead={2}, numNewLines={3}", encoding, newLine, numBytesRead, numNewLines);

        com1.ReadTimeout = 500;
        com1.Encoding = encoding;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        switch (readDataFrom)
        {
            case ReadDataFromEnum.NonBuffered:
                return VerifyReadNonBuffered(com1, com2, strBldrToWrite.ToString(), newLine);

            case ReadDataFromEnum.Buffered:
                return VerifyReadBuffered(com1, com2, strBldrToWrite.ToString(), newLine);

            case ReadDataFromEnum.BufferedAndNonBuffered:
                return VerifyReadBufferedAndNonBuffered(com1, com2, strBldrToWrite.ToString(), newLine);
        }
        return false;
    }


    private bool VerifyReadNonBuffered(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
    {
        return VerifyReadTo(com1, com2, strToWrite, newLine);
    }


    private bool VerifyReadBuffered(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
    {
        BufferData(com1, com2, strToWrite);
        return PerformReadOnCom1FromCom2(com1, com2, strToWrite, newLine);
    }


    private bool VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
    {
        BufferData(com1, com2, strToWrite);
        return VerifyReadTo(com1, com2, strToWrite, strToWrite + strToWrite, newLine);
    }


    private bool BufferData(SerialPort com1, SerialPort com2, string strToWrite)
    {
        bool retValue = true;
        char[] charsToWrite;
        byte[] bytesToWrite;

        charsToWrite = strToWrite.ToCharArray();
        bytesToWrite = com1.Encoding.GetBytes(charsToWrite);

        com2.Write(bytesToWrite, 0, 1); // Write one byte at the begining because we are going to read this to buffer the rest of the data    
        com2.Write(bytesToWrite, 0, bytesToWrite.Length);

        while (com1.BytesToRead < bytesToWrite.Length)
        {
            System.Threading.Thread.Sleep(50);
        }

        com1.Read(new char[1], 0, 1); // This should put the rest of the bytes in SerialPorts own internal buffer

        if (com1.BytesToRead != bytesToWrite.Length)
        {
            Console.WriteLine("Err_7083zaz Expected com1.BytesToRead={0} actual={1}", bytesToWrite.Length, com1.BytesToRead);
            retValue = false;
        }

        return retValue;
    }


    private bool VerifyReadTo(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
    {
        return VerifyReadTo(com1, com2, strToWrite, strToWrite, newLine);
    }


    private bool VerifyReadTo(SerialPort com1, SerialPort com2, string strToWrite, string expectedString, string newLine)
    {
        char[] charsToWrite;
        byte[] bytesToWrite;

        charsToWrite = strToWrite.ToCharArray();
        bytesToWrite = com1.Encoding.GetBytes(charsToWrite);

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 500;
        System.Threading.Thread.Sleep((int)((((bytesToWrite.Length + 1) * 10.0) / com1.BaudRate) * 1000) + 250);

        return PerformReadOnCom1FromCom2(com1, com2, expectedString, newLine);
    }


    private bool PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, string strToWrite, string newLine)
    {
        bool retValue = true;
        char[] charsToWrite;
        byte[] bytesToWrite;
        string rcvString;
        System.Text.StringBuilder strBldrRead = new System.Text.StringBuilder();
        char[] rcvCharBuffer;
        int newLineStringLength = newLine.Length;
        int numNewLineChars = newLine.ToCharArray().Length;
        int numNewLineBytes = com1.Encoding.GetByteCount(newLine.ToCharArray());
        int bytesRead, totalBytesRead, charsRead, totalCharsRead;
        int indexOfNewLine, lastIndexOfNewLine = -newLineStringLength;
        string expectedString;
        bool isUTF7Encoding = com1.Encoding.EncodingName == System.Text.Encoding.UTF7.EncodingName;

        charsToWrite = strToWrite.ToCharArray();
        bytesToWrite = com1.Encoding.GetBytes(charsToWrite);
        expectedString = new string(com1.Encoding.GetChars(bytesToWrite));

        totalBytesRead = 0;
        totalCharsRead = 0;

        while (true)
        {
            try
            {
                rcvString = com1.ReadTo(newLine);
            }
            catch (TimeoutException)
            {
                break;
            }

            //While their are more characters to be read
            rcvCharBuffer = rcvString.ToCharArray();
            charsRead = rcvCharBuffer.Length;

            if (isUTF7Encoding)
            {
                totalBytesRead = GetUTF7EncodingBytes(charsToWrite, 0, totalCharsRead + charsRead + numNewLineChars);
            }
            else
            {
                bytesRead = com1.Encoding.GetByteCount(rcvCharBuffer, 0, charsRead);
                totalBytesRead += bytesRead + numNewLineBytes;
            }

            //			indexOfNewLine = strToWrite.IndexOf(newLine, lastIndexOfNewLine + newLineStringLength);
            indexOfNewLine = TCSupport.OrdinalIndexOf(expectedString, lastIndexOfNewLine + newLineStringLength, newLine);//SerialPort does a Ordinal comparison


            if ((indexOfNewLine - (lastIndexOfNewLine + newLineStringLength)) != charsRead)
            {
                //If we have not read all of the characters that we should have
                Console.WriteLine("Err_1707ahsp!!!: Read did not return all of the characters that were in SerialPort buffer");
                Console.WriteLine("indexOfNewLine={0} lastIndexOfNewLine={1} charsRead={2} numNewLineChars={3} newLineStringLength={4} strToWrite.Length={5}",
                    indexOfNewLine, lastIndexOfNewLine, charsRead, numNewLineChars, newLineStringLength, strToWrite.Length);
                Console.WriteLine(strToWrite);

                retValue = false;
            }

            if (charsToWrite.Length < totalCharsRead + charsRead)
            {
                //If we have read in more characters then we expect
                Console.WriteLine("Err_21707adad!!!: We have received more characters then were sent");
                retValue = false;
                break;
            }

            strBldrRead.Append(rcvString);
            strBldrRead.Append(newLine);

            totalCharsRead += charsRead + numNewLineChars;

            lastIndexOfNewLine = indexOfNewLine;

            if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
            {
                System.Console.WriteLine("Err_99087ahpbx!!!: Expected BytesToRead={0} actual={1}", bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                retValue = false;
            }
        }//End while there are more characters to read

        if (0 != com1.BytesToRead)
        {
            //If there are more bytes to read but there must not be a new line char at the end
            int charRead;

            while (true)
            {
                try
                {
                    charRead = com1.ReadChar();
                    strBldrRead.Append((char)charRead);
                }
                catch (TimeoutException) { break; }
                catch (Exception e)
                {
                    Console.WriteLine("Err_15054akjeid!!!: The following exception was thrown while reading remaining chars");
                    Console.WriteLine(e);
                    retValue = false;
                }
            }
        }

        if (0 != com1.BytesToRead && (!isUTF7Encoding || 1 != com1.BytesToRead))
        {
            System.Console.WriteLine("Err_558596ahbpa!!!: BytesToRead is not zero");
            retValue = false;
        }

        if (0 != expectedString.CompareTo(strBldrRead.ToString()))
        {
            System.Console.WriteLine("Err_7797ajpba!!!: Expected to read \"{0}\"  actual read  \"{1}\"", expectedString, strBldrRead.ToString());
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("\nstrToWrite = ");
            TCSupport.PrintChars(strToWrite.ToCharArray());

            Console.WriteLine("\nnewLine = ");
            TCSupport.PrintChars(newLine.ToCharArray());
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private bool VerifyReadToWithWriteLine(System.Text.Encoding encoding, string newLine)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        System.Text.StringBuilder strBldrToWrite = new System.Text.StringBuilder();
        string strRead, strWrite;
        string strExpected;
        bool retValue = true;
        bool isUTF7Encoding = encoding.EncodingName == System.Text.Encoding.UTF7.EncodingName;

        Console.WriteLine("Verifying ReadTo with WriteLine encoding={0}, newLine={1}", encoding, newLine);

        com1.ReadTimeout = 500;
        com2.NewLine = newLine;
        com1.Encoding = encoding;
        com2.Encoding = encoding;

        //Genrate random characters
        do
        {
            strBldrToWrite = new System.Text.StringBuilder();
            for (int i = 0; i < DEFAULT_NUM_CHARS_TO_READ; i++)
            {
                strBldrToWrite.Append((char)rndGen.Next(0, 128));
            }
        } while (-1 != TCSupport.OrdinalIndexOf(strBldrToWrite.ToString(), newLine));//SerialPort does a Ordinal comparison

        strWrite = strBldrToWrite.ToString();
        strExpected = new string(com1.Encoding.GetChars(com1.Encoding.GetBytes(strWrite.ToCharArray())));

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        com2.WriteLine(strBldrToWrite.ToString());
        strRead = com1.ReadTo(newLine);

        if (0 != strBldrToWrite.ToString().CompareTo(strRead))
        {
            Console.WriteLine("ERROR!!! The string written: \"{0}\" and the string read \"{1}\" differ", strBldrToWrite, strRead);
            retValue = false;
        }

        if (0 != com1.BytesToRead && (!isUTF7Encoding || 1 != com1.BytesToRead))
        {
            Console.WriteLine("ERROR!!! BytesToRead={0} expected 0", com1.BytesToRead);
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private string GenRandomNewLine(bool validAscii)
    {
        Random rndGen = new Random(-55);
        int newLineLength = rndGen.Next(MIN_NUM_NEWLINE_CHARS, MAX_NUM_NEWLINE_CHARS);

        if (validAscii)
            return new String(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.ASCII));
        else
            return new String(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.Surrogates));
    }

    private int GetUTF7EncodingBytes(char[] chars, int index, int count)
    {
        byte[] bytes = System.Text.Encoding.UTF7.GetBytes(chars, index, count);
        int byteCount = bytes.Length;

        while (System.Text.Encoding.UTF7.GetCharCount(bytes, 0, byteCount) == count)
        {
            --byteCount;
        }

        return byteCount + 1;
    }

    public class ASyncRead
    {
        private SerialPort _com;
        private string _value;
        private string _result;

        private System.Threading.AutoResetEvent _readCompletedEvent;
        private System.Threading.AutoResetEvent _readStartedEvent;

        private Exception _exception;

        public ASyncRead(SerialPort com, string value)
        {
            _com = com;
            _value = value;

            _result = null;

            _readCompletedEvent = new System.Threading.AutoResetEvent(false);
            _readStartedEvent = new System.Threading.AutoResetEvent(false);

            _exception = null;
        }

        public void Read()
        {
            try
            {
                _readStartedEvent.Set();
                _result = _com.ReadTo(_value);
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

        public System.Threading.AutoResetEvent ReadStartedEvent
        {
            get
            {
                return _readStartedEvent;
            }
        }

        public System.Threading.AutoResetEvent ReadCompletedEvent
        {
            get
            {
                return _readCompletedEvent;
            }
        }

        public string Result
        {
            get
            {
                return _result;
            }
        }

        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }
    }
    #endregion
}

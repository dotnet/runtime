// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class Read_char_int_int
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Read(char[], int, int,)";
    public static readonly String s_strTFName = "Read_byte_int_int.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The number of random bytes to receive for read method testing
    public static readonly int numRndBytesToRead = 8;

    //The number of random bytes to receive for large input buffer testing
    public static readonly int largeNumRndCharsToRead = 2048;

    //When we test Read and do not care about actually reading anything we must still
    //create an byte array to pass into the method the following is the size of the 
    //byte array used in this situation
    public static readonly int defaultCharArraySize = 1;
    public static readonly int defaultCharOffset = 0;
    public static readonly int defaultCharCount = 1;

    //The maximum buffer size when a exception occurs
    public static readonly int maxBufferSizeForException = 255;

    //The maximum buffer size when a exception is not expected
    public static readonly int maxBufferSize = 8;

    public enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Read_char_int_int objTest = new Read_char_int_int();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Buffer_Null), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_NEG1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_NEGRND), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_MinInt), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_NEG1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_NEGRND), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_MinInt), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OffsetCount_EQ_Length_Plus_1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OffsetCount_GT_Length), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_GT_Length), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_GT_Length), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OffsetCount_EQ_Length), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_EQ_Length_Minus_1), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_EQ_Length), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_EQ_Zero), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding_1Char), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding_1Char), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding_1Char), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding_1Char), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeInputBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesFollowedByCharsASCII), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesFollowedByCharsUTF7), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesFollowedByCharsUTF8), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesFollowedByCharsUTF32), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(GreedyRead), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_DataReceivedBeforeTimeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_ResizeBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_Timeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_Partial), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_SurrogateBoundry), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Buffer_Null()
    {
        if (!VerifyReadException(null, 0, 1, typeof(System.ArgumentNullException)))
        {
            Console.WriteLine("Err_001!!! Verifying read method throws exception with buffer equal to null FAILED");
            return false;
        }

        return true;
    }


    public bool Offset_NEG1()
    {
        if (!VerifyReadException(new char[defaultCharArraySize], -1, defaultCharCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_002!!! Verifying read method throws exception with offset equal to -1");
            return false;
        }

        return true;
    }


    public bool Offset_NEGRND()
    {
        Random rndGen = new Random(-55);

        if (!VerifyReadException(new char[defaultCharArraySize], rndGen.Next(Int32.MinValue, 0), defaultCharCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_003!!! Verifying read method throws exception with offset equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Offset_MinInt()
    {
        if (!VerifyReadException(new char[defaultCharArraySize], Int32.MinValue, defaultCharCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_004!!! Verifying read method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool Count_NEG1()
    {
        if (!VerifyReadException(new char[defaultCharArraySize], defaultCharOffset, -1, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_005!!! Verifying read method throws exception with count equal to -1");
            return false;
        }

        return true;
    }


    public bool Count_NEGRND()
    {
        Random rndGen = new Random(-55);

        if (!VerifyReadException(new char[defaultCharArraySize], defaultCharOffset, rndGen.Next(Int32.MinValue, 0), typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_006!!! Verifying read method throws exception with count equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Count_MinInt()
    {
        if (!VerifyReadException(new char[defaultCharArraySize], defaultCharOffset, Int32.MinValue, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_007!!! Verifying read method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length_Plus_1()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(0, bufferLength);
        int count = bufferLength + 1 - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new char[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_009!!! Verifying read method throws exception with offset+count=buffer.length+1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(0, bufferLength);
        int count = rndGen.Next(bufferLength + 1 - offset, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new char[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_010!!! Verifying read method throws exception with offset+count>buffer.length+1");
            return false;
        }

        return true;
    }


    public bool Offset_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(bufferLength, Int32.MaxValue);
        int count = defaultCharCount;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new char[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_011!!! Verifying read method throws exception with offset>buffer.length");
            return false;
        }

        return true;
    }


    public bool Count_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = defaultCharOffset;
        int count = rndGen.Next(bufferLength + 1, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new char[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_012!!! Verifying read method throws exception with count>buffer.length + 1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = bufferLength - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count))
        {
            Console.WriteLine("Err_013!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool Offset_EQ_Length_Minus_1()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = bufferLength - 1;
        int count = 1;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count))
        {
            Console.WriteLine("Err_014!!! Verifying read method with offset=buffer.length - 1");
            return false;
        }

        return true;
    }


    public bool Count_EQ_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = 0;
        int count = bufferLength;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count))
        {
            Console.WriteLine("Err_015!!! Verifying read method with count=buffer.length");
            return false;
        }

        return true;
    }


    public bool Count_EQ_Zero()
    {
        bool retValue = true;
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = 0;
        int count = 0;

        com.Open();
        if (0 != com.Read(new char[bufferLength], offset, count))
        {
            Console.WriteLine("Read did not return 0 with a count of zero");
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("Err_2345jhu!!! Verifying read method with count=0");
        }

        com.Close();

        return retValue;
    }


    public bool ASCIIEncoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count, new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_016!!! Verifying read method with count=buffer.length and ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count, new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_017!!! Verifying read method with count=buffer.length and UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count, new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_018!!! Verifying read method with count=buffer.length and UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count, new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_019!!! Verifying read method with count=buffer.length and UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool ASCIIEncoding_1Char()
    {
        if (!VerifyRead(new char[1], 0, 1, new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_016!!! Verifying read method with count=buffer.length and ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding_1Char()
    {
        if (!VerifyRead(new char[1], 0, 1, new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_017!!! Verifying read method with count=buffer.length and UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding_1Char()
    {
        if (!VerifyRead(new char[1], 0, 1, new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_018!!! Verifying read method with count=buffer.length and UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding_1Char()
    {
        if (!VerifyRead(new char[1], 0, 1, new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_019!!! Verifying read method with count=buffer.length and UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool LargeInputBuffer()
    {
        int bufferLength = largeNumRndCharsToRead;
        int offset = 0;
        int count = bufferLength;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new char[bufferLength], offset, count, largeNumRndCharsToRead))
        {
            Console.WriteLine("Err_020!!! Verifying read method with large input buffer");
            return false;
        }

        return true;
    }


    public bool BytesFollowedByCharsASCII()
    {
        if (!VerifyBytesFollowedByChars(new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_021!!! Verifying read method does not alter stream of bytes after chars have been read with ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool BytesFollowedByCharsUTF7()
    {
        if (!VerifyBytesFollowedByChars(new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_022!!! Verifying read method does not alter stream of bytes after chars have been read with UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool BytesFollowedByCharsUTF8()
    {
        if (!VerifyBytesFollowedByChars(new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_023!!! Verifying read method does not alter stream of bytes after chars have been read with UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool BytesFollowedByCharsUTF32()
    {
        if (!VerifyBytesFollowedByChars(new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_024!!! Verifying read method does not alter stream of bytes after chars have been read with UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedData()
    {
        int bufferLength = 32 + 8;
        int offset = 3;
        int count = 32;

        if (!VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_2507ajlsp!!! Verifying read method with reading all of the buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedData()
    {
        int bufferLength = 8;
        int offset = 3;
        int count = 3;

        if (!VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_1659akl!!! Verifying read method with reading the buffered data in several calls");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedAndNonBufferedData()
    {
        int bufferLength = 64 + 8;
        int offset = 3;
        int count = 64;

        if (!VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered))
        {
            Console.WriteLine("Err_2082aspzh!!! Verifying read method with reading all of the buffered an non buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedAndNonBufferedData()
    {
        int bufferLength = 8;
        int offset = 3;
        int count = 3;

        if (!VerifyRead(new char[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered))
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
        Random rndGen = new Random(-55);
        char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.Surrogates);
        byte[] byteXmitBuffer = new byte[1024];
        char[] expectedChars;
        char[] charRcvBuffer;
        char utf32Char = (char)8169;
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        int numCharsRead;
        bool retValue = true;
        int numBytes;

        Console.WriteLine("Verifying that Read(char[], int, int) will read everything from internal buffer and drivers buffer");

        for (int i = 0; i < byteXmitBuffer.Length; i++)
        {
            byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
        }

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

        numBytes = System.Text.Encoding.UTF32.GetByteCount(charXmitBuffer);

        byte[] byteBuffer = System.Text.Encoding.UTF32.GetBytes(charXmitBuffer);

        expectedChars = new char[1 + System.Text.Encoding.UTF32.GetCharCount(byteBuffer)];
        expectedChars[0] = utf32Char;

        System.Text.Encoding.UTF32.GetChars(byteBuffer, 0, byteBuffer.Length, expectedChars, 1);
        charRcvBuffer = new char[(int)(expectedChars.Length * 1.5)];

        while (com1.BytesToRead < 4 + numBytes)
        {
            System.Threading.Thread.Sleep(50);
        }

        if (expectedChars.Length != (numCharsRead = com1.Read(charRcvBuffer, 0, charRcvBuffer.Length)))
        {
            Console.WriteLine("Err_6481sfadw Expected read to read {0} chars actually read {1}", expectedChars.Length, numCharsRead);
            retValue = false;
        }

        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (expectedChars[i] != charRcvBuffer[i])
            {
                Console.WriteLine("Err_70782apzh Expected to read {0} actually read {1}", (int)expectedChars[i], (int)charRcvBuffer[i]);
                retValue = false;
                break;
            }
        }

        if (0 != com1.BytesToRead)
        {
            Console.WriteLine("Err_78028asdf ExpectedByteToRead={0} actual={1}", 0, com1.BytesToRead);
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_1389 Verifying that Read(char[], int, int) will read everything from internal buffer and drivers buffer failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_DataReceivedBeforeTimeout()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
        char[] charRcvBuffer = new char[charXmitBuffer.Length];
        ASyncRead asyncRead = new ASyncRead(com1, charRcvBuffer, 0, charRcvBuffer.Length);
        System.Threading.Thread asyncReadThread = new System.Threading.Thread(new System.Threading.ThreadStart(asyncRead.Read));
        bool retValue = true;

        Console.WriteLine("Verifying that Read(char[], int, int) will read characters that have been received after the call to Read was made");

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

        asyncRead.ReadCompletedEvent.WaitOne();

        if (null != asyncRead.Exception)
        {
            retValue = false;
            Console.WriteLine("Err_04448ajhied Unexpected exception thrown from async read:\n{0}", asyncRead.Exception);
        }
        else if (asyncRead.Result < 1)
        {
            retValue = false;
            Console.WriteLine("Err_0158ahei Expected Read to read at least one character {0}", asyncRead.Result);
        }
        else
        {
            System.Threading.Thread.Sleep(1000); //We need to wait for all of the bytes to be received
            int readResult = com1.Read(charRcvBuffer, asyncRead.Result, charRcvBuffer.Length - asyncRead.Result);

            if (asyncRead.Result + readResult != charXmitBuffer.Length)
            {
                retValue = false;
                Console.WriteLine("Err_051884ajoedo Expected Read to read {0} cahracters actually read {1}",
                    charXmitBuffer.Length - asyncRead.Result, readResult);
            }
            else
            {
                for (int i = 0; i < charXmitBuffer.Length; ++i)
                {
                    if (charRcvBuffer[i] != charXmitBuffer[i])
                    {
                        retValue = false;
                        Console.WriteLine("Err_05188ahed Characters differ at {0} expected:{1}({1:X}) actual:{2}({2:X}) asyncRead.Result={3}",
                            i, charXmitBuffer[i], charRcvBuffer[i], asyncRead.Result);
                    }
                }
            }
        }

        if (!retValue)
            Console.WriteLine("Err_018068ajkid Verifying that Read(char[], int, int) will read characters that have been received after the call to Read was made failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_ResizeBuffer()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(1023, TCSupport.CharacterOptions.ASCII);
        char[] charRcvBuffer;
        char[] expectedChars;
        int readResult;
        bool retValue = true;

        Console.WriteLine("Verifying that Read(char[], int, int) will compact data in the buffer buffer");

        com1.Encoding = System.Text.Encoding.ASCII;
        com2.Encoding = System.Text.Encoding.ASCII;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();


        //[] Fill the buffer up then read in all but one of the chars
        expectedChars = new char[charXmitBuffer.Length - 1];
        charRcvBuffer = new char[charXmitBuffer.Length - 1];
        Array.Copy(charXmitBuffer, 0, expectedChars, 0, charXmitBuffer.Length - 1);

        com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
        TCSupport.WaitForPredicate(delegate () { return com1.BytesToRead == charXmitBuffer.Length; }, 2000,
            "Err_29829haie Expected to received {0} bytes actual={1}", charXmitBuffer.Length, com1.BytesToRead);

        if (charXmitBuffer.Length - 1 != (readResult = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length - 1)))
        {
            retValue = false;
            Console.WriteLine("Err_55084aheid Expected to read {0} chars actual {1}", charXmitBuffer.Length - 1, readResult);
        }
        else if (!TCSupport.VerifyArray<char>(expectedChars, charRcvBuffer))
        {
            retValue = false;
            Console.WriteLine("Err_505818ahkijed Verifying characters read in FAILED");
        }

        //[] Write 16 more cahrs and read in 16 chars
        expectedChars = new char[16];
        charRcvBuffer = new char[16];
        expectedChars[0] = charXmitBuffer[charXmitBuffer.Length - 1];
        Array.Copy(charXmitBuffer, 0, expectedChars, 1, 15);

        com2.Write(charXmitBuffer, 0, 16);
        TCSupport.WaitForPredicate(delegate () { return com1.BytesToRead == 17; }, 2000,
            "Err_0516848aied Expected to received {0} bytes actual={1}", 17, com1.BytesToRead);

        if (16 != (readResult = com1.Read(charRcvBuffer, 0, 16)))
        {
            retValue = false;
            Console.WriteLine("Err_650848ahide Expected to read {0} chars actual {1}", 16, readResult);
        }
        else if (!TCSupport.VerifyArray<char>(expectedChars, charRcvBuffer))
        {
            retValue = false;
            Console.WriteLine("Err_015615ajied Verifying characters read in FAILED");
        }

        //[] Write more chars and read in all of the chars
        expectedChars = new char[charXmitBuffer.Length + 1];
        charRcvBuffer = new char[charXmitBuffer.Length + 1];
        expectedChars[0] = charXmitBuffer[15];
        Array.Copy(charXmitBuffer, 0, expectedChars, 1, charXmitBuffer.Length);

        com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
        TCSupport.WaitForPredicate(delegate () { return com1.BytesToRead == charXmitBuffer.Length + 1; }, 2000,
            "Err_41515684 Expected to received {0} bytes actual={1}", charXmitBuffer.Length + 2, com1.BytesToRead);

        if (charXmitBuffer.Length + 1 != (readResult = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length + 1)))
        {
            retValue = false;
            Console.WriteLine("Err_460574ajied Expected to read {0} chars actual {1}", charXmitBuffer.Length + 1, readResult);
        }
        else if (!TCSupport.VerifyArray<char>(expectedChars, charRcvBuffer))
        {
            retValue = false;
            Console.WriteLine("Err_20484ahjeid Verifying characters read in FAILED");
        }

        if (!retValue)
            Console.WriteLine("Err_08846ajieaVerifying that Read(char[], int, int) will compact data in the buffer buffer failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_Timeout()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
        byte[] byteXmitBuffer = new System.Text.UTF32Encoding().GetBytes(charXmitBuffer);
        char[] charRcvBuffer = new char[charXmitBuffer.Length];
        bool retValue = true;
        int result;

        Console.WriteLine("Verifying that Read(char[], int, int) works appropriately after TimeoutException has been thrown");

        com1.Encoding = new System.Text.UTF32Encoding();
        com2.Encoding = new System.Text.UTF32Encoding();
        com1.ReadTimeout = 500; // 20 seconds

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        //Write the first 3 bytes of a character
        com2.Write(byteXmitBuffer, 0, 3);

        try
        {
            com1.Read(charXmitBuffer, 0, charXmitBuffer.Length);
            Console.WriteLine("Err_29299aize Expected ReadTo to throw TimeoutException");
            retValue = false;
        }
        catch (TimeoutException) { }//Expected

        if (3 != com1.BytesToRead)
        {
            Console.WriteLine("Err_689855ozpbea Expected BytesToRead: {0} actual: {1}", 3, com1.BytesToRead);
            retValue = false;
        }

        com2.Write(byteXmitBuffer, 3, byteXmitBuffer.Length - 3);

        //		retValue &= TCSupport.WaitForPredicate(delegate() {return com1.BytesToRead == byteXmitBuffer.Length; }, 
        //			5000, "Err_91818aheid Expected BytesToRead={0} actual={1}", byteXmitBuffer.Length, com1.BytesToRead);

        retValue &= TCSupport.WaitForExpected(delegate () { return com1.BytesToRead; }, byteXmitBuffer.Length,
            5000, "Err_91818aheid BytesToRead");
        result = com1.Read(charRcvBuffer, 0, charRcvBuffer.Length);

        if (result != charXmitBuffer.Length)
        {
            Console.WriteLine("Err_56548ajied Expected Read to return {0} chars actual {1}", charXmitBuffer.Length, result);
            retValue = false;
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

        if (!VerifyBytesReadOnCom1FromCom2(com1, com2, byteXmitBuffer, charXmitBuffer, charRcvBuffer, 0, charRcvBuffer.Length))
        {
            Console.WriteLine("Err_05188ajied Verify ReadLine after read failed");
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_9858wiapxzg Verifying that Read(char[], int, int) works appropriately after TimeoutException has been thrown failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_Partial()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char utf32Char = (char)0x254b; //Box drawing char
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        char[] charRcvBuffer = new char[3];
        bool retValue = true;
        int result;

        Console.WriteLine("Verifying that Read(char[], int, int) works when reading partial characters");

        com1.Encoding = new System.Text.UTF32Encoding();
        com2.Encoding = new System.Text.UTF32Encoding();
        com1.ReadTimeout = 500;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        //Write the first 3 bytes of a character
        com2.Write(utf32CharBytes, 0, 4);
        com2.Write(utf32CharBytes, 0, 3);

        retValue &= TCSupport.WaitForExpected(delegate () { return com1.BytesToRead; }, 7,
            5000, "Err_018158ajid BytesToRead");
        result = com1.Read(charRcvBuffer, 0, charRcvBuffer.Length);

        if (result != 1)
        {
            Console.WriteLine("Err_56548ajied Expected Read to return {0} chars actual {1}", 1, result);
            retValue = false;
        }
        else
        {
            com2.Write(utf32CharBytes, 3, 1);

            result = com1.Read(charRcvBuffer, 1, charRcvBuffer.Length - 1);
            if (result != 1)
            {
                Console.WriteLine("Err_56548ajied Expected Read to return {0} chars actual {1}", 1, result);
                retValue = false;
            }

            if (charRcvBuffer[0] != utf32Char)
            {
                Console.WriteLine("Err_015985akeid Expected Char 0 to be {0}({1:X}) chars actual {2}({3:X})",
                    utf32Char, (int)utf32Char, charRcvBuffer[0], (int)charRcvBuffer[0]);
                retValue = false;
            }

            if (charRcvBuffer[1] != utf32Char)
            {
                Console.WriteLine("Err_0158836ajied Expected Char 1 to be {0}({1:X}) chars actual {2}({3:X})",
                    utf32Char, (int)utf32Char, charRcvBuffer[1], (int)charRcvBuffer[1]);
                retValue = false;
            }
        }

        if (!VerifyBytesReadOnCom1FromCom2(com1, com2, utf32CharBytes, new char[] { utf32Char }, charRcvBuffer, 0, charRcvBuffer.Length))
        {
            Console.WriteLine("Err_05188ajied Verify ReadLine after read failed");
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_9858wiapxzg Verifying that Read(char[], int, int) works when reading partial characters failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_SurrogateBoundry()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = new char[32];
        char[] charRcvBuffer;
        bool retValue = true;
        int result;

        Console.WriteLine("Verifying that Read(char[], int, int) works with reading surrogate characters");

        TCSupport.GetRandomChars(charXmitBuffer, 0, charXmitBuffer.Length - 2, TCSupport.CharacterOptions.Surrogates);
        charXmitBuffer[charXmitBuffer.Length - 2] = TCSupport.GenerateRandomHighSurrogate();
        charXmitBuffer[charXmitBuffer.Length - 1] = TCSupport.GenerateRandomLowSurrogate();

        com1.Encoding = System.Text.Encoding.Unicode;
        com2.Encoding = System.Text.Encoding.Unicode;
        com1.ReadTimeout = 500;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        //[] First lets try with buffer size that is larger then what we are asking for
        com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
        retValue &= TCSupport.WaitForExpected(delegate () { return com1.BytesToRead; }, charXmitBuffer.Length * 2,
            5000, "Err_018158ajid BytesToRead");

        charRcvBuffer = new char[charXmitBuffer.Length];

        result = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length - 1);

        if (result == charXmitBuffer.Length - 2)
        {
            char[] actualChars = new char[charXmitBuffer.Length];

            Array.Copy(charRcvBuffer, 0, actualChars, 0, result);
            result = com1.Read(actualChars, actualChars.Length - 2, 2);

            if (result == 2)
            {
                for (int i = 0; i < charXmitBuffer.Length; ++i)
                {
                    if (actualChars[i] != charXmitBuffer[i])
                    {
                        Console.WriteLine("Err_0555jegv Expected to read {0:X} actually read {1:X} at {2}", (int)charXmitBuffer[i], (int)actualChars[i], i);
                        retValue = false;
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Err_0548ahuide Expected to read {0} chars actually read {1}", 2, result);
                retValue = false;
            }
        }
        else
        {
            Console.WriteLine("Err_29298aheid Expected to read {0} chars actually read {1}", charXmitBuffer.Length - 2, result);
            retValue = false;
        }

        //[] Next lets try with buffer size that is the same size as what we are asking for
        com2.Write(charXmitBuffer, 0, charXmitBuffer.Length);
        retValue &= TCSupport.WaitForExpected(delegate () { return com1.BytesToRead; }, charXmitBuffer.Length * 2,
            5000, "Err_018158ajid BytesToRead");

        charRcvBuffer = new char[charXmitBuffer.Length - 1];

        result = com1.Read(charRcvBuffer, 0, charXmitBuffer.Length - 1);

        if (result == charXmitBuffer.Length - 2)
        {
            char[] actualChars = new char[charXmitBuffer.Length];

            Array.Copy(charRcvBuffer, 0, actualChars, 0, result);
            result = com1.Read(actualChars, actualChars.Length - 2, 2);

            if (result == 2)
            {
                for (int i = 0; i < charXmitBuffer.Length; ++i)
                {
                    if (actualChars[i] != charXmitBuffer[i])
                    {
                        Console.WriteLine("Err_0486aied Expected to read {0:X} actually read {1:X} at {2}", (int)charXmitBuffer[i], (int)actualChars[i], i);
                        retValue = false;
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Err_0548ahuide Expected to read {0} chars actually read {1}", 2, result);
                retValue = false;
            }
        }
        else
        {
            Console.WriteLine("Err_29298aheid Expected to read {0} chars actually read {1}", charXmitBuffer.Length - 2, result);
            retValue = false;
        }

        return retValue;
    }

    #endregion

    #region Verification for Test Cases
    public bool VerifyReadException(char[] buffer, int offset, int count, Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        int bufferLength = null == buffer ? 0 : buffer.Length;

        Console.WriteLine("Verifying read method throws {0} buffer.Lenght={1}, offset={2}, count={3}", expectedException, bufferLength, offset, count);
        com.Open();

        try
        {
            com.Read(buffer, offset, count);
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

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool VerifyRead(char[] buffer, int offset, int count)
    {
        return VerifyRead(buffer, offset, count, new System.Text.ASCIIEncoding(), numRndBytesToRead, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(char[] buffer, int offset, int count, int numberOfBytesToRead)
    {
        return VerifyRead(buffer, offset, count, new System.Text.ASCIIEncoding(), numberOfBytesToRead, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(char[] buffer, int offset, int count, int numberOfBytesToRead, ReadDataFromEnum readDataFrom)
    {
        return VerifyRead(buffer, offset, count, new System.Text.ASCIIEncoding(), numberOfBytesToRead, readDataFrom);
    }


    public bool VerifyRead(char[] buffer, int offset, int count, System.Text.Encoding encoding)
    {
        return VerifyRead(buffer, offset, count, encoding, numRndBytesToRead, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(char[] buffer, int offset, int count, System.Text.Encoding encoding, int numberOfBytesToRead, ReadDataFromEnum readDataFrom)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
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
            char randChar = (char)rndGen.Next(0, System.UInt16.MaxValue);

            buffer[i] = randChar;
        }

        Console.WriteLine("Verifying read method buffer.Length={0}, offset={1}, count={2}, endocing={3} with {4} random chars", buffer.Length, offset, count, encoding.EncodingName, bytesToWrite.Length);
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
                return VerifyReadNonBuffered(com1, com2, bytesToWrite, buffer, offset, count);

            case ReadDataFromEnum.Buffered:
                return VerifyReadBuffered(com1, com2, bytesToWrite, buffer, offset, count);

            case ReadDataFromEnum.BufferedAndNonBuffered:
                return VerifyReadBufferedAndNonBuffered(com1, com2, bytesToWrite, buffer, offset, count);
        }
        return false;
    }


    private bool VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] rcvBuffer, int offset, int count)
    {
        char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars, rcvBuffer, offset, count);
    }


    private bool VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] rcvBuffer, int offset, int count)
    {
        char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

        BufferData(com1, com2, bytesToWrite);

        return PerformReadOnCom1FromCom2(com1, com2, expectedChars, rcvBuffer, offset, count);
    }


    private bool VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] rcvBuffer, int offset, int count)
    {
        char[] expectedChars = new char[com1.Encoding.GetCharCount(bytesToWrite, 0, bytesToWrite.Length) * 2];
        char[] encodedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

        Array.Copy(encodedChars, 0, expectedChars, 0, bytesToWrite.Length);
        Array.Copy(encodedChars, 0, expectedChars, encodedChars.Length, encodedChars.Length);

        BufferData(com1, com2, bytesToWrite);

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars, rcvBuffer, offset, count);
    }


    private bool BufferData(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        bool retValue = true;
        char c = TCSupport.GenerateRandomCharNonSurrogate();
        byte[] bytesForSingleChar = com1.Encoding.GetBytes(new char[] { c }, 0, 1);

        com2.Write(bytesForSingleChar, 0, bytesForSingleChar.Length); // Write one byte at the begining because we are going to read this to buffer the rest of the data
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


    private bool VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars, char[] rcvBuffer, int offset, int count)
    {
        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 500;

        //This is pretty lame but we will have to live with if for now becuase we can not 
        //gaurentee the number of bytes Write will add
        System.Threading.Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

        return PerformReadOnCom1FromCom2(com1, com2, expectedChars, rcvBuffer, offset, count);
    }


    private bool PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, char[] expectedChars, char[] rcvBuffer, int offset, int count)
    {
        bool retValue = true;
        char[] buffer = new char[expectedChars.Length];
        int bytesRead, totalBytesRead, charsRead, totalCharsRead;
        char[] oldRcvBuffer = (char[])rcvBuffer.Clone();
        int bytesToRead;
        int numBytesWritten = com1.Encoding.GetByteCount(expectedChars);
        bool isUTF7Encoding = com1.Encoding.EncodingName == System.Text.Encoding.UTF7.EncodingName;

        totalBytesRead = 0;
        totalCharsRead = 0;

        bytesToRead = com1.BytesToRead;

        while (true)
        {
            try
            {
                charsRead = com1.Read(rcvBuffer, offset, count);
            }
            catch (TimeoutException)
            {
                break;
            }

            //While their are more characters to be read
            if (isUTF7Encoding)
                totalBytesRead = GetUTF7EncodingBytes(expectedChars, 0, totalCharsRead + charsRead);
            else
            {
                bytesRead = com1.Encoding.GetByteCount(rcvBuffer, offset, charsRead);
                totalBytesRead += bytesRead;
            }

            if (expectedChars.Length < totalCharsRead + charsRead)
            {
                //If we have read in more characters then we expect
                Console.WriteLine("ERROR!!!: We have received more characters then were sent");


                //1<DEBUG>
                Console.WriteLine("count={0}, charsRead={1} expectedChars.Length={2}, totalCharsRead={3}", count, charsRead, expectedChars.Length, totalCharsRead);

                Console.WriteLine("rcvBuffer");
                TCSupport.PrintChars(rcvBuffer);

                Console.WriteLine("\nexpectedChars");
                TCSupport.PrintChars(expectedChars);
                //1</DEBUG>

                retValue = false;
                break;
            }

            if (count != charsRead &&
                (count < charsRead ||
                ((expectedChars.Length - totalCharsRead) != charsRead &&
                     !TCSupport.IsSurrogate(expectedChars[totalCharsRead + charsRead]))))
            {
                //If we have not read all of the characters that we should have
                Console.WriteLine("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");

                //1<DEBUG>
                Console.WriteLine("count={0}, charsRead={1} expectedChars.Length={2}, totalCharsRead={3}", count, charsRead, expectedChars.Length, totalCharsRead);

                Console.WriteLine("rcvBuffer");
                TCSupport.PrintChars(rcvBuffer);

                Console.WriteLine("\nexpectedChars");
                TCSupport.PrintChars(expectedChars);
                //1</DEBUG>

                retValue = false;
            }

            if (!VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, charsRead))
                retValue = false;

            System.Array.Copy(rcvBuffer, offset, buffer, totalCharsRead, charsRead);

            totalCharsRead += charsRead;

            if (numBytesWritten - totalBytesRead != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", numBytesWritten - totalBytesRead, com1.BytesToRead);
                retValue = false;
            }

            oldRcvBuffer = (char[])rcvBuffer.Clone();
            bytesToRead = com1.BytesToRead;
        }

        if (!VerifyBuffer(rcvBuffer, oldRcvBuffer, 0, rcvBuffer.Length))
            retValue = false;

        //Compare the chars that were written with the ones we expected to read
        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (expectedChars[i] != buffer[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", (int)expectedChars[i], (int)buffer[i], i);
                retValue = false;
            }
        }

        if (!isUTF7Encoding && 0 != com1.BytesToRead)
        {
            System.Console.WriteLine("ERROR!!!: Expected BytesToRead=0  actual BytesToRead={0}", com1.BytesToRead);
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool VerifyBytesFollowedByChars(System.Text.Encoding encoding)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] xmitCharBuffer = TCSupport.GetRandomChars(numRndBytesToRead, TCSupport.CharacterOptions.Surrogates);
        char[] rcvCharBuffer = new char[xmitCharBuffer.Length];
        byte[] xmitByteBuffer = new byte[numRndBytesToRead];
        byte[] rcvByteBuffer = new byte[xmitByteBuffer.Length];
        Random rndGen = new Random(-55);
        bool retValue = true;
        int numRead;

        Console.WriteLine("Verifying read method does not alter stream of bytes after chars have been read with {0}", encoding.GetType());

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

        System.Threading.Thread.Sleep((int)(((xmitByteBuffer.Length + com2.Encoding.GetByteCount(xmitCharBuffer) * 10.0) / com1.BaudRate) * 1000) + 500);
        System.Threading.Thread.Sleep(500);

        if (xmitCharBuffer.Length != (numRead = com1.Read(rcvCharBuffer, 0, rcvCharBuffer.Length)))
        {
            System.Console.WriteLine("ERROR!!!: Expected to read {0} chars actually read {1}", xmitCharBuffer.Length, numRead);
            retValue = false;
        }

        if (encoding.EncodingName == System.Text.Encoding.UTF7.EncodingName)
        {//If UTF7Encoding is being used we we might leave a - in the stream
            if (com1.BytesToRead == xmitByteBuffer.Length + 1)
            {
                int byteRead;

                if ('-' != (char)(byteRead = com1.ReadByte()))
                {
                    Console.WriteLine("Err_29282naie Expected '-' to be left in the stream with UTF7Encoding and read {0}", byteRead);
                    retValue = false;
                }
            }
        }

        if (xmitByteBuffer.Length != (numRead = com1.Read(rcvByteBuffer, 0, rcvByteBuffer.Length)))
        {
            System.Console.WriteLine("ERROR!!!: Expected to read {0} bytes actually read {1}", xmitByteBuffer.Length, numRead);
            retValue = false;
        }

        for (int i = 0; i < xmitByteBuffer.Length; i++)
        {
            if (xmitByteBuffer[i] != rcvByteBuffer[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", (int)xmitByteBuffer[i], (int)rcvByteBuffer[i], i);
                retValue = false;
            }
        }

        if (0 != com1.BytesToRead)
        {
            System.Console.WriteLine("ERROR!!!: Expected BytesToRead=0  actual BytesToRead={0}", com1.BytesToRead);
            retValue = false;
        }

        /*DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG DEBUG
        if(!retValue) {
            for(int i=0; i<xmitCharBuffer.Length; ++i) {
                Console.WriteLine("(char){0}, ", (int)xmitCharBuffer[i]);
            }

            for(int i=0; i<xmitCharBuffer.Length; ++i) {
                Console.WriteLine("{0}, ", (int)xmitByteBuffer[i]);
            }			
        }*/

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private bool VerifyBuffer(char[] actualBuffer, char[] expectedBuffer, int offset, int count)
    {
        bool retValue = true;

        //Verify all character before the offset
        for (int i = 0; i < offset; i++)
        {
            if (actualBuffer[i] != expectedBuffer[i])
            {
                Console.WriteLine("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                retValue = false;
            }
        }

        //Verify all character after the offset + count
        for (int i = offset + count; i < actualBuffer.Length; i++)
        {
            if (actualBuffer[i] != expectedBuffer[i])
            {
                Console.WriteLine("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                retValue = false;
            }
        }

        return retValue;
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
        private char[] _buffer;
        private int _offset;
        private int _count;
        private int _result;

        private System.Threading.AutoResetEvent _readCompletedEvent;
        private System.Threading.AutoResetEvent _readStartedEvent;

        private Exception _exception;

        public ASyncRead(SerialPort com, char[] buffer, int offset, int count)
        {
            _com = com;
            _buffer = buffer;
            _offset = offset;
            _count = count;

            _result = -1;

            _readCompletedEvent = new System.Threading.AutoResetEvent(false);
            _readStartedEvent = new System.Threading.AutoResetEvent(false);

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

        public int Result
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

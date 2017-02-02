// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class ReadExisting
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadExisting()";
    public static readonly String s_strTFName = "ReadReadExisting.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The number of random bytes to receive for read method testing
    public static readonly int numRndBytesToRead = 8;

    //The number of random bytes to receive for large input buffer testing
    public static readonly int largeNumRndBytesToRead = 2048;

    //When we test Read and do not care about actually reading anything we must still
    //create an byte array to pass into the method the following is the size of the 
    //byte array used in this situation
    public static readonly int defaultCharArraySize = 1;
    public static readonly int defaultCharOffset = 0;
    public static readonly int defaultCharCount = 1;

    //The maximum buffer size when a exception is not expected
    public static readonly int maxBufferSize = 8;

    public enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadExisting objTest = new ReadExisting();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(GreedyRead), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeInputBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ASCIIEncoding()
    {
        if (!VerifyRead(new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_016!!! Verifying read method with count=buffer.length and ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        if (!VerifyRead(new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_017!!! Verifying read method with count=buffer.length and UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        if (!VerifyRead(new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_018!!! Verifying read method with count=buffer.length and UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        if (!VerifyRead(new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_019!!! Verifying read method with count=buffer.length and UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedData()
    {
        int numberOfBytesToRead = 32;

        if (!VerifyRead(System.Text.Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_2507ajlsp!!! Verifying read method with reading all of the buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedData()
    {
        int numberOfBytesToRead = 32;

        if (!VerifyRead(System.Text.Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_1659akl!!! Verifying read method with reading the buffered data in several calls");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedAndNonBufferedData()
    {
        int numberOfBytesToRead = 64;

        if (!VerifyRead(System.Text.Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.BufferedAndNonBuffered))
        {
            Console.WriteLine("Err_2082aspzh!!! Verifying read method with reading all of the buffered an non buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedAndNonBufferedData()
    {
        int numberOfBytesToRead = 3;

        if (!VerifyRead(System.Text.Encoding.ASCII, numberOfBytesToRead, ReadDataFromEnum.BufferedAndNonBuffered))
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
        char[] charXmitBuffer = TCSupport.GetRandomChars(128, true);
        byte[] byteXmitBuffer = new byte[1024];
        char[] expectedChars;
        string rcvString;
        char[] actualChars;
        char utf32Char = TCSupport.GenerateRandomCharNonSurrogate();
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        int numBytes;
        bool retValue = true;

        Console.WriteLine("Verifying that ReadExisting() will read everything from internal buffer and drivers buffer");

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

        while (com1.BytesToRead < 4 + numBytes)
        {
            System.Threading.Thread.Sleep(50);
        }

        if (null == (rcvString = com1.ReadExisting()))
        {
            Console.WriteLine("Err_6481sfadw ReadExisting returned null");
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
            Console.WriteLine("Err_1389 Verifying that ReadExisting() will read everything from internal buffer and drivers buffer failed");

        com1.Close();
        com2.Close();
        return retValue;
    }


    public bool LargeInputBuffer()
    {
        if (!VerifyRead(largeNumRndBytesToRead))
        {
            Console.WriteLine("Err_020!!! Verifying read method with large input buffer");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyRead()
    {
        return VerifyRead(new System.Text.ASCIIEncoding(), numRndBytesToRead);
    }


    public bool VerifyRead(int numberOfBytesToRead)
    {
        return VerifyRead(new System.Text.ASCIIEncoding(), numberOfBytesToRead);
    }


    public bool VerifyRead(System.Text.Encoding encoding)
    {
        return VerifyRead(encoding, numRndBytesToRead);
    }


    public bool VerifyRead(System.Text.Encoding encoding, int numberOfBytesToRead)
    {
        return VerifyRead(encoding, numberOfBytesToRead, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(System.Text.Encoding encoding, int numberOfBytesToRead, ReadDataFromEnum readDataFrom)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        char[] charsToWrite = new char[numberOfBytesToRead];
        byte[] bytesToWrite = new byte[numberOfBytesToRead];

        //Genrate random chars to send
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            char randChar = (char)rndGen.Next(0, System.UInt16.MaxValue);

            charsToWrite[i] = randChar;
        }

        Console.WriteLine("Verifying read method endocing={0} with {1} random chars", encoding.EncodingName, bytesToWrite.Length);

        com1.ReadTimeout = 500;
        com1.Encoding = encoding;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        bytesToWrite = com1.Encoding.GetBytes(charsToWrite, 0, charsToWrite.Length);

        switch (readDataFrom)
        {
            case ReadDataFromEnum.NonBuffered:
                return VerifyReadNonBuffered(com1, com2, bytesToWrite);

            case ReadDataFromEnum.Buffered:
                return VerifyReadBuffered(com1, com2, bytesToWrite);

            case ReadDataFromEnum.BufferedAndNonBuffered:
                return VerifyReadBufferedAndNonBuffered(com1, com2, bytesToWrite);
        }
        return false;
    }


    private bool VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars);
    }


    private bool VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        char[] expectedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

        BufferData(com1, com2, bytesToWrite);

        return PerformReadOnCom1FromCom2(com1, com2, expectedChars);
    }


    private bool VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        char[] expectedChars = new char[com1.Encoding.GetCharCount(bytesToWrite, 0, bytesToWrite.Length) * 2];
        char[] encodedChars = com1.Encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);

        Array.Copy(encodedChars, 0, expectedChars, 0, bytesToWrite.Length);
        Array.Copy(encodedChars, 0, expectedChars, encodedChars.Length, encodedChars.Length);

        BufferData(com1, com2, bytesToWrite);

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedChars);
    }


    private bool BufferData(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        bool retValue = true;

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


    private bool VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars)
    {
        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 500;
        System.Threading.Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);
        return PerformReadOnCom1FromCom2(com1, com2, expectedChars);
    }


    private bool PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, char[] expectedChars)
    {
        bool retValue = true;
        string rcvString;
        char[] buffer = new char[expectedChars.Length];
        char[] rcvBuffer;
        int numBytesWritten = com1.Encoding.GetByteCount(expectedChars);
        bool isUTF7Encoding = com1.Encoding.EncodingName == System.Text.Encoding.UTF7.EncodingName;

        rcvString = com1.ReadExisting();
        rcvBuffer = rcvString.ToCharArray();

        if (expectedChars.Length != rcvBuffer.Length)
        {
            //If we have read in more characters then we expect
            Console.WriteLine("ERROR!!!: We have not read all of the characters that were sent");
            retValue = false;
            //break;
        }

        //Compare the chars that were written with the ones we expected to read
        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (expectedChars[i] != rcvBuffer[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1}", (int)expectedChars[i], (int)rcvBuffer[i]);
                retValue = false;
            }
        }

        if (0 != com1.BytesToRead && (!isUTF7Encoding || 1 != com1.BytesToRead))
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
    #endregion
}

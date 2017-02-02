// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class ReadChar
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadChar()";
    public static readonly String s_strTFName = "ReadChar.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Set bounds fore random timeout values.
    //If the min is to low read will not timeout accurately and the testcase will fail
    public static int minRandomTimeout = 100;

    //If the max is to large then the testcase will take forever to run
    public static int maxRandomTimeout = 2000;

    //The number of random bytes to receive for large input buffer testing
    public static readonly int largeNumRndBytesToRead = 4096;

    //The number of random characters to receive
    public static int numRndChar = 8;

    public enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadChar objTest = new ReadChar();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Zero_ResizeBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_NonZero_ResizeBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(GreedyRead), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeInputBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Zero_Bytes), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_DataReceivedBeforeTimeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_Timeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_Surrogate), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ASCIIEncoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with ASCIIEncoding");
        if (!VerifyRead(new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_001!!! Verifying read with bytes encoded with ASCIIEncoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with UTF7Encoding");
        if (!VerifyRead(new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_002!!! Verifying read with bytes encoded with UTF7Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with UTF8Encoding");
        if (!VerifyRead(new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_003!!! Verifying read with bytes encoded with UTF8Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with UTF32Encoding");
        if (!VerifyRead(new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_004!!! Verifying read with bytes encoded with UTF32Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_2507ajlsp!!! Verifying read method with reading all of the buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_1659akl!!! Verifying read method with reading the buffered data in several calls");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedAndNonBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.BufferedAndNonBuffered))
        {
            Console.WriteLine("Err_2082aspzh!!! Verifying read method with reading all of the buffered an non buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedAndNonBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.BufferedAndNonBuffered))
        {
            Console.WriteLine("Err_5687nhnhl!!! Verifying read method with reading the buffered and non buffereddata in several calls");
            return false;
        }

        return true;
    }

    public bool ReadTimeout_Zero_ResizeBuffer()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        int bufferSize = numRndChar;
        byte[] byteXmitBuffer = new byte[1024];
        char utf32Char = 'A';
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        char[] charXmitBuffer = TCSupport.GetRandomChars(16, false);
        char[] expectedChars = new char[charXmitBuffer.Length + 1];
        bool retValue = true;

        Console.WriteLine("Verifying Read method with zero timeout that resizes SerialPort's buffer");

        expectedChars[0] = utf32Char;
        Array.Copy(charXmitBuffer, 0, expectedChars, 1, charXmitBuffer.Length);

        //Put the first byte of the utf32 encoder char in the last byte of this buffer
        //when we read this later the buffer will have to be resized
        byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];
        com1.ReadTimeout = 0;

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
            Console.WriteLine("Err_8728asfdi ExpectedByteToRead={0} actual={1}", 1, com1.BytesToRead);
            retValue = false;
        }

        com1.Encoding = System.Text.Encoding.UTF32;
        byteXmitBuffer = System.Text.Encoding.UTF32.GetBytes(charXmitBuffer);

        try
        {
            Char c = (char)com1.ReadChar();
            Console.WriteLine("Err_1027asdh ReadChar() did not throw timeout exception when it only contained part of a character in it's buffer returned:{0}({1})", c, (int)c);
            retValue = false;
        }
        catch (TimeoutException) { } //Expected

        if (1 != com1.BytesToRead)
        {
            Console.WriteLine("Err_54232ashz ExpectedByteToRead={0} actual={1}", 1, com1.BytesToRead);
            retValue = false;
        }

        if (retValue)
        {
            com2.Write(utf32CharBytes, 1, 3);
            com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

            while (com1.BytesToRead < 4 + byteXmitBuffer.Length)
            {
                System.Threading.Thread.Sleep(50);
            }

            retValue &= PerformReadOnCom1FromCom2(com1, com2, expectedChars);
        }

        if (!retValue)
            Console.WriteLine("Err_2348asdz Verifying Read method with zero timeout that resizes SerialPort's buffer");

        return retValue;
    }

    public bool ReadTimeout_NonZero_ResizeBuffer()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        int bufferSize = numRndChar;
        byte[] byteXmitBuffer = new byte[1024];
        char utf32Char = 'A';
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        char[] charXmitBuffer = TCSupport.GetRandomChars(16, false);
        char[] expectedChars = new char[charXmitBuffer.Length + 1];
        bool retValue = true;

        Console.WriteLine("Verifying Read method with non zero timeout that resizes SerialPort's buffer");

        expectedChars[0] = utf32Char;
        Array.Copy(charXmitBuffer, 0, expectedChars, 1, charXmitBuffer.Length);

        //Put the first byte of the utf32 encoder char in the last byte of this buffer
        //when we read this later the buffer will have to be resized
        byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];

        com1.ReadTimeout = 500;
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
            Console.WriteLine("Err_6797auji ExpectedByteToRead={0} actual={1}", 1, com1.BytesToRead);
            retValue = false;
        }

        com1.Encoding = System.Text.Encoding.UTF32;
        byteXmitBuffer = System.Text.Encoding.UTF32.GetBytes(charXmitBuffer);

        try
        {
            com1.ReadChar();
            Console.WriteLine("Err_1027asdh ReadChar() did not timeout when it only contained part of a character in it's buffer");
            retValue = false;
        }
        catch (TimeoutException) { }

        if (1 != com1.BytesToRead)
        {
            Console.WriteLine("Err_6189jhxcn ExpectedByteToRead={0} actual={1}", 1, com1.BytesToRead);
            retValue = false;
        }

        if (retValue)
        {
            com2.Write(utf32CharBytes, 1, 3);
            com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

            while (com1.BytesToRead < 4 + byteXmitBuffer.Length)
            {
                System.Threading.Thread.Sleep(50);
            }

            retValue &= PerformReadOnCom1FromCom2(com1, com2, expectedChars);
        }

        if (!retValue)
            Console.WriteLine("Err_2348asdz Verifying Read method with non zero timeout that resizes SerialPort's buffer");

        return retValue;
    }


    public bool GreedyRead()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        byte[] byteXmitBuffer = new byte[1024];
        char utf32Char = TCSupport.GenerateRandomCharNonSurrogate();
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        int charRead;
        bool retValue = true;

        Console.WriteLine("Verifying that ReadChar() will read everything from internal buffer and drivers buffer");

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
        com2.Write(utf32CharBytes, 1, 3);

        while (com1.BytesToRead < 4)
        {
            System.Threading.Thread.Sleep(50);
        }

        if (utf32Char != (charRead = com1.ReadChar()))
        {
            Console.WriteLine("Err_6481sfadw ReadChar() returned={0} expected={1}", charRead, utf32Char);
            retValue = false;
        }

        if (0 != com1.BytesToRead)
        {
            Console.WriteLine("Err_78028asdf ExpectedByteToRead={0} actual={1}", 0, com1.BytesToRead);
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_1389 Verifying that ReadChar() will read everything from internal buffer and drivers buffer failed");

        com1.Close();
        com2.Close();

        return retValue;
    }


    public bool LargeInputBuffer()
    {
        Console.WriteLine("Verifying read with large input buffer");
        if (!VerifyRead(System.Text.Encoding.ASCII, largeNumRndBytesToRead))
        {
            Console.WriteLine("Err_7207ajpah!!! Verifying read with large input buffeg FAILED");
            return false;
        }

        return true;
    }


    public bool ReadTimeout_Zero_Bytes()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char utf32Char = (char)0x254b; //Box drawing char
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        bool retValue = true;
        int readChar;

        Console.WriteLine("Verifying Read method with zero timeout that resizes SerialPort's buffer");

        com1.Encoding = System.Text.Encoding.UTF32;
        com1.ReadTimeout = 0;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        //[] Try ReadChar with no bytes available
        try
        {
            try
            {
                readChar = com1.ReadChar();
                Console.WriteLine("Err_28292haied Expected ReadChar to throw TimeoutException with no bytes in " +
                    "the buffer {0}({0:X}) returned instead", readChar);
                retValue = false;
            }
            catch (TimeoutException)
            {
                //Expected
            }

            if (0 != com1.BytesToRead)
            {
                Console.WriteLine("Err_155anjied Expected BytesToRead={0} actual={1}", 0, com1.BytesToRead);
                retValue = false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Err_205566ahuied Unexpected exception thrown: {0}", e);
            retValue = false;
        }

        //[] Try ReadChar with 1 byte available
        try
        {
            com2.Write(utf32CharBytes, 0, 1);

            retValue &= TCSupport.WaitForPredicate(delegate () { return com1.BytesToRead == 1; }, 2000,
                "Err_28292aheid Expected BytesToRead to be 1");

            try
            {
                readChar = com1.ReadChar();
                Console.WriteLine("Err_9279ajei Expected ReadChar to throw TimeoutException with some bytes available " +
                    "in the SerialPort {0}({0:X}) returned instead", readChar);
                retValue = false;
            }
            catch (TimeoutException)
            {
                //Expected
            }

            if (1 != com1.BytesToRead)
            {
                Console.WriteLine("Err_1983218aheid Expected BytesToRead={0} actual={1}", 1, com1.BytesToRead);
                retValue = false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Err_021855aueid Unexpected exception thrown: {0}", e);
            retValue = false;
        }

        //[] Try ReadChar with the bytes in the buffer and in available in the SerialPort
        try
        {
            com2.Write(utf32CharBytes, 1, 3);

            retValue &= TCSupport.WaitForPredicate(delegate () { return com1.BytesToRead == 4; }, 2000,
                "Err_415568haikpas Expected BytesToRead to be 4");

            readChar = com1.ReadChar();

            if (readChar != utf32Char)
            {
                retValue = false;
                Console.WriteLine("Err_19173ahid Expected to read {0}({0:X}) actually read {1}({1:X})", utf32Char, readChar);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Err_4929aheidea Unexpected exception thrown: {0}", e);
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        if (!retValue)
            Console.WriteLine("Err_115858ahjeid Verifying Read method with zero timeout");

        return retValue;
    }

    public bool Read_DataReceivedBeforeTimeout()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(512, TCSupport.CharacterOptions.None);
        char[] charRcvBuffer = new char[charXmitBuffer.Length];
        ASyncRead asyncRead = new ASyncRead(com1);
        System.Threading.Thread asyncReadThread = new System.Threading.Thread(new System.Threading.ThreadStart(asyncRead.Read));
        bool retValue = true;

        Console.WriteLine("Verifying that ReadChar will read characters that have been received after the call to Read was made");

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
        else if (asyncRead.Result != charXmitBuffer[0])
        {
            retValue = false;
            Console.WriteLine("Err_0158ahei Expected ReadChar to read {0}({0:X}) actual {1}({1:X})", charXmitBuffer[0], asyncRead.Result);
        }
        else
        {
            System.Threading.Thread.Sleep(1000); //We need to wait for all of the bytes to be received
            charRcvBuffer[0] = (char)asyncRead.Result;
            int readResult = com1.Read(charRcvBuffer, 1, charRcvBuffer.Length - 1);

            if (readResult + 1 != charXmitBuffer.Length)
            {
                retValue = false;
                Console.WriteLine("Err_051884ajoedo Expected Read to read {0} cahracters actually read {1}",
                    charXmitBuffer.Length - 1, readResult);
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
            Console.WriteLine("Err_018068ajkid Verifying that ReadChar() will read characters that have been received after the call to Read was made failed");

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
            com1.ReadChar();
            Console.WriteLine("Err_29299aize Expected ReadChar to throw TimeoutException");
            retValue = false;
        }
        catch (TimeoutException) { }//Expected

        if (3 != com1.BytesToRead)
        {
            Console.WriteLine("Err_689855ozpbea Expected BytesToRead: {0} actual: {1}", 3, com1.BytesToRead);
            retValue = false;
        }

        com2.Write(byteXmitBuffer, 3, byteXmitBuffer.Length - 3);

        retValue &= TCSupport.WaitForExpected(delegate () { return com1.BytesToRead; }, byteXmitBuffer.Length,
            5000, "Err_91818aheid BytesToRead");

        result = com1.ReadChar();

        if (result != charXmitBuffer[0])
        {
            retValue = false;
            Console.WriteLine("Err_0158ahei Expected ReadChar to read {0}({0:X}) actual {1}({1:X})", charXmitBuffer[0], result);
        }
        else
        {
            charRcvBuffer[0] = (char)result;
            int readResult = com1.Read(charRcvBuffer, 1, charRcvBuffer.Length - 1);

            if (readResult + 1 != charXmitBuffer.Length)
            {
                retValue = false;
                Console.WriteLine("Err_051884ajoedo Expected Read to read {0} cahracters actually read {1}",
                    charXmitBuffer.Length - 1, readResult);
            }
            else
            {
                for (int i = 0; i < charXmitBuffer.Length; ++i)
                {
                    if (charRcvBuffer[i] != charXmitBuffer[i])
                    {
                        retValue = false;
                        Console.WriteLine("Err_05188ahed Characters differ at {0} expected:{1}({1:X}) actual:{2}({2:X})",
                            i, charXmitBuffer[i], charRcvBuffer[i]);
                    }
                }
            }
        }

        if (!VerifyBytesReadOnCom1FromCom2(com1, com2, byteXmitBuffer, charXmitBuffer))
        {
            Console.WriteLine("Err_05188ajied Verify ReadLine after read failed");
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_9858wiapxzg Verifying that ReadChar() works appropriately after TimeoutException has been thrown failed");

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool Read_Surrogate()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] surrogateChars = new char[] { (char)0xDB26, (char)0xDC49 };
        char[] additionalChars = TCSupport.GetRandomChars(32, TCSupport.CharacterOptions.None);
        char[] charRcvBuffer = new char[2];
        int numBytes;
        bool retValue = true;
        int result;

        Console.WriteLine("Verifying that ReadChar works correctly when trying to read surrogate characters");

        com1.Encoding = new System.Text.UTF32Encoding();
        com2.Encoding = new System.Text.UTF32Encoding();
        com1.ReadTimeout = 500; // 20 seconds

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        numBytes = com2.Encoding.GetByteCount(surrogateChars);
        numBytes += com2.Encoding.GetByteCount(additionalChars);

        com2.Write(surrogateChars, 0, 2);
        com2.Write(additionalChars, 0, additionalChars.Length);

        retValue &= TCSupport.WaitForExpected(delegate () { return com1.BytesToRead; }, numBytes,
            5000, "Err_91818aheid BytesToRead");

        try
        {
            com1.ReadChar();
            retValue = false;
            Console.WriteLine("Err_11919aueic Expected ReadChar() to throw when reading a surrogate");
        }
        catch (ArgumentException) { } //Expected		

        result = com1.Read(charRcvBuffer, 0, 2);

        if (2 != result)
        {
            retValue = false;
            Console.WriteLine("Err_2829aheid Expected to Read 2 chars instead read={0}", result);
        }
        else if (charRcvBuffer[0] != surrogateChars[0])
        {
            retValue = false;
            Console.WriteLine("Err_12929anied Expected first char read={0}({1:X}) actually read={2}({3:X})",
                surrogateChars[0], (int)surrogateChars[0], charRcvBuffer[0], (int)charRcvBuffer[0]);
        }
        else if (charRcvBuffer[1] != surrogateChars[1])
        {
            Console.WriteLine("Err_12929anied Expected second char read={0}({1:X}) actually read={2}({3:X})",
                surrogateChars[1], (int)surrogateChars[1], charRcvBuffer[1], (int)charRcvBuffer[1]);
        }


        if (!PerformReadOnCom1FromCom2(com1, com2, additionalChars))
        {
            Console.WriteLine("Err_01588akeid Verify ReadChar after read failed");
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_0154688aieide Verifying that ReadChar works correctly when trying to read surrogate characters failed");

        com1.Close();
        com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyRead(System.Text.Encoding encoding)
    {
        return VerifyRead(encoding, ReadDataFromEnum.NonBuffered);
    }


    private bool VerifyRead(System.Text.Encoding encoding, int bufferSize)
    {
        return VerifyRead(encoding, ReadDataFromEnum.NonBuffered, bufferSize);
    }


    private bool VerifyRead(System.Text.Encoding encoding, ReadDataFromEnum readDataFrom)
    {
        return VerifyRead(encoding, readDataFrom, numRndChar);
    }


    private bool VerifyRead(System.Text.Encoding encoding, ReadDataFromEnum readDataFrom, int bufferSize)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        char[] charXmitBuffer = TCSupport.GetRandomChars(bufferSize, false);
        byte[] byteXmitBuffer;

        byteXmitBuffer = encoding.GetBytes(charXmitBuffer);

        com1.ReadTimeout = 500;
        com1.Encoding = encoding;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        switch (readDataFrom)
        {
            case ReadDataFromEnum.NonBuffered:
                return VerifyReadNonBuffered(com1, com2, byteXmitBuffer);

            case ReadDataFromEnum.Buffered:
                return VerifyReadBuffered(com1, com2, byteXmitBuffer);

            case ReadDataFromEnum.BufferedAndNonBuffered:
                return VerifyReadBufferedAndNonBuffered(com1, com2, byteXmitBuffer);
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
        int bytesToRead = com1.Encoding.GetByteCount(expectedChars);
        char[] charRcvBuffer = new char[expectedChars.Length];
        int rcvBufferSize = 0;
        int readInt;
        int i;
        bool isUTF7Encoding = com1.Encoding.EncodingName == System.Text.Encoding.UTF7.EncodingName;

        i = 0;
        while (true)
        {
            try
            {
                readInt = com1.ReadChar();
            }
            catch (TimeoutException)
            {
                if (i != expectedChars.Length)
                {
                    Console.WriteLine("Err_1282198anied Expected to read {0} chars actually read{1}", expectedChars.Length, i);
                    retValue = false;
                }

                break;
            }

            //While their are more characters to be read
            if (expectedChars.Length <= i)
            {
                //If we have read in more characters then were actually sent
                Console.WriteLine("ERROR!!!: We have received more characters then were sent");
                retValue = false;
                break;
            }

            charRcvBuffer[i] = (char)readInt;
            if (isUTF7Encoding)
                rcvBufferSize = GetUTF7EncodingBytes(expectedChars, 0, i + 1);
            else
                rcvBufferSize += com1.Encoding.GetByteCount(charRcvBuffer, i, 1);

            if (bytesToRead - rcvBufferSize != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1} at {2}", bytesToRead - rcvBufferSize, com1.BytesToRead, i);
                retValue = false;
                break;
            }

            if (readInt != expectedChars[i])
            {
                //If the character read is not the expected character
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read char {1} at {2}", (int)expectedChars[i], readInt, i);
                retValue = false;
                break;
            }

            i++;
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
        private int _result;

        private System.Threading.AutoResetEvent _readCompletedEvent;
        private System.Threading.AutoResetEvent _readStartedEvent;

        private Exception _exception;

        public ASyncRead(SerialPort com)
        {
            _com = com;
            _result = Int32.MinValue;

            _readCompletedEvent = new System.Threading.AutoResetEvent(false);
            _readStartedEvent = new System.Threading.AutoResetEvent(false);

            _exception = null;
        }

        public void Read()
        {
            try
            {
                _readStartedEvent.Set();
                _result = _com.ReadChar();
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

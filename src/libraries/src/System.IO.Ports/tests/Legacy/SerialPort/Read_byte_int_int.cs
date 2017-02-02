// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class Read_byte_int_int_Generic
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Read(byte[], int, int,)";
    public static readonly String s_strTFName = "Read_byte_int_int.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The number of random bytes to receive for read method testing
    public static readonly int numRndBytesToRead = 16;

    //The number of random bytes to receive for large input buffer testing
    public static readonly int largeNumRndBytesToRead = 2048;

    //When we test Read and do not care about actually reading anything we must still
    //create an byte array to pass into the method the following is the size of the 
    //byte array used in this situation
    public static readonly int defaultByteArraySize = 1;
    public static readonly int defaultByteOffset = 0;
    public static readonly int defaultByteCount = 1;

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
        Read_byte_int_int_Generic objTest = new Read_byte_int_int_Generic();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(GreedyRead), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeInputBuffer), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_DataReceivedBeforeTimeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

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
        if (!VerifyReadException(new byte[defaultByteArraySize], -1, defaultByteCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_002!!! Verifying read method throws exception with offset equal to -1");
            return false;
        }

        return true;
    }


    public bool Offset_NEGRND()
    {
        Random rndGen = new Random();

        if (!VerifyReadException(new byte[defaultByteArraySize], rndGen.Next(Int32.MinValue, 0), defaultByteCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_003!!! Verifying read method throws exception with offset equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Offset_MinInt()
    {
        if (!VerifyReadException(new byte[defaultByteArraySize], Int32.MinValue, defaultByteCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_004!!! Verifying read method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool Count_NEG1()
    {
        if (!VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, -1, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_005!!! Verifying read method throws exception with count equal to -1");
            return false;
        }

        return true;
    }


    public bool Count_NEGRND()
    {
        Random rndGen = new Random();

        if (!VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, rndGen.Next(Int32.MinValue, 0), typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_006!!! Verifying read method throws exception with count equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Count_MinInt()
    {
        if (!VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, Int32.MinValue, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_007!!! Verifying read method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length_Plus_1()
    {
        Random rndGen = new Random();
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(0, bufferLength);
        int count = bufferLength + 1 - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_009!!! Verifying read method throws exception with offset+count=buffer.length+1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_GT_Length()
    {
        Random rndGen = new Random();
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(0, bufferLength);
        int count = rndGen.Next(bufferLength + 1 - offset, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_010!!! Verifying read method throws exception with offset+count>buffer.length+1");
            return false;
        }

        return true;
    }


    public bool Offset_GT_Length()
    {
        Random rndGen = new Random();
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(bufferLength, Int32.MaxValue);
        int count = defaultByteCount;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_011!!! Verifying read method throws exception with offset>buffer.length");
            return false;
        }

        return true;
    }


    public bool Count_GT_Length()
    {
        Random rndGen = new Random();
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = defaultByteOffset;
        int count = rndGen.Next(bufferLength + 1, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_012!!! Verifying read method throws exception with count>buffer.length + 1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length()
    {
        Random rndGen = new Random();
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = bufferLength - offset;

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_013!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool Offset_EQ_Length_Minus_1()
    {
        Random rndGen = new Random();
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = bufferLength - 1;
        int count = 1;

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_014!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool Count_EQ_Length()
    {
        Random rndGen = new Random();
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = 0;
        int count = bufferLength;

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_015!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedData()
    {
        int bufferLength = 32 + 8;
        int offset = 3;
        int count = 32;

        if (!VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered))
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

        if (!VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.Buffered))
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

        if (!VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered))
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

        if (!VerifyRead(new byte[bufferLength], offset, count, 32, ReadDataFromEnum.BufferedAndNonBuffered))
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
        Random rndGen = new Random();
        byte[] byteXmitBuffer = new byte[1024];
        byte[] expectedBytes = new byte[byteXmitBuffer.Length + 4];
        byte[] byteRcvBuffer;
        char utf32Char = (char)8169;
        byte[] utf32CharBytes = System.Text.Encoding.UTF32.GetBytes(new char[] { utf32Char });
        int numBytesRead;
        bool retValue = true;

        Console.WriteLine("Verifying that Read(byte[], int, int) will read everything from internal buffer and drivers buffer");
        for (int i = 0; i < utf32CharBytes.Length; i++)
        {
            expectedBytes[i] = utf32CharBytes[i];
        }

        for (int i = 0; i < byteXmitBuffer.Length; i++)
        {
            byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
            expectedBytes[i + 4] = byteXmitBuffer[i];
        }

        //Put the first byte of the utf32 encoder char in the last byte of this buffer
        //when we read this later the buffer will have to be resized
        byteXmitBuffer[byteXmitBuffer.Length - 1] = utf32CharBytes[0];
        expectedBytes[byteXmitBuffer.Length + 4 - 1] = utf32CharBytes[0];

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

        com2.Write(utf32CharBytes, 1, 3);
        com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

        byteRcvBuffer = new byte[(int)(expectedBytes.Length * 1.5)];

        while (com1.BytesToRead < 4 + byteXmitBuffer.Length)
        {
            System.Threading.Thread.Sleep(50);
        }

        if (expectedBytes.Length != (numBytesRead = com1.Read(byteRcvBuffer, 0, byteRcvBuffer.Length)))
        {
            Console.WriteLine("Err_6481sfadw Expected read to read {0} chars actually read {1}", expectedBytes.Length, numBytesRead);
            retValue = false;
        }

        for (int i = 0; i < expectedBytes.Length; i++)
        {
            if (expectedBytes[i] != byteRcvBuffer[i])
            {
                Console.WriteLine("Err_70782apzh Expected to read {0} actually read {1} at {2}", (int)expectedBytes[i], (int)byteRcvBuffer[i], i);
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
            Console.WriteLine("Err_1389 Verifying that Read(byte[], int, int) will read everything from internal buffer and drivers buffer failed");

        com1.Close();
        com2.Close();

        return retValue;
    }


    public bool LargeInputBuffer()
    {
        Random rndGen = new Random();
        int bufferLength = largeNumRndBytesToRead;
        int offset = 0;
        int count = bufferLength;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new byte[bufferLength], offset, count, largeNumRndBytesToRead))
        {
            Console.WriteLine("Err_016!!! Verifying read method with large input buffer");
            return false;
        }

        return true;
    }

    public bool Read_DataReceivedBeforeTimeout()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        byte[] byteXmitBuffer = TCSupport.GetRandomBytes(512);
        byte[] byteRcvBuffer = new byte[byteXmitBuffer.Length];
        ASyncRead asyncRead = new ASyncRead(com1, byteRcvBuffer, 0, byteRcvBuffer.Length);
        System.Threading.Thread asyncReadThread = new System.Threading.Thread(new System.Threading.ThreadStart(asyncRead.Read));
        bool retValue = true;

        Console.WriteLine("Verifying that Read(byte[], int, int) will read characters that have been received after the call to Read was made");

        com1.Encoding = System.Text.Encoding.UTF8;
        com2.Encoding = System.Text.Encoding.UTF8;
        com1.ReadTimeout = 20000; // 20 seconds

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        asyncReadThread.Start();
        asyncRead.ReadStartedEvent.WaitOne(); //This only tells us that the thread has started to execute code in the method
        System.Threading.Thread.Sleep(2000); //We need to wait to guarentee that we are executing code in SerialPort
        com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

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
            int readResult = com1.Read(byteRcvBuffer, asyncRead.Result, byteRcvBuffer.Length - asyncRead.Result);

            if (asyncRead.Result + readResult != byteXmitBuffer.Length)
            {
                retValue = false;
                Console.WriteLine("Err_051884ajoedo Expected Read to read {0} cahracters actually read {1}",
                    byteXmitBuffer.Length - asyncRead.Result, readResult);
            }
            else
            {
                for (int i = 0; i < byteXmitBuffer.Length; ++i)
                {
                    if (byteRcvBuffer[i] != byteXmitBuffer[i])
                    {
                        retValue = false;
                        Console.WriteLine("Err_05188ahed Characters differ at {0} expected:{1}({1:X}) actual:{2}({2:X}) asyncRead.Result={3}",
                            i, byteXmitBuffer[i], byteRcvBuffer[i], asyncRead.Result);
                    }
                }
            }
        }

        if (!retValue)
            Console.WriteLine("Err_018068ajkid Verifying that Read(byte[], int, int) will read characters that have been received after the call to Read was made failed");

        com1.Close();
        com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyReadException(byte[] buffer, int offset, int count, Type expectedException)
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


    public bool VerifyRead(byte[] buffer, int offset, int count)
    {
        return VerifyRead(buffer, offset, count, numRndBytesToRead);
    }


    public bool VerifyRead(byte[] buffer, int offset, int count, int numberOfBytesToRead)
    {
        return VerifyRead(buffer, offset, count, numberOfBytesToRead, ReadDataFromEnum.NonBuffered);
    }


    public bool VerifyRead(byte[] buffer, int offset, int count, int numberOfBytesToRead, ReadDataFromEnum readDataFrom)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random();
        byte[] bytesToWrite = new byte[numberOfBytesToRead];

        //Genrate random bytes
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 256);

            bytesToWrite[i] = randByte;
        }

        //Genrate some random bytes in the buffer
        for (int i = 0; i < buffer.Length; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 256);

            buffer[i] = randByte;
        }

        Console.WriteLine("Verifying read method buffer.Lenght={0}, offset={1}, count={2} with {3} random chars", buffer.Length, offset, count, bytesToWrite.Length);

        com1.ReadTimeout = 500;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

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


    private bool VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
    {
        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, rcvBuffer, offset, count);
    }


    private bool VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
    {
        BufferData(com1, com2, bytesToWrite);
        return PerformReadOnCom1FromCom2(com1, com2, bytesToWrite, rcvBuffer, offset, count);
    }


    private bool VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
    {
        byte[] expectedBytes = new byte[(2 * bytesToWrite.Length)];

        BufferData(com1, com2, bytesToWrite);

        Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, 0, bytesToWrite.Length);
        Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, bytesToWrite.Length, bytesToWrite.Length);

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedBytes, rcvBuffer, offset, count);
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


    private bool VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
    {
        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, bytesToWrite, rcvBuffer, offset, count);
    }


    private bool VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] expectedBytes, byte[] rcvBuffer, int offset, int count)
    {
        com2.Write(bytesToWrite, 0, bytesToWrite.Length);

        com1.ReadTimeout = 500;

        System.Threading.Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

        return PerformReadOnCom1FromCom2(com1, com2, expectedBytes, rcvBuffer, offset, count);
    }


    private bool PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] expectedBytes, byte[] rcvBuffer, int offset, int count)
    {
        bool retValue = true;
        byte[] buffer = new byte[expectedBytes.Length];
        int bytesRead, totalBytesRead;
        int bytesToRead;
        byte[] oldRcvBuffer = (byte[])rcvBuffer.Clone();

        totalBytesRead = 0;
        bytesToRead = com1.BytesToRead;

        while (true)
        {
            try
            {
                bytesRead = com1.Read(rcvBuffer, offset, count);
            }
            catch (TimeoutException)
            {
                break;
            }

            //While their are more characters to be read
            if ((bytesToRead > bytesRead && count != bytesRead) || (bytesToRead <= bytesRead && bytesRead != bytesToRead))
            {
                //If we have not read all of the characters that we should have
                Console.WriteLine("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                retValue = false;
            }

            if (expectedBytes.Length < totalBytesRead + bytesRead)
            {
                //If we have read in more characters then we expect
                Console.WriteLine("ERROR!!!: We have received more characters then were sent {0}", totalBytesRead + bytesRead);
                retValue = false;
                break;
            }

            if (!VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, bytesRead))
                retValue = false;

            System.Array.Copy(rcvBuffer, offset, buffer, totalBytesRead, bytesRead);
            totalBytesRead += bytesRead;

            if (expectedBytes.Length - totalBytesRead != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", expectedBytes.Length - totalBytesRead, com1.BytesToRead);
                retValue = false;
            }

            oldRcvBuffer = (byte[])rcvBuffer.Clone();
            bytesToRead = com1.BytesToRead;
        }

        if (!VerifyBuffer(rcvBuffer, oldRcvBuffer, 0, rcvBuffer.Length))
            retValue = false;

        //Compare the bytes that were written with the ones we read
        for (int i = 0; i < expectedBytes.Length; i++)
        {
            if (expectedBytes[i] != buffer[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", expectedBytes[i], buffer[i], i);
                retValue = false;
            }
        }

        if (0 != com1.BytesToRead)
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


    private bool VerifyBuffer(byte[] actualBuffer, byte[] expectedBuffer, int offset, int count)
    {
        bool retValue = true;

        //Verify all character before the offset
        for (int i = 0; i < offset; i++)
        {
            if (actualBuffer[i] != expectedBuffer[i])
            {
                Console.WriteLine("Err_2038apzn!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                retValue = false;
            }
        }

        //Verify all character after the offset + count
        for (int i = offset + count; i < actualBuffer.Length; i++)
        {
            if (actualBuffer[i] != expectedBuffer[i])
            {
                Console.WriteLine("Err_7025nbht!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                retValue = false;
            }
        }

        return retValue;
    }

    public class ASyncRead
    {
        private SerialPort _com;
        private byte[] _buffer;
        private int _offset;
        private int _count;
        private int _result;

        private System.Threading.AutoResetEvent _readCompletedEvent;
        private System.Threading.AutoResetEvent _readStartedEvent;

        private Exception _exception;

        public ASyncRead(SerialPort com, byte[] buffer, int offset, int count)
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class BeginRead
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.BeginRead(byte[], int, int, AsyncCallback callback, object state)";
    public static readonly String s_strTFName = "BeginRead.cs";
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

    //Maximum time to wait for processing the read command to complete
    private const int MAX_WAIT_READ_COMPLETE = 1000;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        BeginRead objTest = new BeginRead();
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
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OffsetCount_EQ_Length), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_EQ_Length_Minus_1), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_EQ_Length), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeInputBuffer), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Callback), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Callback_EndReadonCallback), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Callback_State), TCSupport.SerialPortRequirements.NullModem);

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
        Random rndGen = new Random(-55);

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
        Random rndGen = new Random(-55);

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
        Random rndGen = new Random(-55);
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
        Random rndGen = new Random(-55);
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
        Random rndGen = new Random(-55);
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
        Random rndGen = new Random(-55);
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
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = bufferLength - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new byte[bufferLength], offset, count))
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

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_014!!! Verifying read method with offset + count=buffer.length");
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

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_015!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool LargeInputBuffer()
    {
        Random rndGen = new Random(-55);
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


    public bool Callback()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        CallbackHandler callbackHandler = new CallbackHandler();
        bool retValue = true;
        System.IAsyncResult readAsyncResult;
        System.IAsyncResult callbackReadAsyncResult;
        int elapsedTime;

        Console.WriteLine("Verifying BeginRead with a callback specified");

        com1.Open();
        com2.Open();

        readAsyncResult = com1.BaseStream.BeginRead(new byte[numRndBytesToRead], 0, numRndBytesToRead, new AsyncCallback(callbackHandler.Callback), null);
        callbackHandler.BeginReadAysncResult = readAsyncResult;
        retValue &= VerifyAsyncResult(readAsyncResult, null, false, false, "of IAsyncResult returned from BeginRead BEFORE Write");

        com2.Write(new byte[numRndBytesToRead], 0, numRndBytesToRead);

        //callbackHandler.ReadAsyncResult  guarantees that the callback has been calledhowever it does not gauarentee that 
        //the code calling the the callback has finished it's processing
        callbackReadAsyncResult = callbackHandler.ReadAysncResult;

        //No we have to wait for the callbackHandler to complete
        elapsedTime = 0;
        while (!callbackReadAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_READ_COMPLETE)
        {
            System.Threading.Thread.Sleep(10);
            elapsedTime += 10;
        }

        retValue &= VerifyAsyncResult(callbackReadAsyncResult, null, false, true, " of IAsyncResult passed into AsyncCallback");
        retValue &= VerifyAsyncResult(readAsyncResult, null, false, true, "of IAsyncResult returned from BeginRead AFTER Write");

        if (!retValue)
            Console.WriteLine("Err_017 Verifying BeginRead with a callback specified FAILED");

        com1.Close();
        com2.Close();
        return retValue;
    }


    public bool Callback_EndReadonCallback()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        CallbackHandler callbackHandler = new CallbackHandler(com1);
        bool retValue = true;
        System.IAsyncResult readAsyncResult;
        System.IAsyncResult callbackReadAsyncResult;
        int elapsedTime;

        Console.WriteLine("Verifying BeginRead with a callback that calls EndRead");

        com1.Open();
        com2.Open();

        readAsyncResult = com1.BaseStream.BeginRead(new byte[numRndBytesToRead], 0, numRndBytesToRead, new AsyncCallback(callbackHandler.Callback), null);
        callbackHandler.BeginReadAysncResult = readAsyncResult;
        retValue &= VerifyAsyncResult(readAsyncResult, null, false, false, "of IAsyncResult returned from BeginRead BEFORE Write");

        com2.Write(new byte[numRndBytesToRead], 0, numRndBytesToRead);

        //callbackHandler.ReadAsyncResult  guarantees that the callback has been calledhowever it does not gauarentee that 
        //the code calling the the callback has finished it's processing
        callbackReadAsyncResult = callbackHandler.ReadAysncResult;

        //No we have to wait for the callbackHandler to complete
        elapsedTime = 0;
        while (!callbackReadAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_READ_COMPLETE)
        {
            System.Threading.Thread.Sleep(10);
            elapsedTime += 10;
        }

        retValue &= VerifyAsyncResult(callbackReadAsyncResult, null, false, true, " of IAsyncResult passed into AsyncCallback");
        retValue &= VerifyAsyncResult(readAsyncResult, null, false, true, "of IAsyncResult returned from BeginRead AFTER Write");

        if (!retValue)
            Console.WriteLine("Err_6897adfha Verifying BeginRead with a callback that calls EndRead FAILED");

        com1.Close();
        com2.Close();
        return retValue;
    }


    public bool Callback_State()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        CallbackHandler callbackHandler = new CallbackHandler();
        bool retValue = true;
        System.IAsyncResult readAsyncResult;
        System.IAsyncResult callbackReadAsyncResult;
        int elapsedTime;

        Console.WriteLine("Verifying BeginRead with a callback and state specified");

        com1.Open();
        com2.Open();

        readAsyncResult = com1.BaseStream.BeginRead(new byte[numRndBytesToRead], 0, numRndBytesToRead, new AsyncCallback(callbackHandler.Callback), this);
        callbackHandler.BeginReadAysncResult = readAsyncResult;
        retValue &= VerifyAsyncResult(readAsyncResult, this, false, false, "of IAsyncResult returned from BeginRead BEFORE Write");

        com2.Write(new byte[numRndBytesToRead], 0, numRndBytesToRead);

        //callbackHandler.ReadAsyncResult  guarantees that the callback has been calledhowever it does not gauarentee that 
        //the code calling the the callback has finished it's processing
        callbackReadAsyncResult = callbackHandler.ReadAysncResult;

        //No we have to wait for the callbackHandler to complete
        elapsedTime = 0;
        while (!callbackReadAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_READ_COMPLETE)
        {
            System.Threading.Thread.Sleep(10);
            elapsedTime += 10;
        }

        retValue &= VerifyAsyncResult(callbackReadAsyncResult, this, false, true, " of IAsyncResult passed into AsyncCallback");
        retValue &= VerifyAsyncResult(readAsyncResult, this, false, true, "of IAsyncResult returned from BeginRead AFTER Write");

        if (!retValue)
            Console.WriteLine("Err_018 Verifying BeginRead with a callback and state specified FAILED");

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
        System.IAsyncResult readAsyncResult;

        Console.WriteLine("Verifying read method throws {0} buffer.Lenght={1}, offset={2}, count={3}", expectedException, bufferLength, offset, count);

        com.Open();

        try
        {
            readAsyncResult = com.BaseStream.BeginRead(buffer, offset, count, null, null);
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
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
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
        com2.Open();

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, buffer, offset, count);
    }


    private bool VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
    {
        bool retValue = true;
        byte[] buffer = new byte[bytesToWrite.Length];
        int bytesRead, totalBytesRead;
        int bytesToRead;
        byte[] oldRcvBuffer = (byte[])rcvBuffer.Clone();
        System.IAsyncResult readAsyncResult;
        CallbackHandler callbackHandler = new CallbackHandler();

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 500;
        System.Threading.Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);
        totalBytesRead = 0;
        bytesToRead = com1.BytesToRead;

        do
        {
            readAsyncResult = com1.BaseStream.BeginRead(rcvBuffer, offset, count, new AsyncCallback(callbackHandler.Callback), this);
            readAsyncResult.AsyncWaitHandle.WaitOne();
            callbackHandler.BeginReadAysncResult = readAsyncResult;

            bytesRead = com1.BaseStream.EndRead(readAsyncResult);
            retValue &= VerifyAsyncResult(callbackHandler.ReadAysncResult, this, false, true, " of IAsyncResult passed into AsyncCallback");
            retValue &= VerifyAsyncResult(readAsyncResult, this, false, true, " of IAsyncResult returned from BeginRead");

            if ((bytesToRead > bytesRead && count != bytesRead) || (bytesToRead <= bytesRead && bytesRead != bytesToRead))
            {
                //If we have not read all of the characters that we should have
                Console.WriteLine("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                retValue = false;
            }

            if (bytesToWrite.Length < totalBytesRead + bytesRead)
            {
                //If we have read in more characters then we expect
                Console.WriteLine("ERROR!!!: We have received more characters then were sent");
                retValue = false;
                break;
            }

            if (!VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, bytesRead))
                retValue = false;

            System.Array.Copy(rcvBuffer, offset, buffer, totalBytesRead, bytesRead);
            totalBytesRead += bytesRead;

            if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                retValue = false;
            }

            oldRcvBuffer = (byte[])rcvBuffer.Clone();
            bytesToRead = com1.BytesToRead;
        } while (0 != com1.BytesToRead); //While there are more bytes to read

        //Compare the bytes that were written with the ones we read
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            if (bytesToWrite[i] != buffer[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1}", bytesToWrite[i], buffer[i]);
                retValue = false;
            }
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



    public class CallbackHandler
    {
        private IAsyncResult _readAysncResult = null;
        private IAsyncResult _beginReadAysncResult = null;
        private SerialPort _com;


        public CallbackHandler() : this(null) { }


        public CallbackHandler(SerialPort com)
        {
            _com = com;
        }


        public void Callback(IAsyncResult readAysncResult)
        {
            lock (this)
            {
                _readAysncResult = readAysncResult;

                if (!readAysncResult.IsCompleted)
                {
                    throw new Exception("Err_23984afaea Expected IAsyncResult passed into callback to not be completed");
                }

                while (null == _beginReadAysncResult)
                {
                    System.Threading.Monitor.Wait(this);
                }

                if (null != _beginReadAysncResult && !_beginReadAysncResult.IsCompleted)
                {
                    throw new Exception("Err_7907azpu Expected IAsyncResult returned from begin read to not be completed");
                }

                if (null != _com)
                {
                    _com.BaseStream.EndRead(_beginReadAysncResult);
                    if (!_beginReadAysncResult.IsCompleted)
                    {
                        throw new Exception("Err_6498afead Expected IAsyncResult returned from begin read to not be completed");
                    }

                    if (!readAysncResult.IsCompleted)
                    {
                        throw new Exception("Err_1398ehpo Expected IAsyncResult passed into callback to not be completed");
                    }
                }

                System.Threading.Monitor.Pulse(this);
            }
        }


        public IAsyncResult ReadAysncResult
        {
            get
            {
                lock (this)
                {
                    while (null == _readAysncResult)
                    {
                        System.Threading.Monitor.Wait(this);
                    }

                    return _readAysncResult;
                }
            }
        }


        public IAsyncResult BeginReadAysncResult
        {
            get
            {
                return _beginReadAysncResult;
            }
            set
            {
                lock (this)
                {
                    _beginReadAysncResult = value;
                    System.Threading.Monitor.Pulse(this);
                }
            }
        }
    }



    public bool VerifyAsyncResult(IAsyncResult asyncResult, object expectedAsyncState, bool expectedCompletedSynchronously, bool expectedIsCompleted, string suffix)
    {
        bool retValue = true;

        if (expectedAsyncState != asyncResult.AsyncState)
        {
            Console.WriteLine("ERROR!!!: Expected readAsyncResult.AsyncState={0} acutual={1} " + suffix, expectedAsyncState, asyncResult.AsyncState);
            retValue = false;
        }

        if (expectedCompletedSynchronously != asyncResult.CompletedSynchronously)
        {
            Console.WriteLine("ERROR!!!: Expected readAsyncResult.CompletedSynchronously={0} acutual={1} " + suffix, expectedCompletedSynchronously, asyncResult.CompletedSynchronously);
            retValue = false;
        }

        if (expectedIsCompleted != asyncResult.IsCompleted)
        {
            Console.WriteLine("ERROR!!!: Expected readAsyncResult.IsCompleted={0} acutual={1} " + suffix, expectedIsCompleted, asyncResult.IsCompleted);
            retValue = false;
        }

        return retValue;
    }
    #endregion
}

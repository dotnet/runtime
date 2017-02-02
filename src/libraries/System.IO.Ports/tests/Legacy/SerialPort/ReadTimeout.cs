// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class ReadTimeout_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadTimeout";
    public static readonly String s_strTFName = "ReadTimeout.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default number of chars to write with when testing timeout with Read(char[], int, int)
    public static readonly int DEFAULT_READ_CHAR_ARRAY_SIZE = 8;

    //The default number of bytes to write with when testing timeout with Read(byte[], int, int)
    public static readonly int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;

    //The ammount of time to wait when expecting an infinite timeout
    public static readonly int DEFAULT_WAIT_INFINITE_TIMEOUT = 250;

    //The maximum acceptable time allowed when a read method should timeout immediately
    public static readonly int MAX_ACCEPTABLE_ZERO_TIMEOUT = 100;

    //The maximum acceptable time allowed when a read method should timeout immediately when it is called for the first time
    public static readonly int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 1000;
    public static readonly int NUM_TRYS = 5;

    //The default new lint to read from when testing timeout with ReadTo(str)
    public static readonly string DEFAULT_READ_TO_STRING = "\r\n";

    public delegate void ReadMethodDelegate(SerialPort com);

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadTimeout_Property objTest = new ReadTimeout_Property();
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
        System.Threading.ThreadAbortException tae = e.ExceptionObject as System.Threading.ThreadAbortException;

        if (null != tae)
        {
            Object o = tae.ExceptionState;

            if (null != o && o == this)
            {
                return;
            }
        }

        _numErrors++;
        Console.WriteLine("\nAn unhandled exception was thrown and not caught in the app domain: \n{0}", e.ExceptionObject);
        Console.WriteLine("Test FAILED!!!\n");

        Environment.ExitCode = 101;
    }

    public bool RunTest()
    {
        bool retValue = true;
        TCSupport tcSupport = new TCSupport();

        //See individual read methods for further testing
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Default_Read_byte_int_int), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Default_Read_char_int_int), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Default_ReadByte), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Default_ReadLine), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Default_ReadTo), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Infinite_Read_byte_int_int), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Infinite_Read_char_int_int), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Infinite_ReadByte), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Infinite_ReadLine), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Infinite_ReadTo), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_Read_byte_int_int_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_Read_char_int_int_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_ReadByte_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_ReadLine_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_ReadTo_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_Read_byte_int_int_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_Read_char_int_int_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_ReadByte_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_ReadLine_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_ReadTo_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_NEG2), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ReadTimeout_Default_Read_byte_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default ReadTimeout with Read(byte[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(Read_byte_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Default_Read_char_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default ReadTimeout with Read(char[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(Read_char_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying default ReadTimeout with Read(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Default_ReadByte()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default ReadTimeout with ReadByte()");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(ReadByte), false);

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying default ReadTimeout with ReadByte() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Default_ReadLine()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default ReadTimeout with ReadLine()");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(ReadLine), false);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying default ReadTimeout with ReadLine() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Default_ReadTo()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default ReadTimeout with ReadTo()");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(ReadTo), false);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying default ReadTimeout with ReadTo() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Infinite_Read_byte_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite ReadTimeout with Read(byte[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(Read_byte_int_int), true);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying infinite ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Infinite_Read_char_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite ReadTimeout with Read(char[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(Read_char_int_int), true);

        if (!retValue)
        {
            Console.WriteLine("Err_007!!! Verifying infinite ReadTimeout with Read(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Infinite_ReadByte()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite ReadTimeout with ReadByte()");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(ReadByte), true);

        if (!retValue)
        {
            Console.WriteLine("Err_008!!! Verifying infinite ReadTimeout with ReadByte() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Infinite_ReadLine()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite ReadTimeout with ReadLine()");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(ReadLine), true);

        if (!retValue)
        {
            Console.WriteLine("Err_009!!! Verifying infinite ReadTimeout with ReadLine() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Infinite_ReadTo()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite ReadTimeout with ReadTo()");
        retValue &= VerifyInfiniteTimeout(new ReadMethodDelegate(ReadTo), true);

        if (!retValue)
        {
            Console.WriteLine("Err_010!!! Verifying infinite ReadTimeout with ReadTo() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_Read_byte_int_int_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting ReadTimeout=0 before Open() with Read(byte[] buffer, int offset, int count)");
        retValue &= VerifyZeroTimeoutBeforeOpen(new ReadMethodDelegate(Read_byte_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_011!!! Verifying zero ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_Read_char_int_int_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting ReadTimeout=0 before Open() with Read(char[] buffer, int offset, int count)");
        retValue &= VerifyZeroTimeoutBeforeOpen(new ReadMethodDelegate(Read_char_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_012!!! Verifying zero ReadTimeout with Read(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_ReadByte_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying zero ReadTimeout before Open with ReadByte()");
        retValue &= VerifyZeroTimeoutBeforeOpen(new ReadMethodDelegate(ReadByte));

        if (!retValue)
        {
            Console.WriteLine("Err_013!!! Verifying zero ReadTimeout before Open with ReadByte() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_ReadLine_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying zero ReadTimeout before Open with ReadLine()");
        retValue &= VerifyZeroTimeoutBeforeOpen(new ReadMethodDelegate(ReadLine));

        if (!retValue)
        {
            Console.WriteLine("Err_014!!! Verifying zero ReadTimeout before Open with ReadLine() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_ReadTo_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying zero ReadTimeout before Open with ReadTo()");
        retValue &= VerifyZeroTimeoutBeforeOpen(new ReadMethodDelegate(ReadTo));

        if (!retValue)
        {
            Console.WriteLine("Err_015!!! Verifying zero ReadTimeout before Open with ReadTo() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_Read_byte_int_int_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting ReadTimeout=0 after Open() with Read(byte[] buffer, int offset, int count)");
        retValue &= VerifyZeroTimeoutAfterOpen(new ReadMethodDelegate(Read_byte_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_016!!! Verifying zero ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_Read_char_int_int_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting ReadTimeout=0 after Open() with Read(char[] buffer, int offset, int count)");
        retValue &= VerifyZeroTimeoutAfterOpen(new ReadMethodDelegate(Read_char_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_017!!! Verifying zero ReadTimeout with Read(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_ReadByte_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying zero ReadTimeout after Open with ReadByte()");
        retValue &= VerifyZeroTimeoutAfterOpen(new ReadMethodDelegate(ReadByte));

        if (!retValue)
        {
            Console.WriteLine("Err_018!!! Verifying zero ReadTimeout after Open with ReadByte() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_ReadLine_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying zero ReadTimeout after Open with ReadLine()");
        retValue &= VerifyZeroTimeoutAfterOpen(new ReadMethodDelegate(ReadLine));

        if (!retValue)
        {
            Console.WriteLine("Err_019!!! Verifying zero ReadTimeout after Open with ReadLine() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_0_ReadTo_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying zero ReadTimeout after Open with ReadTo()");
        retValue &= VerifyZeroTimeoutAfterOpen(new ReadMethodDelegate(ReadTo));

        if (!retValue)
        {
            Console.WriteLine("Err_020!!! Verifying zero ReadTimeout after Open with ReadTo() FAILED");
        }

        return retValue;
    }


    public bool ReadTimeout_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue ReadTimeout");

        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_021!!! Verifying Int32.MinValue ReadTimeout FAILED");
            return false;
        }

        return true;
    }


    public bool ReadTimeout_NEG2()
    {
        Console.WriteLine("Verifying -2 ReadTimeout");

        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_022!!! Verifying -2 ReadTimeout FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyInfiniteTimeout(ReadMethodDelegate readMethod, bool setInfiniteTimeout)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ReadDelegateThread readThread = new ReadDelegateThread(com1, readMethod);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(readThread.CallRead));
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("WriteTimeout", 10);

        com1.WriteTimeout = 10;
        com1.Open();

        if (!com2.IsOpen)
            com2.Open();

        if (setInfiniteTimeout)
        {
            com1.ReadTimeout = 500;
            com1.ReadTimeout = SerialPort.InfiniteTimeout;
        }

        t.Start();
        System.Threading.Thread.Sleep(DEFAULT_WAIT_INFINITE_TIMEOUT);

        if (!t.IsAlive)
        {
            Console.WriteLine("ERROR!!! {0} terminated with infinite timeout", readMethod.Method.Name);
            retValue = false;
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        com2.WriteLine(String.Empty);

        while (t.IsAlive)
            System.Threading.Thread.Sleep(10);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool VerifyZeroTimeoutBeforeOpen(ReadMethodDelegate readMethod)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com.ReadTimeout = 0;
        com.Open();

        retValue &= VerifyZeroTimeout(com, readMethod);

        return retValue;
    }


    public bool VerifyZeroTimeoutAfterOpen(ReadMethodDelegate readMethod)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com.Open();
        com.ReadTimeout = 0;

        retValue &= VerifyZeroTimeout(com, readMethod);

        return retValue;
    }


    public bool VerifyZeroTimeout(SerialPort com, ReadMethodDelegate readMethod)
    {
        SerialPortProperties serPortProp = new SerialPortProperties();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        bool retValue = true;
        int actualTime = 0;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        serPortProp.SetProperty("ReadTimeout", 0);
        serPortProp.SetProperty("WriteTimeout", 1000);

        com.WriteTimeout = 1000;

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        sw.Start();
        readMethod(com);
        sw.Stop();

        if (MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT < sw.ElapsedMilliseconds)
        {
            Console.WriteLine("Err_2570ajdlkj!!! Read Method {0} timed out in {1}ms expected something less then {2}ms", readMethod.Method.Name, sw.ElapsedMilliseconds, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT);
            retValue = false;
        }

        sw.Reset();

        for (int i = 0; i < NUM_TRYS; i++)
        {
            sw.Start();
            readMethod(com);
            sw.Stop();

            actualTime += (int)sw.ElapsedMilliseconds;
            sw.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;

        if (MAX_ACCEPTABLE_ZERO_TIMEOUT < actualTime)
        {
            Console.WriteLine("ERROR!!! Read Method {0} timed out in {1}ms expected something less then {2}ms", readMethod.Method.Name, actualTime, MAX_ACCEPTABLE_ZERO_TIMEOUT);
            retValue = false;
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        com.ReadTimeout = 0;

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyException(int readTimeout, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, readTimeout, throwAt, expectedException);

        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, readTimeout, expectedException);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, int readTimeout, ThrowAt throwAt, System.Type expectedException)
    {
        int origReadTimeout = com.ReadTimeout;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("ReadTimeout", readTimeout);

        try
        {
            com.ReadTimeout = readTimeout;

            if (ThrowAt.Open == throwAt)
                com.Open();

            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        com.ReadTimeout = origReadTimeout;

        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, int readTimeout, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.ReadTimeout = readTimeout;
            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the ReadTimeout after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the ReadTimeout after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the ReadTimeout after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        return retValue;
    }


    private void Read_byte_int_int(SerialPort com)
    {
        try
        {
            com.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
        }
        catch (TimeoutException) { }
    }


    private void Read_char_int_int(SerialPort com)
    {
        try
        {
            com.Read(new char[DEFAULT_READ_CHAR_ARRAY_SIZE], 0, DEFAULT_READ_CHAR_ARRAY_SIZE);
        }
        catch (TimeoutException) { }
    }


    private void ReadByte(SerialPort com)
    {
        try
        {
            com.ReadByte();
        }
        catch (TimeoutException) { }
    }


    private void ReadChar(SerialPort com)
    {
        try
        {
            com.ReadChar();
        }
        catch (TimeoutException) { }
    }


    private void ReadLine(SerialPort com)
    {
        try
        {
            com.ReadLine();
        }
        catch (TimeoutException) { }
    }


    private void ReadTo(SerialPort com)
    {
        try
        {
            com.ReadTo(com.NewLine);
        }
        catch (TimeoutException) { }
    }



    public class ReadDelegateThread
    {
        public ReadDelegateThread(SerialPort com, ReadMethodDelegate readMethod)
        {
            _com = com;
            this._readMethod = readMethod;
        }

        public void CallRead()
        {
            readMethod(_com);
        }

        private ReadMethodDelegate _readMethod;
        private SerialPort _com;
    }

    #endregion
}

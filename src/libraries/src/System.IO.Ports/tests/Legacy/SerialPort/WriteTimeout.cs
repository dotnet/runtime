// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class WriteTimeout_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.WriteTimeout";
    public static readonly String s_strTFName = "WriteTimeout.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;
    //The default number of chars to write with when testing timeout with Write(char[], int, int)
    public static readonly int DEFAULT_WRITE_CHAR_ARRAY_SIZE = 8;

    //The default number of bytes to write with when testing timeout with Write(byte[], int, int)
    public static readonly int DEFAULT_WRITE_BYTE_ARRAY_SIZE = 8;

    //The ammount of time to wait when expecting an infinite timeout
    public static readonly int DEFAULT_WAIT_INFINITE_TIMEOUT = 250;

    //The maximum acceptable time allowed when a write method should timeout immediately
    public static readonly int MAX_ACCEPTABLE_ZERO_TIMEOUT = 100;

    //The maximum acceptable time allowed when a write method should timeout immediately when it is called for the first time
    public static readonly int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 5000;

    //The default string to write with when testing timeout with Write(str)
    public static readonly string DEFAULT_STRING_TO_WRITE = "TEST";
    public static readonly int NUM_TRYS = 5;

    public delegate void WriteMethodDelegate(SerialPort com);

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        WriteTimeout_Property objTest = new WriteTimeout_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Default_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Default_Write_char_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Default_Write_str), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Default_WriteLine), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Infinite_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Infinite_Write_char_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Infinite_Write_str), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Infinite_WriteLine), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_Write_byte_int_int_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_Write_char_int_int_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_Write_str_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_WriteLine_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_Write_byte_int_int_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_Write_char_int_int_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_Write_str_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_1_WriteLine_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_NEG2), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool WriteTimeout_Default_Write_byte_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default WriteTimeout with Write(byte[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(Write_byte_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Default_Write_char_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default WriteTimeout with Write(char[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(Write_char_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying default WriteTimeout with Write(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Default_Write_str()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default WriteTimeout with Write(string)");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(Write_str), false);
        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying default WriteTimeout with Write(string) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Default_WriteLine()
    {
        bool retValue = true;

        Console.WriteLine("Verifying default WriteTimeout with WriteLine()");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(WriteLine), false);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying default WriteTimeout with WriteLine() FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Infinite_Write_byte_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite WriteTimeout with Write(byte[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(Write_byte_int_int), true);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying infinite WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Infinite_Write_char_int_int()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite WriteTimeout with Write(char[] buffer, int offset, int count)");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(Write_char_int_int), true);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying infinite WriteTimeout with Write(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Infinite_Write_str()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite WriteTimeout with Write(string)");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(Write_str), true);

        if (!retValue)
        {
            Console.WriteLine("Err_007!!! Verifying infinite WriteTimeout with Write(string) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Infinite_WriteLine()
    {
        bool retValue = true;

        Console.WriteLine("Verifying infinite WriteTimeout with WriteLine()");
        retValue &= VerifyInfiniteTimeout(new WriteMethodDelegate(WriteLine), true);

        if (!retValue)
        {
            Console.WriteLine("Err_008!!! Verifying infinite WriteTimeout with WriteLine() FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_Write_byte_int_int_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting WriteTimeout=1 before Open() with Write(byte[] buffer, int offset, int count)");
        retValue &= Verify1TimeoutBeforeOpen(new WriteMethodDelegate(Write_byte_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_009!!! Verifying 1 WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_Write_char_int_int_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting WriteTimeout=1 before Open() with Write(char[] buffer, int offset, int count)");
        retValue &= Verify1TimeoutBeforeOpen(new WriteMethodDelegate(Write_char_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_010!!! Verifying 1 WriteTimeout with Write(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_Write_str_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying 1 WriteTimeout before Open with Write(string)");
        retValue &= Verify1TimeoutBeforeOpen(new WriteMethodDelegate(Write_str));

        if (!retValue)
        {
            Console.WriteLine("Err_012!!! Verifying 1 WriteTimeout before Open with Write(string) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_WriteLine_BeforeOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying 1 WriteTimeout before Open with WriteLine()");
        retValue &= Verify1TimeoutBeforeOpen(new WriteMethodDelegate(WriteLine));

        if (!retValue)
        {
            Console.WriteLine("Err_013!!! Verifying 1 WriteTimeout before Open with WriteLine() FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_Write_byte_int_int_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting WriteTimeout=1 after Open() with Write(byte[] buffer, int offset, int count)");
        retValue &= Verify1TimeoutAfterOpen(new WriteMethodDelegate(Write_byte_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_014!!! Verifying 1 WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_Write_char_int_int_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting WriteTimeout=1 after Open() with Write(char[] buffer, int offset, int count)");
        retValue &= Verify1TimeoutAfterOpen(new WriteMethodDelegate(Write_char_int_int));

        if (!retValue)
        {
            Console.WriteLine("Err_015!!! Verifying 1 WriteTimeout with Write(char[] buffer, int offset, int count) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_Write_str_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying 1 WriteTimeout after Open with Write(string)");
        retValue &= Verify1TimeoutAfterOpen(new WriteMethodDelegate(Write_str));

        if (!retValue)
        {
            Console.WriteLine("Err_016!!! Verifying 1 WriteTimeout after Open with Write(string) FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_1_WriteLine_AfterOpen()
    {
        bool retValue = true;

        Console.WriteLine("Verifying 1 WriteTimeout after Open with WriteLine()");
        retValue &= Verify1TimeoutAfterOpen(new WriteMethodDelegate(WriteLine));

        if (!retValue)
        {
            Console.WriteLine("Err_017!!! Verifying 1 WriteTimeout after Open with WriteLine() FAILED");
        }

        return retValue;
    }


    public bool WriteTimeout_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue WriteTimeout");
        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_018!!! Verifying Int32.MinValue WriteTimeout FAILED");
            return false;
        }

        return true;
    }


    public bool WriteTimeout_NEG2()
    {
        Console.WriteLine("Verifying -2 WriteTimeout");
        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_019!!! Verifying -2 WriteTimeout FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyInfiniteTimeout(WriteMethodDelegate readMethod, bool setInfiniteTimeout)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = null;
        WriteDelegateThread readThread = new WriteDelegateThread(com1, readMethod);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(readThread.CallWrite));
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Handshake = Handshake.RequestToSend;

        serPortProp.SetProperty("ReadTimeout", 10);
        com1.ReadTimeout = 10;

        com1.Open();



        if (setInfiniteTimeout)
        {
            com1.WriteTimeout = 500;
            com1.WriteTimeout = SerialPort.InfiniteTimeout;
        }

        t.Start();
        System.Threading.Thread.Sleep(DEFAULT_WAIT_INFINITE_TIMEOUT);

        if (!t.IsAlive)
        {
            Console.WriteLine("ERROR!!! {0} terminated with infinite timeout", readMethod.Method.Name);
            retValue = false;
        }



        com1.Handshake = Handshake.None;

        while (t.IsAlive)
            System.Threading.Thread.Sleep(10);


        com1.DiscardOutBuffer();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();



        return retValue;
    }


    public bool Verify1TimeoutBeforeOpen(WriteMethodDelegate readMethod)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com.WriteTimeout = 1;
        com.Open();

        retValue &= Verify1Timeout(com, readMethod);

        return retValue;
    }


    public bool Verify1TimeoutAfterOpen(WriteMethodDelegate readMethod)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com.Open();
        com.WriteTimeout = 1;

        retValue &= Verify1Timeout(com, readMethod);

        return retValue;
    }


    public bool Verify1Timeout(SerialPort com, WriteMethodDelegate readMethod)
    {
        SerialPortProperties serPortProp = new SerialPortProperties();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        bool retValue = true;
        int actualTime = 0;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("WriteTimeout", 1);

        serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
        com.Handshake = Handshake.RequestToSend;

        serPortProp.SetProperty("ReadTimeout", 1000);
        com.ReadTimeout = 1000;

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        sw.Start();
        readMethod(com);
        sw.Stop();

        if (MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT < sw.ElapsedMilliseconds)
        {
            Console.WriteLine("Err_2570ajdlkj!!! Write Method {0} timed out in {1}ms expected something less then {2}ms", readMethod.Method.Name, sw.ElapsedMilliseconds, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT);
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
            Console.WriteLine("ERROR!!! Write Method {0} timed out in {1}ms expected something less then {2}ms", readMethod.Method.Name, actualTime, MAX_ACCEPTABLE_ZERO_TIMEOUT);
            retValue = false;
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyException(int writeTimeout, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, writeTimeout, throwAt, expectedException);

        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, writeTimeout, expectedException);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, int writeTimeout, ThrowAt throwAt, System.Type expectedException)
    {
        int origWriteTimeout = com.WriteTimeout;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("WriteTimeout", writeTimeout);

        try
        {
            com.WriteTimeout = writeTimeout;

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
        com.WriteTimeout = origWriteTimeout;

        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, int writeTimeout, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.WriteTimeout = writeTimeout;

            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the WriteTimeout after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the WriteTimeout after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the WriteTimeout after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        return retValue;
    }


    private void Write_byte_int_int(SerialPort com)
    {
        try
        {
            com.Write(new byte[DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_ARRAY_SIZE);
        }
        catch (System.TimeoutException)
        {
        }
    }


    private void Write_char_int_int(SerialPort com)
    {
        try
        {
            com.Write(new char[DEFAULT_WRITE_CHAR_ARRAY_SIZE], 0, DEFAULT_WRITE_CHAR_ARRAY_SIZE);
        }
        catch (System.TimeoutException)
        {
        }
    }


    private void Write_str(SerialPort com)
    {
        try
        {
            com.Write(DEFAULT_STRING_TO_WRITE);
        }
        catch (System.TimeoutException)
        {
        }
    }


    private void WriteLine(SerialPort com)
    {
        try
        {
            com.WriteLine(DEFAULT_STRING_TO_WRITE);
        }
        catch (System.TimeoutException)
        {
        }
    }



    public class WriteDelegateThread
    {
        public WriteDelegateThread(SerialPort com, WriteMethodDelegate readMethod)
        {
            _com = com;
            _readMethod = readMethod;
        }


        public void CallWrite()
        {
            readMethod(_com);
        }


        private WriteMethodDelegate _readMethod;
        private SerialPort _com;
    }

    #endregion
}

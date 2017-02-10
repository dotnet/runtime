// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;

public class WriteTimeout_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.WriteTimeout";
    public static readonly String s_strTFName = "WriteTimeout.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;


    //The default number of bytes to write with when testing timeout with Write(byte[], int, int)
    public static readonly int DEFAULT_WRITE_BYTE_ARRAY_SIZE = 8;

    //The large number of bytes to write with when testing timeout with Write(byte[], int, int)
    //This needs to be large enough for Write timeout
    public static readonly int DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE = 1024 * 100;

    //The BaudRate to use to make Write timeout when writing DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE bytes 
    public static readonly int LARGEWRITE_BAUDRATE = 1200;

    //The timeout to use to make Write timeout when writing DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE
    public static readonly int LARGEWRITE_TIMEOUT = 750;

    //The default byte to call with WriteByte
    public static readonly byte DEFAULT_WRITE_BYTE = 33;

    //The ammount of time to wait when expecting an long timeout
    public static readonly int DEFAULT_WAIT_LONG_TIMEOUT = 250;

    //The maximum acceptable time allowed when a write method should timeout immediately
    public static readonly int MAX_ACCEPTABLE_ZERO_TIMEOUT = 100;

    //The maximum acceptable time allowed when a write method should timeout immediately when it is called for the first time
    public static readonly int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 1000;

    //The maximum acceptable percentage difference allowed when a write method is called for the first time
    public static readonly double MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE = .5;

    //The maximum acceptable percentage difference allowed
    public static readonly double MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE = .15;

    public static readonly int SUCCESSIVE_WriteTimeout_SOMEDATA = 950;

    public static readonly int NUM_TRYS = 5;

    public delegate void WriteMethodDelegate(Stream stream);

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
            Console.WriteLine(s_strTFAbbrev + " : FAIL The following exception was thrown in RunTest(): \n" + e.ToString());
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_DefaultValue), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_AfterClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_NEG2), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_ZERO), TCSupport.SerialPortRequirements.OneSerialPort);


        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Default_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Default_WriteByte), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Infinite_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Infinite_WriteByte), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Int32MaxValue_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_Int32MaxValue_WriteByte), TCSupport.SerialPortRequirements.OneSerialPort);


        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_750_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_750_WriteByte), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveWriteTimeoutNoData_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveWriteTimeoutSomeData_Write_byte_int_int), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveWriteTimeoutNoData_WriteByte), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveWriteTimeoutSomeData_WriteByte), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_0_Write_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_0_WriteByte), TCSupport.SerialPortRequirements.OneSerialPort);

        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_100_BeginWrite), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteTimeout_LargeWrite), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool WriteTimeout_DefaultValue()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Stream stream;
        bool retValue;

        com.Open();
        stream = com.BaseStream;

        Console.WriteLine("Verifying the default value of WriteTimeout");

        retValue = Eval(stream.WriteTimeout == -1,
            String.Format("Err_1707azhpbn Verifying the default value of WriteTimeout Expected={0} Actual={1} FAILED", -1, stream.WriteTimeout));

        com.Close();
        return retValue;
    }

    public bool WriteTimeout_AfterClose()
    {
        Console.WriteLine("Verifying setting WriteTimeout after the SerialPort was closed");

        return Eval(VerifyException(2048, null, typeof(System.ObjectDisposedException)), "Err_64658ajpba!!! Verifying setting WriteTimeout after the SerialPort was closed FAILED");
    }

    public bool WriteTimeout_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue WriteTimeout");

        return Eval(VerifyException(Int32.MinValue, typeof(System.ArgumentOutOfRangeException)), "Err_9988ahpab!!! Verifying Int32.MinValue WriteTimeout FAILED");
    }


    public bool WriteTimeout_NEG2()
    {
        Console.WriteLine("Verifying -2 WriteTimeout");

        return Eval(VerifyException(-2, typeof(System.ArgumentOutOfRangeException)), "Err_7789ajpx!!! Verifying -2 WriteTimeout FAILED");
    }


    public bool WriteTimeout_ZERO()
    {
        Console.WriteLine("Verifying 0 WriteTimeout");

        return Eval(VerifyException(0, typeof(System.ArgumentOutOfRangeException)), "Err_1790871hapba!!! Verifying 0 WriteTimeout FAILED");
    }

    public bool WriteTimeout_Default_Write_byte_int_int()
    {
        Console.WriteLine("Verifying default WriteTimeout with Write(byte[] buffer, int offset, int count)");

        return Eval(VerifyDefaultTimeout(new WriteMethodDelegate(Write_byte_int_int)),
            "Err_001!!! Verifying default WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
    }

    public bool WriteTimeout_Default_WriteByte()
    {
        Console.WriteLine("Verifying default WriteTimeout with WriteByte()");

        return Eval(VerifyDefaultTimeout(new WriteMethodDelegate(WriteByte)), "Err_003!!! Verifying default WriteTimeout with WriteByte() FAILED");
    }

    public bool WriteTimeout_Infinite_Write_byte_int_int()
    {
        Console.WriteLine("Verifying infinite WriteTimeout with Write(byte[] buffer, int offset, int count)");

        return Eval(VerifyLongTimeout(new WriteMethodDelegate(Write_byte_int_int), -1),
            "Err_006!!! Verifying infinite WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
    }

    public bool WriteTimeout_Infinite_WriteByte()
    {
        Console.WriteLine("Verifying infinite WriteTimeout with WriteByte()");

        return Eval(VerifyLongTimeout(new WriteMethodDelegate(WriteByte), -1), "Err_008!!! Verifying infinite WriteTimeout with WriteByte() FAILED");
    }

    public bool WriteTimeout_Int32MaxValue_Write_byte_int_int()
    {
        Console.WriteLine("Verifying Int32.MaxValue WriteTimeout with Write(byte[] buffer, int offset, int count)");

        return Eval(VerifyLongTimeout(new WriteMethodDelegate(Write_byte_int_int), Int32.MaxValue - 1),
            "Err_27072ahps!!! Verifying Int32.MaxValue WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
    }

    public bool WriteTimeout_Int32MaxValue_WriteByte()
    {
        Console.WriteLine("Verifying Int32.MaxValue WriteTimeout with WriteByte()");

        return Eval(VerifyLongTimeout(new WriteMethodDelegate(WriteByte), Int32.MaxValue - 1),
            "Err_79071aps!!! Verifying Int32.MaxValue WriteTimeout with WriteByte() FAILED");
    }

    public bool WriteTimeout_750_Write_byte_int_int()
    {
        Console.WriteLine("Verifying 750 WriteTimeout with Write(byte[] buffer, int offset, int count)");

        return Eval(VerifyTimeout(new WriteMethodDelegate(Write_byte_int_int), 750),
            "Err_27072ahps!!! Verifying 750 WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
    }

    public bool WriteTimeout_750_WriteByte()
    {
        Console.WriteLine("Verifying 750 WriteTimeout with WriteByte()");

        return Eval(VerifyTimeout(new WriteMethodDelegate(WriteByte), 750), "Err_79071aps!!! Verifying 750 WriteTimeout with WriteByte() FAILED");
    }


    public bool SuccessiveWriteTimeoutNoData_Write_byte_int_int()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        Stream stream;

        com.Open();
        com.Handshake = Handshake.RequestToSend;
        stream = com.BaseStream;
        stream.WriteTimeout = 850;

        Console.WriteLine("Verifying WriteTimeout={0} with successive call to Write(byte[], int, int) and no data", stream.WriteTimeout);

        try
        {
            stream.Write(new byte[DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_ARRAY_SIZE);
            retValue &= Eval(false, "Err_1707ahbap!!!: Write did not throw TimeouException when it timed out");
        }
        catch (TimeoutException) { }

        retValue &= Eval(VerifyTimeout(new WriteMethodDelegate(Write_byte_int_int), stream),
            "Err_7207qahps!!! Verifying with successive call to Write(byte[], int, int) and no data FAILED");

        com.Close();

        return retValue;
    }


    public bool SuccessiveWriteTimeoutSomeData_Write_byte_int_int()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncEnableRts asyncEnableRts = new AsyncEnableRts();
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncEnableRts.EnableRTS));
        bool retValue = true;
        Stream stream;
        int waitTime = 0;

        com1.Open();
        com1.Handshake = Handshake.RequestToSend;
        stream = com1.BaseStream;
        stream.WriteTimeout = SUCCESSIVE_WriteTimeout_SOMEDATA;

        Console.WriteLine("Verifying WriteTimeout={0} with successive call to Write(byte[], int, int) and some data being received in the first call", stream.WriteTimeout);

        //Call EnableRTS asynchronously this will enable RTS in the middle of the following write call allowing it to succeed 
        //before the timeout is reached
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        try
        {
            stream.Write(new byte[DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_ARRAY_SIZE);
        }
        catch (TimeoutException) { }

        asyncEnableRts.Stop();

        //Wait for the thread to finish
        while (t.IsAlive)
            System.Threading.Thread.Sleep(50);

        //Make sure there is no bytes in the buffer so the next call to write will timeout
        com1.DiscardInBuffer();

        retValue &= Eval(VerifyTimeout(new WriteMethodDelegate(Write_byte_int_int), stream),
            "Err_7017ahbap!!! Verifying with with successive call to write method and some data being received in the first call FAILED");

        com1.Close();

        return retValue;
    }

    public bool SuccessiveWriteTimeoutNoData_WriteByte()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        Stream stream;

        com.Open();
        com.Handshake = Handshake.RequestToSend;
        stream = com.BaseStream;
        stream.WriteTimeout = 850;

        Console.WriteLine("Verifying WriteTimeout={0} with successive call to WriteByte() and no data", stream.WriteTimeout);

        try
        {
            stream.WriteByte(DEFAULT_WRITE_BYTE);
            retValue &= Eval(false, "Err_1707abad!!!: Write did not throw TimeouException when it timed out");
        }
        catch (TimeoutException) { }

        retValue &= Eval(VerifyTimeout(new WriteMethodDelegate(WriteByte), stream),
            "Err_170717ahbpx!!! Verifying WriteTimeout={0} with successive call to WriteByte() and no data FAILED");

        com.Close();

        return retValue;
    }


    public bool SuccessiveWriteTimeoutSomeData_WriteByte()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncEnableRts asyncEnableRts = new AsyncEnableRts();
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncEnableRts.EnableRTS));
        bool retValue = true;
        Stream stream;
        int waitTime = 0;

        com1.Open();
        com1.Handshake = Handshake.RequestToSend;
        stream = com1.BaseStream;
        stream.WriteTimeout = SUCCESSIVE_WriteTimeout_SOMEDATA;

        Console.WriteLine("Verifying WriteTimeout={0} with successive call to WriteByte() and some data being received in the first call", stream.WriteTimeout);

        //Call EnableRTS asynchronously this will enable RTS in the middle of the following write call allowing it to succeed 
        //before the timeout is reached
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        try
        {
            stream.WriteByte(DEFAULT_WRITE_BYTE);
        }
        catch (TimeoutException) { }

        asyncEnableRts.Stop();

        //Wait for the thread to finish
        while (t.IsAlive)
            System.Threading.Thread.Sleep(50);

        //Make sure there is no bytes in the buffer so the next call to write will timeout
        com1.DiscardInBuffer();

        retValue &= Eval(VerifyTimeout(new WriteMethodDelegate(WriteByte), stream),
            "Err_44458ahbp!!! Verifying with with successive call to write method and some data being received in the first call FAILED");

        com1.Close();

        return retValue;
    }


    public bool WriteTimeout_0_Write_byte_int_int()
    {
        Console.WriteLine("Verifying 0 WriteTimeout with Write(byte[] buffer, int offset, int count)");

        return Eval(Verify0Timeout(new WriteMethodDelegate(Write_byte_int_int)),
            "Err_8858ahpsb!!! Verifying 0 WriteTimeout with Write(byte[] buffer, int offset, int count) FAILED");
    }

    public bool WriteTimeout_0_WriteByte()
    {
        Console.WriteLine("Verifying 0 WriteTimeout with WriteByte()");

        return Eval(Verify0Timeout(new WriteMethodDelegate(WriteByte)), "Err_51389apbhyu!!! Verifying 0 WriteTimeout with WriteByte() FAILED");
    }

    public bool WriteTimeout_100_BeginWrite()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Stream stream;
        IAsyncResult asyncResult;
        bool retValue = true;

        com.Open();
        com.Handshake = Handshake.RequestToSend;
        stream = com.BaseStream;
        stream.WriteTimeout = 100;

        Console.WriteLine("Verifying WriteTimeout of 100ms and BeginWrite does not timeout");

        asyncResult = stream.BeginWrite(new byte[DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_ARRAY_SIZE, null, null);
        System.Threading.Thread.Sleep(DEFAULT_WAIT_LONG_TIMEOUT);

        retValue &= Eval(!asyncResult.IsCompleted, "Err_17071abjap!!! Expected IsCompleted to be fase on IAsyncResult returned from BeginWrite");
        retValue &= Eval(!asyncResult.AsyncWaitHandle.WaitOne(10, false),
            "Err_00727agh!!! Expected AsyncWaitHandle.WaitOne to return fase on IAsyncResult returned from BeginWrite");

        return Eval(retValue, "Err_708737ahpbn!!! Verifying WriteTimeout of 100ms and BeginWrite does not timeout FAILED");
    }

    public bool WriteTimeout_LargeWrite()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Stream stream;
        bool retValue = true;

        com.Open();
        com.BaudRate = LARGEWRITE_BAUDRATE;
        com.Handshake = Handshake.RequestToSend;
        stream = com.BaseStream;
        stream.WriteTimeout = LARGEWRITE_TIMEOUT;


        Console.WriteLine("Verifying {0} WriteTimeout with Write(byte[] , int, int) and writing {1} bytes", LARGEWRITE_TIMEOUT, DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE);

        retValue &= Eval(VerifyTimeout(new WriteMethodDelegate(Write_byte_int_int_Large), stream),
            String.Format("Err_17071ahpba!!! Verifying {0} WriteTimeout with Write(byte[] , int, int) and writing {1} bytes FAILED",
            LARGEWRITE_TIMEOUT, DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE));

        com.Close();

        return retValue;
    }

    public class AsyncEnableRts
    {
        private bool _stop = false;

        public void EnableRTS()
        {
            lock (this)
            {
                SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
                int sleepPeriod = SUCCESSIVE_WriteTimeout_SOMEDATA / 2;

                //Sleep some random period with of a maximum duration of half the largest possible timeout value for a write method on COM1
                System.Threading.Thread.Sleep(sleepPeriod);

                com2.Open();
                com2.RtsEnable = true;

                while (!_stop)
                    System.Threading.Monitor.Wait(this);

                com2.RtsEnable = false;

                if (com2.IsOpen)
                    com2.Close();
            }
        }


        public void Stop()
        {
            lock (this)
            {
                _stop = true;
                System.Threading.Monitor.Pulse(this);
            }
        }
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyDefaultTimeout(WriteMethodDelegate writeMethod)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        Console.WriteLine("Serial port being used : " + com1.PortName);
        bool retValue = true;

        com1.Open();
        com1.Handshake = Handshake.RequestToSend;
        com1.BaseStream.ReadTimeout = 1;

        retValue &= Eval(-1 == com1.BaseStream.WriteTimeout,
            String.Format("Err_70217shpza!!! Expected WriteTimeout to be {0} actual {1}", -1, com1.BaseStream.WriteTimeout));

        retValue &= VerifyLongTimeout(writeMethod, com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool VerifyLongTimeout(WriteMethodDelegate writeMethod, int WriteTimeout)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        bool retValue = true;

        com1.Open();
        com1.Handshake = Handshake.RequestToSend;
        com1.BaseStream.ReadTimeout = 1;
        com1.BaseStream.WriteTimeout = 1;

        com1.BaseStream.WriteTimeout = WriteTimeout;

        retValue &= Eval(WriteTimeout == com1.BaseStream.WriteTimeout,
            String.Format("Err_7071ahpsb!!! Expected WriteTimeout to be {0} actual {1}", WriteTimeout, com1.BaseStream.WriteTimeout));

        retValue &= VerifyLongTimeout(writeMethod, com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool VerifyLongTimeout(WriteMethodDelegate writeMethod, SerialPort com1)
    {
        SerialPort com2 = null;
        WriteDelegateThread writeThread = new WriteDelegateThread(com1.BaseStream, writeMethod);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(writeThread.CallWrite));
        bool retValue = true;
        t.Start();
        System.Threading.Thread.Sleep(DEFAULT_WAIT_LONG_TIMEOUT);
        retValue &= Eval(t.IsAlive,
            String.Format("Err_17071ahpa!!! {0} terminated with a long timeout of {1}ms", writeMethod.Method.Name, com1.BaseStream.WriteTimeout));
        com1.Handshake = Handshake.None;
        while (t.IsAlive)
            System.Threading.Thread.Sleep(10);
        return retValue;
    }

    private bool VerifyTimeout(WriteMethodDelegate writeMethod, int WriteTimeout)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com1.Open();
        com1.Handshake = Handshake.RequestToSend;
        com1.BaseStream.ReadTimeout = 1;
        com1.BaseStream.WriteTimeout = 1;

        com1.BaseStream.WriteTimeout = WriteTimeout;

        retValue &= Eval(WriteTimeout == com1.BaseStream.WriteTimeout,
            String.Format("Err_236897ahpbm!!! Expected WriteTimeout to be {0} actaul {1}", WriteTimeout, com1.BaseStream.WriteTimeout));

        retValue &= VerifyTimeout(writeMethod, com1.BaseStream);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool VerifyTimeout(WriteMethodDelegate writeMethod, Stream stream)
    {
        System.Diagnostics.Stopwatch timer = new Stopwatch();
        int expectedTime = stream.WriteTimeout;
        int actualTime;
        double percentageDifference;
        bool retValue = true;

        //Warmup the write method. When called for the first time the write method seems to take much longer then subsequent calls
        timer.Start();
        try
        {
            writeMethod(stream);
        }
        catch (TimeoutException) { }
        timer.Stop();
        actualTime = (int)timer.ElapsedMilliseconds;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / (double)expectedTime);

        //Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
        retValue &= Eval(percentageDifference <= MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE,
            String.Format("Err_88558amuph!!!: The write method timedout in {0} expected {1} percentage difference: {2} when called for the first time",
                actualTime, expectedTime, percentageDifference));

        actualTime = 0;
        timer.Reset();

        //Perform the actual test verifying that the write method times out in approximately WriteTimeout milliseconds
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();
            try { writeMethod(stream); }
            catch (TimeoutException) { }
            timer.Stop();

            actualTime += (int)timer.ElapsedMilliseconds;
            timer.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / (double)expectedTime);

        //Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
        retValue &= Eval(percentageDifference <= MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE,
            String.Format("Err_56485ahpbz!!!: The write method timedout in {0} expected {1} percentage difference: {2}", actualTime, expectedTime, percentageDifference));

        return retValue;
    }

    private bool Verify0Timeout(WriteMethodDelegate writeMethod)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com1.Open();
        com1.Handshake = Handshake.RequestToSend;
        com1.BaseStream.ReadTimeout = 1;
        com1.BaseStream.WriteTimeout = 1;

        com1.BaseStream.WriteTimeout = 1;

        retValue &= Eval(1 == com1.BaseStream.WriteTimeout,
            String.Format("Err_72072ahps!!! Expected WriteTimeout to be {0} actaul {1}", 1, com1.BaseStream.WriteTimeout));

        retValue &= Verify0Timeout(writeMethod, com1.BaseStream);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool Verify0Timeout(WriteMethodDelegate writeMethod, Stream stream)
    {
        System.Diagnostics.Stopwatch timer = new Stopwatch();
        int expectedTime = stream.WriteTimeout;
        int actualTime;
        bool retValue = true;

        //Warmup the write method. When called for the first time the write method seems to take much longer then subsequent calls
        timer.Start();
        try
        {
            writeMethod(stream);
        }
        catch (TimeoutException) { }
        timer.Stop();
        actualTime = (int)timer.ElapsedMilliseconds;

        //Verify that the time the method took to timeout is less then the maximum acceptable time
        retValue &= Eval(actualTime <= MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT,
            String.Format("Err_277a0ahpsb!!!: With a timeout of 0 the write method timedout in {0} expected something less then {1} when called for the first time",
                actualTime, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT));

        actualTime = 0;
        timer.Reset();

        //Perform the actual test verifying that the write method times out in approximately WriteTimeout milliseconds
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();
            try { writeMethod(stream); }
            catch (TimeoutException) { }
            timer.Stop();

            actualTime += (int)timer.ElapsedMilliseconds;
            timer.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;

        //Verify that the time the method took to timeout is less then the maximum acceptable time
        retValue &= Eval(actualTime <= MAX_ACCEPTABLE_ZERO_TIMEOUT,
            String.Format("Err_112389ahbp!!!: With a timeout of 0 the write method timedout in {0} expected something less then {1}",
                actualTime, MAX_ACCEPTABLE_ZERO_TIMEOUT));

        return retValue;
    }

    private bool VerifyException(int readTimeout, System.Type expectedException)
    {
        return VerifyException(readTimeout, expectedException, expectedException);
    }

    private bool VerifyException(int readTimeout, System.Type expectedExceptionAfterOpen, System.Type expectedExceptionAfterClose)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        Stream stream;

        com.Open();
        stream = com.BaseStream;

        retValue &= Eval(VerifyException(stream, readTimeout, expectedExceptionAfterOpen), "Err_7789lkkh!!! Verifying Exception behavior when the port was opened failed");

        com.Close();

        retValue &= Eval(VerifyException(stream, readTimeout, expectedExceptionAfterClose), "Err_00872ahbo!!! Verifying Exception behavior when the port was closed failed");

        return retValue;
    }

    private bool VerifyException(Stream stream, int WriteTimeout, System.Type expectedException)
    {
        int origWriteTimeout = stream.WriteTimeout;
        bool retValue = true;

        try
        {
            stream.WriteTimeout = WriteTimeout;

            retValue &= Eval(null == expectedException,
                String.Format("Err_12707ahsp!!! Expected setting WriteTimeout={0} to throw {1} and nothing was thrown", WriteTimeout, expectedException));
        }
        catch (System.Exception e)
        {
            retValue &= Eval(null != expectedException,
                String.Format("Err_66498poyad!!! Expected setting WriteTimeout={0} NOT to throw an exception and the following was thrown\n{1}", WriteTimeout, e));

            retValue &= Eval(null != expectedException && e.GetType() == expectedException,
                String.Format("Err_444568ajhzpb!!! Expected setting WriteTimeout={0} to throw {1} an exception and the following was thrown\n{2}",
                    WriteTimeout, expectedException, e));
        }

        if (null == expectedException)
        {
            retValue &= Eval(WriteTimeout == stream.WriteTimeout,
                String.Format("Err_2707ashpb!!! Expected setting WriteTimeout={0} actual {1}", WriteTimeout, stream.WriteTimeout));
        }
        else
        {
            retValue &= Eval(origWriteTimeout == stream.WriteTimeout,
                String.Format("Err_12707aahpxzb!!! Expected setting WriteTimeout={0} actual {1}", origWriteTimeout, stream.WriteTimeout));
        }

        return retValue;
    }

    private void Write_byte_int_int(Stream stream)
    {
        stream.Write(new byte[DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_ARRAY_SIZE);
    }

    private void Write_byte_int_int_Large(Stream stream)
    {
        stream.Write(new byte[DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_LARGE_ARRAY_SIZE);
    }


    private void WriteByte(Stream stream)
    {
        stream.WriteByte(DEFAULT_WRITE_BYTE);
    }


    public class WriteDelegateThread
    {
        public WriteDelegateThread(Stream stream, WriteMethodDelegate writeMethod)
        {
            _stream = stream;
            _writeMethod = writeMethod;
        }

        public void CallWrite()
        {
            _writeMethod(_stream);
        }

        private WriteMethodDelegate _writeMethod;
        private Stream _stream;
    }

    private bool Eval(bool expression, string message)
    {
        if (!expression)
        {
            Console.WriteLine(message);
        }

        return expression;
    }

    #endregion
}

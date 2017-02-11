// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;

public class ReadTimeout_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadTimeout";
    public static readonly String s_strTFName = "ReadTimeout.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;


    //The default number of bytes to write with when testing timeout with Read(byte[], int, int)
    public static readonly int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;

    //The ammount of time to wait when expecting an long timeout
    public static readonly int DEFAULT_WAIT_LONG_TIMEOUT = 250;

    //The maximum acceptable time allowed when a read method should timeout immediately
    public static readonly int MAX_ACCEPTABLE_ZERO_TIMEOUT = 100;

    //The maximum acceptable time allowed when a read method should timeout immediately when it is called for the first time
    public static readonly int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 1000;

    //The maximum acceptable percentage difference allowed when a read method is called for the first time
    public static readonly double MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE = .5;

    //The maximum acceptable percentage difference allowed
    public static readonly double MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE = .15;

    public static readonly int SUCCESSIVE_READTIMEOUT_SOMEDATA = 950;

    public static readonly int NUM_TRYS = 5;

    public delegate void ReadMethodDelegate(Stream stream);

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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_DefaultValue), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_AfterClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_NEG2), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Default_Read_byte_int_int), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Default_ReadByte), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Infinite_Read_byte_int_int), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Infinite_ReadByte), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Int32MaxValue_Read_byte_int_int), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_Int32MaxValue_ReadByte), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_750_Read_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_750_ReadByte), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutNoData_Read_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutSomeData_Read_byte_int_int), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutNoData_ReadByte), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutSomeData_ReadByte), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_Read_byte_int_int), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_ReadByte), TCSupport.SerialPortRequirements.OneSerialPort);

        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_100_BeginRead), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_1ByteAvailable_Read_byte_int_int), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTimeout_0_1ByteAvailable_ReadByte), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ReadTimeout_DefaultValue()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Stream stream;
        bool retValue;

        com.Open();
        stream = com.BaseStream;

        Console.WriteLine("Verifying the default value of ReadTimeout");

        retValue = Eval(stream.ReadTimeout == -1,
            String.Format("Err_1707azhpbn Verifying the default value of ReadTimeout Expected={0} Actual={1} FAILED", -1, stream.ReadTimeout));

        com.Close();
        return retValue;
    }


    public bool ReadTimeout_AfterClose()
    {
        Console.WriteLine("Verifying setting ReadTimeout after the SerialPort was closed");

        return Eval(VerifyException(2048, null, typeof(System.ObjectDisposedException)), "Err_64658ajpba!!! Verifying setting ReadTime after the SerialPort was closed FAILED");
    }

    public bool ReadTimeout_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue ReadTimeout");

        return Eval(VerifyException(Int32.MinValue, typeof(System.ArgumentOutOfRangeException)), "Err_9988ahpab!!! Verifying Int32.MinValue ReadTimeout FAILED");
    }


    public bool ReadTimeout_NEG2()
    {
        Console.WriteLine("Verifying -2 ReadTimeout");

        return Eval(VerifyException(-2, typeof(System.ArgumentOutOfRangeException)), "Err_7789ajpx!!! Verifying -2 ReadTimeout FAILED");
    }

    public bool ReadTimeout_Default_Read_byte_int_int()
    {
        Console.WriteLine("Verifying default ReadTimeout with Read(byte[] buffer, int offset, int count)");

        return Eval(VerifyDefaultTimeout(new ReadMethodDelegate(Read_byte_int_int)),
            "Err_001!!! Verifying default ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
    }

    public bool ReadTimeout_Default_ReadByte()
    {
        Console.WriteLine("Verifying default ReadTimeout with ReadByte()");

        return Eval(VerifyDefaultTimeout(new ReadMethodDelegate(ReadByte)), "Err_003!!! Verifying default ReadTimeout with ReadByte() FAILED");
    }

    public bool ReadTimeout_Infinite_Read_byte_int_int()
    {
        Console.WriteLine("Verifying infinite ReadTimeout with Read(byte[] buffer, int offset, int count)");

        return Eval(VerifyLongTimeout(new ReadMethodDelegate(Read_byte_int_int), -1),
            "Err_006!!! Verifying infinite ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
    }

    public bool ReadTimeout_Infinite_ReadByte()
    {
        Console.WriteLine("Verifying infinite ReadTimeout with ReadByte()");

        return Eval(VerifyLongTimeout(new ReadMethodDelegate(ReadByte), -1), "Err_008!!! Verifying infinite ReadTimeout with ReadByte() FAILED");
    }

    public bool ReadTimeout_Int32MaxValue_Read_byte_int_int()
    {
        Console.WriteLine("Verifying Int32.MaxValue ReadTimeout with Read(byte[] buffer, int offset, int count)");

        return Eval(VerifyLongTimeout(new ReadMethodDelegate(Read_byte_int_int), Int32.MaxValue - 1),
            "Err_27072ahps!!! Verifying Int32.MaxValue ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
    }

    public bool ReadTimeout_Int32MaxValue_ReadByte()
    {
        Console.WriteLine("Verifying Int32.MaxValue ReadTimeout with ReadByte()");

        return Eval(VerifyLongTimeout(new ReadMethodDelegate(ReadByte), Int32.MaxValue - 1),
            "Err_79071aps!!! Verifying Int32.MaxValue ReadTimeout with ReadByte() FAILED");
    }

    public bool ReadTimeout_750_Read_byte_int_int()
    {
        Console.WriteLine("Verifying 750 ReadTimeout with Read(byte[] buffer, int offset, int count)");

        return Eval(VerifyTimeout(new ReadMethodDelegate(Read_byte_int_int), 750),
            "Err_27072ahps!!! Verifying 750 ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
    }

    public bool ReadTimeout_750_ReadByte()
    {
        Console.WriteLine("Verifying 750 ReadTimeout with ReadByte()");

        return Eval(VerifyTimeout(new ReadMethodDelegate(ReadByte), 750), "Err_79071aps!!! Verifying 750 ReadTimeout with ReadByte() FAILED");
    }


    public bool SuccessiveReadTimeoutNoData_Read_byte_int_int()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        Stream stream;

        com.Open();
        stream = com.BaseStream;
        stream.ReadTimeout = 850;

        Console.WriteLine("Verifying ReadTimeout={0} with successive call to Read(byte[], int, int) and no data", stream.ReadTimeout);

        try
        {
            stream.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
            retValue &= Eval(false, "Err_1707ahbap!!!: Read did not throw TimeouException when it timed out");
        }
        catch (TimeoutException) { }

        retValue &= Eval(VerifyTimeout(new ReadMethodDelegate(Read_byte_int_int), stream),
            "Err_7207qahps!!! Verifying with successive call to Read(byte[], int, int) and no data FAILED");

        com.Close();

        return retValue;
    }


    public bool SuccessiveReadTimeoutSomeData_Read_byte_int_int()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(WriteToCom1));
        bool retValue = true;
        Stream stream;

        com1.Open();
        stream = com1.BaseStream;
        stream.ReadTimeout = SUCCESSIVE_READTIMEOUT_SOMEDATA;

        Console.WriteLine("Verifying ReadTimeout={0} with successive call to Read(byte[], int, int) and some data being received in the first call", stream.ReadTimeout);

        //Call WriteToCom1 asynchronously this will write to com1 some time before the following call 
        //to a read method times out
        t.Start();

        try
        {
            stream.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
        }
        catch (TimeoutException) { }

        //Wait for the thread to finish
        while (t.IsAlive)
            System.Threading.Thread.Sleep(50);

        //Make sure there is no bytes in the buffer so the next call to read will timeout
        com1.DiscardInBuffer();

        retValue &= Eval(VerifyTimeout(new ReadMethodDelegate(Read_byte_int_int), stream),
            "Err_7017ahbap!!! Verifying with with successive call to read method and some data being received in the first call FAILED");

        com1.Close();

        return retValue;
    }

    public bool SuccessiveReadTimeoutNoData_ReadByte()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        Stream stream;

        com.Open();
        stream = com.BaseStream;
        stream.ReadTimeout = 850;

        Console.WriteLine("Verifying ReadTimeout={0} with successive call to ReadByte() and no data", stream.ReadTimeout);

        try
        {
            stream.ReadByte();
            retValue &= Eval(false, "Err_1707abad!!!: Read did not throw TimeouException when it timed out");
        }
        catch (TimeoutException) { }

        retValue &= Eval(VerifyTimeout(new ReadMethodDelegate(ReadByte), stream),
            "Err_170717ahbpx!!! Verifying ReadTimeout={0} with successive call to ReadByte() and no data FAILED");

        com.Close();

        return retValue;
    }


    public bool SuccessiveReadTimeoutSomeData_ReadByte()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(WriteToCom1));
        bool retValue = true;
        Stream stream;

        com1.Open();
        stream = com1.BaseStream;
        stream.ReadTimeout = SUCCESSIVE_READTIMEOUT_SOMEDATA;

        Console.WriteLine("Verifying ReadTimeout={0} with successive call to ReadByte() and some data being received in the first call", stream.ReadTimeout);

        //Call WriteToCom1 asynchronously this will write to com1 some time before the following call 
        //to a read method times out
        t.Start();

        try
        {
            stream.ReadByte();
        }
        catch (TimeoutException) { }

        //Wait for the thread to finish
        while (t.IsAlive)
            System.Threading.Thread.Sleep(50);

        //Make sure there is no bytes in the buffer so the next call to read will timeout
        com1.DiscardInBuffer();

        retValue &= Eval(VerifyTimeout(new ReadMethodDelegate(ReadByte), stream),
            "Err_44458ahbp!!! Verifying with with successive call to read method and some data being received in the first call FAILED");

        com1.Close();

        return retValue;
    }


    public bool ReadTimeout_0_Read_byte_int_int()
    {
        Console.WriteLine("Verifying 0 ReadTimeout with Read(byte[] buffer, int offset, int count)");

        return Eval(Verify0Timeout(new ReadMethodDelegate(Read_byte_int_int)),
            "Err_8858ahpsb!!! Verifying 0 ReadTimeout with Read(byte[] buffer, int offset, int count) FAILED");
    }

    public bool ReadTimeout_0_ReadByte()
    {
        Console.WriteLine("Verifying 0 ReadTimeout with ReadByte()");

        return Eval(Verify0Timeout(new ReadMethodDelegate(ReadByte)), "Err_51389apbhyu!!! Verifying 0 ReadTimeout with ReadByte() FAILED");
    }

    public bool ReadTimeout_100_BeginRead()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Stream stream;
        IAsyncResult asyncResult;
        bool retValue = true;

        com.Open();
        stream = com.BaseStream;
        stream.ReadTimeout = 100;

        Console.WriteLine("Verifying ReadTimeout of 100ms and BeginRead does not timeout");

        asyncResult = stream.BeginRead(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE, null, null);
        System.Threading.Thread.Sleep(DEFAULT_WAIT_LONG_TIMEOUT);

        retValue &= Eval(!asyncResult.IsCompleted, "Err_17071abjap!!! Expected IsCompleted to be fase on IAsyncResult returned from BeginRead");
        retValue &= Eval(!asyncResult.AsyncWaitHandle.WaitOne(10, false),
            "Err_00727agh!!! Expected AsyncWaitHandle.WaitOne to return fase on IAsyncResult returned from BeginRead");

        return Eval(retValue, "Err_708737ahpbn!!! Verifying ReadTimeout of 100ms and BeginRead does not timeout FAILED");
    }

    public bool ReadTimeout_0_1ByteAvailable_Read_byte_int_int()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Stream stream;
        bool retValue = true;
        byte[] rcvBytes = new byte[128];
        int bytesRead;

        Console.WriteLine("Verifying 0 ReadTimeout with Read(byte[] buffer, int offset, int count) and one byte available");

        com1.Open();
        com2.Open();
        stream = com1.BaseStream;
        stream.ReadTimeout = 0;

        com2.Write(new byte[] { 50 }, 0, 1);

        while (com1.BytesToRead == 0)
        {
            System.Threading.Thread.Sleep(50);
        }

        retValue &= Eval(1 == (bytesRead = com1.Read(rcvBytes, 0, rcvBytes.Length)),
            String.Format("Err_31597ahpba, Expected to Read to return 1 actual={0}", bytesRead));

        retValue &= Eval(50 == rcvBytes[0],
            String.Format("Err_778946ahba, Expected to read 50 actual={0}", rcvBytes[0]));

        com1.Close();
        com2.Close();

        return retValue;
    }

    public bool ReadTimeout_0_1ByteAvailable_ReadByte()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Stream stream;
        bool retValue = true;
        int byteRead;

        Console.WriteLine("Verifying 0 ReadTimeout with ReadByte() and one byte available");

        com1.Open();
        com2.Open();
        stream = com1.BaseStream;
        stream.ReadTimeout = 0;

        com2.Write(new byte[] { 50 }, 0, 1);

        while (com1.BytesToRead == 0)
        {
            System.Threading.Thread.Sleep(50);
        }

        retValue &= Eval(50 == (byteRead = com1.ReadByte()),
            String.Format("Err_05949aypa, Expected to Read to return 50 actual={0}", byteRead));

        com1.Close();
        com2.Close();

        return retValue;
    }

    private void WriteToCom1()
    {
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        byte[] xmitBuffer = new byte[1];
        int sleepPeriod = SUCCESSIVE_READTIMEOUT_SOMEDATA / 2;

        //Sleep some random period with of a maximum duration of half the largest possible timeout value for a read method on COM1
        System.Threading.Thread.Sleep(sleepPeriod);

        com2.Open();
        com2.Write(xmitBuffer, 0, xmitBuffer.Length);

        if (com2.IsOpen)
            com2.Close();
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyDefaultTimeout(ReadMethodDelegate readMethod)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;

        com1.Open();

        if (!com2.IsOpen)
            com2.Open();

        com1.BaseStream.WriteTimeout = 1;

        retValue &= Eval(-1 == com1.BaseStream.ReadTimeout,
            String.Format("Err_70217shpza!!! Expected ReadTimeout to be {0} actaul {1}", -1, com1.BaseStream.ReadTimeout));

        retValue &= VerifyLongTimeout(readMethod, com1, com2);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool VerifyLongTimeout(ReadMethodDelegate readMethod, int readTimeout)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        bool retValue = true;

        com1.Open();

        if (!com2.IsOpen)
            com2.Open();

        com1.BaseStream.WriteTimeout = 1;
        com1.BaseStream.ReadTimeout = 1;

        com1.BaseStream.ReadTimeout = readTimeout;

        retValue &= Eval(readTimeout == com1.BaseStream.ReadTimeout,
            String.Format("Err_7071ahpsb!!! Expected ReadTimeout to be {0} actaul {1}", readTimeout, com1.BaseStream.ReadTimeout));

        retValue &= VerifyLongTimeout(readMethod, com1, com2);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool VerifyLongTimeout(ReadMethodDelegate readMethod, SerialPort com1, SerialPort com2)
    {
        ReadDelegateThread readThread = new ReadDelegateThread(com1.BaseStream, readMethod);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(readThread.CallRead));
        bool retValue = true;

        t.Start();
        System.Threading.Thread.Sleep(DEFAULT_WAIT_LONG_TIMEOUT);

        retValue &= Eval(t.IsAlive,
            String.Format("Err_17071ahpa!!! {0} terminated with a long timeout of {1}ms", readMethod.Method.Name, com1.BaseStream.ReadTimeout));

        com2.Write(new byte[8], 0, 8);

        while (t.IsAlive)
            System.Threading.Thread.Sleep(10);

        return retValue;
    }

    private bool VerifyTimeout(ReadMethodDelegate readMethod, int readTimeout)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com1.Open();
        com1.BaseStream.WriteTimeout = 1;
        com1.BaseStream.ReadTimeout = 1;

        com1.BaseStream.ReadTimeout = readTimeout;

        retValue &= Eval(readTimeout == com1.BaseStream.ReadTimeout,
            String.Format("Err_236897ahpbm!!! Expected ReadTimeout to be {0} actaul {1}", readTimeout, com1.BaseStream.ReadTimeout));

        retValue &= VerifyTimeout(readMethod, com1.BaseStream);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool VerifyTimeout(ReadMethodDelegate readMethod, Stream stream)
    {
        System.Diagnostics.Stopwatch timer = new Stopwatch();
        int expectedTime = stream.ReadTimeout;
        int actualTime;
        double percentageDifference;
        bool retValue = true;

        //Warmup the read method. When called for the first time the read method seems to take much longer then subsequent calls
        timer.Start();
        try
        {
            readMethod(stream);
        }
        catch (TimeoutException) { }
        timer.Stop();
        actualTime = (int)timer.ElapsedMilliseconds;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / (double)expectedTime);

        //Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
        retValue &= Eval(percentageDifference <= MAX_ACCEPTABLE_WARMUP_PERCENTAGE_DIFFERENCE,
            String.Format("Err_88558amuph!!!: The read method timedout in {0} expected {1} percentage difference: {2} when called for the first time",
                actualTime, expectedTime, percentageDifference));

        actualTime = 0;
        timer.Reset();

        //Perform the actual test verifying that the read method times out in approximately ReadTime milliseconds
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();
            try { readMethod(stream); }
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
            String.Format("Err_56485ahpbz!!!: The read method timedout in {0} expected {1} percentage difference: {2}", actualTime, expectedTime, percentageDifference));

        return retValue;
    }

    private bool Verify0Timeout(ReadMethodDelegate readMethod)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        com1.Open();
        com1.BaseStream.WriteTimeout = 1;
        com1.BaseStream.ReadTimeout = 1;

        com1.BaseStream.ReadTimeout = 0;

        retValue &= Eval(0 == com1.BaseStream.ReadTimeout,
            String.Format("Err_72072ahps!!! Expected ReadTimeout to be {0} actaul {1}", 0, com1.BaseStream.ReadTimeout));

        retValue &= Verify0Timeout(readMethod, com1.BaseStream);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool Verify0Timeout(ReadMethodDelegate readMethod, Stream stream)
    {
        System.Diagnostics.Stopwatch timer = new Stopwatch();
        int expectedTime = stream.ReadTimeout;
        int actualTime;
        bool retValue = true;

        //Warmup the read method. When called for the first time the read method seems to take much longer then subsequent calls
        timer.Start();
        try
        {
            readMethod(stream);
        }
        catch (TimeoutException) { }
        timer.Stop();
        actualTime = (int)timer.ElapsedMilliseconds;

        //Verify that the time the method took to timeout is less then the maximum acceptable time
        retValue &= Eval(actualTime <= MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT,
            String.Format("Err_277a0ahpsb!!!: With a timeout of 0 the read method timedout in {0} expected something less then {1} when called for the first time",
                actualTime, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT));

        actualTime = 0;
        timer.Reset();

        //Perform the actual test verifying that the read method times out in approximately ReadTime milliseconds
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();
            try { readMethod(stream); }
            catch (TimeoutException) { }
            timer.Stop();

            actualTime += (int)timer.ElapsedMilliseconds;
            timer.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;

        //Verify that the time the method took to timeout is less then the maximum acceptable time
        retValue &= Eval(actualTime <= MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT,
            String.Format("Err_112389ahbp!!!: With a timeout of 0 the read method timedout in {0} expected something less then {1}",
                actualTime, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT));

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

    private bool VerifyException(Stream stream, int readTimeout, System.Type expectedException)
    {
        int origReadTimeout = stream.ReadTimeout;
        bool retValue = true;

        try
        {
            stream.ReadTimeout = readTimeout;

            retValue &= Eval(null == expectedException,
                String.Format("Err_12707ahsp!!! Expected setting ReadTimeout={0} to throw {1} and nothing was thrown", readTimeout, expectedException));
        }
        catch (System.Exception e)
        {
            retValue &= Eval(null != expectedException,
                String.Format("Err_66498poyad!!! Expected setting ReadTimeout={0} NOT to throw an exception and the following was thrown\n{1}", readTimeout, e));

            retValue &= Eval(null != expectedException && e.GetType() == expectedException,
                String.Format("Err_444568ajhzpb!!! Expected setting ReadTimeout={0} to throw {1} an exception and the following was thrown\n{2}",
                    readTimeout, expectedException, e));
        }

        if (null == expectedException)
        {
            retValue &= Eval(readTimeout == stream.ReadTimeout,
                String.Format("Err_2707ashpb!!! Expected setting ReadTimeout={0} actual {1}", readTimeout, stream.ReadTimeout));
        }
        else
        {
            retValue &= Eval(origReadTimeout == stream.ReadTimeout,
                String.Format("Err_12707aahpxzb!!! Expected setting ReadTimeout={0} actual {1}", origReadTimeout, stream.ReadTimeout));
        }

        return retValue;
    }

    private void Read_byte_int_int(Stream stream)
    {
        stream.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
    }

    private void ReadByte(Stream stream)
    {
        stream.ReadByte();
    }


    public class ReadDelegateThread
    {
        public ReadDelegateThread(Stream stream, ReadMethodDelegate readMethod)
        {
            _stream = stream;
            _readMethod = readMethod;
        }

        public void CallRead()
        {
            _readMethod(_stream);
        }

        private ReadMethodDelegate _readMethod;
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

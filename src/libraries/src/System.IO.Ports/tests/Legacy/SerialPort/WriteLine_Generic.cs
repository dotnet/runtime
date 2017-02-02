// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class WriteLine
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/20 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.WriteLine(string)";
    public static readonly String s_strTFName = "WriteLine_Generic.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Set bounds for random timeout values.
    //If the min is to low write will not timeout accurately and the testcase will fail
    public static int minRandomTimeout = 250;

    //If the max is to large then the testcase will take forever to run
    public static int maxRandomTimeout = 2000;

    //If the percentage difference between the expected timeout and the actual timeout
    //found through Stopwatch is greater then 10% then the timeout value was not correctly
    //to the write method and the testcase fails.
    public static double maxPercentageDifference = .15;

    //The string used when we expect the ReadCall to throw an exception for something other
    //then the contents of the string itself
    public static readonly string DEFAULT_STRING = "DEFAULT_STRING";

    //The string size used when veryifying BytesToWrite 
    public static readonly int STRING_SIZE_BYTES_TO_WRITE = 4;

    //The string size used when veryifying Handshake 
    public static readonly int STRING_SIZE_HANDSHAKE = 8;
    public static readonly int NUM_TRYS = 5;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        WriteLine objTest = new WriteLine();
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
        _numErrors++;
        Console.WriteLine("\nAn unhandled exception was thrown and not caught in the app domain: \n{0}", e.ExceptionObject);
        Console.WriteLine("Test FAILED!!!\n");

        Environment.ExitCode = 101;
    }

    public bool RunTest()
    {
        bool retValue = true;
        TCSupport tcSupport = new TCSupport();

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteWithoutOpen), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteAfterFailedOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteAfterClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SimpleTimeout), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeout), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutWithWriteSucceeding), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWriteSuccessive),
            TCSupport.SerialPortRequirements.OneSerialPort, TCSupport.OperatingSystemRequirements.NotWin9X);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_None), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSend), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_XOnXOff), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSendXOnXOff), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool WriteWithoutOpen()
    {
        SerialPort com = new SerialPort();

        Console.WriteLine("Case WriteWithoutOpen : Verifying write method throws System.InvalidOperationException without a call to Open()");

        if (!VerifyWriteException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_001!!! Verifying write method throws exception without a call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool WriteAfterFailedOpen()
    {
        SerialPort com = new SerialPort("BAD_PORT_NAME");

        Console.WriteLine("Case WriteAfterFailedOpen : Verifying write method throws exception with a failed call to Open()");

        //Since the PortName is set to a bad port name Open will thrown an exception
        //however we don't care what it is since we are verfifying a write method
        try
        {
            com.Open();
        }
        catch (System.Exception)
        {
        }
        if (!VerifyWriteException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_002!!! Verifying write method throws exception with a failed call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool WriteAfterClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Case WriteAfterClose : Verifying write method throws exception after a call to Close()");
        com.Open();
        com.Close();

        if (!VerifyWriteException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_003!!! Verifying write method throws exception after a call to Close() FAILED");
            return false;
        }

        return true;
    }


    public bool SimpleTimeout()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        byte[] XOffBuffer = new Byte[1];

        XOffBuffer[0] = 19;

        com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        com1.Handshake = Handshake.XOnXOff;

        Console.WriteLine("Case SimpleTimeout : Verifying WriteTimeout={0}", com1.WriteTimeout);

        com1.Open();
        com2.Open();

        com2.Write(XOffBuffer, 0, 1);
        System.Threading.Thread.Sleep(250);
        com2.Close();

        if (!VerifyTimeout(com1))
        {
            Console.WriteLine("Err_004!!! Verifying timeout FAILED");
            return false;
        }

        return true;
    }


    public bool SuccessiveReadTimeout()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        bool retValue = true;

        com.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        com.Handshake = Handshake.RequestToSendXOnXOff;
        com.Encoding = System.Text.Encoding.Unicode;

        Console.WriteLine("Case SuccessiveReadTimeout : Verifying WriteTimeout={0} with successive call to write method", com.WriteTimeout);
        com.Open();

        try
        {
            com.WriteLine(DEFAULT_STRING);
        }
        catch (System.TimeoutException)
        {
        }
        catch (System.Exception e)
        {
            Console.WriteLine("The following exception was thrown: {0}", e.GetType());
            retValue = false;
        }

        retValue &= VerifyTimeout(com);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying WriteTimeout with successive call to write method FAILED");
            return false;
        }

        return true;
    }


    public bool SuccessiveReadTimeoutWithWriteSucceeding()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        AsyncEnableRts asyncEnableRts = new AsyncEnableRts();
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncEnableRts.EnableRTS));
        bool retValue = true;
        int waitTime = 0;

        com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        com1.Handshake = Handshake.RequestToSend;
        com1.Encoding = new System.Text.UTF8Encoding();

        Console.WriteLine("Case SuccessiveReadTimeoutWithWriteSucceeding : Verifying WriteTimeout={0} with successive call to write method with the write succeeding sometime before it's timeout", com1.WriteTimeout);
        com1.Open();

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
            com1.WriteLine(DEFAULT_STRING);
        }
        catch (System.TimeoutException)
        {
        }
        catch (System.Exception e)
        {
            Console.WriteLine("The following exception was thrown: {0}", e.GetType());
            retValue = false;
        }

        asyncEnableRts.Stop();
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        retValue &= VerifyTimeout(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying WriteTimeout with  successive call to write method FAILED");
            return false;
        }

        return true;
    }


    private bool BytesToWrite()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com, STRING_SIZE_BYTES_TO_WRITE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndStr.WriteRndStr));
        bool retValue = true;
        int numNewLineBytes;
        int waitTime = 0;

        Console.WriteLine("Case BytesToWrite : Verifying BytesToWrite with one call to Write");

        com.Handshake = Handshake.RequestToSend;
        com.Open();
        com.WriteTimeout = 500;

        numNewLineBytes = com.Encoding.GetByteCount(com.NewLine.ToCharArray());

        //Write a random string asynchronously so we can verify some things while the write call is blocking
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;
        while (STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes > com.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes != com.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1} after first write", STRING_SIZE_BYTES_TO_WRITE, com.BytesToWrite);
        }

        //Wait for write method to timeout
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (com.IsOpen)
            com.Close();

        if (!retValue)
            Console.WriteLine("Err_007!!! Verifying BytesToWrite with one call to Write FAILED");

        return retValue;
    }


    private bool BytesToWriteSuccessive()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com, STRING_SIZE_BYTES_TO_WRITE);
        System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndStr.WriteRndStr));
        System.Threading.Thread t2 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndStr.WriteRndStr));
        bool retValue = true;
        int numNewLineBytes;
        int waitTime = 0;

        Console.WriteLine("Case BytesToWriteSuccessive : Verifying BytesToWrite with successive calls to Write");

        com.Handshake = Handshake.RequestToSend;
        com.Open();
        com.WriteTimeout = 1000;
        numNewLineBytes = com.Encoding.GetByteCount(com.NewLine.ToCharArray());

        //Write a random string asynchronously so we can verify some things while the write call is blocking
        t1.Start();
        waitTime = 0;
        while (t1.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;
        while (STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes > com.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes != com.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1} after first write", STRING_SIZE_BYTES_TO_WRITE, com.BytesToWrite);
        }

        //Write a random string asynchronously so we can verify some things while the write call is blocking
        t2.Start();
        waitTime = 0;
        while (t2.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;
        while ((STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes) * 2 > com.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if ((STRING_SIZE_BYTES_TO_WRITE + numNewLineBytes) * 2 != com.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1} after second write", STRING_SIZE_BYTES_TO_WRITE * 2, com.BytesToWrite);
        }

        //Wait for both write methods to timeout
        while (t1.IsAlive || t2.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (com.IsOpen)
            com.Close();

        if (!retValue)
            Console.WriteLine("Err_008!!! Verifying BytesToWrite with successive calls to Write FAILED");

        return retValue;
    }


    public bool Handshake_None()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com, STRING_SIZE_HANDSHAKE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndStr.WriteRndStr));
        bool retValue = true;
        int waitTime;

        //Write a random string asynchronously so we can verify some things while the write call is blocking
        Console.WriteLine("Case Handshake_None : Verifying Handshake=None");

        com.Open();
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        //Wait for both write methods to timeout
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (0 != com.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite=0 actual {0}", com.BytesToWrite);
        }

        if (!retValue)
            Console.WriteLine("Err_009!!! Verifying Handshake=None FAILED");

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool Handshake_RequestToSend()
    {
        bool retValue = true;
        Console.WriteLine("Case Handshake_RequestToSend : Verifying Handshake=RequestToSend");
        retValue &= Verify_Handshake(Handshake.RequestToSend);
        if (!retValue)
            Console.WriteLine("Err_010!!! Verifying Handshake=RequestToSend FAILED");

        return retValue;
    }


    public bool Handshake_XOnXOff()
    {
        bool retValue = true;
        Console.WriteLine("Case Handshake_XOnXOff : Verifying Handshake=XOnXOff");
        retValue &= Verify_Handshake(Handshake.XOnXOff);
        if (!retValue)
            Console.WriteLine("Err_011!!! Verifying Handshake=XOnXOff FAILED");

        return retValue;
    }


    public bool Handshake_RequestToSendXOnXOff()
    {
        bool retValue = true;
        Console.WriteLine("Case Handshake_RequestToSendXOnXOff : Verifying Handshake=RequestToSendXOnXOff");
        retValue &= Verify_Handshake(Handshake.RequestToSendXOnXOff);
        if (!retValue)
            Console.WriteLine("Err_012!!! Verifying Handshake=RequestToSendXOnXOff FAILED");

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
                Random rndGen = new Random(-55);
                int sleepPeriod = rndGen.Next(minRandomTimeout, maxRandomTimeout / 2);

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



    public class AsyncWriteRndStr
    {
        private SerialPort _com;
        private int _strSize;


        public AsyncWriteRndStr(SerialPort com, int strSize)
        {
            _com = com;
            _strSize = strSize;
        }


        public void WriteRndStr()
        {
            String stringToWrite = TCSupport.GetRandomString(_strSize, TCSupport.CharacterOptions.Surrogates);

            try
            {
                _com.WriteLine(stringToWrite);
            }
            catch (System.TimeoutException)
            {
            }
        }
    }
    #endregion

    #region Verification for Test Cases
    public static bool VerifyWriteException(SerialPort com, Type expectedException)
    {
        bool retValue = true;

        try
        {
            com.WriteLine(DEFAULT_STRING);
            Console.WriteLine("ERROR!!!: No Exception was thrown");
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


    private bool VerifyTimeout(SerialPort com)
    {
        System.Diagnostics.Stopwatch timer = new Stopwatch();
        int expectedTime = com.WriteTimeout;
        int actualTime = 0;
        double percentageDifference;
        bool retValue = true;

        try
        {
            com.WriteLine(DEFAULT_STRING); //Warm up write method
        }
        catch (System.TimeoutException) { }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();

            try
            {
                com.WriteLine(DEFAULT_STRING);
            }
            catch (System.TimeoutException) { }
            catch (System.Exception e)
            {
                Console.WriteLine("The following exception was thrown: {0}", e.GetType());
                retValue = false;
            }

            timer.Stop();
            actualTime += (int)timer.ElapsedMilliseconds;
            timer.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / (double)expectedTime);

        //Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
        if (maxPercentageDifference < percentageDifference)
        {
            Console.WriteLine("ERROR!!!: The write method timedout in {0} expected {1} percentage difference: {2}", actualTime, expectedTime, percentageDifference);
            retValue = false;
        }

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool Verify_Handshake(Handshake handshake)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        AsyncWriteRndStr asyncWriteRndStr = new AsyncWriteRndStr(com1, STRING_SIZE_HANDSHAKE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndStr.WriteRndStr));
        bool retValue = true;
        byte[] XOffBuffer = new Byte[1];
        byte[] XOnBuffer = new Byte[1];

        XOffBuffer[0] = 19;
        XOnBuffer[0] = 17;

        int numNewLineBytes;
        int waitTime = 0;

        Console.WriteLine("Verifying Handshake={0}", handshake);

        com1.Handshake = handshake;
        com1.Open();
        com2.Open();

        numNewLineBytes = com1.Encoding.GetByteCount(com1.NewLine.ToCharArray());

        //Setup to ensure write will bock with type of handshake method being used
        if (Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake)
        {
            com2.RtsEnable = false;
        }

        if (Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake)
        {
            com2.Write(XOffBuffer, 0, 1);
            System.Threading.Thread.Sleep(250);
        }

        //Write a random string asynchronously so we can verify some things while the write call is blocking
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;

        while (STRING_SIZE_HANDSHAKE + numNewLineBytes > com1.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        //Verify that the correct number of bytes are in the buffer
        if (STRING_SIZE_HANDSHAKE + numNewLineBytes != com1.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1}", STRING_SIZE_HANDSHAKE, com1.BytesToWrite);
        }

        //Verify that CtsHolding is false if the RequestToSend or RequestToSendXOnXOff handshake method is used
        if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) && com1.CtsHolding)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted CtsHolding={0} actual {1}", false, com1.CtsHolding);
        }

        //Setup to ensure write will succeed
        if (Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake)
        {
            com2.RtsEnable = true;
        }

        if (Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake)
        {
            com2.Write(XOnBuffer, 0, 1);
        }

        //Wait till write finishes
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        //Verify that the correct number of bytes are in the buffer
        if (0 != com1.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite=0 actual {0}", com1.BytesToWrite);
        }

        //Verify that CtsHolding is true if the RequestToSend or RequestToSendXOnXOff handshake method is used
        if ((Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake) && !com1.CtsHolding)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted CtsHolding={0} actual {1}", true, com1.CtsHolding);
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    #endregion
}

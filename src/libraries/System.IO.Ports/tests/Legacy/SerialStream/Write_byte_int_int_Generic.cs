// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class Write_byte_int_int
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/17 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.Write(byte[], int, int)";
    public static readonly String s_strTFName = "Write_byte_int_int_Generic.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Set bounds fore random timeout values.
    //If the min is to low write will not timeout accurately and the testcase will fail
    public static int minRandomTimeout = 250;

    //If the max is to large then the testcase will take forever to run
    public static int maxRandomTimeout = 2000;

    //If the percentage difference between the expected timeout and the actual timeout
    //found through Stopwatch is greater then 10% then the timeout value was not correctly
    //to the write method and the testcase fails.
    public static double maxPercentageDifference = .15;

    //The byte size used when veryifying exceptions that write will throw 
    public static readonly int BYTE_SIZE_EXCEPTION = 4;

    //The byte size used when veryifying timeout 
    public static readonly int BYTE_SIZE_TIMEOUT = 4;

    //The byte size used when veryifying BytesToWrite 
    public static readonly int BYTE_SIZE_BYTES_TO_WRITE = 4;

    //The bytes size used when veryifying Handshake 
    public static readonly int BYTE_SIZE_HANDSHAKE = 8;

    //The maximum time to wait
    public static readonly int MAX_WAIT = 2000;

    public static readonly int NUM_TRYS = 5;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Write_byte_int_int objTest = new Write_byte_int_int();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteAfterClose), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteAfterBaseStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Timeout), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveWriteTimeout), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveWriteTimeoutWithWriteSucceeding), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWriteSuccessive),
            TCSupport.SerialPortRequirements.OneSerialPort, TCSupport.OperatingSystemRequirements.NotWin9X);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_None), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSend), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_XOnXOff), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSendXOnXOff), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool WriteAfterClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying write method throws exception after a call to Cloes()");

        com.Open();
        serialStream = com.BaseStream;
        com.Close();

        if (!VerifyWriteException(serialStream, typeof(System.ObjectDisposedException)))
        {
            Console.WriteLine("Err_003!!! Verifying write method throws exception after a call to Cloes() FAILED");
            return false;
        }

        return true;
    }


    public bool WriteAfterBaseStreamClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying write method throws exception after a call to BaseStream.Close()");

        com.Open();
        serialStream = com.BaseStream;
        com.BaseStream.Close();

        if (!VerifyWriteException(serialStream, typeof(System.ObjectDisposedException)))
        {
            Console.WriteLine("Err_004!!! Verifying write method throws exception after a call to BaseStream.Close() FAILED");
            return false;
        }

        return true;
    }


    public bool Timeout()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        byte[] XOffBuffer = new Byte[1];

        XOffBuffer[0] = 19;

        com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        com1.Handshake = Handshake.XOnXOff;

        Console.WriteLine("Verifying WriteTimeout={0}", com1.WriteTimeout);

        com1.Open();
        com2.Open();

        com2.BaseStream.Write(XOffBuffer, 0, 1);
        System.Threading.Thread.Sleep(250);
        com2.Close();

        if (!VerifyTimeout(com1))
        {
            Console.WriteLine("Err_004!!! Verifying timeout FAILED");
            return false;
        }

        return true;
    }


    public bool SuccessiveWriteTimeout()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        bool retValue = true;

        com.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        com.Handshake = Handshake.RequestToSendXOnXOff;
        //		com.Encoding = new System.Text.UTF7Encoding();
        com.Encoding = System.Text.Encoding.Unicode;

        Console.WriteLine("Verifying WriteTimeout={0} with successive call to write method", com.WriteTimeout);
        com.Open();

        try
        {
            com.BaseStream.Write(new byte[BYTE_SIZE_TIMEOUT], 0, BYTE_SIZE_TIMEOUT);
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


    public bool SuccessiveWriteTimeoutWithWriteSucceeding()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        AsyncEnableRts asyncEnableRts = new AsyncEnableRts();
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncEnableRts.EnableRTS));
        bool retValue = true;
        int waitTime;

        com1.WriteTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        com1.Handshake = Handshake.RequestToSend;
        com1.Encoding = new System.Text.UTF8Encoding();

        Console.WriteLine("Verifying WriteTimeout={0} with successive call to write method with the write succeeding sometime before it's timeout", com1.WriteTimeout);
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
            com1.BaseStream.Write(new byte[BYTE_SIZE_TIMEOUT], 0, BYTE_SIZE_TIMEOUT);
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
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com, BYTE_SIZE_BYTES_TO_WRITE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int waitTime = 0;

        Console.WriteLine("Verifying BytesToWrite with one call to Write");

        com.Handshake = Handshake.RequestToSend;
        com.Open();
        com.WriteTimeout = 500;

        //Write a random byte[] asynchronously so we can verify some things while the write call is blocking
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;

        while (BYTE_SIZE_BYTES_TO_WRITE > com.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (BYTE_SIZE_BYTES_TO_WRITE != com.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1} after first write", BYTE_SIZE_BYTES_TO_WRITE, com.BytesToWrite);
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
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com, BYTE_SIZE_BYTES_TO_WRITE);
        System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        System.Threading.Thread t2 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int waitTime = 0;

        Console.WriteLine("Verifying BytesToWrite with successive calls to Write");

        com.Handshake = Handshake.RequestToSend;
        com.Open();
        com.WriteTimeout = 4000;

        //Write a random byte[] asynchronously so we can verify some things while the write call is blocking
        t1.Start();
        waitTime = 0;

        while (t1.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;

        while (BYTE_SIZE_BYTES_TO_WRITE > com.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (BYTE_SIZE_BYTES_TO_WRITE != com.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1} after first write", BYTE_SIZE_BYTES_TO_WRITE, com.BytesToWrite);
        }

        //Write a random byte[] asynchronously so we can verify some things while the write call is blocking
        t2.Start();
        waitTime = 0;

        while (t2.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;

        while (BYTE_SIZE_BYTES_TO_WRITE * 2 > com.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (BYTE_SIZE_BYTES_TO_WRITE * 2 != com.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1} after second write", BYTE_SIZE_BYTES_TO_WRITE * 2, com.BytesToWrite);
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
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com, BYTE_SIZE_HANDSHAKE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int waitTime;

        //Write a random byte[] asynchronously so we can verify some things while the write call is blocking
        Console.WriteLine("Verifying Handshake=None");

        com.Open();
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        //Wait for write method to timeout
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

        retValue &= Verify_Handshake(Handshake.RequestToSend);
        if (!retValue)
            Console.WriteLine("Err_010!!! Verifying Handshake=RequestToSend FAILED");

        return retValue;
    }


    public bool Handshake_XOnXOff()
    {
        bool retValue = true;

        retValue &= Verify_Handshake(Handshake.XOnXOff);
        if (!retValue)
            Console.WriteLine("Err_011!!! Verifying Handshake=XOnXOff FAILED");

        return retValue;
    }


    public bool Handshake_RequestToSendXOnXOff()
    {
        bool retValue = true;

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



    public class AsyncWriteRndByteArray
    {
        private SerialPort _com;
        private int _byteLength;


        public AsyncWriteRndByteArray(SerialPort com, int byteLength)
        {
            this._com = com;
            this._byteLength = byteLength;
        }


        public void WriteRndByteArray()
        {
            byte[] buffer = new byte[_byteLength];
            Random rndGen = new Random(-55);

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)rndGen.Next(0, 256);
            }

            try
            {
                _com.BaseStream.Write(buffer, 0, buffer.Length);
            }
            catch (System.TimeoutException)
            {
            }
        }
    }
    #endregion

    #region Verification for Test Cases
    public static bool VerifyWriteException(System.IO.Stream serialStream, Type expectedException)
    {
        bool retValue = true;

        try
        {
            serialStream.Write(new byte[BYTE_SIZE_EXCEPTION], 0, BYTE_SIZE_EXCEPTION);

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
            com.BaseStream.Write(new byte[BYTE_SIZE_TIMEOUT], 0, BYTE_SIZE_TIMEOUT); //Warm up write method
        }
        catch (System.TimeoutException) { }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();
            try
            {
                com.BaseStream.Write(new byte[BYTE_SIZE_TIMEOUT], 0, BYTE_SIZE_TIMEOUT);
            }
            catch (System.TimeoutException)
            {
            }
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
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, BYTE_SIZE_HANDSHAKE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        byte[] XOffBuffer = new Byte[1];
        byte[] XOnBuffer = new Byte[1];
        int waitTime = 0;

        XOffBuffer[0] = 19;
        XOnBuffer[0] = 17;

        Console.WriteLine("Verifying Handshake={0}", handshake);
        com1.Handshake = handshake;

        com1.Open();
        com2.Open();

        //Setup to ensure write will bock with type of handshake method being used
        if (Handshake.RequestToSend == handshake || Handshake.RequestToSendXOnXOff == handshake)
        {
            com2.RtsEnable = false;
        }

        if (Handshake.XOnXOff == handshake || Handshake.RequestToSendXOnXOff == handshake)
        {
            com2.BaseStream.Write(XOffBuffer, 0, 1);
            System.Threading.Thread.Sleep(250);
        }

        //Write a random byte asynchronously so we can verify some things while the write call is blocking
        t.Start();
        waitTime = 0;

        while (t.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < 2000)
        { //Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        waitTime = 0;
        while (BYTE_SIZE_HANDSHAKE > com1.BytesToWrite && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        //Verify that the correct number of bytes are in the buffer
        if (BYTE_SIZE_HANDSHAKE != com1.BytesToWrite)
        {
            retValue = false;
            Console.WriteLine("ERROR!!! Expcted BytesToWrite={0} actual {1}", BYTE_SIZE_HANDSHAKE, com1.BytesToWrite);
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
            com2.BaseStream.Write(XOnBuffer, 0, 1);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class BeginWrite
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/17 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.BeginWrite(byte[], int, int, AsyncCallback callback, object state)";
    public static readonly String s_strTFName = "BeginWrite_Generic.cs";
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
    public static readonly int MAX_WAIT = 250;
    public static readonly int ITERATION_WAIT = 50;
    public static readonly int NUM_TRYS = 5;

    private const int MAX_WAIT_THREAD = 1000;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        BeginWrite objTest = new BeginWrite();
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
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteAfterSerialStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Timeout), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWriteSuccessive), TCSupport.SerialPortRequirements.NullModem);

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


    public bool WriteAfterSerialStreamClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying write method throws exception after a call to BaseStream.Close()");

        com.Open();
        serialStream = com.BaseStream;
        com.BaseStream.Close();

        if (!VerifyWriteException(serialStream, typeof(System.ObjectDisposedException)))
        {
            Console.WriteLine("Err_003!!! Verifying write method throws exception after a call to BaseStream.Close() FAILED");
            return false;
        }

        return true;
    }


    public bool Timeout()
    {
        Random rndGen = new Random(-55);
        int writeTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);

        Console.WriteLine("Verifying WriteTimeout={0}", writeTimeout);

        if (!VerifyTimeout(writeTimeout))
        {
            Console.WriteLine("Err_004!!! Verifying timeout FAILED");
            return false;
        }

        return true;
    }

    private bool BytesToWrite()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        System.IAsyncResult writeAsyncResult;
        int elapsedTime = 0;

        Console.WriteLine("Verifying BytesToWrite with one call to Write");

        com1.Handshake = Handshake.RequestToSend;
        com1.Open();
        com2.Open();
        com1.WriteTimeout = MAX_WAIT;

        writeAsyncResult = WriteRndByteArray(com1, BYTE_SIZE_BYTES_TO_WRITE);
        System.Threading.Thread.Sleep(100);

        while (elapsedTime < MAX_WAIT && BYTE_SIZE_BYTES_TO_WRITE != com1.BytesToWrite)
        {
            elapsedTime += ITERATION_WAIT;
            System.Threading.Thread.Sleep(ITERATION_WAIT);
        }

        if (elapsedTime >= MAX_WAIT)
        {
            retValue = false;
            Console.WriteLine("Err_2257asap!!! Expcted BytesToWrite={0} actual {1} after first write", BYTE_SIZE_BYTES_TO_WRITE, com1.BytesToWrite);
        }

        com2.RtsEnable = true;

        //Wait for write method to complete
        elapsedTime = 0;
        while (!writeAsyncResult.IsCompleted && elapsedTime < MAX_WAIT_THREAD)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        if (MAX_WAIT_THREAD <= elapsedTime)
        {
            Console.WriteLine("Err_40888ajhied!!!: Expected write method to complete");
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        if (!retValue)
            Console.WriteLine("Err_007!!! Verifying BytesToWrite with one call to Write FAILED");

        return retValue;
    }


    private bool BytesToWriteSuccessive()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        IAsyncResult writeAsyncResult1, writeAsyncResult2;
        int elapsedTime = 0;

        Console.WriteLine("Verifying BytesToWrite with successive calls to Write");

        com1.Handshake = Handshake.RequestToSend;
        com1.Open();
        com2.Open();
        com1.WriteTimeout = 2000;

        //Write a random byte[] asynchronously so we can verify some things while the write call is blocking
        writeAsyncResult1 = WriteRndByteArray(com1, BYTE_SIZE_BYTES_TO_WRITE);
        while (elapsedTime < 1000 && BYTE_SIZE_BYTES_TO_WRITE != com1.BytesToWrite)
        {
            elapsedTime += ITERATION_WAIT;
            System.Threading.Thread.Sleep(ITERATION_WAIT);
        }

        if (elapsedTime >= 1000)
        {
            retValue = false;
            Console.WriteLine("Err_5201afwp!!! Expcted BytesToWrite={0} actual {1} after first write", BYTE_SIZE_BYTES_TO_WRITE, com1.BytesToWrite);
        }

        //Write a random byte[] asynchronously so we can verify some things while the write call is blocking
        writeAsyncResult2 = WriteRndByteArray(com1, BYTE_SIZE_BYTES_TO_WRITE);
        elapsedTime = 0;

        while (elapsedTime < 1000 && BYTE_SIZE_BYTES_TO_WRITE * 2 != com1.BytesToWrite)
        {
            elapsedTime += ITERATION_WAIT;
            System.Threading.Thread.Sleep(ITERATION_WAIT);
        }

        if (elapsedTime >= 1000)
        {
            retValue = false;
            Console.WriteLine("Err_2541afpsduz!!! Expcted BytesToWrite={0} actual {1} after first write", BYTE_SIZE_BYTES_TO_WRITE, com1.BytesToWrite);
        }


        com2.RtsEnable = true;

        //Wait for write method to complete
        elapsedTime = 0;
        while ((!writeAsyncResult1.IsCompleted || !writeAsyncResult2.IsCompleted) && elapsedTime < MAX_WAIT_THREAD)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        if (MAX_WAIT_THREAD <= elapsedTime)
        {
            Console.WriteLine("Err_65825215ahue!!!: Expected write method to complete");
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        if (!retValue)
            Console.WriteLine("Err_008!!! Verifying BytesToWrite with successive calls to Write FAILED");

        return retValue;
    }


    public bool Handshake_None()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        System.IAsyncResult writeAsyncResult;

        //Write a random byte[] asynchronously so we can verify some things while the write call is blocking
        Console.WriteLine("Verifying Handshake=None");

        com.Open();
        writeAsyncResult = WriteRndByteArray(com, BYTE_SIZE_BYTES_TO_WRITE);

        //Wait for both write methods to timeout
        while (!writeAsyncResult.IsCompleted)
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



    private System.IAsyncResult WriteRndByteArray(SerialPort com, int byteLength)
    {
        byte[] buffer = new byte[byteLength];
        Random rndGen = new Random(-55);

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)rndGen.Next(0, 256);
        }

        return com.BaseStream.BeginWrite(buffer, 0, buffer.Length, null, null);
    }
    #endregion

    #region Verification for Test Cases
    public static bool VerifyWriteException(System.IO.Stream serialStream, Type expectedException)
    {
        bool retValue = true;
        System.IAsyncResult writeAsyncResult;

        try
        {
            writeAsyncResult = serialStream.BeginWrite(new byte[BYTE_SIZE_EXCEPTION], 0, BYTE_SIZE_EXCEPTION, null, null);
            writeAsyncResult.AsyncWaitHandle.WaitOne();
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

    private bool VerifyTimeout(int writeTimeout)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        System.IAsyncResult writeAsyncResult;

        AsyncWrite asyncRead = new AsyncWrite(com1);
        System.Threading.Thread asyncEndWrite = new System.Threading.Thread(new System.Threading.ThreadStart(asyncRead.EndWrite));
        int waitTime;
        bool asyncCallbackCalled = false;

        com1.Open();
        com2.Open();
        com1.Handshake = Handshake.RequestToSend;
        com1.WriteTimeout = writeTimeout;

        writeAsyncResult = com1.BaseStream.BeginWrite(new byte[8], 0, 8, delegate (IAsyncResult ar) { asyncCallbackCalled = true; }, null);
        asyncRead.WriteAsyncResult = writeAsyncResult;

        System.Threading.Thread.Sleep(100 > com1.WriteTimeout ? 2 * com1.WriteTimeout : 200); //Sleep for 200ms or 2 times the WriteTimeout

        if (writeAsyncResult.IsCompleted)
        {//Verify the IAsyncResult has not completed
            Console.WriteLine("Err_565088aueiud!!!: Expected read to not have completed");
            retValue = false;
        }

        asyncEndWrite.Start();

        waitTime = 0;
        while (asyncEndWrite.ThreadState == System.Threading.ThreadState.Unstarted && waitTime < MAX_WAIT_THREAD)
        {//Wait for the thread to start
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (MAX_WAIT_THREAD <= waitTime)
        {
            Console.WriteLine("Err_018158ajied!!!: Expected EndRead to have returned");
            retValue = false;
        }

        System.Threading.Thread.Sleep(100 < com1.WriteTimeout ? 2 * com1.WriteTimeout : 200); //Sleep for 200ms or 2 times the WriteTimeout

        if (!asyncEndWrite.IsAlive)
        {//Verify EndRead is blocking and is still alive
            Console.WriteLine("Err_4085858aiehe!!!: Expected read to not have completed");
            retValue = false;
        }

        if (asyncCallbackCalled)
        {
            Console.WriteLine("Err_750551aiuehd!!!: Expected AsyncCallback not to be called");
            retValue = false;
        }

        com2.RtsEnable = true;

        waitTime = 0;
        while (asyncEndWrite.IsAlive && waitTime < MAX_WAIT_THREAD)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (MAX_WAIT_THREAD <= waitTime)
        {
            Console.WriteLine("Err_018158ajied!!!: Expected EndRead to have returned");
            retValue = false;
        }

        waitTime = 0;
        while (!asyncCallbackCalled && waitTime < 5000)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        if (!asyncCallbackCalled)
        {
            Console.WriteLine("Err_21208aheide!!!: Expected AsyncCallback to be called after some data was written to the port");
            retValue = false;
        }


        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool Verify_Handshake(Handshake handshake)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        byte[] XOffBuffer = new Byte[1];
        byte[] XOnBuffer = new Byte[1];
        System.IAsyncResult writeAsyncResult;
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
        writeAsyncResult = WriteRndByteArray(com1, BYTE_SIZE_HANDSHAKE);
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
        while (!writeAsyncResult.IsCompleted)
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

    public class AsyncWrite
    {
        private SerialPort _com;
        private IAsyncResult _writeAsyncResult;

        public AsyncWrite(SerialPort com)
        {
            _com = com;
        }

        public void BeginWrite()
        {
            _writeAsyncResult = _com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);
        }

        public IAsyncResult WriteAsyncResult
        {
            get
            {
                return _writeAsyncResult;
            }
            set
            {
                _writeAsyncResult = value;
            }
        }

        public void EndWrite()
        {
            _com.BaseStream.EndWrite(_writeAsyncResult);
        }
    }

    #endregion
}

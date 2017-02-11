// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class Flush
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/19 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.Flush()";
    public static readonly String s_strTFName = "Flush.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The string used with Write(str) to fill the input buffer
    public static readonly string DEFAULT_STRING = "Hello World";
    public static readonly int DEFAULT_BUFFER_SIZE = 32;
    public static readonly int MAX_WAIT_TIME = 500;

    //The buffer lenght used whe filling the ouput buffer
    public static readonly int DEFAULT_BUFFER_LENGTH = 8;

    //Delegate to start asynchronous write on the SerialPort com with byte[] of size bufferLength
    public delegate void AsyncWriteDelegate(SerialPort com, int bufferLength);

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Flush objTest = new Flush();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Flush_Open_Close), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Flush_Open_BaseStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBufferFilled_Flush_Once), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBufferFilled_Flush_Multiple), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBufferFilled_Flush_Cycle), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OutBufferFilled_Flush_Once), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OutBufferFilled_Flush_Multiple), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OutBufferFilled_Flush_Cycle), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(InOutBufferFilled_Flush_Once), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(InOutBufferFilled_Flush_Multiple), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(InOutBufferFilled_Flush_Cycle), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Flush_Open_Close()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        com.Open();
        serialStream = com.BaseStream;
        com.Close();

        Console.WriteLine("Verifying Flush throws exception After Open() then Close()");

        if (!VerifyException(serialStream, typeof(System.ObjectDisposedException)))
        {
            Console.WriteLine("Err_001!!! Verifying Flush throws exception After Open() then Close() FAILED");
            return false;
        }

        return true;
    }


    public bool Flush_Open_BaseStreamClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        com.Open();
        serialStream = com.BaseStream;
        com.BaseStream.Close();

        Console.WriteLine("Verifying Flush throws exception After Open() then BaseStream.Close()");

        if (!VerifyException(serialStream, typeof(System.ObjectDisposedException)))
        {
            Console.WriteLine("Err_001!!! Verifying Flush throws exception After Open() then BaseStream.Close() FAILED");
            return false;
        }

        return true;
    }


    public bool InBufferFilled_Flush_Once()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        int elapsedTime = 0;
        byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

        Console.WriteLine("Verifying Flush method after input buffer has been filled");
        com1.Open();
        com2.Open();

        for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

        com2.Write(xmitBytes, 0, xmitBytes.Length);

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying Flush method after input buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool InBufferFilled_Flush_Multiple()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        int elapsedTime = 0;
        byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

        Console.WriteLine("Verifying call Flush method several times after input buffer has been filled");
        com1.Open();
        com2.Open();

        for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

        com2.Write(xmitBytes, 0, xmitBytes.Length);

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);
        retValue &= VerifyFlush(com1);
        retValue &= VerifyFlush(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying call Flush method several times after input buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool InBufferFilled_Flush_Cycle()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        int elapsedTime = 0;
        byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

        Console.WriteLine("Verifying call Flush method after input buffer has been filled discarded and filled again");

        com1.Open();
        com2.Open();

        for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

        com2.Write(xmitBytes, 0, xmitBytes.Length);

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);

        com2.Write(xmitBytes, 0, xmitBytes.Length);
        elapsedTime = 0;

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE * 2 && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying call Flush method after input buffer has been filled discarded and filled again FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool OutBufferFilled_Flush_Once()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int elapsedTime = 0;

        Console.WriteLine("Verifying Flush method after output buffer has been filled");

        com1.Open();
        com1.WriteTimeout = 500;
        com1.Handshake = Handshake.RequestToSend;

        t.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);

        //Wait for write method to timeout
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying Flush method after output buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool OutBufferFilled_Flush_Multiple()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int elapsedTime = 0;

        Console.WriteLine("Verifying call Flush method several times after output buffer has been filled");

        com1.Open();
        com1.WriteTimeout = 500;
        com1.Handshake = Handshake.RequestToSend;

        t.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);
        retValue &= VerifyFlush(com1);
        retValue &= VerifyFlush(com1);

        //Wait for write method to timeout
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying call Flush method several times after output buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool OutBufferFilled_Flush_Cycle()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
        System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        System.Threading.Thread t2 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int elapsedTime = 0;

        Console.WriteLine("Verifying call Flush method after output buffer has been filled discarded and filled again");

        com1.Open();
        com1.WriteTimeout = 500;
        com1.Handshake = Handshake.RequestToSend;

        t1.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        if (!VerifyFlush(com1))
        {
            Console.WriteLine("Err_29292jsazie Verifying Flush FAILED after buffer was filled once");
            retValue = false;
        }

        //Wait for write method to timeout
        while (t1.IsAlive)
            System.Threading.Thread.Sleep(100);


        t2.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        if (!VerifyFlush(com1))
        {
            Console.WriteLine("Err_065784ahzoabh Verifying Flush FAILED after buffer was filled the second time");
            retValue = false;
        }

        //Wait for write method to timeout
        while (t2.IsAlive)
            System.Threading.Thread.Sleep(100);


        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying call Flush method after output buffer has been filled discarded and filled again FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool InOutBufferFilled_Flush_Once()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int elapsedTime = 0;
        byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

        Console.WriteLine("Verifying Flush method after input and output buffer has been filled");

        com1.Open();
        com2.Open();
        com1.WriteTimeout = 500;
        com1.Handshake = Handshake.RequestToSend;

        for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

        com2.Write(xmitBytes, 0, xmitBytes.Length);

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        t.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);

        //Wait for write method to timeout
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (!retValue)
        {
            Console.WriteLine("Err_007!!! Verifying Flush method after input and output buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool InOutBufferFilled_Flush_Multiple()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int elapsedTime = 0;
        byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

        Console.WriteLine("Verifying call Flush method several times after input and output buffer has been filled");

        com1.Open();
        com2.Open();
        com1.WriteTimeout = 500;
        com1.Handshake = Handshake.RequestToSend;

        for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

        com2.Write(xmitBytes, 0, xmitBytes.Length);

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        t.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);
        retValue &= VerifyFlush(com1);
        retValue &= VerifyFlush(com1);

        //Wait for write method to timeout
        while (t.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (!retValue)
        {
            Console.WriteLine("Err_008!!! Verifying call Flush method several times after input and output buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool InOutBufferFilled_Flush_Cycle()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        AsyncWriteRndByteArray asyncWriteRndByteArray = new AsyncWriteRndByteArray(com1, DEFAULT_BUFFER_SIZE);
        System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        System.Threading.Thread t2 = new System.Threading.Thread(new System.Threading.ThreadStart(asyncWriteRndByteArray.WriteRndByteArray));
        bool retValue = true;
        int elapsedTime = 0;
        byte[] xmitBytes = new byte[DEFAULT_BUFFER_SIZE];

        Console.WriteLine("Verifying call Flush method after input and output buffer has been filled discarded and filled again");

        com1.Open();
        com2.Open();
        com1.WriteTimeout = 500;
        com1.Handshake = Handshake.RequestToSend;

        for (int i = 0; i < xmitBytes.Length; i++) xmitBytes[i] = (byte)i;

        com2.Write(xmitBytes, 0, xmitBytes.Length);

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        t1.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);

        //Wait for write method to timeout
        while (t1.IsAlive)
            System.Threading.Thread.Sleep(100);

        t2.Start();
        elapsedTime = 0;

        while (com1.BytesToWrite < DEFAULT_BUFFER_LENGTH && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        com2.Write(xmitBytes, 0, xmitBytes.Length);
        elapsedTime = 0;

        while (com1.BytesToRead < DEFAULT_BUFFER_SIZE && elapsedTime < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            elapsedTime += 50;
        }

        retValue &= VerifyFlush(com1);

        //Wait for write method to timeout
        while (t2.IsAlive)
            System.Threading.Thread.Sleep(100);

        if (!retValue)
        {
            Console.WriteLine("Err_009!!! Verifying call Flush method after input and output buffer has been filled discarded and filled again FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }



    public class AsyncWriteRndByteArray
    {
        private SerialPort _com;
        private int _byteLength;


        public AsyncWriteRndByteArray(SerialPort com, int byteLength)
        {
            _com = com;
            _byteLength = byteLength;
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
                _com.Write(buffer, 0, buffer.Length);
            }
            catch (System.TimeoutException)
            {
            }
        }
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyException(System.IO.Stream serialStream, Type expectedException)
    {
        bool retValue = true;

        try
        {
            serialStream.Flush();

            Console.WriteLine("ERROR!!!: No Excpetion was thrown from Flush()");
            retValue = false;
        }
        catch (System.Exception e)
        {
            if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!!: {0} exception was thrown expected {1} from Flush()", e.GetType(), expectedException);
                retValue = false;
            }
        }
        return retValue;
    }


    private bool VerifyFlush(SerialPort com)
    {
        bool retValue = true;
        int origBytesToRead = com.BytesToRead;
        int byteRead;
        int i = 0;

        com.BaseStream.Flush();

        if (origBytesToRead != com.BytesToRead)
        {
            Console.WriteLine("ERROR!!! Expected BytesToRead={0} Actual BytesToRead={1}", origBytesToRead, com.BytesToRead);
            retValue = false;
        }

        if (0 != com.BytesToWrite)
        {
            Console.WriteLine("ERROR!!! Expected BytesToWrite=0 Actual BytesToWrite={0}", com.BytesToWrite);
            retValue = false;
        }

        com.ReadTimeout = 0;

        if (origBytesToRead != 0)
        {
            while (true)
            {
                try
                {
                    byteRead = com.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }

                if (i != byteRead)
                {
                    Console.WriteLine("Err_7083apnh Expecte to read={0} actual={1}", i, byteRead);
                    retValue = false;
                }

                i++;
            }

            if (i != DEFAULT_BUFFER_SIZE)
            {
                Console.WriteLine("Err_09778asdh Expected to read {0} bytes actually read {1}", DEFAULT_BUFFER_SIZE, i);
                retValue = false;
            }
        }

        return retValue;
    }
    #endregion
}

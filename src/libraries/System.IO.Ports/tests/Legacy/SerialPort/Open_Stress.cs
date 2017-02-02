// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections;
using System.Threading;

public class Open_exception
{
    public static readonly String s_strActiveBugNums = "234598 23595";
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Open()";
    public static readonly String s_strTFName = "Open.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Determines how long the randomly generated PortName is
    public static readonly int rndPortNameSize = 256;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Open_exception objTest = new Open_exception();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenReceiveData), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenReceiveDataAndRTS), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    public bool OpenReceiveData()
    {
        Thread workerThread = new Thread(new ThreadStart(OpenReceiveData_WorkerThread));
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Open and Close port while the port is recieving data");

        _continueLoop = true;
        workerThread.Start();

        try
        {
            for (int i = 0; i < 1000; ++i)
            {
                com.RtsEnable = true;
                com.Open();
                com.Close();
            }
        }
        finally
        {
            _continueLoop = false;

            if (com.IsOpen)
                com.Close();
        }

        workerThread.Join();

        return true;
    }

    private bool _continueLoop;

    public void OpenReceiveData_WorkerThread()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        byte[] xmitBytes = new byte[16];

        for (int i = 0; i < xmitBytes.Length; ++i)
            xmitBytes[i] = (byte)i;

        try
        {
            com.Open();

            while (_continueLoop)
            {
                com.Write(xmitBytes, 0, xmitBytes.Length);
            }
        }
        finally
        {
            com.Close();
        }
    }

    public bool OpenReceiveDataAndRTS()
    {
        Thread workerThread = new Thread(new ThreadStart(OpenReceiveDataAndRTS_WorkerThread));
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Open and Close port while the port is recieving data and the RTS pin is changing states");

        workerThread.Start();

        _continueLoop = true;

        byte[] xmitBytes = new byte[16];

        for (int i = 0; i < xmitBytes.Length; ++i)
            xmitBytes[i] = (byte)i;


        try
        {
            for (int i = 0; i < 1000; ++i)
            {
                com.Open();
                com.Handshake = Handshake.RequestToSend;
                com.Write(xmitBytes, 0, xmitBytes.Length);
                com.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Thread1 through the following exception:\n{0}", e);
        }
        finally
        {
            _continueLoop = false;

            if (com.IsOpen)
                com.Close();
        }

        workerThread.Join();

        return true;
    }


    public void OpenReceiveDataAndRTS_WorkerThread()
    {
        try
        {
            SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
            byte[] xmitBytes = new byte[16];

            for (int i = 0; i < xmitBytes.Length; ++i)
                xmitBytes[i] = (byte)i;

            com.Open();

            while (_continueLoop)
            {
                com.Write(xmitBytes, 0, xmitBytes.Length);
                com.RtsEnable = !com.RtsEnable;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Thread1 through the following exception:\n{0}", e);
        }
    }
}

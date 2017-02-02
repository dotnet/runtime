// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Threading;

public class Event_Generic
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2005/06/14 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Events";
    public static readonly String s_strTFName = "Event_Close_Stress.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Maximum time to wait for all of the expected events to be firered
    public static readonly int MAX_TEST_TIME = 3 * 60 * 1000;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Event_Generic objTest = new Event_Generic();

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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(PinChanged_Close_Stress), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataReceived_Close_Stress), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ErrorReceived_Close_Stress), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool PinChanged_Close_Stress()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        int count = 0;

        com1.PinChanged += CatchPinChangedEvent;
        com2.Open();

        stopwatch.Start();
        while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < MAX_TEST_TIME)
        {
            com1.Open();

            for (int j = 0; j < 10; ++j)
            {
                com2.RtsEnable = !com2.RtsEnable;
            }

            com1.Close();

            ++count;
        }

        com2.Close();

        Console.WriteLine("PinChanged={0}", _pinChangedCount);

        return true;
    }

    private int _pinChangedCount = 0;

    public void CatchPinChangedEvent(Object sender, SerialPinChangedEventArgs e)
    {
        ++_pinChangedCount;
    }

    public bool DataReceived_Close_Stress()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        int count = 0;

        com1.DataReceived += CatchDataReceivedEvent;
        com2.Open();

        stopwatch.Start();
        while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < MAX_TEST_TIME)
        {
            com1.Open();

            for (int j = 0; j < 10; ++j)
            {
                com2.WriteLine("foo");
            }

            com1.Close();

            ++count;
        }

        com2.Close();

        Console.WriteLine("DataReceived={0}", _dataReceivedCount);

        return true;
    }

    private int _dataReceivedCount = 0;

    public void CatchDataReceivedEvent(Object sender, SerialDataReceivedEventArgs e)
    {
        ++_dataReceivedCount;
    }

    public bool ErrorReceived_Close_Stress()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        byte[] frameErrorBytes = new byte[1];
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        int count = 0;

        com1.DataBits = 7;
        com1.ErrorReceived += CatchErrorReceivedEvent;
        com2.Open();

        //This should cause a fame error since the 8th bit is not set 
        //and com1 is set to 7 data bits ao the 8th bit will +12v where
        //com1 expects the stop bit at the 8th bit to be -12v
        frameErrorBytes[0] = 0x01;

        stopwatch.Start();
        while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < MAX_TEST_TIME)
        {
            com1.Open();

            for (int j = 0; j < 10; ++j)
            {
                com2.Write(frameErrorBytes, 0, 1);
            }

            com1.Close();

            ++count;
        }

        com2.Close();

        Console.WriteLine("ErrorReceived={0}", _errorReceivedCount);

        return true;
    }

    private int _errorReceivedCount = 0;

    public void CatchErrorReceivedEvent(Object sender, SerialErrorReceivedEventArgs e)
    {
        ++_errorReceivedCount;
    }
    #endregion

    #region Verification for Test Cases
    #endregion
}


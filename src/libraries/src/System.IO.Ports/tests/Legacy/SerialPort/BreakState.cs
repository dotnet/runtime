// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class BreakState_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.BreakState";
    public static readonly String s_strTFName = "BreakState.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The maximum time we will wait for the pin changed event to get firered for the break state
    public static readonly int MAX_WAIT_FOR_BREAK = 800;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        BreakState_Property objTest = new BreakState_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BreakState_Default), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BreakState_BeforeOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BreakState_true), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BreakState_false), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BreakState_true_false), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BreakState_true_false_true), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool BreakState_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default BreakState");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default BreakState FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool BreakState_BeforeOpen()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying setting BreakState before open");
        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com1.BreakState = true;
            Console.WriteLine("Errr_7092zaqjh Expected setting BreakState before calling open to throw InvalidOperationException");
            retValue = false;
        }
        catch (InvalidOperationException) { }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_25412ppyh!!! Verifying setting BreakState before open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool BreakState_true()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying true BreakState");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= SetBreakStateandVerify(com1);

        serPortProp.SetProperty("BreakState", true);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying true BreakState FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool BreakState_false()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying false BreakState");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= SetBreakStateandVerify(com1);
        serPortProp.SetProperty("BreakState", false);

        com1.BreakState = false;
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        retValue &= !GetCurrentBreakState();

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying false BreakState FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool BreakState_true_false()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying setting BreakState to true then false");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        retValue &= SetBreakStateandVerify(com1);
        serPortProp.SetProperty("BreakState", true);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        serPortProp.SetProperty("BreakState", false);
        com1.BreakState = false;
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        retValue &= !GetCurrentBreakState();

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying setting BreakState to true then false FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool BreakState_true_false_true()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying setting BreakState to true then false then true again");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= SetBreakStateandVerify(com1);

        serPortProp.SetProperty("BreakState", true);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        serPortProp.SetProperty("BreakState", false);
        com1.BreakState = false;
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        retValue &= !GetCurrentBreakState();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= SetBreakStateandVerify(com1);
        serPortProp.SetProperty("BreakState", true);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying setting BreakState to true then false then true again FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public bool SetBreakStateandVerify(SerialPort com1)
    {
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        BreakStateEventHandler breakState = new BreakStateEventHandler();

        com2.PinChanged += new SerialPinChangedEventHandler(breakState.HandleEvent);
        com2.Open();
        com1.BreakState = true;

        if (!breakState.WaitForBreak(MAX_WAIT_FOR_BREAK))
        {
            Console.WriteLine("Err_2078aspznd!!!: The PinChangedEvent handler never got called with SerialPinChanges.Break event type");
            retValue = false;
        }

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool GetCurrentBreakState()
    {
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        BreakStateEventHandler breakState = new BreakStateEventHandler();

        com2.PinChanged += new SerialPinChangedEventHandler(breakState.HandleEvent);
        com2.Open();
        retValue = breakState.WaitForBreak(MAX_WAIT_FOR_BREAK);

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }



    public class BreakStateEventHandler
    {
        private bool _breakOccured = false;


        public void HandleEvent(object source, SerialPinChangedEventArgs e)
        {
            lock (this)
            {
                if (SerialPinChange.Break == e.EventType)
                {
                    _breakOccured = true;
                    System.Threading.Monitor.Pulse(this);
                }
            }
        }


        public void WaitForBreak()
        {
            lock (this)
            {
                if (!_breakOccured)
                {
                    System.Threading.Monitor.Wait(this);
                }
            }
        }


        public bool WaitForBreak(int timeout)
        {
            lock (this)
            {
                if (!_breakOccured)
                {
                    return System.Threading.Monitor.Wait(this, timeout);
                }

                return true;
            }
        }
    }

    #endregion
}


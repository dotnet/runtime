// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class DtrEnable_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.DtrEnable";
    public static readonly String s_strTFName = "DtrEnable.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        DtrEnable_Property objTest = new DtrEnable_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DtrEnable_Default), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DtrEnable_true_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DtrEnable_false_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DtrEnable_true_false_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DtrEnable_true_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DtrEnable_false_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DtrEnable_true_false_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool DtrEnable_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DtrEnable");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDtrEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default DtrEnable FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DtrEnable_true_BeforeOpen()
    {
        Console.WriteLine("Verifying true DtrEnable before open");
        if (!VerifyDtrEnableBeforeOpen(true))
        {
            Console.WriteLine("Err_002!!! Verifying true DtrEnable before open FAILED");
            return false;
        }

        return true;
    }


    public bool DtrEnable_false_BeforeOpen()
    {
        Console.WriteLine("Verifying false DtrEnable before open");
        if (!VerifyDtrEnableBeforeOpen(false))
        {
            Console.WriteLine("Err_003!!! Verifying false DtrEnable before open FAILED");
            return false;
        }

        return true;
    }


    public bool DtrEnable_true_false_BeforeOpen()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying seting DtrEnable to true then false before open");

        com1.DtrEnable = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        serPortProp.SetProperty("DtrEnable", false);
        com1.DtrEnable = false;
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDtrEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        if (!retValue)
        {
            Console.WriteLine("Err_9072akldjs!!! Verifying seting DtrEnable to true then false before open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DtrEnable_true_AfterOpen()
    {
        Console.WriteLine("Verifying true DtrEnable after open");
        if (!VerifyDtrEnableAfterOpen(true))
        {
            Console.WriteLine("Err_002!!! Verifying true DtrEnable after open FAILED");
            return false;
        }

        return true;
    }


    public bool DtrEnable_false_AfterOpen()
    {
        Console.WriteLine("Verifying false DtrEnable after open");
        if (!VerifyDtrEnableAfterOpen(false))
        {
            Console.WriteLine("Err_003!!! Verifying false DtrEnable after open FAILED");
            return false;
        }

        return true;
    }


    public bool DtrEnable_true_false_AfterOpen()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying seting DtrEnable to true then false after open");

        com1.Open();

        com1.DtrEnable = true;
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        serPortProp.SetProperty("DtrEnable", false);
        com1.DtrEnable = false;

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDtrEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_15987aphl!!! Verifying seting DtrEnable to true then false after open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyDtrEnableBeforeOpen(bool dtrEnable)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.DtrEnable = dtrEnable;
        com1.Open();

        serPortProp.SetProperty("DtrEnable", dtrEnable);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDtrEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyDtrEnableAfterOpen(bool dtrEnable)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        com1.DtrEnable = dtrEnable;

        serPortProp.SetProperty("DtrEnable", dtrEnable);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDtrEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyDtrEnable(SerialPort com1)
    {
        bool retValue = true;
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        com2.Open();
        retValue = (com1.DtrEnable && com2.DsrHolding) || (!com1.DtrEnable && !com2.DsrHolding);

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    #endregion
}

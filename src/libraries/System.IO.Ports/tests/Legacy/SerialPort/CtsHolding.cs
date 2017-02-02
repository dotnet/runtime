// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class CtsHolding_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.CtsHolding";
    public static readonly String s_strTFName = "CtsHolding.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        CtsHolding_Property objTest = new CtsHolding_Property();
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

        //See individual read methods for further testing
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CtsHolding_Default), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CtsHolding_Default_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CtsHolding_true), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CtsHolding_true_false), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CtsHolding_true_local_close), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CtsHolding_true_remote_close), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool CtsHolding_Default()
    {
        SerialPort com1 = new SerialPort();
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default CtsHolding before Open");

        serPortProp.SetAllPropertiesToDefaults();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default CtsHolding before Open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool CtsHolding_Default_AfterOpen()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default CtsHolding after Open");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying default CtsHolding after Open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool CtsHolding_true()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying CtsHolding=true on com1 when com2.RtsEnable=true");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("CtsHolding", true);

        com1.Open();
        com2.Open();
        com2.RtsEnable = true;

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying CtsHolding=true on com1 when com2.RtsEnable=true FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool CtsHolding_true_false()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then false");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("CtsHolding", true);

        com1.Open();
        com2.Open();
        com2.RtsEnable = true;

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        com2.RtsEnable = false;
        serPortProp.SetProperty("CtsHolding", false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then false FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool CtsHolding_true_local_close()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then com1 is closed");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("CtsHolding", true);

        com1.Open();
        com2.Open();
        com2.RtsEnable = true;

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then com1 is closed FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool CtsHolding_true_remote_close()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then com2 is closed");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("CtsHolding", true);

        com1.Open();
        com2.Open();
        com2.RtsEnable = true;

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com2.IsOpen)
            com2.Close();

        System.Threading.Thread.Sleep(100);
        serPortProp.SetProperty("CtsHolding", false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then com2 is closed FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases

    #endregion
}

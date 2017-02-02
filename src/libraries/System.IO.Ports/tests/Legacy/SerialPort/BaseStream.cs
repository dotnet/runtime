// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class BaseStream_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.BaseStream";
    public static readonly String s_strTFName = "BaseStream.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        BaseStream_Property objTest = new BaseStream_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaseStream_Default), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaseStream_Open), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaseStream_Open_Close), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaseStream_Open_BaseStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool BaseStream_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default BaseStream");
        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default BaseStream FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool BaseStream_Open()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying BaseStream after Open() has been called");
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        if (null == com1.BaseStream || !com1.BaseStream.GetType().BaseType.Equals(typeof(System.IO.Stream)))
        {
            Console.WriteLine("ERROR!!! BaseStream is either null or is not of the Type System.IO.Stream");
            retValue = false;
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying BaseStream after Open() has been called FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool BaseStream_Open_Close()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying BaseStream after Open() and Close have been called");

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.Close();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying BaseStream after Open() and Close have been called FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool BaseStream_Open_BaseStreamClose()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying BaseStream after Open() and BaseStream.Close() have been called");

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.BaseStream.Close();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying BaseStream after Open() and BaseStream.Close have been called FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases

    #endregion
}

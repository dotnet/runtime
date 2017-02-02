// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class RtsEnable_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.RtsEnable";
    public static readonly String s_strTFName = "RtsEnable.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        RtsEnable_Property objTest = new RtsEnable_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_Default), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_true_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_false_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_true_false_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_true_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_false_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_true_false_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_true_Handshake_XOnXOff), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_false_Handshake_XOnXOff), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_true_Handshake_RequestToSend), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_false_Handshake_RequestToSend), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_true_Handshake_RequestToSendXOnXOff), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_false_Handshake_RequestToSendXOnXOff), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_Get_Handshake_None), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_Get_Handshake_RequestToSend), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_Get_Handshake_RequestToSendXOnXOff), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RtsEnable_Get_Handshake_XOnXOff), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool RtsEnable_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default RtsEnable");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyRtsEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default RtsEnable FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool RtsEnable_true_BeforeOpen()
    {
        Console.WriteLine("Verifying true RtsEnable before open");
        if (!VerifyRtsEnableBeforeOpen(true))
        {
            Console.WriteLine("Err_002!!! Verifying true RtsEnable before open FAILED");
            return false;
        }

        return true;
    }


    public bool RtsEnable_false_BeforeOpen()
    {
        Console.WriteLine("Verifying false RtsEnable before open");
        if (!VerifyRtsEnableBeforeOpen(false))
        {
            Console.WriteLine("Err_003!!! Verifying false RtsEnable before open FAILED");
            return false;
        }

        return true;
    }


    public bool RtsEnable_true_false_BeforeOpen()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying seting RtsEnable to true then false before open");

        com1.RtsEnable = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("RtsEnable", false);
        com1.RtsEnable = false;

        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyRtsEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying seting RtsEnable to true then false before open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool RtsEnable_true_AfterOpen()
    {
        Console.WriteLine("Verifying true RtsEnable after open");
        if (!VerifyRtsEnableAfterOpen(true))
        {
            Console.WriteLine("Err_002!!! Verifying true RtsEnable after open FAILED");
            return false;
        }

        return true;
    }


    public bool RtsEnable_false_AfterOpen()
    {
        Console.WriteLine("Verifying false RtsEnable after open");
        if (!VerifyRtsEnableAfterOpen(false))
        {
            Console.WriteLine("Err_003!!! Verifying false RtsEnable after open FAILED");
            return false;
        }

        return true;
    }

    public bool RtsEnable_true_Handshake_XOnXOff()
    {
        Console.WriteLine("Verifying true RtsEnable after setting Handshake to XOnXOff");


        if (!VerifyRtsEnableWithHandshake(true, Handshake.XOnXOff))
        {
            Console.WriteLine("Err_15858ajied!!! Verifying true RtsEnable after setting Handshake to XOnXOff FAILED");
            return false;
        }

        return true;
    }

    public bool RtsEnable_false_Handshake_XOnXOff()
    {
        Console.WriteLine("Verifying false RtsEnable after setting Handshake to XOnXOff");


        if (!VerifyRtsEnableWithHandshake(false, Handshake.XOnXOff))
        {
            Console.WriteLine("Err_255488ajoed!!! Verifying false RtsEnable after setting Handshake to XOnXOff FAILED");
            return false;
        }

        return true;
    }

    public bool RtsEnable_true_Handshake_RequestToSend()
    {
        Console.WriteLine("Verifying true RtsEnable after setting Handshake to RequestToSend");


        if (!VerifyRtsEnableWithHandshake(true, Handshake.RequestToSend))
        {
            Console.WriteLine("Err_5548ahied!!! Verifying true RtsEnable after setting Handshake to RequestToSend FAILED");
            return false;
        }

        return true;
    }

    public bool RtsEnable_false_Handshake_RequestToSend()
    {
        Console.WriteLine("Verifying false RtsEnable after setting Handshake to RequestToSend");


        if (!VerifyRtsEnableWithHandshake(false, Handshake.RequestToSend))
        {
            Console.WriteLine("Err_155896ajied!!! Verifying false RtsEnable after setting Handshake to RequestToSend FAILED");
            return false;
        }

        return true;
    }

    public bool RtsEnable_true_Handshake_RequestToSendXOnXOff()
    {
        Console.WriteLine("Verifying true RtsEnable after setting Handshake to RequestToSendXOnXOff");


        if (!VerifyRtsEnableWithHandshake(true, Handshake.RequestToSendXOnXOff))
        {
            Console.WriteLine("Err_541548ajied!!! Verifying true RtsEnable after setting Handshake to RequestToSendXOnXOff FAILED");
            return false;
        }

        return true;
    }

    public bool RtsEnable_false_Handshake_RequestToSendXOnXOff()
    {
        Console.WriteLine("Verifying false RtsEnable after setting Handshake to RequestToSendXOnXOff");


        if (!VerifyRtsEnableWithHandshake(false, Handshake.RequestToSendXOnXOff))
        {
            Console.WriteLine("Err_02588ajiied!!! Verifying false RtsEnable after setting Handshake to RequestToSendXOnXOff FAILED");
            return false;
        }

        return true;
    }


    public bool RtsEnable_true_false_AfterOpen()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying seting RtsEnable to true then false after open");

        com1.RtsEnable = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("RtsEnable", false);
        com1.RtsEnable = false;

        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyRtsEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_0548ahied!!! Verifying seting RtsEnable to true then false after open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool RtsEnable_Get_Handshake_None()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying getting RtsEnable with Handshake set to None");

        com1.Open();
        com1.Handshake = Handshake.None;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("Handshake", Handshake.None);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_10818aheud!!! Verifying getting RtsEnable with Handshake set to None FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool RtsEnable_Get_Handshake_RequestToSend()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying getting RtsEnable with Handshake set to RequestToSend");

        com1.Open();
        com1.Handshake = Handshake.RequestToSend;

        try
        {
            bool rtsEnable = com1.RtsEnable;
            retValue = false;
            Console.WriteLine("Err_18218ahiee Expected RtsEnable to throw");
        }
        catch (InvalidOperationException)
        {
        }

        if (!retValue)
        {
            Console.WriteLine("Err_10518ajied!!! Verifying getting RtsEnable with Handshake set to RequestToSend FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool RtsEnable_Get_Handshake_RequestToSendXOnXOff()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying getting RtsEnable with Handshake set to RequestToSendXOnXOff");

        com1.Open();
        com1.Handshake = Handshake.RequestToSendXOnXOff;

        try
        {
            bool rtsEnable = com1.RtsEnable;
            retValue = false;
            Console.WriteLine("Err_051854ahied Expected RtsEnable to throw");
        }
        catch (InvalidOperationException)
        {
        }

        if (!retValue)
        {
            Console.WriteLine("Err_45744aheid!!! Verifying getting RtsEnable with Handshake set to RequestToSendXOnXOff FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool RtsEnable_Get_Handshake_XOnXOff()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying getting RtsEnable with Handshake set to XOnXOff");

        com1.Open();
        com1.Handshake = Handshake.XOnXOff;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("Handshake", Handshake.XOnXOff);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_01848ahied!!! Verifying getting RtsEnable with Handshake set to XOnXOff FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyRtsEnableBeforeOpen(bool rtsEnable)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.RtsEnable = rtsEnable;
        com1.Open();
        serPortProp.SetProperty("RtsEnable", rtsEnable);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyRtsEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyRtsEnableAfterOpen(bool rtsEnable)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.RtsEnable = rtsEnable;
        serPortProp.SetProperty("RtsEnable", rtsEnable);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyRtsEnable(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool VerifyRtsEnableWithHandshake(bool rtsEnable, Handshake handshake)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        Handshake originalHandshake;
        bool expetectedRtsEnable;
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.RtsEnable = rtsEnable;
        com1.Open();
        originalHandshake = com1.Handshake;
        serPortProp.SetProperty("RtsEnable", rtsEnable);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!VerifyRtsEnable(com1, rtsEnable))
        {
            Console.WriteLine("Err_2198219ahied Verifying RtsEnable failed before setting Handshake");
            retValue = false;
        }

        com1.Handshake = handshake;

        if (IsRequestToSend(com1))
        {
            try
            {
                com1.RtsEnable = !rtsEnable;
            }
            catch (InvalidOperationException) { }
        }
        else
        {
            com1.RtsEnable = !rtsEnable;
            com1.RtsEnable = rtsEnable;
        }

        expetectedRtsEnable = handshake == Handshake.RequestToSend || handshake == Handshake.RequestToSendXOnXOff ?
            true : rtsEnable;

        if (!VerifyRtsEnable(com1, expetectedRtsEnable))
        {
            Console.WriteLine("Err_6648ajheid Verifying RtsEnable failed after setting Handshake={0}", handshake);
            retValue = false;
        }

        com1.Handshake = originalHandshake;

        expetectedRtsEnable = rtsEnable;

        if (!VerifyRtsEnable(com1, expetectedRtsEnable))
        {
            Console.WriteLine("Err_1250588ajied Verifying RtsEnable failed after setting Handshake back to the original value={0}", originalHandshake);
            retValue = false;
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool VerifyRtsEnable(SerialPort com1)
    {
        return VerifyRtsEnable(com1, com1.RtsEnable);
    }

    private bool VerifyRtsEnable(SerialPort com1, bool expectedRtsEnable)
    {
        bool retValue = true;
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        com2.Open();

        retValue = (expectedRtsEnable && com2.CtsHolding) || (!expectedRtsEnable && !com2.CtsHolding);

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    private bool IsRequestToSend(SerialPort com)
    {
        return com.Handshake == Handshake.RequestToSend || com.Handshake == Handshake.RequestToSendXOnXOff;
    }

    #endregion
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections;

public class Close
{
    public static readonly String s_strActiveBugNums = "23595";
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/10 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Close()";
    public static readonly String s_strTFName = "Close.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The number of the bytes that should read/write buffers
    public static readonly int numReadBytes = 32;
    public static readonly int numWriteBytes = 32;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Close objTest = new Close();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(CloseWithoutOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenClose), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenFillBuffersClose), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenCloseNewInstanceOpen), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool CloseWithoutOpen()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Calling Close() without first calling Open()");

        try
        {
            com.Close();
        }
        catch (System.Exception e)
        {
            Console.WriteLine("EROOR!!!: {0} exception was thrown ", e.GetType());
            retValue = false;
        }

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying calling Close() without first calling Open() FAILED");
        }

        return retValue;
    }


    public bool OpenClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue;

        Console.WriteLine("Calling Close() after calling Open()");
        com.Open();
        com.Close();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue = serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying calling Close() after calling Open() FAILED");
        }

        return retValue;
    }


    public bool OpenFillBuffersClose()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Calling Open(), fill both trasmit and receive buffers, call Close()");
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        //Setting com1 to use Handshake so we can fill read buffer
        com1.Handshake = Handshake.RequestToSend;
        com1.Open();
        com2.Open();

        //BeginWrite is used so we can fill the read buffer then go onto to verify
        com1.BaseStream.BeginWrite(new Byte[numWriteBytes], 0, numWriteBytes, null, null);
        com2.Write(new Byte[numReadBytes], 0, numReadBytes);
        System.Threading.Thread.Sleep(500);

        serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
        serPortProp.SetProperty("BytesToWrite", numWriteBytes);
        serPortProp.SetProperty("BytesToRead", numReadBytes);

        Console.WriteLine("Verifying properties after port is open and bufferes have been filled");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        com1.Handshake = Handshake.None;
        com1.Close();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying properties after port has been closed");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        com1.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying properties after port has been opened again");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying calling Open(), fill both trasmit and receive buffers, call Close() FAILED");
        }

        return retValue;
    }


    public bool OpenCloseNewInstanceOpen()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort newCom;
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue;

        Console.WriteLine("Calling Close() after calling Open() then create a new instance of SerialPort and call Open() again");
        com.Open();
        com.Close();

        newCom = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        newCom.Open();

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue = serPortProp.VerifyPropertiesAndPrint(newCom);

        if (com.IsOpen)
            com.Close();

        if (newCom.IsOpen)
            newCom.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying calling Close() after calling Open() then create a new instance of SerialPort and call Open() again FAILED");
        }

        return retValue;
    }
}

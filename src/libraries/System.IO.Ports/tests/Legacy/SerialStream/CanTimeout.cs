// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class CanTimeout
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.CanTimeout";
    public static readonly String s_strTFName = "CanTimeout.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        CanTimeout objTest = new CanTimeout();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(CanTimeout_Open), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CanTimeout_Open_Close), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CanTimeout_Open_Close_Open), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(CanTimeout_Open_BaseStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool CanTimeout_Open()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com.Open();

        Console.WriteLine("Verifying CanTimeout property returns true after a call to Open()");

        if (!VerifyCanTimeout(com.BaseStream, true))
        {
            Console.WriteLine("Err_12078ahsp!!! Verifying CanTimeout property returns true after a call to Open() FAILED");
            return false;
        }

        com.Close();


        return true;
    }

    public bool CanTimeout_Open_Close()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        com.Open();
        serialStream = com.BaseStream;
        com.Close();

        Console.WriteLine("Verifying CanTimeout property retunrs false After Open() then Close()");

        if (!VerifyCanTimeout(serialStream, false))
        {
            Console.WriteLine("Err_7107ahsp!!! Verifying CanTimeout property returns false After Open() then Close() FAILED");
            return false;
        }

        return true;
    }

    public bool CanTimeout_Open_Close_Open()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        com.Open();
        com.Close();
        com.Open();
        serialStream = com.BaseStream;

        Console.WriteLine("Verifying CanTimeout property retunrs false After Open() then Close()");

        if (!VerifyCanTimeout(serialStream, true))
        {
            Console.WriteLine("Err_55585ajoee!!! Verifying CanTimeout property returns false After Open() then Close() FAILED");
            return false;
        }

        com.Close();

        return true;
    }


    public bool CanTimeout_Open_BaseStreamClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        com.Open();
        serialStream = com.BaseStream;
        com.BaseStream.Close();

        Console.WriteLine("Verifying CanTimeout property returns false After Open() then BaseStream.Close()");

        if (!VerifyCanTimeout(serialStream, false))
        {
            Console.WriteLine("Err_1077ahps!!! Verifying CanTimeout property returns false After Open() then BaseStream.Close() FAILED");
            return false;
        }

        return true;
    }

    private bool VerifyCanTimeout(System.IO.Stream serialStream, bool expectedValue)
    {
        bool retValue = true;

        if (serialStream.CanTimeout != expectedValue)
        {
            Console.WriteLine("Err_1707ahps!!! BaseStream.CanTimeout={0} expected={1}", serialStream.CanTimeout, expectedValue);
            retValue = false;
        }

        return retValue;
    }
}
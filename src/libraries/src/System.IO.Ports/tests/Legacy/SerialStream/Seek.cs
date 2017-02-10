// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class Seek
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.Seek(long offset, SeekOrigin origin)";
    public static readonly String s_strTFName = "Seek.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    public static readonly int DEFAULT_OFFSET = 0;
    public static readonly int DEFAULT_ORIGIN = (int)System.IO.SeekOrigin.Begin;
    public static readonly int BAD_OFFSET = -1;
    public static readonly int BAD_ORIGIN = -1;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Seek objTest = new Seek();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Seek_Open_Close), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Seek_Open_BaseStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Seek_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Seek_BadOffset), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Seek_BadOrigin), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Seek_BadOffset_BadOrigin), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Seek_Open_Close()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        com.Open();
        serialStream = com.BaseStream;
        com.Close();

        Console.WriteLine("Verifying Seek property throws exception After Open() then Close()");

        if (!VerifySeekException(serialStream, DEFAULT_OFFSET, DEFAULT_ORIGIN, typeof(System.NotSupportedException)))
        {
            Console.WriteLine("Err_001!!! Verifying Seek property throws exception After Open() then Close() FAILED");
            return false;
        }

        return true;
    }


    public bool Seek_Open_BaseStreamClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        System.IO.Stream serialStream;

        com.Open();
        serialStream = com.BaseStream;
        com.BaseStream.Close();

        Console.WriteLine("Verifying Seek property throws exception After Open() then BaseStream.Close()");

        if (!VerifySeekException(serialStream, DEFAULT_OFFSET, DEFAULT_ORIGIN, typeof(System.NotSupportedException)))
        {
            Console.WriteLine("Err_001!!! Verifying Seek property throws exception After Open() then BaseStream.Close() FAILED");
            return false;
        }

        return true;
    }


    public bool Seek_AfterOpen()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com.Open();

        Console.WriteLine("Verifying seek method throws exception after a call to Open()");

        if (!VerifySeekException(com.BaseStream, DEFAULT_OFFSET, DEFAULT_ORIGIN, typeof(System.NotSupportedException)))
        {
            Console.WriteLine("Err_002!!! Verifying seek method throws exception after a call to Open() FAILED");
            return false;
        }

        com.Close();
        return true;
    }


    public bool Seek_BadOffset()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com.Open();

        Console.WriteLine("Verifying seek method throws exception with a bad offset after a call to Open()");

        if (!VerifySeekException(com.BaseStream, BAD_OFFSET, DEFAULT_ORIGIN, typeof(System.NotSupportedException)))
        {
            Console.WriteLine("Err_003!!! Verifying seek method throws exception with a bad offset after a call to Open()) FAILED");
            return false;
        }

        com.Close();
        return true;
    }


    public bool Seek_BadOrigin()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com.Open();

        Console.WriteLine("Verifying seek method throws exception with a bad origin after a call to Open()");

        if (!VerifySeekException(com.BaseStream, DEFAULT_OFFSET, BAD_ORIGIN, typeof(System.NotSupportedException)))
        {
            Console.WriteLine("Err_004!!! Verifying seek method throws exception with a bad origin after a call to Open() FAILED");
            return false;
        }

        com.Close();
        return true;
    }


    public bool Seek_BadOffset_BadOrigin()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com.Open();

        Console.WriteLine("Verifying seek method throws exception with a bad offset/origin after a call to Open()");

        if (!VerifySeekException(com.BaseStream, BAD_OFFSET, BAD_ORIGIN, typeof(System.NotSupportedException)))
        {
            Console.WriteLine("Err_005!!! Verifying seek method throws exception with a bad offset/origin after a call to Open() FAILED");
            return false;
        }

        com.Close();
        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifySeekException(System.IO.Stream serialStream, long offset, int origin, Type expectedException)
    {
        bool retValue = true;

        try
        {
            serialStream.Seek(offset, (System.IO.SeekOrigin)origin);
            Console.WriteLine("ERROR!!!: No Excpetion was thrown");
            retValue = false;
        }
        catch (System.Exception e)
        {
            if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!!: {0} exception was thrown expected {1}", e.GetType(), expectedException);
                retValue = false;
            }
        }
        return retValue;
    }
    #endregion
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class DiscardInBuffer
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/19 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.DiscardInBuffer()";
    public static readonly String s_strTFName = "DiscardInBuffer_Generic.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        DiscardInBuffer objTest = new DiscardInBuffer();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardWithoutOpen), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardAfterFailedOpen), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardAfterClose), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardAfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;
        return retValue;
    }

    #region Test Cases
    public bool DiscardWithoutOpen()
    {
        SerialPort com = new SerialPort();

        Console.WriteLine("Verifying Discard method throws exception without a call to Open()");
        if (!VerifyDiscardException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_001!!! Verifying Discard method throws exception without a call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardAfterFailedOpen()
    {
        SerialPort com = new SerialPort("BAD_PORT_NAME");

        Console.WriteLine("Verifying read Discard throws exception with a failed call to Open()");

        //Since the PortName is set to a bad port name Open will thrown an exception
        //however we don't care what it is since we are verfifying a read method
        try
        {
            com.Open();
        }
        catch (System.Exception)
        {
        }
        if (!VerifyDiscardException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_002!!! Verifying Discard method throws exception with a failed call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardAfterClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying Discard method throws exception after a call to Cloes()");

        com.Open();
        com.Close();

        if (!VerifyDiscardException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_003!!! Verifying Discard method throws exception after a call to Cloes() FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardAfterOpen()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying Discard method does not throw an exception after a call to Open()");

        com.Open();
        retValue &= VerifyDiscardException(com, null);

        if (0 != com.BytesToRead)
        {
            Console.WriteLine("Error!!! BytesToRead is not 0");
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying Discard method does not throw an exception after a call to Open() FAILED");
            retValue = false;
        }

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyDiscardException(SerialPort com, Type expectedException)
    {
        bool retValue = true;

        try
        {
            com.DiscardInBuffer();
            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!!: No Excpetion was thrown");
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!!: No Excpetion was expected and {0} was thrown", e.GetType());
                retValue = false;
            }

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
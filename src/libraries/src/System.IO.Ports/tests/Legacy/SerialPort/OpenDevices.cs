// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class OpenDevices
{
    public static readonly String s_strActiveBugNums = "234598 23595";
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Open()";
    public static readonly String s_strTFName = "Open.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Determines how long the randomly generated PortName is
    public static readonly int rndPortNameSize = 256;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        OpenDevices objTest = new OpenDevices();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenDevices01), TCSupport.SerialPortRequirements.None);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool OpenDevices01()
    {
        DosDevices dosDevices = new DosDevices();
        Regex comPortNameRegex = new Regex(@"com\d{1,3}", RegexOptions.IgnoreCase);
        bool retValue = true;

        foreach (KeyValuePair<string, string> keyValuePair in dosDevices)
        {
            SerialPort com1;

            if (!string.IsNullOrEmpty(keyValuePair.Key) && !comPortNameRegex.IsMatch(keyValuePair.Key))
            {
                com1 = new SerialPort(keyValuePair.Key);
                try
                {
                    com1.Open();
                    Console.WriteLine("Error <KEY> no exception thrown with {0}", keyValuePair.Key);
                    retValue = false;
                    com1.Close();
                }
                catch (Exception) { }
            }

            if (!string.IsNullOrEmpty(keyValuePair.Value) && !comPortNameRegex.IsMatch(keyValuePair.Key))
            {
                com1 = new SerialPort(keyValuePair.Value);
                try
                {
                    com1.Open();
                    Console.WriteLine("Error <VALUE> no exception thrown with {0}", keyValuePair.Value);
                    retValue = false;
                    com1.Close();
                }
                catch (Exception) { }
            }
        }

        return retValue;
    }
}

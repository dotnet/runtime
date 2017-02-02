// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Threading;

public class GetPortNames
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.GetPortNames";
    public static readonly String s_strTFName = "GetPortNames.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Maximum time to wait for all of the expected events to be firered
    public static readonly int MAX_TIME_WAIT = 5000;

    //Time to wait inbetween trigering events
    public static readonly int TRIGERING_EVENTS_WAIT_TIME = 500;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        GetPortNames objTest = new GetPortNames();

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

        //We can't really do a lot here
        //Try opening every port
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenEveryPortName), TCSupport.SerialPortRequirements.None);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    private bool OpenEveryPortName()
    {
        bool retValue = true;
        String[] portNames = SerialPort.GetPortNames();
        SerialPort serialPort;


        for (int i = 0; i < portNames.Length; ++i)
        {
            Console.WriteLine("Opening port " + portNames[i]);
            bool portExists = false;
            foreach (string str in PortHelper.GetPorts())
            {
                if (str == portNames[i])
                {
                    portExists = true;
                    break;
                }
            }
            if (!portExists)
            {
                Console.WriteLine("Real Port does not exist. Ignore the output from SerialPort.GetPortNames()");
                continue;
            }
            serialPort = new SerialPort(portNames[i]);

            try
            {
                serialPort.Open();
            }
            catch (UnauthorizedAccessException) { }
        }

        return retValue;
    }
    #endregion
}


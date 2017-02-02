// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections;
using System.Threading;

public class Open_exception
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
        Open_exception objTest = new Open_exception();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(openDefault), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenTwice), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenTwoInstances), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool openDefault()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue;

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying after calling Open()");
        Console.WriteLine("BytesToWrite={0}", com.BytesToWrite);
        retValue = serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool OpenTwice()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying after calling Open() twice");

        try
        {
            com.Open();
            com.Open();
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!!: After calling Open() twice the following exception was thrown: \n{0}", e.ToString());
            retValue = false;
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool OpenTwoInstances()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp1 = new SerialPortProperties();
        SerialPortProperties serPortProp2 = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying calling Open() on two instances of SerialPort");
        serPortProp1.SetAllPropertiesToOpenDefaults();
        serPortProp1.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        serPortProp2.SetAllPropertiesToDefaults();
        serPortProp2.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com1.Open();
            com2.Open();
        }
        catch (System.Exception e)
        {
            if (!e.GetType().Equals(typeof(System.UnauthorizedAccessException)))
            {
                Console.WriteLine("ERROR!!!: After calling Open() on two instances the exception was thrown: \n{0}", e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp1.VerifyPropertiesAndPrint(com1);
        retValue &= serPortProp2.VerifyPropertiesAndPrint(com2);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
}

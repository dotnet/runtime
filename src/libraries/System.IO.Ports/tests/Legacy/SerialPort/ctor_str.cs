// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections;

public class ctor_str
{
    public static readonly String s_strActiveBugNums = "";
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ctor(string)";
    public static readonly String s_strTFName = "ctor_str.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Determines how long the randomly generated PortName is
    public static readonly int rndPortNameSize = 256;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ctor_str objTest = new ctor_str();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM2), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM3), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM4), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM256), TCSupport.SerialPortRequirements.None);

        //[] Error checking for PortName
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Empty), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Null), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SlashSlash), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM257), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Filename), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PHYSICALDRIVE0), TCSupport.SerialPortRequirements.None);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool COM1()
    {
        string portName = "COM1";

        return VerifyCtor(portName);
    }


    public bool COM2()
    {
        string portName = "COM2";

        return VerifyCtor(portName);
    }


    public bool COM3()
    {
        string portName = "COM3";

        return VerifyCtor(portName);
    }


    public bool COM4()
    {
        string portName = "COM4";

        return VerifyCtor(portName);
    }


    public bool COM256()
    {
        string portName = "COM256";

        return VerifyCtor(portName);
    }


    //[] Error checking for PortName
    public bool Empty()
    {
        string portName = String.Empty;

        return VerifyCtor(portName, typeof(ArgumentException), ThrowAt.Set);
    }


    public bool Null()
    {
        string portName = null;

        return VerifyCtor(portName, typeof(ArgumentNullException), ThrowAt.Set);
    }


    public bool SlashSlash()
    {
        string portName = "\\\\";

        return VerifyCtor(portName, typeof(ArgumentException), ThrowAt.Set);
    }


    public bool COM257()
    {
        string portName = "COM257";

        return VerifyCtor(portName);
    }


    public bool Filename()
    {
        string portName;
        string fileName = portName = "PortNameEqualToFileName.txt";
        System.IO.FileStream testFile = System.IO.File.Open(fileName, System.IO.FileMode.Create);
        System.Text.ASCIIEncoding asciiEncd = new System.Text.ASCIIEncoding();
        string testStr = "Hello World";
        bool retValue = false;

        testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
        testFile.Close();
        try
        {
            retValue = VerifyCtor(portName, typeof(ArgumentException), ThrowAt.Open);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            System.IO.File.Delete(fileName);
        }
        return retValue;
    }


    public bool PHYSICALDRIVE0()
    {
        string portName = "PHYSICALDRIVE0";

        return VerifyCtor(portName, typeof(ArgumentException), ThrowAt.Open);
    }


    private bool VerifyCtor(string portName)
    {
        return VerifyCtor(portName, null, ThrowAt.Set);
    }


    private bool VerifyCtor(string portName, Type expectedException, ThrowAt throwAt)
    {
        SerialPortProperties serPortProp = new SerialPortProperties();

        Console.WriteLine("Verifying properties where PortName={0}", portName);
        try
        {
            SerialPort com = new SerialPort(portName);

            if (null != expectedException && throwAt == ThrowAt.Set)
            {
                Console.WriteLine("Err_7212ahsdj Expected Ctor to throw {0}", expectedException);
                return false;
            }

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", portName);

            return serPortProp.VerifyPropertiesAndPrint(com);
        }
        catch (Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("Err_07081hadnh Did not expect exception to be thrown and the following was thrown: \n{0}", e);
                return false;
            }
            else if (throwAt == ThrowAt.Open)
            {
                Console.WriteLine("Err_88916adfa Expected {0} to be thrown at Open and the following was thrown at Set: \n{1}", expectedException, e);
                return false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("Err_90282ahwhp Expected {0} to be thrown and the following was thrown: \n{1}", expectedException, e);
                return false;
            }

            return true;
        }
    }
}

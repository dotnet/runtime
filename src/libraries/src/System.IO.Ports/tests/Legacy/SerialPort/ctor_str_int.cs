// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections;

public class ctor_str_int
{
    public static readonly String s_strActiveBugNums = "";
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ctor(string, int)";
    public static readonly String s_strTFName = "ctor_str_int.cs";
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
        ctor_str_int objTest = new ctor_str_int();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1_9600), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM2_14400), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM3_28800), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM4_57600), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM256_115200), TCSupport.SerialPortRequirements.None);

        //[] Error checking for PortName
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Empty_9600), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Null_14400), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SlashSlash_28800), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM257_57600), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Filename_9600), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PHYSICALDRIVE0_14400), TCSupport.SerialPortRequirements.None);

        //[] Error checking for BaudRate
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1_Int32MinValue), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM2_Neg1), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM3_0), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM4_Int32MaxValue), TCSupport.SerialPortRequirements.None);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool COM1_9600()
    {
        string portName = "COM1";
        int baudRate = 9600;

        return VerifyCtor(portName, baudRate);
    }


    public bool COM2_14400()
    {
        string portName = "COM2";
        int baudRate = 14400;

        return VerifyCtor(portName, baudRate);
    }


    public bool COM3_28800()
    {
        string portName = "COM3";
        int baudRate = 28800;

        return VerifyCtor(portName, baudRate);
    }


    public bool COM4_57600()
    {
        string portName = "COM4";
        int baudRate = 57600;

        return VerifyCtor(portName, baudRate);
    }


    public bool COM256_115200()
    {
        string portName = "COM256";
        int baudRate = 115200;

        return VerifyCtor(portName, baudRate);
    }


    //[] Error checking for PortName
    public bool Empty_9600()
    {
        string portName = String.Empty;
        int baudRate = 9600;

        return VerifyCtor(portName, baudRate, typeof(ArgumentException), ThrowAt.Set);
    }


    public bool Null_14400()
    {
        string portName = null;
        int baudRate = 14400;

        return VerifyCtor(portName, baudRate, typeof(ArgumentNullException), ThrowAt.Set);
    }


    public bool SlashSlash_28800()
    {
        string portName = "\\\\";
        int baudRate = 28800;

        return VerifyCtor(portName, baudRate, typeof(ArgumentException), ThrowAt.Set);
    }


    public bool COM257_57600()
    {
        string portName = "COM257";
        int baudRate = 57600;

        return VerifyCtor(portName, baudRate);
    }


    public bool Filename_9600()
    {
        string portName;
        int baudRate = 9600;
        string fileName = portName = "PortNameEqualToFileName.txt";
        System.IO.FileStream testFile = System.IO.File.Open(fileName, System.IO.FileMode.Create);
        System.Text.ASCIIEncoding asciiEncd = new System.Text.ASCIIEncoding();
        string testStr = "Hello World";
        bool retValue = false;

        testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
        testFile.Close();
        try
        {
            retValue = VerifyCtor(portName, baudRate, typeof(ArgumentException), ThrowAt.Open);
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


    public bool PHYSICALDRIVE0_14400()
    {
        string portName = "PHYSICALDRIVE0";
        int baudRate = 14400;

        return VerifyCtor(portName, baudRate, typeof(ArgumentException), ThrowAt.Open);
    }


    //[] Error checking for BaudRate
    public bool COM1_Int32MinValue()
    {
        string portName = "Com1";
        int baudRate = Int32.MinValue;

        return VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM2_Neg1()
    {
        string portName = "Com2";
        int baudRate = -1;

        return VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM3_0()
    {
        string portName = "Com3";
        int baudRate = 0;

        return VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM4_Int32MaxValue()
    {
        string portName = "Com4";
        int baudRate = Int32.MaxValue;

        return VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Open);
    }


    private bool VerifyCtor(string portName, int baudRate)
    {
        return VerifyCtor(portName, baudRate, null, ThrowAt.Set);
    }


    private bool VerifyCtor(string portName, int baudRate, Type expectedException, ThrowAt throwAt)
    {
        SerialPortProperties serPortProp = new SerialPortProperties();

        Console.WriteLine("Verifying properties where PortName={0},BaudRate={1}", portName, baudRate);
        try
        {
            SerialPort com = new SerialPort(portName, baudRate);

            if (null != expectedException && throwAt == ThrowAt.Set)
            {
                Console.WriteLine("Err_7212ahsdj Expected Ctor to throw {0}", expectedException);
                return false;
            }

            serPortProp.SetAllPropertiesToDefaults();

            serPortProp.SetProperty("PortName", portName);
            serPortProp.SetProperty("BaudRate", baudRate);

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

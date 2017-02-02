// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.IO.Ports;
using System.Collections;

public class ctor_str_int_parity_int
{
    public static readonly String s_strActiveBugNums = "";
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ctor(string, int, parity, int)";
    public static readonly String s_strTFName = "ctor_str_int_parity_int.cs";
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
        ctor_str_int_parity_int objTest = new ctor_str_int_parity_int();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1_9600_Odd_5), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM2_14400_None_5), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM3_28800_Mark_6), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM4_57600_Space_7), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM256_115200_Even_8), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1_9600_None_8), TCSupport.SerialPortRequirements.None);

        //[] Error checking for PortName
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Empty_9600_None_5), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Null_14400_Even_6), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SlashSlash_28800_Odd_7), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM257_57600_Mark_8), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Filename_9600_Space_8), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PHYSICALDRIVE0_14400_Even_5), TCSupport.SerialPortRequirements.None);

        //[] Error checking for BaudRate
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1_Int32MinValue_None_5), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM2_Neg1_Even_6), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM3_0_Odd_7), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM4_Int32MaxValue_Mark_8), TCSupport.SerialPortRequirements.None);

        //[] Error checking for Parity
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1_9600_Int32MinValue_5), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM2_14400_Neg1_6), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM3_28800_5_7), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM4_57600_Int32MaxValue_8), TCSupport.SerialPortRequirements.None);

        //[] Error checking for DataBits
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM1_9600_None_Int32MinValue), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM2_14400_Even_Neg1), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM3_28800_Odd_4), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM4_57600_Mark_9), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(COM255_115200_Space_Int32MaxValue), TCSupport.SerialPortRequirements.None);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool COM1_9600_Odd_5()
    {
        string portName = "COM1";
        int baudRate = 9600;
        int parity = (int)Parity.Odd;
        int dataBits = 5;

        return VerifyCtor(portName, baudRate, parity, dataBits);
    }


    public bool COM2_14400_None_5()
    {
        string portName = "COM2";
        int baudRate = 14400;
        int parity = (int)Parity.None;
        int dataBits = 5;

        return VerifyCtor(portName, baudRate, parity, dataBits);
    }


    public bool COM3_28800_Mark_6()
    {
        string portName = "COM3";
        int baudRate = 28800;
        int parity = (int)Parity.Mark;
        int dataBits = 6;

        return VerifyCtor(portName, baudRate, parity, dataBits);
    }


    public bool COM4_57600_Space_7()
    {
        string portName = "COM4";
        int baudRate = 57600;
        int parity = (int)Parity.Space;
        int dataBits = 7;

        return VerifyCtor(portName, baudRate, parity, dataBits);
    }


    public bool COM256_115200_Even_8()
    {
        string portName = "COM256";
        int baudRate = 115200;
        int parity = (int)Parity.Even;
        int dataBits = 8;

        return VerifyCtor(portName, baudRate, parity, dataBits);
    }


    public bool COM1_9600_None_8()
    {
        string portName = "COM1";
        int baudRate = 9600;
        int parity = (int)Parity.None;
        int dataBits = 8;

        return VerifyCtor(portName, baudRate, parity, dataBits);
    }


    //[] Error checking for PortName
    public bool Empty_9600_None_5()
    {
        string portName = String.Empty;
        int baudRate = 9600;
        int parity = (int)Parity.None;
        int dataBits = 5;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentException), ThrowAt.Set);
    }


    public bool Null_14400_Even_6()
    {
        string portName = null;
        int baudRate = 14400;
        int parity = (int)Parity.Even;
        int dataBits = 6;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentNullException), ThrowAt.Set);
    }


    public bool SlashSlash_28800_Odd_7()
    {
        string portName = "\\\\";
        int baudRate = 28800;
        int parity = (int)Parity.Odd;
        int dataBits = 7;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentException), ThrowAt.Set);
    }


    public bool COM257_57600_Mark_8()
    {
        string portName = "COM257";
        int baudRate = 57600;
        int parity = (int)Parity.Mark;
        int dataBits = 8;

        return VerifyCtor(portName, baudRate, parity, dataBits);
    }


    public bool Filename_9600_Space_8()
    {
        string portName;
        int baudRate = 9600;
        int parity = (int)Parity.Space;
        int dataBits = 8;
        string fileName = portName = "PortNameEqualToFileName.txt";
        System.IO.FileStream testFile = System.IO.File.Open(fileName, System.IO.FileMode.Create);
        System.Text.ASCIIEncoding asciiEncd = new System.Text.ASCIIEncoding();
        string testStr = "Hello World";
        bool retValue = false;

        testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
        testFile.Close();
        try
        {
            retValue = VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentException), ThrowAt.Open);
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


    public bool PHYSICALDRIVE0_14400_Even_5()
    {
        string portName = "PHYSICALDRIVE0";
        int baudRate = 14400;
        int parity = (int)Parity.Even;
        int dataBits = 5;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentException), ThrowAt.Open);
    }


    //[] Error checking for BaudRate
    public bool COM1_Int32MinValue_None_5()
    {
        string portName = "Com1";
        int baudRate = Int32.MinValue;
        int parity = (int)Parity.None;
        int dataBits = 5;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM2_Neg1_Even_6()
    {
        string portName = "Com2";
        int baudRate = -1;
        int parity = (int)Parity.Even;
        int dataBits = 6;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM3_0_Odd_7()
    {
        string portName = "Com3";
        int baudRate = 0;
        int parity = (int)Parity.Odd;
        int dataBits = 7;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM4_Int32MaxValue_Mark_8()
    {
        string portName = "Com4";
        int baudRate = Int32.MaxValue;
        int parity = (int)Parity.Mark;
        int dataBits = 8;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Open);
    }


    //[] Error checking for Parity
    public bool COM1_9600_Int32MinValue_5()
    {
        string portName = "Com1";
        int baudRate = 9600;
        int parity = Int32.MinValue;
        int dataBits = 5;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM2_14400_Neg1_6()
    {
        string portName = "Com2";
        int baudRate = 14400;
        int parity = -1;
        int dataBits = 6;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM3_28800_5_7()
    {
        string portName = "Com3";
        int baudRate = 28800;
        int parity = 5;
        int dataBits = 7;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM4_57600_Int32MaxValue_8()
    {
        string portName = "Com4";
        int baudRate = 57600;
        int parity = Int32.MaxValue;
        int dataBits = 8;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    //[] Error checking for DataBits
    public bool COM1_9600_None_Int32MinValue()
    {
        string portName = "Com1";
        int baudRate = 9600;
        int parity = (int)Parity.None;
        int dataBits = Int32.MinValue;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM2_14400_Even_Neg1()
    {
        string portName = "Com2";
        int baudRate = 14400;
        int parity = (int)Parity.Even;
        int dataBits = -1;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM3_28800_Odd_4()
    {
        string portName = "Com3";
        int baudRate = 28800;
        int parity = (int)Parity.Odd;
        int dataBits = 4;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM4_57600_Mark_9()
    {
        string portName = "Com4";
        int baudRate = 57600;
        int parity = (int)Parity.Mark;
        int dataBits = 9;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    public bool COM255_115200_Space_Int32MaxValue()
    {
        string portName = "Com255";
        int baudRate = 115200;
        int parity = (int)Parity.Space;
        int dataBits = Int32.MaxValue;

        return VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
    }


    private bool VerifyCtor(string portName, int baudRate, int parity, int dataBits)
    {
        return VerifyCtor(portName, baudRate, parity, dataBits, null, ThrowAt.Set);
    }


    private bool VerifyCtor(string portName, int baudRate, int parity, int dataBits, Type expectedException, ThrowAt throwAt)
    {
        SerialPortProperties serPortProp = new SerialPortProperties();

        Console.WriteLine("Verifying properties where PortName={0},BaudRate={1},Parity={2},DatBits={3}", portName, baudRate, parity, dataBits);
        try
        {
            SerialPort com = new SerialPort(portName, baudRate, (Parity)parity, dataBits);

            if (null != expectedException && throwAt == ThrowAt.Set)
            {
                Console.WriteLine("Err_7212ahsdj Expected Ctor to throw {0}", expectedException);
                return false;
            }

            serPortProp.SetAllPropertiesToDefaults();

            serPortProp.SetProperty("PortName", portName);
            serPortProp.SetProperty("BaudRate", baudRate);
            serPortProp.SetProperty("Parity", (Parity)parity);
            serPortProp.SetProperty("DataBits", dataBits);

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


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class Parity_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Parity";
    public static readonly String s_strTFName = "Parity.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default number of bytes to read/write to verify the speed of the port
    //and that the bytes were transfered successfully
    public static readonly int DEFAULT_BYTE_SIZE = 512;

    //If the percentage difference between the expected time to transfer with the specified parity
    //and the actual time found through Stopwatch is greater then 10% then the Parity value was not correctly
    //set and the testcase fails.
    public static readonly double MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE = .10;

    //The default number of databits to use when testing Parity
    public static readonly int DEFUALT_DATABITS = 8;
    public static readonly int NUM_TRYS = 3;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Parity_Property objTest = new Parity_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Default), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_None_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Even_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Odd_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Mark_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Space_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_None_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Even_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Odd_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Mark_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Space_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_None_DataBits_5), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Even_DataBits_5_Read), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Odd_DataBits_5_Read), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Mark_DataBits_5_Read), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Space_DataBits_5_Read), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Even_DataBits_5_Write), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Odd_DataBits_5_Write), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Mark_DataBits_5_Write), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Space_DataBits_5_Write), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Neg1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_5), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Int32MaxValue), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Even_Odd), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Odd_Even), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Parity_Odd_Mark), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Parity_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default Parity");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParity(com1, DEFAULT_BYTE_SIZE);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default Parity FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool Parity_None_BeforeOpen()
    {
        Console.WriteLine("Verifying None Parity before open");
        if (!VerifyParityBeforeOpen((int)Parity.None, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_002!!! Verifying None Parity before open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Even_BeforeOpen()
    {
        Console.WriteLine("Verifying Even Parity before open");
        if (!VerifyParityBeforeOpen((int)Parity.Even, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_003!!! Verifying Even Parity before open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Odd_BeforeOpen()
    {
        Console.WriteLine("Verifying Odd Parity before open");
        if (!VerifyParityBeforeOpen((int)Parity.Odd, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_004!!! Verifying Odd Parity before open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Mark_BeforeOpen()
    {
        Console.WriteLine("Verifying Mark Parity before open");
        if (!VerifyParityBeforeOpen((int)Parity.Mark, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_005!!! Verifying Mark Parity before open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Space_BeforeOpen()
    {
        Console.WriteLine("Verifying Space before open");
        if (!VerifyParityBeforeOpen((int)Parity.Space, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_006!!! Verifying Space Parity before open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_None_AfterOpen()
    {
        Console.WriteLine("Verifying None Parity after open");
        if (!VerifyParityAfterOpen((int)Parity.None, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_007!!! Verifying None Parity after open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Even_AfterOpen()
    {
        Console.WriteLine("Verifying Even Parity after open");
        if (!VerifyParityAfterOpen((int)Parity.Even, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_008!!! Verifying Even Parity after open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Odd_AfterOpen()
    {
        Console.WriteLine("Verifying Odd Parity after open");
        if (!VerifyParityAfterOpen((int)Parity.Odd, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_009!!! Verifying Odd Parity after open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Mark_AfterOpen()
    {
        Console.WriteLine("Verifying Mark Parity after open");
        if (!VerifyParityAfterOpen((int)Parity.Mark, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_010!!! Verifying Mark Parity after open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Space_AfterOpen()
    {
        Console.WriteLine("Verifying Space Parity after open");
        if (!VerifyParityAfterOpen((int)Parity.Space, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_011!!! Verifying Space Parity after open FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_None_DataBits_5()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying None Parity with 5 DataBits");
        com1.DataBits = 5;
        com1.Open();
        if (!VerifyParity(com1, DEFAULT_BYTE_SIZE, 5))
        {
            Console.WriteLine("Err_012!!! Verifying None Parity with 5 DataBits FAILED");
            return false;
        }

        if (com1.IsOpen)
            com1.Close();

        return true;
    }


    public bool Parity_Even_DataBits_5_Read()
    {
        Console.WriteLine("Verifying Even Parity with 5 DataBits on Read");
        if (!VerifyReadParity((int)Parity.Even, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_013!!! Verifying Even Parity with 5 DataBits on Read FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Odd_DataBits_5_Read()
    {
        Console.WriteLine("Verifying Odd Parity with 5 DataBits on Read");
        if (!VerifyReadParity((int)Parity.Odd, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_014!!! Verifying Odd Parity with 5 DataBits on Read FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Mark_DataBits_5_Read()
    {
        Console.WriteLine("Verifying Mark Parity with 5 DataBits on Read");
        if (!VerifyReadParity((int)Parity.Mark, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_015!!! Verifying Mark Parity with 5 DataBits on Read FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Space_DataBits_5_Read()
    {
        Console.WriteLine("Verifying Space Parity with 5 DataBits on Read");
        if (!VerifyReadParity((int)Parity.Space, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_016!!! Verifying Space Parity with 5 DataBits on Read FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Even_DataBits_5_Write()
    {
        Console.WriteLine("Verifying Even Parity with 5 DataBits on Write");
        if (!VerifyWriteParity((int)Parity.Even, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_017!!! Verifying Even Parity with 5 DataBits on Write FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Odd_DataBits_5_Write()
    {
        Console.WriteLine("Verifying Odd Parity with 5 DataBits on Write");
        if (!VerifyWriteParity((int)Parity.Odd, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_018!!! Verifying Odd Parity with 5 DataBits on Write FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Mark_DataBits_5_Write()
    {
        Console.WriteLine("Verifying Mark Parity with 5 DataBits on Write");
        if (!VerifyWriteParity((int)Parity.Mark, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_019!!! Verifying Mark Parity with 5 DataBits on Write FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Space_DataBits_5_Write()
    {
        Console.WriteLine("Verifying Space Parity with 5 DataBits on Write");
        if (!VerifyWriteParity((int)Parity.Space, 5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_020!!! Verifying Space Parity with 5 DataBits on Write FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue Parity");
        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_021!!! Verifying Int32.MinValue Parity FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Neg1()
    {
        Console.WriteLine("Verifying -1 Parity");
        if (!VerifyException(-1, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_022!!! Verifying -1 Parity FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_5()
    {
        Console.WriteLine("Verifying 5 Parity");
        if (!VerifyException(5, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_023!!! Verifying 5 Parity FAILED");
            return false;
        }

        return true;
    }


    public bool Parity_Int32MaxValue()
    {
        Console.WriteLine("Verifying Int32.MaxValue Parity");
        if (!VerifyException(Int32.MaxValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_024!!! Verifying Int32.MaxValue Parity FAILED");
            return false;
        }

        return true;
    }

    public bool Parity_Even_Odd()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying Parity Even and then Odd");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        com1.Parity = Parity.Even;
        com1.Parity = Parity.Odd;
        serPortProp.SetProperty("Parity", Parity.Odd);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParity(com1, DEFAULT_BYTE_SIZE);

        if (!retValue)
        {
            Console.WriteLine("Err_551808ahied!!! Verifying Parity Even and then Odd FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool Parity_Odd_Even()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying Parity Odd and then Even");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        com1.Parity = Parity.Odd;
        com1.Parity = Parity.Even;
        serPortProp.SetProperty("Parity", Parity.Even);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParity(com1, DEFAULT_BYTE_SIZE);

        if (!retValue)
        {
            Console.WriteLine("Err_648ahied!!! Verifying Parity Odd and then Even FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool Parity_Odd_Mark()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying Parity Odd and then Mark");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        com1.Parity = Parity.Odd;
        com1.Parity = Parity.Mark;
        serPortProp.SetProperty("Parity", Parity.Mark);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParity(com1, DEFAULT_BYTE_SIZE);

        if (!retValue)
        {
            Console.WriteLine("Err_05188ahiued!!! Verifying Parity Odd and then Mark FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyException(int parity, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, parity, throwAt, expectedException);
        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, parity, expectedException);
        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, int parity, ThrowAt throwAt, System.Type expectedException)
    {
        int origParity = (int)com.Parity;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("Parity", (Parity)parity);

        try
        {
            com.Parity = (Parity)parity;

            if (ThrowAt.Open == throwAt)
                com.Open();

            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        com.Parity = (Parity)origParity;

        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, int parity, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.Parity = (Parity)parity;
            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Parity after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Parity after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Parity after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        return retValue;
    }


    private bool VerifyParityBeforeOpen(int parity, int numBytesToSend)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Parity = (Parity)parity;
        com1.Open();
        serPortProp.SetProperty("Parity", (Parity)parity);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParity(com1, numBytesToSend);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyParityAfterOpen(int parity, int numBytesToSend)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.Parity = (Parity)parity;
        serPortProp.SetProperty("Parity", (Parity)parity);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParity(com1, numBytesToSend);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyParity(SerialPort com1, int numBytesToSend)
    {
        return VerifyParity(com1, numBytesToSend, DEFUALT_DATABITS);
    }


    private bool VerifyParity(SerialPort com1, int numBytesToSend, int dataBits)
    {
        bool retValue = true;
        byte[] xmitBytes = new byte[numBytesToSend];
        byte[] expectedBytes = new byte[numBytesToSend];
        Random rndGen = new System.Random();
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        byte shiftMask = 0xFF;

        //Create a mask that when logicaly and'd with the transmitted byte will 
        //will result in the byte recievied due to the leading bits being chopped
        //off due to Parity less then 8
        if (8 > dataBits)
            shiftMask >>= 8 - com1.DataBits;

        //Generate some random bytes to read/write for this Parity setting
        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
            expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
        }

        com2.DataBits = dataBits;
        com2.Parity = com1.Parity;
        com1.DataBits = dataBits;
        com2.Open();

        retValue &= PerformWriteRead(com1, com2, xmitBytes, expectedBytes);

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private bool VerifyReadParity(int parity, int dataBits, int numBytesToSend)
    {
        bool retValue = true;
        byte[] xmitBytes = new byte[numBytesToSend];
        byte[] expectedBytes = new byte[numBytesToSend];
        Random rndGen = new System.Random();
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        byte shiftMask = 0xFF;
        bool parityErrorOnLastByte = false, isParityError = false;
        int parityIndex;
        byte parityErrorByte;

        if (8 > dataBits)
            com1.DataBits = dataBits + 1;
        else
            com1.Parity = (Parity)parity;

        com2.Parity = (Parity)parity;
        com2.DataBits = dataBits;
        com1.StopBits = StopBits.One;
        com2.StopBits = StopBits.One;

        //Create a mask that when logicaly and'd with the transmitted byte will 
        //will result in the byte recievied due to the leading bits being chopped
        //off due to Parity less then 8
        shiftMask >>= 8 - dataBits;

        //Generate some random bytes to read/write for this Parity setting
        for (int i = 0; i < xmitBytes.Length; i++)
        {
            do
            {
                xmitBytes[i] = (byte)rndGen.Next(0, 256);
                isParityError = !VerifyParityByte(xmitBytes[i], com1.DataBits, (Parity)parity);
            } while (isParityError); //Prevent adacent parity errors see VSWhidbey 103979

            expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
            parityErrorOnLastByte = isParityError;
        }

        do
        {
            parityErrorByte = (byte)rndGen.Next(0, 256);
            isParityError = !VerifyParityByte(parityErrorByte, com1.DataBits, (Parity)parity);
        } while (!isParityError);

        parityIndex = rndGen.Next(xmitBytes.Length / 4, xmitBytes.Length / 2);
        xmitBytes[parityIndex] = parityErrorByte;
        expectedBytes[parityIndex] = com2.ParityReplace;

        Console.WriteLine("parityIndex={0}", parityIndex);

        parityIndex = rndGen.Next((3 * xmitBytes.Length) / 4, xmitBytes.Length - 1);
        xmitBytes[parityIndex] = parityErrorByte;
        expectedBytes[parityIndex] = com2.ParityReplace;

        Console.WriteLine("parityIndex={0}", parityIndex);

        /*
            for(int i=0; i<xmitBytes.Length; i++) {
              do {
                xmitBytes[i] = (byte)rndGen.Next(0, 256);
                isParityError = !VerifyParityByte(xmitBytes[i], com1.DataBits, (Parity)parity);
              }while(parityErrorOnLastByte && isParityError); //Prevent adacent parity errors see VSWhidbey 103979

              expectedBytes[i] =  isParityError ? com2.ParityReplace :(byte)(xmitBytes[i] & shiftMask);
              parityErrorOnLastByte = isParityError;
            }
        */
        com1.Open();
        com2.Open();
        retValue &= PerformWriteRead(com1, com2, xmitBytes, expectedBytes);
        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private bool VerifyWriteParity(int parity, int dataBits, int numBytesToSend)
    {
        bool retValue = true;
        byte[] xmitBytes = new byte[numBytesToSend];
        byte[] expectedBytes = new byte[numBytesToSend];
        Random rndGen = new System.Random();
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        //Generate some random bytes to read/write for this Parity setting
        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
            expectedBytes[i] = SetParityBit((byte)(xmitBytes[i]), dataBits, (Parity)parity);
        }

        if (8 > dataBits)
            com2.DataBits = dataBits + 1;
        else
            com2.Parity = (Parity)parity;

        com1.Parity = (Parity)parity;
        com1.DataBits = dataBits;
        com1.Open();
        com2.Open();

        retValue &= PerformWriteRead(com1, com2, xmitBytes, expectedBytes);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private bool PerformWriteRead(SerialPort com1, SerialPort com2, byte[] xmitBytes, byte[] expectedBytes)
    {
        bool retValue = true;
        byte[] rcvBytes = new byte[expectedBytes.Length * 4];
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        double expectedTime, actualTime, percentageDifference;
        int numParityBits = (Parity)com1.Parity == Parity.None ? 0 : 1;
        double numStopBits = GetNumberOfStopBits(com1);
        int length = xmitBytes.Length;
        int rcvLength;

        // TODO: Consider removing all of the code to check the time it takes to transfer the bytes. 
        // This was likely just a copy and paste from another test case
        actualTime = 0;
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        for (int i = 0; i < NUM_TRYS; i++)
        {
            com2.DiscardInBuffer();

            IAsyncResult beginWriteResult = com1.BaseStream.BeginWrite(xmitBytes, 0, length, null, null);

            while (0 == com2.BytesToRead) ;

            sw.Start();
            while (length > com2.BytesToRead) ; //Wait for all of the bytes to reach the input buffer of com2

            sw.Stop();
            actualTime += sw.ElapsedMilliseconds;
            beginWriteResult.AsyncWaitHandle.WaitOne();
            sw.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;
        expectedTime = ((xmitBytes.Length * (1 + numStopBits + com1.DataBits + numParityBits)) / com1.BaudRate) * 1000;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / expectedTime);

        //If the percentageDifference between the expected time and the actual time is to high
        //then the expected baud rate must not have been used and we should report an error
        if (MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE < percentageDifference)
        {
            Console.WriteLine("ERROR!!! Parity not used Expected time:{0}, actual time:{1} percentageDifference:{2}", expectedTime, actualTime, percentageDifference);
            retValue = false;
        }

        rcvLength = com2.Read(rcvBytes, 0, rcvBytes.Length);
        if (0 != com2.BytesToRead)
        {
            Console.WriteLine("ERROR!!! BytesToRead={0} expected 0", com2.BytesToRead);
            retValue = false;
        }

        //Verify that the bytes we sent were the same ones we received
        int expectedIndex = 0, actualIndex = 0;

        for (; expectedIndex < expectedBytes.Length && actualIndex < rcvBytes.Length; ++expectedIndex, ++actualIndex)
        {
            if (expectedBytes[expectedIndex] != rcvBytes[actualIndex])
            {
                if (actualIndex != rcvBytes.Length - 1 && expectedBytes[expectedIndex] == rcvBytes[actualIndex + 1])
                {
                    //Sometimes if there is a parity error an extra byte gets added to the input stream so 
                    //look ahead at the next byte
                    actualIndex++;
                }
                else
                {
                    Console.WriteLine(
                        "ERROR: Expected to read {0,2:X} at {1,3} actually read {2,2:X} sent {3,2:X}",
                        expectedBytes[expectedIndex],
                        expectedIndex,
                        rcvBytes[actualIndex],
                        xmitBytes[expectedIndex]);

                    Console.WriteLine();
                    Console.WriteLine("Bytes Sent:");
                    TCSupport.PrintBytes(xmitBytes);
                    Console.WriteLine();

                    Console.WriteLine();
                    Console.WriteLine("Bytes Recieved:");
                    TCSupport.PrintBytes(rcvBytes);
                    Console.WriteLine();

                    Console.WriteLine();
                    Console.WriteLine("Expected Bytes:");
                    TCSupport.PrintBytes(expectedBytes);
                    Console.WriteLine();

                    retValue = false;
                    break;
                }
            }
        }

        if (expectedIndex < expectedBytes.Length)
        {
            Console.WriteLine("ERRROR: Did not enumerate all of the expected bytes index={0} length={1}", expectedIndex, expectedBytes.Length);
            retValue = false;
        }

        return retValue;
    }


    private double GetNumberOfStopBits(SerialPort com)
    {
        double stopBits = -1;

        switch ((int)com.StopBits)
        {
            case (int)StopBits.One:
                stopBits = 1.0;
                break;

            case (int)StopBits.OnePointFive:
                stopBits = 1.5;
                break;

            case (int)StopBits.Two:
                stopBits = 2.0;
                break;
        }
        return stopBits;
    }


    private byte SetParityBit(byte parityByte, int dataBitSize, Parity parity)
    {
        byte result;

        if (8 == dataBitSize)
        {
            return parityByte;
        }

        switch (parity)
        {
            case Parity.Even:
                if (VerifyEvenParity(parityByte, dataBitSize))
                    result = (byte)(parityByte & (0xFF >> 8 - dataBitSize));
                else
                    result = (byte)(parityByte | (0x80 >> 8 - (dataBitSize + 1)));

                break;

            case Parity.Odd:
                if (VerifyOddParity(parityByte, dataBitSize))
                    result = (byte)(parityByte & (0xFF >> 8 - dataBitSize));
                else
                    result = (byte)(parityByte | (0x80 >> 8 - (dataBitSize + 1)));

                break;

            case Parity.Mark:
                result = (byte)(parityByte | (0x80 >> 8 - (dataBitSize + 1)));
                break;

            case Parity.Space:
                result = (byte)(parityByte & (0xFF >> 8 - dataBitSize));
                break;

            default:
                result = 0;
                break;
        }
        if (7 > dataBitSize)
            result &= (byte)(0xFF >> 8 - (dataBitSize + 1));

        return result;
    }


    private bool VerifyParityByte(byte parityByte, int parityWordSize, Parity parity)
    {
        switch (parity)
        {
            case Parity.Even:
                return VerifyEvenParity(parityByte, parityWordSize);

            case Parity.Odd:
                return VerifyOddParity(parityByte, parityWordSize);

            case Parity.Mark:
                return VerifyMarkParity(parityByte, parityWordSize);

            case Parity.Space:
                return VerifySpaceParity(parityByte, parityWordSize);

            default:
                return false;
        }
    }


    private bool VerifyEvenParity(byte parityByte, int parityWordSize)
    {
        return (0 == CalcNumberOfTrueBits(parityByte, parityWordSize) % 2);
    }


    private bool VerifyOddParity(byte parityByte, int parityWordSize)
    {
        return (1 == CalcNumberOfTrueBits(parityByte, parityWordSize) % 2);
    }


    private bool VerifyMarkParity(byte parityByte, int parityWordSize)
    {
        byte parityMask = 0x80;

        parityByte <<= 8 - parityWordSize;
        return (0 != (parityByte & parityMask));
    }


    private bool VerifySpaceParity(byte parityByte, int parityWordSize)
    {
        byte parityMask = 0x80;

        parityByte <<= 8 - parityWordSize;
        return (0 == (parityByte & parityMask));
    }


    private int CalcNumberOfTrueBits(byte parityByte, int parityWordSize)
    {
        byte parityMask = 0x80;
        int numTrueBits = 0;

        //Console.WriteLine("parityByte={0}", System.Convert.ToString(parityByte, 16));
        parityByte <<= 8 - parityWordSize;

        for (int i = 0; i < parityWordSize; i++)
        {
            if (0 != (parityByte & parityMask))
                numTrueBits++;

            parityByte <<= 1;
        }

        //Console.WriteLine("Number of true bits: {0}", numTrueBits);
        return numTrueBits;
    }
    #endregion
}

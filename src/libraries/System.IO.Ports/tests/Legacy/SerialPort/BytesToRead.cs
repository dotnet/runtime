// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class BytesToRead_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.BytesToRead";
    public static readonly String s_strTFName = "BytesToRead.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;
    public static readonly int DEFUALT_NUM_RND_BYTES = 8;

    public delegate void ReadMethodDelegate(SerialPort com, int bufferSize);

    //The default new lint to read from when testing timeout with ReadTo(str)
    public static readonly string DEFAULT_READ_TO_STRING = "\r\n";

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        BytesToRead_Property objTest = new BytesToRead_Property();
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

        //See individual read methods for further testing
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_Default), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_RcvRndNumBytes), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_Read_byte_int_int), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_Read_char_int_int), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_ReadChar), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_ReadByte), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_ReadLine), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_ReadTo), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_ReadExisting), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool BytesToRead_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default BytesToRead");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default BytesToRead FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool BytesToRead_RcvRndNumBytes()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying BytesToRead after receiving a random number of bytes");
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com2.Open();

        com2.Write(new byte[DEFUALT_NUM_RND_BYTES], 0, DEFUALT_NUM_RND_BYTES);

        serPortProp.SetProperty("BytesToRead", DEFUALT_NUM_RND_BYTES);
        System.Threading.Thread.Sleep(100);//Wait for com1 to get all of the bytes

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying BytesToRead after receiving a random number of bytes FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool BytesToRead_Read_byte_int_int()
    {
        Console.WriteLine("Verifying BytesToRead with Read(byte[] buffer, int offset, int count)");
        if (!VerifyBytesToRead(new ReadMethodDelegate(Read_byte_int_int), DEFUALT_NUM_RND_BYTES, false))
        {
            Console.WriteLine("Err_003!!! Verifying BytesToRead with Read(byte[] buffer, int offset, int count) FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_Read_char_int_int()
    {
        Console.WriteLine("Verifying BytesToRead with Read(char[] buffer, int offset, int count)");
        if (!VerifyBytesToRead(new ReadMethodDelegate(Read_char_int_int), DEFUALT_NUM_RND_BYTES, false))
        {
            Console.WriteLine("Err_004!!! Verifying BytesToRead with Read(char[] buffer, int offset, int count) FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_ReadByte()
    {
        Console.WriteLine("Verifying BytesToRead with ReadByte()");
        if (!VerifyBytesToRead(new ReadMethodDelegate(ReadByte), 1, false))
        {
            Console.WriteLine("Err_005!!! Verifying BytesToRead with ReadByte() FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_ReadChar()
    {
        Console.WriteLine("Verifying BytesToRead with ReadChar()");
        if (!VerifyBytesToRead(new ReadMethodDelegate(ReadChar), 1, false))
        {
            Console.WriteLine("Err_006!!! Verifying BytesToRead with ReadChar() FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_ReadLine()
    {
        Console.WriteLine("Verifying BytesToRead with ReadLine()");
        if (!VerifyBytesToRead(new ReadMethodDelegate(ReadLine), DEFUALT_NUM_RND_BYTES, true))
        {
            Console.WriteLine("Err_007!!! Verifying BytesToRead with ReadLine() FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_ReadTo()
    {
        Console.WriteLine("Verifying BytesToRead with ReadTo(string value)");
        if (!VerifyBytesToRead(new ReadMethodDelegate(ReadTo), DEFUALT_NUM_RND_BYTES, true))
        {
            Console.WriteLine("Err_008!!! Verifying BytesToRead with ReadTo(string value) FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_ReadExisting()
    {
        Console.WriteLine("Verifying BytesToRead with ReadExisting()");
        if (!VerifyBytesToRead(new ReadMethodDelegate(ReadExisting), DEFUALT_NUM_RND_BYTES, false))
        {
            Console.WriteLine("Err_009!!! Verifying BytesToRead with ReadExisting() FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases

    private bool VerifyBytesToRead(ReadMethodDelegate readMethod, int bufferSize, bool sendNewLine)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;
        int numNewLineBytes = 0;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com2.Open();

        com2.Write(new byte[bufferSize], 0, bufferSize);

        if (sendNewLine)
        {
            com2.Write(com2.NewLine);
            numNewLineBytes = com2.Encoding.GetByteCount(com2.NewLine.ToCharArray());
        }

        while (bufferSize + numNewLineBytes > com1.BytesToRead)
            System.Threading.Thread.Sleep(10);

        readMethod(com1, bufferSize + numNewLineBytes);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private void Read_byte_int_int(SerialPort com, int bufferSize)
    {
        com.Read(new byte[bufferSize], 0, bufferSize);
    }


    private void Read_char_int_int(SerialPort com, int bufferSize)
    {
        com.Read(new char[bufferSize], 0, bufferSize);
    }


    private void ReadByte(SerialPort com, int bufferSize)
    {
        com.ReadByte();
    }


    private void ReadChar(SerialPort com, int bufferSize)
    {
        com.ReadChar();
    }


    private void ReadLine(SerialPort com, int bufferSize)
    {
        com.ReadLine();
    }


    private void ReadTo(SerialPort com, int bufferSize)
    {
        com.ReadTo(com.NewLine);
    }


    private void ReadExisting(SerialPort com, int bufferSize)
    {
        com.ReadExisting();
    }
    #endregion
}

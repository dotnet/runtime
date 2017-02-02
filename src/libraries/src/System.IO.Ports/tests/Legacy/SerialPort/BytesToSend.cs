// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class BytesToWrite_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.BytesToWrite";
    public static readonly String s_strTFName = "BytesToWrite.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;
    public static readonly int DEFUALT_NUM_RND_BYTES = 8;

    //The default number of chars to write with when testing timeout with Write(char[], int, int)
    public static readonly int DEFAULT_WRITE_CHAR_ARRAY_SIZE = 8;

    //The default number of bytes to write with when testing timeout with Write(byte[], int, int)
    public static readonly int DEFAULT_WRITE_BYTE_ARRAY_SIZE = 8;

    //The default string to write with when testing timeout with Write(str)
    public static readonly string DEFAULT_STRING_TO_WRITE = "TEST";

    //Delegate to start asynchronous write on the SerialPort com with buffer of size bufferLength
    public delegate int WriteMethodDelegate(SerialPort com, int bufferSize);

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        BytesToWrite_Property objTest = new BytesToWrite_Property();
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
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite_Default), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite_Write_byte_int_int), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite_Write_char_int_int), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite_Write_str), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToWrite_WriteLine), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool BytesToWrite_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default BytesToWrite");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default BytesToWrite FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool BytesToWrite_Write_byte_int_int()
    {
        Console.WriteLine("Verifying BytesToWrite with Write(byte[] buffer, int offset, int count)");
        if (!VerifyBytesToWrite(new WriteMethodDelegate(Write_byte_int_int), DEFUALT_NUM_RND_BYTES, false))
        {
            Console.WriteLine("Err_003!!! Verifying BytesToWrite with Write(byte[] buffer, int offset, int count) FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToWrite_Write_char_int_int()
    {
        Console.WriteLine("Verifying BytesToWrite with Write(char[] buffer, int offset, int count)");
        if (!VerifyBytesToWrite(new WriteMethodDelegate(Write_char_int_int), DEFUALT_NUM_RND_BYTES, false))
        {
            Console.WriteLine("Err_004!!! Verifying BytesToWrite with Write(char[] buffer, int offset, int count) FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToWrite_Write_str()
    {
        Console.WriteLine("Verifying BytesToWrite with WriteChar()");
        if (!VerifyBytesToWrite(new WriteMethodDelegate(Write_str), 1, false))
        {
            Console.WriteLine("Err_006!!! Verifying BytesToWrite with WriteChar() FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToWrite_WriteLine()
    {
        Console.WriteLine("Verifying BytesToWrite with WriteLine()");
        if (!VerifyBytesToWrite(new WriteMethodDelegate(WriteLine), DEFUALT_NUM_RND_BYTES, true))
        {
            Console.WriteLine("Err_007!!! Verifying BytesToWrite with WriteLine() FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyBytesToWrite(WriteMethodDelegate writeMethod, int bufferSize, bool sendNewLine)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;
        IAsyncResult writeMethodResult;
        int expectedlBytesToWrite;
        int acutualBytesToWrite;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
        serPortProp.SetProperty("WriteTimeout", 500);

        com1.Open();
        com1.WriteTimeout = 500;
        com1.Handshake = Handshake.RequestToSend;

        writeMethodResult = writeMethod.BeginInvoke(com1, bufferSize, null, null);
        System.Threading.Thread.Sleep(200);

        acutualBytesToWrite = com1.BytesToWrite;
        writeMethodResult.AsyncWaitHandle.WaitOne();
        expectedlBytesToWrite = (int)writeMethod.EndInvoke(writeMethodResult);

        com2.Open();
        com2.RtsEnable = true;

        if (acutualBytesToWrite != expectedlBytesToWrite)
        {
            Console.WriteLine("ERRROR!!! Expected BytesToWrite={0} acutual BytesToWrite={1}", expectedlBytesToWrite, acutualBytesToWrite);
            retValue = true;
        }

        com2.RtsEnable = false;
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private int Write_byte_int_int(SerialPort com, int bufferSize)
    {
        try
        {
            com.Write(new byte[DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, DEFAULT_WRITE_BYTE_ARRAY_SIZE);
        }
        catch (System.TimeoutException)
        {
        }
        return bufferSize;
    }


    private int Write_char_int_int(SerialPort com, int bufferSize)
    {
        char[] charsToWrite = new char[DEFAULT_WRITE_CHAR_ARRAY_SIZE];

        try
        {
            com.Write(charsToWrite, 0, charsToWrite.Length);
        }
        catch (System.TimeoutException)
        {
        }
        return com.Encoding.GetByteCount(charsToWrite);
    }


    private int Write_str(SerialPort com, int bufferSize)
    {
        try
        {
            com.Write(DEFAULT_STRING_TO_WRITE);
        }
        catch (System.TimeoutException)
        {
        }
        return com.Encoding.GetByteCount(DEFAULT_STRING_TO_WRITE.ToCharArray());
    }


    private int WriteLine(SerialPort com, int bufferSize)
    {
        try
        {
            com.WriteLine(DEFAULT_STRING_TO_WRITE);
        }
        catch (System.TimeoutException)
        {
        }
        return com.Encoding.GetByteCount(DEFAULT_STRING_TO_WRITE.ToCharArray()) + com.Encoding.GetByteCount(com.NewLine.ToCharArray());
    }
    #endregion
}

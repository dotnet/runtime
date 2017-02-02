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
    public static readonly String s_strTFName = "DiscardInBuffer.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The string used with Write(str) to fill the input buffer
    public static readonly string DEFAULT_STRING = "Hello World";

    //The buffer lenght used whe filling the ouput buffer
    public static readonly int DEFAULT_BUFFER_LENGTH = 8;

    //Delegate to start asynchronous write on the SerialPort com with byte[] of size bufferLength
    public delegate void AsyncWriteDelegate(SerialPort com, int bufferLength);

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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBufferFilled_Discard_Once), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBufferFilled_Discard_Multiple), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBufferFilled_Discard_Cycle), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(InAndOutBufferFilled_Discard), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool InBufferFilled_Discard_Once()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying Discard method after input buffer has been filled");
        com1.Open();
        com2.Open();
        com2.Write(DEFAULT_STRING);
        while (com1.BytesToRead < DEFAULT_STRING.Length)
            System.Threading.Thread.Sleep(50);

        retValue &= VerifyDiscard(com1);
        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying Discard method after input buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool InBufferFilled_Discard_Multiple()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying call Discard method several times after input buffer has been filled");
        com1.Open();
        com2.Open();
        com2.Write(DEFAULT_STRING);
        while (com1.BytesToRead < DEFAULT_STRING.Length)
            System.Threading.Thread.Sleep(50);

        retValue &= VerifyDiscard(com1);
        retValue &= VerifyDiscard(com1);
        retValue &= VerifyDiscard(com1);
        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying call Discard method several times after input buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool InBufferFilled_Discard_Cycle()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying call Discard method after input buffer has been filled discarded and filled again");
        com1.Open();
        com2.Open();
        com2.Write(DEFAULT_STRING);
        while (com1.BytesToRead < DEFAULT_STRING.Length)
            System.Threading.Thread.Sleep(50);

        retValue &= VerifyDiscard(com1);
        com2.Write(DEFAULT_STRING);
        while (com1.BytesToRead < DEFAULT_STRING.Length)
            System.Threading.Thread.Sleep(50);

        retValue &= VerifyDiscard(com1);
        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying call Discard method after input buffer has been filled discarded and filled again FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool InAndOutBufferFilled_Discard()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        AsyncWriteDelegate write = new AsyncWriteDelegate(WriteRndByteArray);
        IAsyncResult asyncResult;
        int origBytesToWrite;
        bool retValue = true;

        Console.WriteLine("Verifying Discard method after input buffer has been filled");
        com1.Open();
        com2.Open();
        com1.WriteTimeout = 500;
        asyncResult = write.BeginInvoke(com1, DEFAULT_BUFFER_LENGTH, null, null);
        System.Threading.Thread.Sleep(100);
        origBytesToWrite = com1.BytesToWrite;
        retValue &= VerifyDiscard(com1);
        if (com1.BytesToWrite != origBytesToWrite)
        {
            Console.WriteLine("Expected BytesToWrite={0} after calling DiscardInBuffer() actual={1}", origBytesToWrite, com1.BytesToWrite);
            retValue = false;
        }

        //Wait for write method to timeout
        while (!asyncResult.IsCompleted)
            System.Threading.Thread.Sleep(100);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying Discard method after input buffer has been filled FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private void WriteRndByteArray(SerialPort com, int byteLength)
    {
        byte[] buffer = new byte[byteLength];
        Random rndGen = new Random();

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)rndGen.Next(0, 256);
        }

        try
        {
            com.Write(buffer, 0, buffer.Length);
        }
        catch (System.TimeoutException)
        {
        }
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyDiscard(SerialPort com)
    {
        bool retValue = true;

        com.DiscardInBuffer();
        if (0 != com.BytesToRead)
        {
            Console.WriteLine("Err_2508wqapq!!! Expected BytesToRead=0 Actual BytesToRead={0}", com.BytesToRead);
            retValue = false;
        }

        com.ReadTimeout = 0;

        try
        {
            com.ReadByte();

            Console.WriteLine("Err_2570pqzz!!! Expected ReadByte to timeout");
            retValue = false;
        }
        catch (TimeoutException) { }

        return retValue;
    }
    #endregion
}
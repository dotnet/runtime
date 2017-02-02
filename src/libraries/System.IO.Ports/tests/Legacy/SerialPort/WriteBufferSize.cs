// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Ports;

public class WriteBufferSize_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/04/01 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.WriteBuffer_Property";
    public static readonly String s_strTFName = "WriteBufferSize_Property.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;
    public static readonly int MAX_RANDMOM_BUFFER_SIZE = 1024 * 16;
    public static readonly int LARGE_BUFFER_SIZE = 1024 * 128;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        WriteBufferSize_Property objTest = new WriteBufferSize_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Default), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_NEG1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_0), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_1), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Smaller), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Larger), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Odd), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Even), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Rnd), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(WriteBufferSize_Large), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool WriteBufferSize_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default WriteBufferSize before Open");
        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        Console.WriteLine("Verifying default WriteBufferSize after Open");
        com1.Open();
        serPortProp = new SerialPortProperties();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        Console.WriteLine("Verifying default WriteBufferSize after Close");
        com1.Close();
        serPortProp = new SerialPortProperties();
        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default WriteBufferSize FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool WriteBufferSize_AfterOpen()
    {
        if (!VerifyException(1024, null, typeof(InvalidOperationException)))
        {
            Console.WriteLine("Err_54458ahpba!!! Verifying setting WriteBufferSize to 1024 FAILED");
            return false;
        }

        return true;
    }

    public bool WriteBufferSize_NEG1()
    {
        if (!VerifyException(-1, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_002!!! Verifying setting WriteBufferSize to -1 FAILED");
            return false;
        }

        return true;
    }


    public bool WriteBufferSize_Int32MinValue()
    {
        if (!VerifyException(Int32.MinValue, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_003!!! Verifying setting WriteBufferSize to Int32.MinValue FAILED");
            return false;
        }

        return true;
    }


    public bool WriteBufferSize_0()
    {
        if (!VerifyException(0, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_004!!! Verifying setting WriteBufferSize to 0 FAILED");
            return false;
        }

        return true;
    }


    public bool WriteBufferSize_1()
    {
        Console.WriteLine("Verifying setting WriteBufferSize=1");


        if (!VerifyException(1, typeof(IOException), typeof(InvalidOperationException), true))
        {
            Console.WriteLine("Err_005!!! Verifying setting WriteBufferSize to 1 FAILED");
            return false;
        }
        return true;
    }

    public bool WriteBufferSize_Smaller()
    {
        UInt32 newWriteBufferSize = (uint)(new SerialPort()).WriteBufferSize;

        newWriteBufferSize /= 2; //Make the new buffer size half the original size
        newWriteBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit

        if (!VerifyWriteBufferSize((int)newWriteBufferSize))
        {
            Console.WriteLine("Err_006!!! Verifying setting WriteBufferSize to a smaller value FAILED");
            return false;
        }

        return true;
    }


    public bool WriteBufferSize_Larger()
    {
        if (!VerifyWriteBufferSize(((new SerialPort()).WriteBufferSize) * 2))
        {
            Console.WriteLine("Err_007!!! Verifying setting WriteBufferSize to a larger value FAILED");
            return false;
        }

        return true;
    }

    public bool WriteBufferSize_Odd()
    {
        Console.WriteLine("Verifying setting WriteBufferSize=Odd");
        int bufferSize = ((new SerialPort()).WriteBufferSize) * 2 + 1;


        if (!VerifyException(bufferSize, typeof(IOException), typeof(InvalidOperationException), true))
        {
            Console.WriteLine("Err_010!!! Verifying setting WriteBufferSize to an odd value FAILED");
            return false;
        }


        return true;
    }


    public bool WriteBufferSize_Even()
    {
        Console.WriteLine("Verifying setting WriteBufferSize=Even");
        if (!VerifyWriteBufferSize(((new SerialPort()).WriteBufferSize) * 2))
        {
            Console.WriteLine("Err_011!!! Verifying setting WriteBufferSize to an even value FAILED");
            return false;
        }

        return true;
    }


    public bool WriteBufferSize_Rnd()
    {
        Random rndGen = new Random(-55);
        UInt32 newWriteBufferSize = (uint)rndGen.Next(MAX_RANDMOM_BUFFER_SIZE);

        newWriteBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit    

        if (!VerifyWriteBufferSize((int)newWriteBufferSize))
        {
            Console.WriteLine("Err_012!!! Verifying setting WriteBufferSize to a random value FAILED");
            return false;
        }

        return true;
    }


    public bool WriteBufferSize_Large()
    {
        if (!VerifyWriteBufferSize(LARGE_BUFFER_SIZE))
        {
            Console.WriteLine("Err_013!!! Verifying setting WriteBufferSize to a large value FAILED");
            return false;
        }

        return true;
    }

    #endregion

    #region Verification for Test Cases
    private bool VerifyException(int newWriteBufferSize, System.Type expectedExceptionBeforeOpen, System.Type expectedExceptionAfterOpen)
    {
        return VerifyException(newWriteBufferSize, expectedExceptionBeforeOpen, expectedExceptionAfterOpen, false);
    }

    private bool VerifyException(int newWriteBufferSize, System.Type expectedExceptionBeforeOpen, System.Type expectedExceptionAfterOpen, bool throwAtOpen)
    {
        bool retValue = true;

        if (!VerifyExceptionBeforeOpen(newWriteBufferSize, expectedExceptionBeforeOpen, throwAtOpen))
        {
            Console.WriteLine("Err_170821hapb Verifying setting WriteBufferSize={0} BEFORE a call to Open() has been made FAILED", newWriteBufferSize);
            retValue = false;
        }


        if (!VerifyExceptionAfterOpen(newWriteBufferSize, expectedExceptionAfterOpen))
        {
            Console.WriteLine("Err_23564ahpba Verifying setting WriteBufferSize={0} AFTER a call to Open() has been made FAILED", newWriteBufferSize);
            retValue = false;
        }

        return retValue;
    }

    private bool VerifyExceptionBeforeOpen(int newWriteBufferSize, Type expectedException, bool throwAtOpen)
    {
        bool retValue = true;
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.WriteBufferSize = newWriteBufferSize;
            if (throwAtOpen)
                com.Open();

            if (null != expectedException)
            {
                Console.WriteLine("Err_707278ahpa!!! expected exception {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("Err_201890ioyun Expected no exception to be thrown and following was thrown \n{0}", e);
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("Err_545498ahpba!!! expected exception {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }
        finally
        {
            if (com.IsOpen)
                com.Close();
        }
        return retValue;
    }


    private bool VerifyExceptionAfterOpen(int newWriteBufferSize, Type expectedException)
    {
        bool retValue = true;
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        int originalWriteBufferSize = com.WriteBufferSize;

        com.Open();
        try
        {
            com.WriteBufferSize = newWriteBufferSize;
            Console.WriteLine("Err_561567anhbp!!! expected exception {0} and nothing was thrown", expectedException);
            retValue = false;
        }
        catch (Exception e)
        {
            if (e.GetType() != expectedException)
            {
                Console.WriteLine("Err_21288ajpbam!!! expected exception {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
            else if (originalWriteBufferSize != com.WriteBufferSize)
            {
                Console.WriteLine("Err_454987ahbopa!!! expected WriteBufferSize={0} and actual={1}", originalWriteBufferSize, com.WriteBufferSize);
                retValue = false;
            }
            else if (TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.NullModem) && !VerifyWriteBufferSize(com, originalWriteBufferSize))
            {
                Console.WriteLine("Err_56459847ahjpba!!! Verifying actual write after exception thrown failed");
                retValue = false;
            }
        }
        finally
        {
            if (com.IsOpen)
                com.Close();
        }
        return retValue;
    }

    private bool VerifyWriteBufferSize(int newWriteBufferSize)
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting WriteBufferSize={0} BEFORE a call to Open() has been made", newWriteBufferSize);
        retValue &= VerifyWriteBufferSizeBeforeOpen(newWriteBufferSize);

        return retValue;
    }

    private bool VerifyWriteBufferSizeBeforeOpen(int newWriteBufferSize)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying setting WriteBufferSize to {0}", newWriteBufferSize);

        com1.WriteBufferSize = newWriteBufferSize;
        com1.Open();

        return VerifyWriteBufferSize(com1, newWriteBufferSize);
    }

    private bool VerifyWriteBufferSize(SerialPort com1, int expectedWriteBufferSize)
    {
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        byte[] xmitBytes = new byte[Math.Max(expectedWriteBufferSize, com1.WriteBufferSize)];
        byte[] rcvBytes = new byte[xmitBytes.Length];
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;
        Random rndGen = new Random(-55);


        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
        }

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com2.ReadBufferSize = expectedWriteBufferSize;
        serPortProp.SetProperty("WriteBufferSize", expectedWriteBufferSize);

        com2.Open();

        int origBaudRate = com1.BaudRate;

        com2.BaudRate = 115200;
        com1.BaudRate = 115200;
        serPortProp.SetProperty("BaudRate", 115200);

        com1.Write(xmitBytes, 0, xmitBytes.Length);

        while (xmitBytes.Length > com2.BytesToRead)
            System.Threading.Thread.Sleep(50);

        Console.WriteLine("Verifying properties after changing WriteBufferSize");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        com2.Read(rcvBytes, 0, rcvBytes.Length);

        for (int i = 0; i < expectedWriteBufferSize; i++)
        {
            if (rcvBytes[i] != xmitBytes[i])
            {
                Console.WriteLine("ERROR!!!: Expected to read byte {0} actual={1} at {2}", xmitBytes[i], rcvBytes[i], i);
                retValue = false;
            }
        }

        com2.BaudRate = origBaudRate;
        com1.BaudRate = origBaudRate;

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion
}

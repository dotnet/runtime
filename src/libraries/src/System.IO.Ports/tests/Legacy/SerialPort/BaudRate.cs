// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class BaudRate_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.BaudRate";
    public static readonly String s_strTFName = "BaudRate.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default ammount of time the a transfer should take at any given baud rate. 
    //The bytes sent should be adjusted to take this ammount of time to transfer at the specified baud rate.
    public static readonly int DEFAULT_TIME = 750;

    //If the percentage difference between the expected BaudRate and the actual baudrate
    //found through Stopwatch is greater then 5% then the BaudRate value was not correctly
    //set and the testcase fails.
    public static readonly double MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE = .07;

    public static readonly int NUM_TRYS = 5;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        BaudRate_Property objTest = new BaudRate_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_Default), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_14400), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_28800), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_1200), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_115200), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_Neg1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_Zero), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_MaxValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BaudRate_12345), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool BaudRate_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default BaudRate");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyBaudRate(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default BaudRate FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool BaudRate_14400()
    {
        Console.WriteLine("Verifying 14400 BaudRate");

        if (!VerifyBaudRate(14400))
        {
            Console.WriteLine("Err_002!!! Verifying 14400 BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_28800()
    {
        Console.WriteLine("Verifying 28800 BaudRate");

        if (!VerifyBaudRate(28800))
        {
            Console.WriteLine("Err_003!!! Verifying 28800 BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_1200()
    {
        Console.WriteLine("Verifying 1200 BaudRate");

        if (!VerifyBaudRate(1200))
        {
            Console.WriteLine("Err_004!!! Verifying 1200 BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_115200()
    {
        Console.WriteLine("Verifying 115200 BaudRate");

        if (!VerifyBaudRate(115200))
        {
            Console.WriteLine("Err_005!!! Verifying 115200 BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue BaudRate");

        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_006!!! Verifying Int32.MinValue BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_Neg1()
    {
        Console.WriteLine("Verifying -1 BaudRate");

        if (!VerifyException(-1, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_007!!! Verifying -1 BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_Zero()
    {
        Console.WriteLine("Verifying 0 BaudRate");

        if (!VerifyException(0, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_008!!! Verifying 0 BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_MaxValue()
    {
        Console.WriteLine("Verifying Int32.MaxValue BaudRate");

        if (!VerifyException(Int32.MaxValue, ThrowAt.Open, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_009!!! Verifying Int32.MaxValue BaudRate FAILED");
            return false;
        }

        return true;
    }

    public bool BaudRate_12345()
    {
        Console.WriteLine("Verifying 12345 BaudRate");
        Type expectedException = typeof(System.IO.IOException);
        if (!VerifyException(12345, ThrowAt.Open, expectedException))
        {
            Console.WriteLine("Err_010!!! Verifying 12345 BaudRate FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyException(int baudRate, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, baudRate, throwAt, expectedException);

        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, baudRate, expectedException);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }

    private bool VerifyExceptionAtOpen(SerialPort com, int baudRate, ThrowAt throwAt, System.Type expectedException)
    {
        int origBaudRate = com.BaudRate;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        if (null == expectedException && throwAt == ThrowAt.Open)
        {
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("BaudRate", baudRate);
        }
        else
        {
            serPortProp.SetAllPropertiesToDefaults();
        }

        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("BaudRate", baudRate);

        try
        {
            com.BaudRate = baudRate;
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

        com.BaudRate = origBaudRate;
        return retValue;
    }

    private bool VerifyExceptionAfterOpen(SerialPort com, int baudRate, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.BaudRate = baudRate;
            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the BaudRate after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
            else
            {
                serPortProp.SetProperty("BaudRate", baudRate);
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the BaudRate after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the BaudRate after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        return retValue;
    }

    private bool VerifyBaudRate(int baudRate)
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting BaudRate BEFORE a call to Open has been made");
        retValue &= VerifyBaudRateAtOpen(baudRate);

        Console.WriteLine("Verifying setting BaudRate AFTER a call to Open has been made");
        retValue &= VerifyBaudRateAfterOpen(baudRate);

        return retValue;
    }

    private bool VerifyBaudRateAtOpen(int baudRate)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        com1.BaudRate = baudRate;
        com1.Open();
        serPortProp.SetProperty("BaudRate", baudRate);
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyBaudRate(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool VerifyBaudRateAfterOpen(int baudRate)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        com1.Open();
        com1.BaudRate = baudRate;
        serPortProp.SetProperty("BaudRate", baudRate);
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyBaudRate(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool VerifyBaudRate(SerialPort com1)
    {
        bool retValue = true;
        int numBytesToSend = Math.Max((int)((com1.BaudRate * (DEFAULT_TIME / 1000.0)) / 10.0), 64);
        byte[] xmitBytes = new byte[numBytesToSend];
        byte[] rcvBytes = new byte[numBytesToSend];
        Random rndGen = new System.Random();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        double expectedTime, actualTime, percentageDifference;
        int numBytes = 0;

        //Generate some random byte to read/write at this baudrate
        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
        }

        com2.ReadBufferSize = numBytesToSend;
        com2.BaudRate = com1.BaudRate;
        com2.Open();

        actualTime = 0;

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            IAsyncResult beginWriteResult;
            int bytesToRead = 0;

            com2.DiscardInBuffer();

            beginWriteResult = com1.BaseStream.BeginWrite(xmitBytes, 0, xmitBytes.Length, null, null);
            while (0 == (bytesToRead = com2.BytesToRead)) ;

            sw.Start();
            while (numBytesToSend > com2.BytesToRead) ; //Wait for all of the bytes to reach the input buffer of com2
            sw.Stop();

            actualTime += sw.ElapsedMilliseconds;
            actualTime += ((bytesToRead * 10.0) / com1.BaudRate) * 1000;
            beginWriteResult.AsyncWaitHandle.WaitOne();
            sw.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;


        expectedTime = ((xmitBytes.Length * 10.0) / com1.BaudRate) * 1000;
        actualTime /= NUM_TRYS;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / expectedTime);

        //If the percentageDifference between the expected time and the actual time is to high
        //then the expected baud rate must not have been used and we should report an error
        if (MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE < percentageDifference)
        {
            Console.WriteLine("ERROR!!! BuadRate not used Expected time:{0}, actual time:{1} percentageDifference:{2}",
                expectedTime, actualTime, percentageDifference, numBytes);
            retValue = false;
        }

        com2.Read(rcvBytes, 0, rcvBytes.Length);

        //Verify that the bytes we sent were the same ones we received
        for (int i = 0; i < xmitBytes.Length; i++)
        {
            if (xmitBytes[i] != rcvBytes[i])
            {
                Console.WriteLine("ERROR!!! Expected to read {0} actual read {1}", xmitBytes[i], rcvBytes[i]);
                retValue = false;
            }
        }

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion
}


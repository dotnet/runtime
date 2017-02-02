// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class StopBits_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.StopBits";
    public static readonly String s_strTFName = "StopBits.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default ammount of time the a transfer should take at any given baud rate and stop bits combination. 
    //The bytes sent should be adjusted to take this ammount of time to transfer at the specified baud rate and stop bits combination.
    public static readonly int DEFAULT_TIME = 750;

    //If the percentage difference between the expected time to transfer with the specified stopBits
    //and the actual time found through Stopwatch is greater then 5% then the StopBits value was not correctly
    //set and the testcase fails.
    public static readonly double MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE = .07;

    //The default number of databits to use when testing StopBits
    public static readonly int DEFAULT_DATABITS = 8;
    public static readonly int NUM_TRYS = 5;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        StopBits_Property objTest = new StopBits_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_Default), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_1_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_1Point5_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_2_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_1_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_1Point5_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_2_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_Neg1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_0), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_4), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_Int32MaxValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StopBits_1Point5), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool StopBits_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default StopBits");
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyStopBits(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default StopBits FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool StopBits_1_BeforeOpen()
    {
        Console.WriteLine("Verifying 1 StopBits before open");
        if (!VerifyStopBitsBeforeOpen((int)StopBits.One))
        {
            Console.WriteLine("Err_002!!! Verifying 1 StopBits before open FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_1Point5_BeforeOpen()
    {
        Console.WriteLine("Verifying 1.5 StopBits before open");
        if (!VerifyStopBitsBeforeOpen((int)StopBits.OnePointFive, 5))
        {
            Console.WriteLine("Err_003!!! Verifying 1.5 StopBits before open FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_2_BeforeOpen()
    {
        Console.WriteLine("Verifying 2 StopBits before open");
        if (!VerifyStopBitsBeforeOpen((int)StopBits.Two))
        {
            Console.WriteLine("Err_004!!! Verifying 2 StopBits before open FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_1_AfterOpen()
    {
        Console.WriteLine("Verifying 1 StopBits after open");
        if (!VerifyStopBitsAfterOpen((int)StopBits.One))
        {
            Console.WriteLine("Err_005!!! Verifying 1 StopBits after open FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_1Point5_AfterOpen()
    {
        Console.WriteLine("Verifying 1.5 StopBits after open");
        if (!VerifyStopBitsAfterOpen((int)StopBits.OnePointFive, 5))
        {
            Console.WriteLine("Err_006!!! Verifying 1.5 StopBits after open FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_2_AfterOpen()
    {
        Console.WriteLine("Verifying 2 StopBits after open");
        if (!VerifyStopBitsAfterOpen((int)StopBits.Two))
        {
            Console.WriteLine("Err_007!!! Verifying 2 StopBits after open FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue StopBits");
        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_008!!! Verifying Int32.MinValue StopBits FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_Neg1()
    {
        Console.WriteLine("Verifying -1 StopBits");
        if (!VerifyException(-1, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_009!!! Verifying -1 StopBits FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_0()
    {
        Console.WriteLine("Verifying 0 StopBits");
        if (!VerifyException(0, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_010!!! Verifying 0 StopBits FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_4()
    {
        Console.WriteLine("Verifying 4 StopBits");
        if (!VerifyException(4, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_011!!! Verifying 4 StopBits FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_Int32MaxValue()
    {
        Console.WriteLine("Verifying Int32.MaxValue StopBits");
        if (!VerifyException(Int32.MaxValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_012!!! Verifying Int32.MaxValue StopBits FAILED");
            return false;
        }

        return true;
    }


    public bool StopBits_1Point5()
    {
        Console.WriteLine("Verifying 1.5 StopBits");
        if (!VerifyException((int)StopBits.OnePointFive, ThrowAt.Open, typeof(System.IO.IOException)))
        {
            Console.WriteLine("Err_013!!! Verifying 1.5 StopBits FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyException(int stopBits, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, stopBits, throwAt, expectedException);

        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, stopBits, expectedException);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, int stopBits, ThrowAt throwAt, System.Type expectedException)
    {
        int origStopBits = (int)com.StopBits;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("StopBits", (StopBits)stopBits);

        try
        {
            com.StopBits = (StopBits)stopBits;
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
        com.StopBits = (StopBits)origStopBits;

        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, int stopBits, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.StopBits = (StopBits)stopBits;

            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the StopBits after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the StopBits after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the StopBits after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        return retValue;
    }


    private bool VerifyStopBitsBeforeOpen(int stopBits)
    {
        return VerifyStopBitsBeforeOpen(stopBits, DEFAULT_DATABITS);
    }


    private bool VerifyStopBitsBeforeOpen(int stopBits, int dataBits)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.DataBits = dataBits;
        com1.StopBits = (StopBits)stopBits;
        com1.Open();

        serPortProp.SetProperty("DataBits", dataBits);
        serPortProp.SetProperty("StopBits", (StopBits)stopBits);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyStopBits(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyStopBitsAfterOpen(int stopBits)
    {
        return VerifyStopBitsAfterOpen(stopBits, DEFAULT_DATABITS);
    }


    private bool VerifyStopBitsAfterOpen(int stopBits, int dataBits)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.DataBits = dataBits;
        com1.StopBits = (StopBits)stopBits;

        serPortProp.SetProperty("DataBits", dataBits);
        serPortProp.SetProperty("StopBits", (StopBits)stopBits);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyStopBits(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyStopBits(SerialPort com1)
    {
        bool retValue = true;
        Random rndGen = new System.Random(-55);
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        double expectedTime, actualTime, percentageDifference;
        int numBytes = 0;
        byte shiftMask = 0xFF;
        double stopBits = -1;

        switch ((int)com1.StopBits)
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

        int numBytesToSend = (int)(((DEFAULT_TIME / 1000.0) * com1.BaudRate) / (stopBits + com1.DataBits + 1));
        byte[] xmitBytes = new byte[numBytesToSend];
        byte[] expectedBytes = new byte[numBytesToSend];
        byte[] rcvBytes = new byte[numBytesToSend];

        //Create a mask that when logicaly and'd with the transmitted byte will 
        //will result in the byte recievied due to the leading bits being chopped
        //off due to DataBits less then 8
        shiftMask >>= 8 - com1.DataBits;

        //Generate some random bytes to read/write for this StopBits setting
        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
            expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
        }

        com2.DataBits = com1.DataBits;
        com2.StopBits = com1.StopBits;
        com2.Open();
        actualTime = 0;

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        int initialNumBytes;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            com2.DiscardInBuffer();

            IAsyncResult beginWriteResult = com1.BaseStream.BeginWrite(xmitBytes, 0, numBytesToSend, null, null);
            while (0 == (initialNumBytes = com2.BytesToRead)) ;

            sw.Start();
            while (numBytesToSend > com2.BytesToRead) ; //Wait for all of the bytes to reach the input buffer of com2
            sw.Stop();

            actualTime += sw.ElapsedMilliseconds;
            actualTime += ((initialNumBytes * (stopBits + com1.DataBits + 1)) / com1.BaudRate) * 1000;
            beginWriteResult.AsyncWaitHandle.WaitOne();

            sw.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;
        expectedTime = ((xmitBytes.Length * (stopBits + com1.DataBits + 1)) / com1.BaudRate) * 1000;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / expectedTime);

        //If the percentageDifference between the expected time and the actual time is to high
        //then the expected baud rate must not have been used and we should report an error
        if (MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE < percentageDifference)
        {
            Console.WriteLine("ERROR!!! StopBits not used Expected time:{0}, actual time:{1} percentageDifference:{2}", expectedTime, actualTime, percentageDifference, numBytes);
            retValue = false;
        }

        com2.Read(rcvBytes, 0, rcvBytes.Length);

        //Verify that the bytes we sent were the same ones we received
        for (int i = 0; i < expectedBytes.Length; i++)
        {
            if (expectedBytes[i] != rcvBytes[i])
            {
                Console.WriteLine("ERROR!!! Expected to read {0} actual read {1}", expectedBytes[i], rcvBytes[i]);
                retValue = false;
            }
        }

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class DataBits_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.DataBits";
    public static readonly String s_strTFName = "DataBits.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default number of bytes to read/write to verify the speed of the port
    //and that the bytes were transfered successfully
    public static readonly int DEFAULT_BYTE_SIZE = 256;

    //If the percentage difference between the expected time to transfer with the specified dataBits
    //and the actual time found through Stopwatch is greater then 5% then the DataBits value was not correctly
    //set and the testcase fails.
    public static readonly double MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE = .05;

    public static readonly int NUM_TRYS = 5;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        DataBits_Property objTest = new DataBits_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_Default), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_5_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_6_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_7_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_8_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_5_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_6_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_7_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_8_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_Neg8), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_Neg1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_0), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_4), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_9), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_Int32MaxValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DataBits_8_StopBitsOnePointFive), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool DataBits_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DataBits");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDataBits(com1, DEFAULT_BYTE_SIZE);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default DataBits FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DataBits_5_BeforeOpen()
    {
        Console.WriteLine("Verifying 5 DataBits before open");
        if (!VerifyDataBitsBeforeOpen(5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_002!!! Verifying 5 DataBits before open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_6_BeforeOpen()
    {
        Console.WriteLine("Verifying 6 DataBits before open");
        if (!VerifyDataBitsBeforeOpen(6, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_003!!! Verifying 6 DataBits before open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_7_BeforeOpen()
    {
        Console.WriteLine("Verifying 7 DataBits before open");
        if (!VerifyDataBitsBeforeOpen(7, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_004!!! Verifying 7 DataBits before open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_8_BeforeOpen()
    {
        Console.WriteLine("Verifying 8 DataBits before open");
        if (!VerifyDataBitsBeforeOpen(8, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_005!!! Verifying 8 DataBits before open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_5_AfterOpen()
    {
        Console.WriteLine("Verifying 5 DataBits after open");
        if (!VerifyDataBitsAfterOpen(5, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_006!!! Verifying 5 DataBits after open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_6_AfterOpen()
    {
        Console.WriteLine("Verifying 6 DataBits after open");
        if (!VerifyDataBitsAfterOpen(6, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_007!!! Verifying 6 DataBits after open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_7_AfterOpen()
    {
        Console.WriteLine("Verifying 7 DataBits after open");
        if (!VerifyDataBitsAfterOpen(7, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_008!!! Verifying 7 DataBits after open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_8_AfterOpen()
    {
        Console.WriteLine("Verifying 8 DataBits after open");
        if (!VerifyDataBitsAfterOpen(8, DEFAULT_BYTE_SIZE))
        {
            Console.WriteLine("Err_009!!! Verifying 8 DataBits after open FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue DataBits");
        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_010!!! Verifying Int32.MinValue DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_Neg8()
    {
        Console.WriteLine("Verifying -8 DataBits");
        if (!VerifyException(-8, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_011!!! -8 DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_Neg1()
    {
        Console.WriteLine("Verifying -1 DataBits");
        if (!VerifyException(-1, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_012!!! Verifying -1 DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_0()
    {
        Console.WriteLine("Verifying 0 DataBits");
        if (!VerifyException(0, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_013!!! Verifying 0 DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_1()
    {
        Console.WriteLine("Verifying 1 DataBits");
        if (!VerifyException(1, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_014!!! Verifying 1 DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_4()
    {
        Console.WriteLine("Verifying 4 DataBits");
        if (!VerifyException(4, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_015!!! Verifying 4 DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_9()
    {
        Console.WriteLine("Verifying 9 DataBits");
        if (!VerifyException(9, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_016!!! Verifying 9 DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_Int32MaxValue()
    {
        Console.WriteLine("Verifying Int32.MaxValue DataBits");
        if (!VerifyException(Int32.MaxValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_017!!! Verifying Int32.MaxValue DataBits FAILED");
            return false;
        }

        return true;
    }


    public bool DataBits_8_StopBitsOnePointFive()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        Console.WriteLine("Verifying setting DataBits=8 from 5 with StopBits=1.5");

        com.DataBits = 5;
        com.StopBits = StopBits.OnePointFive;
        com.Open();

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("StopBits", StopBits.OnePointFive);
        serPortProp.SetProperty("DataBits", 5);

        try
        {
            com.DataBits = 8;
            Console.WriteLine("ERROR!!! Setting DataBits did not thow an exception");
            retValue = false;
        }
        catch (System.IO.IOException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Expected IOException and {0} was thrown", e.GetType());
            retValue = false;
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (!retValue)
            Console.WriteLine("Err_018!!! Verifying setting DataBits=8 from 5 with StopBits=1.5 FAILED");

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyException(int dataBits, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, dataBits, throwAt, expectedException);
        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, dataBits, expectedException);
        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, int dataBits, ThrowAt throwAt, System.Type expectedException)
    {
        int origDataBits = com.DataBits;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("DataBits", dataBits);

        try
        {
            com.DataBits = dataBits;

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
        com.DataBits = origDataBits;
        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, int dataBits, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.DataBits = dataBits;
            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the DataBits after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the DataBits after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the DataBits after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        return retValue;
    }


    private bool VerifyDataBitsBeforeOpen(int dataBits, int numBytesToSend)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.DataBits = dataBits;
        com1.Open();

        serPortProp.SetProperty("DataBits", dataBits);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDataBits(com1, numBytesToSend);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyDataBitsAfterOpen(int dataBits, int numBytesToSend)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.DataBits = dataBits;

        serPortProp.SetProperty("DataBits", dataBits);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDataBits(com1, numBytesToSend);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyDataBits(SerialPort com1, int numBytesToSend)
    {
        bool retValue = true;
        byte[] xmitBytes = new byte[numBytesToSend];
        byte[] expectedBytes = new byte[numBytesToSend];
        byte[] rcvBytes = new byte[numBytesToSend];
        Random rndGen = new System.Random();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        double expectedTime, actualTime, percentageDifference;
        int numBytes = 0;
        byte shiftMask = 0xFF;

        //Create a mask that when logicaly and'd with the transmitted byte will 
        //will result in the byte recievied due to the leading bits being chopped
        //off due to DataBits less then 8
        shiftMask >>= 8 - com1.DataBits;

        //Generate some random bytes to read/write for this DataBits setting
        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
            expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
        }

        com2.DataBits = com1.DataBits;
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
            actualTime += ((bytesToRead * (2.0 + com1.DataBits)) / com1.BaudRate) * 1000;
            beginWriteResult.AsyncWaitHandle.WaitOne();
            sw.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;

        expectedTime = ((xmitBytes.Length * (2.0 + com1.DataBits)) / com1.BaudRate) * 1000;
        actualTime /= NUM_TRYS;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / expectedTime);

        //If the percentageDifference between the expected time and the actual time is to high
        //then the expected baud rate must not have been used and we should report an error
        if (MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE < percentageDifference)
        {
            Console.WriteLine("ERROR!!! DataBits not used Expected time:{0}, actual time:{1} percentageDifference:{2}", expectedTime, actualTime, percentageDifference, numBytes);
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
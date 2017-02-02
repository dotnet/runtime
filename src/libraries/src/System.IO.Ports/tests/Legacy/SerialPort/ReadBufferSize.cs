// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.IO;

public class ReadBufferSize_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/04/01 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadBuffer_Property";
    public static readonly String s_strTFName = "ReadBufferSize_Property.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    public static readonly int MAX_RANDMOM_BUFFER_SIZE = 1024 * 16;
    public static readonly int LARGE_BUFFER_SIZE = 1024 * 128;

    public delegate void ReadMethodDelegate(SerialPort com, int bufferSize);

    //The default new lint to read from when testing timeout with ReadTo(str)
    public static readonly string DEFAULT_READ_TO_STRING = "\r\n";

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadBufferSize_Property objTest = new ReadBufferSize_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_NEG1), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Int32MinValue), TCSupport.SerialPortRequirements.NullModem);	// call to VerifyReadBufferSize requires 2 com ports
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_0), TCSupport.SerialPortRequirements.NullModem);		// call to VerifyReadBufferSize requires 2 com ports
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_1), TCSupport.SerialPortRequirements.NullModem);		// call to VerifyReadBufferSize requires 2 com ports		

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Default), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_1), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_2), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Smaller), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Larger), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Odd), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Even), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Rnd), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadBufferSize_Large), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ReadBufferSize_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default ReadBufferSize before Open");

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        Console.WriteLine("Verifying default ReadBufferSize after Open");

        com1.Open();
        serPortProp = new SerialPortProperties();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        Console.WriteLine("Verifying default ReadBufferSize after Close");

        com1.Close();
        serPortProp = new SerialPortProperties();
        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default ReadBufferSize FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool ReadBufferSize_AfterOpen()
    {
        if (!VerifyException(1024, null, typeof(InvalidOperationException)))
        {
            Console.WriteLine("Err_54458ahpba!!! Verifying setting ReadBufferSize to 1024 FAILED");
            return false;
        }

        return true;
    }

    public bool ReadBufferSize_NEG1()
    {
        if (!VerifyException(-1, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_002!!! Verifying setting ReadBufferSize to -1 FAILED");
            return false;
        }

        return true;
    }


    public bool ReadBufferSize_Int32MinValue()
    {
        if (!VerifyException(Int32.MinValue, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_003!!! Verifying setting ReadBufferSize to Int32.MinValue FAILED");
            return false;
        }

        return true;
    }


    public bool ReadBufferSize_0()
    {
        if (!VerifyException(0, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_004!!! Verifying setting ReadBufferSize to 0 FAILED");
            return false;
        }

        return true;
    }


    public bool ReadBufferSize_1()
    {
        Console.WriteLine("Verifying setting ReadBufferSize=1");

        if (!VerifyException(1, typeof(IOException), typeof(InvalidOperationException), true))
        {
            Console.WriteLine("Err_005!!! Verifying setting ReadBufferSize to 1 FAILED");
            return false;
        }

        return true;
    }

    public bool ReadBufferSize_2()
    {
        Console.WriteLine("Verifying setting ReadBufferSize=");

        if (!VerifyReadBufferSize(2))
        {
            Console.WriteLine("Err_005a!!! Verifying setting ReadBufferSize to 2 FAILED");
            return false;
        }

        return true;
    }

    public bool ReadBufferSize_Smaller()
    {
        UInt32 newReadBufferSize = (uint)(new SerialPort()).ReadBufferSize;

        newReadBufferSize /= 2; //Make the new buffer size half the original size
        newReadBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit

        if (!VerifyReadBufferSize((int)newReadBufferSize))
        {
            Console.WriteLine("Err_006!!! Verifying setting ReadBufferSize to a smaller value FAILED");
            return false;
        }

        return true;
    }


    public bool ReadBufferSize_Larger()
    {
        if (!VerifyReadBufferSize(((new SerialPort()).ReadBufferSize) * 2))
        {
            Console.WriteLine("Err_007!!! Verifying setting ReadBufferSize to a larger value FAILED");
            return false;
        }

        return true;
    }

    public bool ReadBufferSize_Odd()
    {
        Console.WriteLine("Verifying setting ReadBufferSize=Odd");

        if (!VerifyException(((new SerialPort()).ReadBufferSize) * 2 + 1, typeof(IOException), typeof(InvalidOperationException), true))
        {
            Console.WriteLine("Err_010!!! Verifying setting ReadBufferSize to an odd value FAILED");
            return false;
        }

        return true;
    }


    public bool ReadBufferSize_Even()
    {
        Console.WriteLine("Verifying setting ReadBufferSize=Even");
        if (!VerifyReadBufferSize(((new SerialPort()).ReadBufferSize) * 2))
        {
            Console.WriteLine("Err_011!!! Verifying setting ReadBufferSize to an even value FAILED");
            return false;
        }

        return true;
    }


    public bool ReadBufferSize_Rnd()
    {
        Random rndGen = new Random(-55);
        UInt32 newReadBufferSize = (uint)rndGen.Next(MAX_RANDMOM_BUFFER_SIZE);

        newReadBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit    

        //		if(!VerifyReadBufferSize((int)newReadBufferSize)){
        if (!VerifyReadBufferSize(11620))
        {
            Console.WriteLine("Err_012!!! Verifying setting ReadBufferSize to a random value FAILED");
            return false;
        }

        return true;
    }


    public bool ReadBufferSize_Large()
    {
        if (!VerifyReadBufferSize(LARGE_BUFFER_SIZE))
        {
            Console.WriteLine("Err_013!!! Verifying setting ReadBufferSize to a large value FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyException(int newReadBufferSize, System.Type expectedExceptionBeforeOpen, System.Type expectedExceptionAfterOpen)
    {
        return VerifyException(newReadBufferSize, expectedExceptionBeforeOpen, expectedExceptionAfterOpen, false);
    }

    private bool VerifyException(int newReadBufferSize, System.Type expectedExceptionBeforeOpen, System.Type expectedExceptionAfterOpen, bool throwAtOpen)
    {
        bool retValue = true;

        if (!VerifyExceptionBeforeOpen(newReadBufferSize, expectedExceptionBeforeOpen, throwAtOpen))
        {
            Console.WriteLine("Err_170821hapb Verifying setting ReadBufferSize={0} BEFORE a call to Open() has been made FAILED", newReadBufferSize);
            retValue = false;
        }


        if (!VerifyExceptionAfterOpen(newReadBufferSize, expectedExceptionAfterOpen))
        {
            Console.WriteLine("Err_23564ahpba Verifying setting ReadBufferSize={0} AFTER a call to Open() has been made FAILED", newReadBufferSize);
            retValue = false;
        }

        return retValue;
    }

    private bool VerifyExceptionBeforeOpen(int newReadBufferSize, Type expectedException, bool throwAtOpen)
    {
        bool retValue = true;
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.ReadBufferSize = newReadBufferSize;
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


    private bool VerifyExceptionAfterOpen(int newReadBufferSize, Type expectedException)
    {
        bool retValue = true;
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        int originalReadBufferSize = com.ReadBufferSize;

        com.Open();
        try
        {
            com.ReadBufferSize = newReadBufferSize;
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
            else if (originalReadBufferSize != com.ReadBufferSize)
            {
                Console.WriteLine("Err_454987ahbopa!!! expected ReadBufferSize={0} and actual={1}", originalReadBufferSize, com.ReadBufferSize);
                retValue = false;
            }
            else if (!VerifyReadBufferSize(com))
            {
                Console.WriteLine("Err_56459847ahjpba!!! Verifying actual read after exception thrown failed");
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


    private bool VerifyReadBufferSize(int newReadBufferSize)
    {
        bool retValue = true;

        Console.WriteLine("Verifying setting ReadBufferSize={0} BEFORE a call to Open() has been made", newReadBufferSize);
        retValue &= VerifyReadBufferSizeBeforeOpen(newReadBufferSize);

        return retValue;
    }


    private bool VerifyReadBufferSizeBeforeOpen(int newReadBufferSize)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        byte[] xmitBytes = new byte[newReadBufferSize];
        byte[] rcvBytes;
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;
        Random rndGen = new Random(-55);
        int newBytesToRead = 0;

        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
        }

        if (newReadBufferSize < 4096)
            newBytesToRead = Math.Min(4096, xmitBytes.Length + (xmitBytes.Length / 2));
        else
            newBytesToRead = Math.Min(newReadBufferSize, xmitBytes.Length);

        rcvBytes = new byte[newBytesToRead];

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.ReadBufferSize = newReadBufferSize;
        serPortProp.SetProperty("ReadBufferSize", newReadBufferSize);

        com1.Open();
        com2.Open();

        int origBaudRate = com1.BaudRate;

        com2.BaudRate = 115200;
        com1.BaudRate = 115200;

        for (int j = 0; j < 1; j++)
        {
            com2.Write(xmitBytes, 0, xmitBytes.Length);
            com2.Write(xmitBytes, xmitBytes.Length / 2, xmitBytes.Length / 2);

            while (newBytesToRead > com1.BytesToRead)
                System.Threading.Thread.Sleep(50);

            System.Threading.Thread.Sleep(250); //This is to wait for the bytes to be received after the buffer is full

            serPortProp.SetProperty("BytesToRead", newBytesToRead);
            serPortProp.SetProperty("BaudRate", 115200);

            Console.WriteLine("Verifying properties after bytes have been written");
            retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

            com1.Read(rcvBytes, 0, newBytesToRead);

            for (int i = 0; i < newReadBufferSize; i++)
            {
                if (rcvBytes[i] != xmitBytes[i])
                {
                    Console.WriteLine("ERROR!!!: Expected to read byte {0} actual={1} at {2}", xmitBytes[i], rcvBytes[i], i);
                    retValue = false;
                }
            }

            Console.WriteLine("Verifying properties after bytes have been read");
            serPortProp.SetProperty("BytesToRead", 0);
            retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        }

        com2.Write(xmitBytes, 0, xmitBytes.Length);
        com2.Write(xmitBytes, xmitBytes.Length / 2, xmitBytes.Length / 2);

        while (newBytesToRead > com1.BytesToRead)
            System.Threading.Thread.Sleep(50);

        serPortProp.SetProperty("BytesToRead", newBytesToRead);
        Console.WriteLine("Verifying properties after writing bytes");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        com2.BaudRate = origBaudRate;
        com1.BaudRate = origBaudRate;

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    private bool VerifyReadBufferSize(SerialPort com1)
    {
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        int readBufferSize = com1.ReadBufferSize;
        byte[] xmitBytes = new byte[1024];
        byte[] rcvBytes;
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;
        Random rndGen = new Random(-55);
        int bytesToRead = readBufferSize < 4096 ? 4096 : readBufferSize;
        int origBaudRate = com1.BaudRate;
        int origReadTimeout = com1.ReadTimeout;
        int bytesRead;

        for (int i = 0; i < xmitBytes.Length; i++)
        {
            xmitBytes[i] = (byte)rndGen.Next(0, 256);
        }

        //bytesToRead = Math.Min(4096, xmitBytes.Length + (xmitBytes.Length / 2));

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.SetProperty("ReadBufferSize", readBufferSize);
        serPortProp.SetProperty("BaudRate", 115200);

        com2.Open();

        com2.BaudRate = 115200;
        com1.BaudRate = 115200;

        for (int i = 0; i < bytesToRead / xmitBytes.Length; i++)
        {
            com2.Write(xmitBytes, 0, xmitBytes.Length);
        }

        com2.Write(xmitBytes, 0, xmitBytes.Length / 2);

        while (bytesToRead > com1.BytesToRead)
        {
            System.Threading.Thread.Sleep(50);
        }

        System.Threading.Thread.Sleep(250); //This is to wait for the bytes to be received after the buffer is full

        rcvBytes = new byte[(int)(bytesToRead * 1.5)];
        if (bytesToRead != (bytesRead = com1.Read(rcvBytes, 0, rcvBytes.Length)))
        {
            Console.WriteLine("Err_2971ahius Did not read all expected bytes({0}) bytesRead={1} ReadBufferSize={2}", bytesToRead, bytesRead, com1.ReadBufferSize);
            retValue = false;
        }

        for (int i = 0; i < bytesToRead; i++)
        {
            if (rcvBytes[i] != xmitBytes[i % xmitBytes.Length])
            {
                Console.WriteLine("Err_70929apba!!!: Expected to read byte {0} actual={1} at {2}", xmitBytes[i % xmitBytes.Length], rcvBytes[i], i);
                retValue = false;
            }
        }

        serPortProp.SetProperty("BytesToRead", 0);
        if (!serPortProp.VerifyPropertiesAndPrint(com1))
        {
            Console.WriteLine("Err_56488wypag!!!: Verifying properties failed");
        }

        com1.ReadTimeout = 250;

        try
        {
            com1.ReadByte();
            Console.WriteLine("Err_1707ahspb!!!: After reading all bytes from buffer ReadByte() did not timeout");
            retValue = false;
        }
        catch (TimeoutException) { }

        com1.ReadTimeout = origReadTimeout;

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion
}


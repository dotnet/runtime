// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Threading;

public class Handshake_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Handshake";
    public static readonly String s_strTFName = "Handshake.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default number of bytes to read/write to verify the speed of the port
    //and that the bytes were transfered successfully
    public static readonly int DEFAULT_BYTE_SIZE = 8;

    //The number of bytes to send when send XOn or XOff, the actual XOn/XOff char will be inserted somewhere
    //in the array of bytes
    public static readonly int DEFAULT_BYTE_SIZE_XON_XOFF = 16;

    //If the percentage difference between the expected time to transfer with the specified handshake
    //and the actual time found through Stopwatch is greater then 5% then the Handshake value was not correctly
    //set and the testcase fails.
    public static readonly double MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE = .05;

    //The default time to wait after writing some bytes
    private const int DEFAULT_WAIT_AFTER_READ_OR_WRITE = 500;

    private const byte XOFF_BYTE = 19;
    private const byte XON_BYTE = 17;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Handshake_Property objTest = new Handshake_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_Default), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_None_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_XOnXOff_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSend_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSendXOnXOff_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_None_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_XOnXOff_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSend_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_RequestToSendXOnXOff_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_Int32MinValue), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_Neg1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_4), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Handshake_Int32MaxValue), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Handshake_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default Handshake");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyHandshake(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default Handshake FAILED");
        }

        com1.DiscardInBuffer();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool Handshake_None_BeforeOpen()
    {
        Console.WriteLine("Verifying None Handshake before open");
        if (!VerifyHandshakeBeforeOpen((int)Handshake.None))
        {
            Console.WriteLine("Err_002!!! Verifying None Handshake before open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_XOnXOff_BeforeOpen()
    {
        Console.WriteLine("Verifying XOnXOff Handshake before open");
        if (!VerifyHandshakeBeforeOpen((int)Handshake.XOnXOff))
        {
            Console.WriteLine("Err_003!!! Verifying XOnXOff Handshake before open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_RequestToSend_BeforeOpen()
    {
        Console.WriteLine("Verifying RequestToSend Handshake before open");
        if (!VerifyHandshakeBeforeOpen((int)Handshake.RequestToSend))
        {
            Console.WriteLine("Err_004!!! Verifying RequestToSend Handshake before open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_RequestToSendXOnXOff_BeforeOpen()
    {
        Console.WriteLine("Verifying RequestToSendXOnXOff Handshake before open");
        if (!VerifyHandshakeBeforeOpen((int)Handshake.RequestToSendXOnXOff))
        {
            Console.WriteLine("Err_005!!! Verifying RequestToSendXOnXOff Handshake before open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_None_AfterOpen()
    {
        Console.WriteLine("Verifying None Handshake after open");
        if (!VerifyHandshakeAfterOpen((int)Handshake.None))
        {
            Console.WriteLine("Err_006!!! Verifying None Handshake after open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_XOnXOff_AfterOpen()
    {
        Console.WriteLine("Verifying XOnXOff Handshake after open");
        if (!VerifyHandshakeAfterOpen((int)Handshake.XOnXOff))
        {
            Console.WriteLine("Err_007!!! Verifying XOnXOff Handshake after open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_RequestToSend_AfterOpen()
    {
        Console.WriteLine("Verifying RequestToSend Handshake after open");
        if (!VerifyHandshakeAfterOpen((int)Handshake.RequestToSend))
        {
            Console.WriteLine("Err_008!!! Verifying RequestToSend Handshake after open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_RequestToSendXOnXOff_AfterOpen()
    {
        Console.WriteLine("Verifying RequestToSendXOnXOff Handshake after open");
        if (!VerifyHandshakeAfterOpen((int)Handshake.RequestToSendXOnXOff))
        {
            Console.WriteLine("Err_009!!! Verifying RequestToSendXOnXOff Handshake after open FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_Int32MinValue()
    {
        Console.WriteLine("Verifying Int32.MinValue Handshake");
        if (!VerifyException(Int32.MinValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_010!!! Verifying Int32.MinValue Handshake FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_Neg1()
    {
        Console.WriteLine("Verifying -1 Handshake");
        if (!VerifyException(-1, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_011!!! Verifying -1 Handshake FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_4()
    {
        Console.WriteLine("Verifying 4 Handshake");
        if (!VerifyException(4, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_012!!! Verifying 4 Handshake FAILED");
            return false;
        }

        return true;
    }


    public bool Handshake_Int32MaxValue()
    {
        Console.WriteLine("Verifying Int32.MaxValue Handshake");
        if (!VerifyException(Int32.MaxValue, ThrowAt.Set, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_013!!! Verifying Int32.MaxValue Handshake FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyException(int handshake, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, handshake, throwAt, expectedException);

        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, handshake, expectedException);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, int handshake, ThrowAt throwAt, System.Type expectedException)
    {
        int origHandshake = (int)com.Handshake;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("Handshake", (Handshake)handshake);

        try
        {
            com.Handshake = (Handshake)handshake;

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
        com.Handshake = (Handshake)origHandshake;

        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, int handshake, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.Handshake = (Handshake)handshake;
            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Handshake after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Handshake after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Handshake after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        return retValue;
    }


    private bool VerifyHandshakeBeforeOpen(int handshake)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Handshake = (Handshake)handshake;
        com1.Open();

        serPortProp.SetProperty("Handshake", (Handshake)handshake);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyHandshake(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyHandshakeAfterOpen(int handshake)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.Handshake = (Handshake)handshake;

        serPortProp.SetProperty("Handshake", (Handshake)handshake);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyHandshake(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyHandshake(SerialPort com1)
    {
        bool retValue = true;
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        int origWriteTimeout = com1.WriteTimeout;
        int origReadTimeout = com1.ReadTimeout;

        com2.Open();
        com1.WriteTimeout = 250;
        com1.ReadTimeout = 250;

        retValue &= VerifyRTSHandshake(com1, com2);
        retValue &= VerifyXOnXOffHandshake(com1, com2);
        retValue &= VerifyRTSXOnXOffHandshake(com1, com2);
        retValue &= VerirfyRTSBufferFull(com1, com2);
        retValue &= VerirfyXOnXOffBufferFull(com1, com2);

        com1.WriteTimeout = origWriteTimeout;
        com1.ReadTimeout = origReadTimeout;

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private bool VerifyRTSHandshake(SerialPort com1, SerialPort com2)
    {
        bool retValue = true;
        bool origRtsEnable = com2.RtsEnable;

        try
        {
            com2.RtsEnable = true;

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);
            }
            catch (System.TimeoutException)
            {
                Console.WriteLine("Err_103948aooh!!! TimeoutException thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                retValue = false;
            }

            com2.RtsEnable = false;

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);
                if (Handshake.RequestToSend == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                {
                    Console.WriteLine("Err_15397lkjh!!! TimeoutException NOT thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }
            catch (System.TimeoutException)
            {
                if (Handshake.RequestToSend != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                {
                    Console.WriteLine("Err_1341pawh!!! TimeoutException thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }

            com2.RtsEnable = true;

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);
            }
            catch (System.TimeoutException)
            {
                Console.WriteLine("Err_143987aqaih!!! TimeoutException thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                retValue = false;
            }
        }
        finally
        {
            com2.RtsEnable = origRtsEnable;
            com2.DiscardInBuffer();
        }

        return retValue;
    }


    private bool VerifyRTSXOnXOffHandshake(SerialPort com1, SerialPort com2)
    {
        bool origRtsEnable = com2.RtsEnable;
        Random rndGen = new Random();
        byte[] xmitXOnBytes = new byte[DEFAULT_BYTE_SIZE_XON_XOFF];
        byte[] xmitXOffBytes = new byte[DEFAULT_BYTE_SIZE_XON_XOFF];
        bool retValue = true;

        try
        {
            for (int i = 0; i < xmitXOnBytes.Length; i++)
            {
                byte rndByte;

                do
                {
                    rndByte = (byte)rndGen.Next(0, 256);
                } while (rndByte == 17 || rndByte == 19);

                xmitXOnBytes[i] = rndByte;
            }

            for (int i = 0; i < xmitXOffBytes.Length; i++)
            {
                byte rndByte;

                do
                {
                    rndByte = (byte)rndGen.Next(0, 256);
                } while (rndByte == 17 || rndByte == 19);

                xmitXOffBytes[i] = rndByte;
            }

            xmitXOnBytes[rndGen.Next(0, xmitXOnBytes.Length)] = (byte)17;
            xmitXOffBytes[rndGen.Next(0, xmitXOffBytes.Length)] = (byte)19;

            com2.RtsEnable = false;

            com2.Write(xmitXOffBytes, 0, xmitXOffBytes.Length);
            com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
            while (true)
            {
                try
                {
                    com1.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);
                if (Handshake.RequestToSend == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                {
                    Console.WriteLine("Err_1253aasyo!!! TimeoutException NOT thrown after XOff and XOn char sent when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }
            catch (System.TimeoutException)
            {
                if (Handshake.RequestToSend != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                {
                    Console.WriteLine("Err_51390awi!!! TimeoutException thrown after XOff and XOn char sent when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }

            com2.Write(xmitXOffBytes, 0, xmitXOffBytes.Length);
            com2.RtsEnable = false;
            com2.RtsEnable = true;
            while (true)
            {
                try
                {
                    com1.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);
                if (Handshake.XOnXOff == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                {
                    Console.WriteLine("Err_2457awez!!! TimeoutException NOT thrown after RTSEnable set to false then true when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }
            catch (System.TimeoutException)
            {
                if (Handshake.XOnXOff != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                {
                    Console.WriteLine("Err_3240aw4er!!! TimeoutException thrown RTSEnable set to false then true when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }

            com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
            while (true)
            {
                try
                {
                    com1.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }
            }
        }
        finally
        {
            com2.RtsEnable = origRtsEnable;
            com2.DiscardInBuffer();
        }

        return retValue;
    }


    private bool VerifyXOnXOffHandshake(SerialPort com1, SerialPort com2)
    {
        bool origRtsEnable = com2.RtsEnable;
        Random rndGen = new Random();
        byte[] xmitXOnBytes = new byte[DEFAULT_BYTE_SIZE_XON_XOFF];
        byte[] xmitXOffBytes = new byte[DEFAULT_BYTE_SIZE_XON_XOFF];
        bool retValue = true;

        try
        {
            com2.RtsEnable = true;

            for (int i = 0; i < xmitXOnBytes.Length; i++)
            {
                byte rndByte;

                do
                {
                    rndByte = (byte)rndGen.Next(0, 256);
                } while (rndByte == 17 || rndByte == 19);

                xmitXOnBytes[i] = rndByte;
            }

            for (int i = 0; i < xmitXOffBytes.Length; i++)
            {
                byte rndByte;

                do
                {
                    rndByte = (byte)rndGen.Next(0, 256);
                } while (rndByte == 17 || rndByte == 19);

                xmitXOffBytes[i] = rndByte;
            }

            int XOnIndex = rndGen.Next(0, xmitXOnBytes.Length);
            int XOffIndex = rndGen.Next(0, xmitXOffBytes.Length);

            xmitXOnBytes[XOnIndex] = (byte)17;
            xmitXOffBytes[XOffIndex] = (byte)19;

            Console.WriteLine("XOnIndex={0} XOffIndex={1}", XOnIndex, XOffIndex);

            com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
            com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
            while (true)
            {
                try
                {
                    com1.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);
            }
            catch (System.TimeoutException)
            {
                Console.WriteLine("Err_2357pquaz!!! TimeoutException thrown after XOn char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                retValue = false;
            }

            com2.Write(xmitXOffBytes, 0, xmitXOffBytes.Length);
            while (true)
            {
                try
                {
                    com1.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);

                if (Handshake.XOnXOff == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                {
                    Console.WriteLine("Err_1349znpq!!! TimeoutException NOT thrown after XOff char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }
            catch (System.TimeoutException)
            {
                if (Handshake.XOnXOff != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                {
                    Console.WriteLine("Err_2507pqzhn!!! TimeoutException thrown after XOff char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    retValue = false;
                }
            }

            com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
            while (true)
            {
                try
                {
                    com1.ReadByte();
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            try
            {
                com1.Write(new byte[DEFAULT_BYTE_SIZE], 0, DEFAULT_BYTE_SIZE);
            }
            catch (System.TimeoutException)
            {
                Console.WriteLine("Err_2570aqpa!!! TimeoutException thrown after XOn char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                retValue = false;
            }
        }
        finally
        {
            com2.RtsEnable = origRtsEnable;
            com2.DiscardInBuffer();
        }

        return retValue;
    }


    private bool VerirfyRTSBufferFull(SerialPort com1, SerialPort com2)
    {
        bool retValue = true;
        int com1BaudRate = com1.BaudRate;
        int com2BaudRate = com2.BaudRate;
        int com1ReadBufferSize = com1.ReadBufferSize;
        int bufferSize = com1.ReadBufferSize;
        int upperLimit = (3 * bufferSize) / 4;
        int lowerLimit = bufferSize / 4;
        byte[] bytes = new byte[upperLimit];

        try
        {
            //Set the BaudRate to something faster so that it does not take so long to fill up the buffer
            com1.BaudRate = 115200;
            com2.BaudRate = 115200;

            //Write 1 less byte then when the RTS pin would be cleared
            com2.Write(bytes, 0, upperLimit - 1);
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsRequestToSend(com1))
            {
                if (!com2.CtsHolding)
                {
                    Console.WriteLine("Err_548458ahiede Expected RTS to be set");
                    retValue = false;
                }
            }
            else
            {
                if (com2.CtsHolding)
                {
                    Console.WriteLine("Err_028538aieoz Expected RTS to be cleared");
                    retValue = false;
                }
            }

            //Write the byte and verify the RTS pin is cleared appropriately
            com2.Write(bytes, 0, 1);
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsRequestToSend(com1))
            {
                if (com2.CtsHolding)
                {
                    Console.WriteLine("Err_508845aueid Expected RTS to be cleared");
                    retValue = false;
                }
            }
            else
            {
                if (com2.CtsHolding)
                {
                    Console.WriteLine("Err_48848ajeoid Expected RTS to be set");
                    retValue = false;
                }
            }

            //Read 1 less byte then when the RTS pin would be set
            com1.Read(bytes, 0, (upperLimit - lowerLimit) - 1);
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsRequestToSend(com1))
            {
                if (com2.CtsHolding)
                {
                    Console.WriteLine("Err_952085aizpea Expected RTS to be cleared");
                    retValue = false;
                }
            }
            else
            {
                if (com2.CtsHolding)
                {
                    Console.WriteLine("Err_725527ahjiuzp Expected RTS to be set");
                    retValue = false;
                }
            }

            //Read the byte and verify the RTS pin is set appropriately
            com1.Read(bytes, 0, 1);
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsRequestToSend(com1))
            {
                if (!com2.CtsHolding)
                {
                    Console.WriteLine("Err_652820aopea Expected RTS to be set");
                    retValue = false;
                }
            }
            else
            {
                if (com2.CtsHolding)
                {
                    Console.WriteLine("Err_35585ajuei Expected RTS to be cleared");
                    retValue = false;
                }
            }
        }
        finally
        {
            //Rollback any changed that were made to the SerialPort
            com1.BaudRate = com1BaudRate;
            com2.BaudRate = com2BaudRate;

            com1.DiscardInBuffer();//This can cuase the XOn character to be sent to com2. 
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);
            com2.DiscardInBuffer();
        }

        return retValue;
    }

    private bool VerirfyXOnXOffBufferFull(SerialPort com1, SerialPort com2)
    {
        bool retValue = true;
        int com1BaudRate = com1.BaudRate;
        int com2BaudRate = com2.BaudRate;
        int com1ReadBufferSize = com1.ReadBufferSize;
        bool com2RtsEnable = com2.RtsEnable;
        int bufferSize = com1.ReadBufferSize;
        int upperLimit = bufferSize - 1024;
        int lowerLimit = 1024;
        byte[] bytes = new byte[upperLimit];
        int byteRead;

        try
        {
            //Set the BaudRate to something faster so that it does not take so long to fill up the buffer
            com1.BaudRate = 115200;
            com2.BaudRate = 115200;

            if (com1.Handshake == Handshake.RequestToSendXOnXOff)
            {
                com2.RtsEnable = true;
                com1.Write("foo");
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);
                com2.DiscardInBuffer();
            }

            //Write 1 less byte then when the XOff character would be sent
            com2.Write(bytes, 0, upperLimit - 1);
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsXOnXOff(com1))
            {
                if (com2.BytesToRead != 0)
                {
                    Console.WriteLine("Err_81919aniee Did not expect anything to be sent");
                    retValue = false;
                }
            }
            else
            {
                if (com2.BytesToRead != 0)
                {
                    Console.WriteLine("Err_5258aieodpo Did not expect anything to be sent com2.BytesToRead={0}", com2.BytesToRead);
                    retValue = false;
                }
            }

            //Write the byte and verify the XOff character was sent as appropriately 
            com2.Write(bytes, 0, 1);
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsXOnXOff(com1))
            {
                if (com2.BytesToRead != 1)
                {
                    Console.WriteLine("Err_12558aoed Expected XOff to be sent and nothing was sent");
                    retValue = false;
                }
                else if (XOFF_BYTE != (byteRead = com2.ReadByte()))
                {
                    Console.WriteLine("Err_0188598aoepad Expected XOff to be sent actually sent={0}", byteRead);
                    retValue = false;
                }
            }
            else
            {
                if (com2.BytesToRead != 0)
                {
                    Console.WriteLine("Err_2258ajoe Did not expect anything to be sent");
                    retValue = false;
                }
            }

            //Read 1 less byte then when the XOn char would be sent
            com1.Read(bytes, 0, (upperLimit - lowerLimit) - 1);
            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsXOnXOff(com1))
            {
                if (com2.BytesToRead != 0)
                {
                    Console.WriteLine("Err_22808aiuepa Did not expect anything to be sent");
                    retValue = false;
                }
            }
            else
            {
                if (com2.BytesToRead != 0)
                {
                    Console.WriteLine("Err_12508aieap Did not expect anything to be sent");
                    retValue = false;
                }
            }

            //Read the byte and verify the XOn char is sent as appropriately 
            com1.Read(bytes, 0, 1);

            Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

            if (IsXOnXOff(com1))
            {
                if (com2.BytesToRead != 1)
                {
                    Console.WriteLine("Err_6887518adizpa Expected XOn to be sent and nothing was sent");
                    retValue = false;
                }
                else if (XON_BYTE != (byteRead = com2.ReadByte()))
                {
                    Console.WriteLine("Err_58145auead Expected XOn to be sent actually sent={0}", byteRead);
                    retValue = false;
                }
            }
            else
            {
                if (com2.BytesToRead != 0)
                {
                    Console.WriteLine("Err_256108aipeg Did not expect anything to be sent");
                    retValue = false;
                }
            }
        }
        finally
        {
            //Rollback any changed that were made to the SerialPort
            com1.BaudRate = com1BaudRate;
            com2.BaudRate = com2BaudRate;


            com1.DiscardInBuffer();
            com2.DiscardInBuffer();

            com2.RtsEnable = com2RtsEnable;
        }

        return retValue;
    }

    private bool IsRequestToSend(SerialPort com)
    {
        return com.Handshake == Handshake.RequestToSend || com.Handshake == Handshake.RequestToSendXOnXOff;
    }

    private bool IsXOnXOff(SerialPort com)
    {
        return com.Handshake == Handshake.XOnXOff || com.Handshake == Handshake.RequestToSendXOnXOff;
    }
    #endregion
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
public class Encoding_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.Encoding";
    public static readonly String s_strTFName = "Encoding.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default number of bytes to read/write to verify the speed of the port
    //and that the bytes were transfered successfully
    public static readonly int DEFAULT_CHAR_ARRAY_SIZE = 8;

    //The maximum time we will wait for all of encoded bytes to be received
    public static readonly int MAX_WAIT_TIME = 1250;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Encoding_Property objTest = new Encoding_Property();
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

        //See individual read/write methods for further testing
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_Default), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_ASCIIEncoding_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UTF7Encoding_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UTF8Encoding_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UTF32Encoding_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UnicodeEncoding_BeforeOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_ASCIIEncoding_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UTF7Encoding_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UTF8Encoding_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UTF32Encoding_AfterOpen), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UnicodeEncoding_AfterOpen), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_ISCIIAssemese), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_UTF7), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_Null), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_IBM_Latin1), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_Japanese_JIS), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_ChineseSimplified_GB18030), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Encoding_Custom), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Encoding_Default()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default Encoding");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyEncoding(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default Encoding FAILED");
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool Encoding_ASCIIEncoding_BeforeOpen()
    {
        Console.WriteLine("Verifying ASCIIEncoding Encoding before open");
        if (!VerifyEncodingBeforeOpen(new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_002!!! Verifying ASCIIEncoding Encoding before open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UTF7Encoding_BeforeOpen()
    {
        Console.WriteLine("Verifying UTF7Encoding Encoding before open");
        if (!VerifyEncodingBeforeOpen(new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_003!!! Verifying UTF7Encoding Encoding before open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UTF8Encoding_BeforeOpen()
    {
        Console.WriteLine("Verifying UTF8Encoding Encoding before open");
        if (!VerifyEncodingBeforeOpen(new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_004!!! Verifying UTF8Encoding Encoding before open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UTF32Encoding_BeforeOpen()
    {
        Console.WriteLine("Verifying UTF32Encoding Encoding before open");
        if (!VerifyEncodingBeforeOpen(new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_005!!! Verifying UTF32Encoding Encoding before open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UnicodeEncoding_BeforeOpen()
    {
        Console.WriteLine("Verifying UnicodeEncoding Encoding before open");
        if (!VerifyEncodingBeforeOpen(new System.Text.UnicodeEncoding()))
        {
            Console.WriteLine("Err_006!!! Verifying UnicodeEncoding Encoding before open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_ASCIIEncoding_AfterOpen()
    {
        Console.WriteLine("Verifying ASCIIEncoding Encoding after open");
        if (!VerifyEncodingAfterOpen(new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_007!!! Verifying ASCIIEncoding Encoding after open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UTF7Encoding_AfterOpen()
    {
        Console.WriteLine("Verifying UTF7Encoding Encoding after open");
        if (!VerifyEncodingAfterOpen(new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_008!!! Verifying UTF7Encoding Encoding after open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UTF8Encoding_AfterOpen()
    {
        Console.WriteLine("Verifying UTF8Encoding Encoding after open");
        if (!VerifyEncodingAfterOpen(new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_009!!! Verifying UTF8Encoding Encoding after open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UTF32Encoding_AfterOpen()
    {
        Console.WriteLine("Verifying UTF32Encoding Encoding after open");
        if (!VerifyEncodingAfterOpen(new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_010!!! Verifying UTF32Encoding Encoding after open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_UnicodeEncoding_AfterOpen()
    {
        Console.WriteLine("Verifying UnicodeEncoding Encoding after open");
        if (!VerifyEncodingAfterOpen(new System.Text.UnicodeEncoding()))
        {
            Console.WriteLine("Err_011!!! Verifying UnicodeEncoding Encoding after open FAILED");
            return false;
        }

        return true;
    }


    public bool Encoding_ISCIIAssemese()
    {
        Console.WriteLine("Verifying ISCIIAssemese Encoding");
        if (!VerifyException(System.Text.Encoding.GetEncoding(57006), ThrowAt.Set, typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_2882haiued!!! Verifying ISCIIAssemese Encoding FAILED");
            return false;
        }

        return true;
    }

    public bool Encoding_UTF7()
    {
        Console.WriteLine("Verifying UTF7Encoding Encoding");
        if (!VerifyException(System.Text.Encoding.UTF7, ThrowAt.Set, typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_219298haied!!! Verifying UTF7 Encoding FAILED");
            return false;
        }

        return true;
    }

    public bool Encoding_Null()
    {
        Console.WriteLine("Verifying null Encoding");
        if (!VerifyException(null, ThrowAt.Set, typeof(System.ArgumentNullException)))
        {
            Console.WriteLine("Err_012!!! Verifying null Encoding FAILED");
            return false;
        }

        return true;
    }

    public bool Encoding_IBM_Latin1()
    {
        Console.WriteLine("Verifying IBM Latin-1 Encoding before open");
        if (!VerifyEncodingBeforeOpen(System.Text.Encoding.GetEncoding(1047)))
        {
            Console.WriteLine("Err_05884ahied!!! Verifying IBM Latin-1 Encoding before open FAILED");
            return false;
        }

        return true;
    }

    public bool Encoding_Japanese_JIS()
    {
        Console.WriteLine("Verifying Japanese (JIS) Encoding before open");
        if (!VerifyException(System.Text.Encoding.GetEncoding(50220), ThrowAt.Set, typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_05884ahied!!! Verifying Japanese (JIS) Encoding before open FAILED");
            return false;
        }

        return true;
    }

    public bool Encoding_ChineseSimplified_GB18030()
    {
        Console.WriteLine("Verifying Chinese Simplified (GB18030) Encoding before open");
        if (!VerifyEncodingBeforeOpen(System.Text.Encoding.GetEncoding(54936)))
        {
            Console.WriteLine("Err_05884ahied!!! Verifying Chinese Simplified (GB18030) Encoding before open FAILED");
            return false;
        }

        return true;
    }

    public bool Encoding_Custom()
    {
        Console.WriteLine("Verifying Custom Encoding before open");

        if (!VerifyException(new MyEncoding(1047), ThrowAt.Set, typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_012!!! Verifying Custom Encoding FAILED");
            return false;
        }
        return true;
    }

    public class MyEncoding : System.Text.Encoding
    {
        public MyEncoding(int codePage)
            : base(codePage)
        {
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            throw new NotSupportedException();
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            throw new NotSupportedException();
        }

        public override int GetMaxByteCount(int charCount)
        {
            throw new NotSupportedException();
        }

        public override int GetMaxCharCount(int byteCount)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

    #region Verification for Test Cases
    private bool VerifyException(System.Text.Encoding encoding, ThrowAt throwAt, System.Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, encoding, throwAt, expectedException);

        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, encoding, expectedException);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, System.Text.Encoding encoding, ThrowAt throwAt, System.Type expectedException)
    {
        System.Text.Encoding origEncoding = com.Encoding;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("Encoding", encoding);

        try
        {
            com.Encoding = encoding;

            if (ThrowAt.Open == throwAt)
                com.Open();

            Object myEncoding = com.Encoding;

            com.Encoding = origEncoding;

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
                Console.WriteLine(e);
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        com.Encoding = origEncoding;

        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, System.Text.Encoding encoding, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.Encoding = encoding;

            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Encoding after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Encoding after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the Encoding after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        return retValue;
    }


    private bool VerifyEncodingBeforeOpen(System.Text.Encoding encoding)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Encoding = encoding;
        com1.Open();
        serPortProp.SetProperty("Encoding", encoding);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyEncoding(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyEncodingAfterOpen(System.Text.Encoding encoding)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.Encoding = encoding;
        serPortProp.SetProperty("Encoding", encoding);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyEncoding(com1);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyEncoding(SerialPort com1)
    {
        bool retValue = true;
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        int origReadTimeout = com1.ReadTimeout;
        char[] xmitChars = TCSupport.GetRandomChars(DEFAULT_CHAR_ARRAY_SIZE, true);
        byte[] xmitBytes;
        char[] rcvChars = new char[DEFAULT_CHAR_ARRAY_SIZE];
        char[] expectedChars;
        int waitTime = 0;

        xmitBytes = com1.Encoding.GetBytes(xmitChars);
        expectedChars = com1.Encoding.GetChars(xmitBytes);

        com2.Open();
        com2.Encoding = com1.Encoding;

        com2.Write(xmitChars, 0, xmitChars.Length);


        //		for(int i=0; i<xmitChars.Length; ++i) {
        //			Console.WriteLine("{0},", (int)xmitChars[i]);
        //		}

        while (com1.BytesToRead < xmitBytes.Length)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;

            if (MAX_WAIT_TIME < waitTime)
            {
                Console.WriteLine("ERROR!!! Expected BytesToRead={0} actual={1}", xmitBytes.Length, com1.BytesToRead);
                break;
            }
        }

        com1.Read(rcvChars, 0, rcvChars.Length);

        if (expectedChars.Length != rcvChars.Length)
        {
            Console.WriteLine("ERROR!!! Expected to read {0} chars actually read {1} chars", expectedChars.Length, rcvChars.Length);
            retValue = false;
        }
        else
        {
            for (int i = 0; i < rcvChars.Length; i++)
            {
                if (rcvChars[i] != expectedChars[i])
                {
                    Console.WriteLine("ERROR!!! Expected to read {0} at {1} actually read {2}", (int)expectedChars[i], i, (int)rcvChars[i]);
                    retValue = false;
                }
            }
        }

        if (com2.IsOpen)
            com2.Close();

        com1.ReadTimeout = origReadTimeout;
        return retValue;
    }
    #endregion
}

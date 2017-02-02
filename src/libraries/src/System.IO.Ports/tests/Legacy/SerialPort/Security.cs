// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;
using System.IO;
using System.IO.Ports;

public class Security_TestCase
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "Security";
    public static readonly String s_strTFName = "Security.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Security_TestCase objTest = new Security_TestCase();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(PermitOnly_UnmanagedCode), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Deny_UnmanagedCode), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    public bool PermitOnly_UnmanagedCode()
    {
        SerialPort com1;
        bool retValue = true;

        Console.WriteLine("PermitOnly UnmanagedCode");
        (new SecurityPermission(SecurityPermissionFlag.UnmanagedCode)).PermitOnly();

        com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        //[] Open
        try
        {
            com1.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine("Open threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        retValue &= CallEvents(com1);
        retValue &= CallProperties(com1);
        retValue &= CallMethods(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! PermitOnly UnmanagedCode FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool Deny_UnmanagedCode()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Deny UnmanagedCode");
        (new SecurityPermission(SecurityPermissionFlag.UnmanagedCode)).Deny();

        try
        {
            com1.Open();
            retValue = false;
            Console.WriteLine("Expected ctor to throw SecurityException");
        }
        catch (System.Security.SecurityException) { }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    private bool CallEvents(SerialPort com)
    {
        bool retValue = true;
        SerialErrorReceivedEventHandler serialErrorReceivedEventHandler = SerialErrorReceivedEventHandler;
        SerialPinChangedEventHandler serialPinChangedEventHandler = SerialPinChangedEventHandler;
        SerialDataReceivedEventHandler serialDataReceivedEventHandler = SerialDataReceivedEventHandler;

        //[] ErrorReceived Add 
        try
        {
            com.ErrorReceived += serialErrorReceivedEventHandler;
        }
        catch (Exception e)
        {
            Console.WriteLine("ErrorReceived Add threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ErrorReceived Remove
        try
        {
            com.ErrorReceived -= serialErrorReceivedEventHandler;
        }
        catch (Exception e)
        {
            Console.WriteLine("ErrorReceived Remove threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] PinChanged Add
        try
        {
            com.PinChanged += serialPinChangedEventHandler;
        }
        catch (Exception e)
        {
            Console.WriteLine("PinChanged Add threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] PinChanged Remove
        try
        {
            com.PinChanged -= serialPinChangedEventHandler;
        }
        catch (Exception e)
        {
            Console.WriteLine("PinChanged Remove threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DataReceived Add
        try
        {
            com.DataReceived += serialDataReceivedEventHandler;
        }
        catch (Exception e)
        {
            Console.WriteLine("DataReceived Add threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DataReceived Remove
        try
        {
            com.DataReceived -= serialDataReceivedEventHandler;
        }
        catch (Exception e)
        {
            Console.WriteLine("DataReceived Remove threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        return retValue;
    }

    private bool CallProperties(SerialPort com)
    {
        bool retValue = true;

        //[] BaseStream get
        try
        {
            Stream stream = com.BaseStream;
        }
        catch (Exception e)
        {
            Console.WriteLine("BaseStream get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] BaudRate get 
        try
        {
            int baudRate = com.BaudRate;
        }
        catch (Exception e)
        {
            Console.WriteLine("BaudRate get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] BaudRate set
        try
        {
            com.BaudRate = 14400;
        }
        catch (Exception e)
        {
            Console.WriteLine("BaudRate set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] BreakState get
        try
        {
            bool breakState = com.BreakState;
        }
        catch (Exception e)
        {
            Console.WriteLine("BreakState get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] BreakState set 
        try
        {
            com.BreakState = true;
            com.BreakState = false;
        }
        catch (Exception e)
        {
            Console.WriteLine("BaudRBreakStateate set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] BytesToWrite get
        try
        {
            int bytesToWrite = com.BytesToWrite;
        }
        catch (Exception e)
        {
            Console.WriteLine("BytesToWrite get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] BytesToRead get
        try
        {
            int bytesToRead = com.BytesToRead;
        }
        catch (Exception e)
        {
            Console.WriteLine("BytesToRead get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] CDHolding get
        try
        {
            bool cDHolding = com.CDHolding;
        }
        catch (Exception e)
        {
            Console.WriteLine("CDHolding get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] CtsHolding get
        try
        {
            bool ctsHolding = com.CtsHolding;
        }
        catch (Exception e)
        {
            Console.WriteLine("CtsHolding get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DataBits get
        try
        {
            int DataBits = com.DataBits;
        }
        catch (Exception e)
        {
            Console.WriteLine("DataBits get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DataBits set
        try
        {
            com.DataBits = 7;
            com.DataBits = 8;
        }
        catch (Exception e)
        {
            Console.WriteLine("DataBits set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DiscardNull get
        try
        {
            bool discardNull = com.DiscardNull;
        }
        catch (Exception e)
        {
            Console.WriteLine("DiscardNull get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DiscarNull set
        try
        {
            com.DiscardNull = true;
            com.DiscardNull = false;
        }
        catch (Exception e)
        {
            Console.WriteLine("DiscardNull set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DsrHolding get
        try
        {
            bool DsrHolding = com.DsrHolding;
        }
        catch (Exception e)
        {
            Console.WriteLine("DsrHolding get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DtrEnable get
        try
        {
            bool dtrEnable = com.DtrEnable;
        }
        catch (Exception e)
        {
            Console.WriteLine("DtrEnable get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DtrEnable set
        try
        {
            com.DtrEnable = true;
            com.DtrEnable = false;
        }
        catch (Exception e)
        {
            Console.WriteLine("DtrEnable set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Encoding get
        try
        {
            System.Text.Encoding encoding = com.Encoding;
        }
        catch (Exception e)
        {
            Console.WriteLine("Encoding get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Encoding set
        try
        {
            com.Encoding = System.Text.Encoding.UTF8;
            com.Encoding = System.Text.Encoding.ASCII;
        }
        catch (Exception e)
        {
            Console.WriteLine("Encoding set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Handshake get
        try
        {
            Handshake handshake = com.Handshake;
        }
        catch (Exception e)
        {
            Console.WriteLine("Handshake get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Handshake set
        try
        {
            com.Handshake = Handshake.RequestToSend;
            com.Handshake = Handshake.None;
        }
        catch (Exception e)
        {
            Console.WriteLine("Handshake set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] IsOpen get
        try
        {
            bool isOpen = com.IsOpen;
        }
        catch (Exception e)
        {
            Console.WriteLine("IsOpen get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] NewLine get
        try
        {
            string newLine = com.NewLine;
        }
        catch (Exception e)
        {
            Console.WriteLine("NewLine get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] NewLine set
        try
        {
            com.NewLine = "foo";
            com.NewLine = "\n";
        }
        catch (Exception e)
        {
            Console.WriteLine("NewLine set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Parity get
        try
        {
            Parity parity = com.Parity;
        }
        catch (Exception e)
        {
            Console.WriteLine("Parity get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Parity set
        try
        {
            com.Parity = Parity.Even;
            com.Parity = Parity.None;
        }
        catch (Exception e)
        {
            Console.WriteLine("Parity set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ParityReplace get
        try
        {
            byte parityReplace = com.ParityReplace;
        }
        catch (Exception e)
        {
            Console.WriteLine("ParityReplace get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ParityReplace set
        try
        {
            com.ParityReplace = 32;
            com.ParityReplace = 63;
        }
        catch (Exception e)
        {
            Console.WriteLine("ParityReplace set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] PortName get
        try
        {
            string portName = com.PortName;
        }
        catch (Exception e)
        {
            Console.WriteLine("PortName get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] PortName set
        try
        {
            com.Close();
            com.PortName = "Com255";
            com.PortName = TCSupport.LocalMachineSerialInfo.FirstAvailablePortName;
            com.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine("PortName set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadBufferSize get
        try
        {
            int readBufferSize = com.ReadBufferSize;
        }
        catch (Exception e)
        {
            Console.WriteLine("ReadBufferSize get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadBufferSize set
        try
        {
            com.Close();
            com.ReadBufferSize = 8192;
            com.ReadBufferSize = 4096;
            com.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine("ReadBufferSize set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadTimeout get
        try
        {
            int readTimeout = com.ReadTimeout;
        }
        catch (Exception e)
        {
            Console.WriteLine("ReadTimeout get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadTimeout set
        try
        {
            com.ReadTimeout = 1000;
            com.ReadTimeout = -1;
        }
        catch (Exception e)
        {
            Console.WriteLine("ReadTimeout set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReceivedBytesThreshold get
        try
        {
            int receivedBytesThreshold = com.ReceivedBytesThreshold;
        }
        catch (Exception e)
        {
            Console.WriteLine("ReceivedBytesThreshold get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReceivedBytesThreshold set
        try
        {
            com.ReceivedBytesThreshold = 1;
            com.ReceivedBytesThreshold = 8;
        }
        catch (Exception e)
        {
            Console.WriteLine("ReceivedBytesThreshold set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] RtsEnable get
        try
        {
            bool rtsEnable = com.RtsEnable;
        }
        catch (Exception e)
        {
            Console.WriteLine("RtsEnable get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] RtsEnable set
        try
        {
            com.RtsEnable = true;
            com.RtsEnable = false;
        }
        catch (Exception e)
        {
            Console.WriteLine("RtsEnable set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] StopBits get
        try
        {
            StopBits stopBits = com.StopBits;
        }
        catch (Exception e)
        {
            Console.WriteLine("StopBits get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] StopBits set
        try
        {
            com.StopBits = StopBits.Two;
            com.StopBits = StopBits.One;
        }
        catch (Exception e)
        {
            Console.WriteLine("StopBits set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] WriteBufferSize get
        try
        {
            int writeBufferSize = com.WriteBufferSize;
        }
        catch (Exception e)
        {
            Console.WriteLine("WriteBufferSize get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] WriteBufferSize set
        try
        {
            com.Close();
            com.WriteBufferSize = 8192;
            com.WriteBufferSize = 4096;
            com.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine("WriteBufferSize set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] WriteTimeout get
        try
        {
            int writeTimeout = com.WriteTimeout;
        }
        catch (Exception e)
        {
            Console.WriteLine("WriteTimeout get threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] WriteTimeout set
        try
        {
            com.WriteTimeout = 1000;
            com.WriteTimeout = -1;
        }
        catch (Exception e)
        {
            Console.WriteLine("WriteTimeout set threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        return retValue;
    }

    private bool CallMethods(SerialPort com)
    {
        bool retValue = true;

        com.ReadTimeout = 0;

        //[] DiscardInBuffer
        try
        {
            com.DiscardInBuffer();
        }
        catch (Exception e)
        {
            Console.WriteLine("DiscardInBuffer threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] DiscardOutBuffer
        try
        {
            com.DiscardOutBuffer();
        }
        catch (Exception e)
        {
            Console.WriteLine("DiscardOutBuffer threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] GetPortNames
        try
        {
            SerialPort.GetPortNames();
        }
        catch (Exception e)
        {
            Console.WriteLine("GetPortNames threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Read(byte[], int, int)
        try
        {
            com.Read(new byte[1], 0, 1);
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Console.WriteLine("Read(byte[], int, int) threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadChar
        try
        {
            com.ReadChar();
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Console.WriteLine("ReadChar threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Read(char[], int, int)
        try
        {
            com.Read(new char[1], 0, 1);
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Console.WriteLine("Read(char[], int, int) threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadByte
        try
        {
            com.ReadByte();
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Console.WriteLine("ReadByte threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadExisting
        try
        {
            com.ReadByte();
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Console.WriteLine("ReadExisting threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadLine
        try
        {
            com.ReadLine();
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Console.WriteLine("ReadLine threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] ReadTo
        try
        {
            com.ReadTo("<END>");
        }
        catch (TimeoutException) { }
        catch (Exception e)
        {
            Console.WriteLine("ReadTo threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Write(string)
        try
        {
            com.Write("foo");
        }
        catch (Exception e)
        {
            Console.WriteLine("Write(string) threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Write(char[], int, int)
        try
        {
            com.Write(new char[1], 0, 1);
        }
        catch (Exception e)
        {
            Console.WriteLine("Write(char[], int, int) threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] Write(byte[], int, int)
        try
        {
            com.Write(new byte[1], 0, 1);
        }
        catch (Exception e)
        {
            Console.WriteLine("Write(byte[], int, int) threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        //[] WriteLine
        try
        {
            com.WriteLine("foo");
        }
        catch (Exception e)
        {
            Console.WriteLine("WriteLine threw the following unexpected exception:");
            Console.WriteLine(e);
            retValue = false;
        }

        return retValue;
    }

    private void SerialErrorReceivedEventHandler(Object sender, SerialErrorReceivedEventArgs e) { }
    private void SerialPinChangedEventHandler(Object sender, SerialPinChangedEventArgs e) { }
    private void SerialDataReceivedEventHandler(Object sender, SerialDataReceivedEventArgs e) { }
}

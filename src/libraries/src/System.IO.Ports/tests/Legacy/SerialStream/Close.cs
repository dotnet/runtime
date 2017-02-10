// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections;

public class Close
{
    public static readonly String s_strActiveBugNums = "";
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/10 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.Close()";
    public static readonly String s_strTFName = "Close.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The number of the bytes that should read/write buffers
    public static readonly int numReadBytes = 32;
    public static readonly int numWriteBytes = 32;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Close objTest = new Close();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenClose_WriteMethods), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenClose_ReadMethods), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenClose_DiscardMethods), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenClose_OpenClose), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenClose_Properties), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenFillBuffersClose), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OpenCloseNewInstanceOpen), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Open_BaseStreamClose_Open), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Open_BaseStreamClose_Close), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Open_MultipleBaseStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }


    public bool OpenClose_WriteMethods()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying calling Write methods after calling Open() and BaseStream.Close()");
        com.Open();
        com.BaseStream.Close();

        try
        {
            com.Write(new byte[8], 0, 8);
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Write(byte[], int, int) threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.Write(new char[8], 0, 8);
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Write(char[], int, int) threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.Write("A");
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Write(string) threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.WriteLine("A");
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! WriteLine(string) threw {0}", e.GetType());
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying calling Write methods after calling Open() and BaseStream.Close() FAILED");
        }

        return retValue;
    }


    public bool OpenClose_ReadMethods()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying calling Read methods after calling Open() and BaseStream.Close()");
        com.Open();
        com.BaseStream.Close();

        try
        {
            com.Read(new byte[8], 0, 8);
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Read(byte[], int, int) threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.Read(new char[8], 0, 8);
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Read(char[], int, int) threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.ReadByte();
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Read() threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.ReadExisting();
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! ReadExisting() threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.ReadLine();
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! ReadLine() threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.ReadTo("A");
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! ReadTo(string) threw {0}", e.GetType());
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying calling Read methods after calling Open() and BaseStream.Close() FAILED");
        }

        return retValue;
    }


    public bool OpenClose_DiscardMethods()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying calling Discard methods after calling Open() and BaseStream.Close()");
        com.Open();
        com.BaseStream.Close();

        try
        {
            com.DiscardInBuffer();
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! DiscardInBuffer() threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.DiscardOutBuffer();
        }
        catch (System.InvalidOperationException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! DiscardOutBuffer() threw {0}", e.GetType());
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying calling Discard methods after calling Open() and BaseStream.Close() FAILED");
        }

        return retValue;
    }


    public bool OpenClose_OpenClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying calling Open/Close methods after calling Open() and BaseStream.Close()");
        com.Open();
        com.BaseStream.Close();

        try
        {
            com.Close();
        }
        catch (System.ObjectDisposedException) { }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Close() threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            com.Open();
        }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Open() threw {0}", e.GetType());
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying calling Open/Close methods after calling Open() and BaseStream.Close() FAILED");
        }

        try
        {
            if (com.IsOpen)
                com.Close();
        }
        catch (Exception) { }

        return retValue;
    }


    public bool OpenClose_Properties()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        Console.WriteLine("Verifying Properites after calling Open() and BaseStream.Close()");

        com.Open();
        com.BaseStream.Close();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! erifying Properites after calling Open() and BaseStream.Close() FAILED");
        }

        return retValue;
    }


    public bool OpenFillBuffersClose()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Calling Open(), fill both trasmit and receive buffers, call Close()");
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        //Setting com1 to use Handshake so we can fill read buffer
        com1.Handshake = Handshake.RequestToSend;
        com1.Open();
        com2.Open();

        //BeginWrite is used so we can fill the read buffer then go onto to verify
        com1.BaseStream.BeginWrite(new Byte[numWriteBytes], 0, numWriteBytes, null, null);
        com2.Write(new Byte[numReadBytes], 0, numReadBytes);
        System.Threading.Thread.Sleep(500);

        serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
        serPortProp.SetProperty("BytesToWrite", numWriteBytes);
        serPortProp.SetProperty("BytesToRead", numReadBytes);

        Console.WriteLine("Verifying properties after port is open and bufferes have been filled");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        com1.Handshake = Handshake.None;
        com1.BaseStream.Close();
        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying properties after port has been closed");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        try
        {
            com1.Open();
        }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Open() threw {0}", e.GetType());
            retValue = false;
        }

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying properties after port has been opened again");
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        try
        {
            if (com1.IsOpen)
                com1.Close();
        }
        catch (Exception) { }

        if (com2.IsOpen)
            com2.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying calling Open(), fill both trasmit and receive buffers, call Close() FAILED");
        }

        return retValue;
    }


    public bool OpenCloseNewInstanceOpen()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort newCom;
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue;

        Console.WriteLine("Calling Close() after calling Open() then create a new instance of SerialPort and call Open() again");
        com.Open();
        com.BaseStream.Close();
        newCom = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            newCom.Open();

            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
            retValue = serPortProp.VerifyPropertiesAndPrint(newCom);
        }
        catch (System.Exception e)
        {
            Console.WriteLine("ERROR!!! Open() threw {0}", e.GetType());
            retValue = false;
        }

        try
        {
            if (com.IsOpen)
                com.Close();
        }
        catch (Exception) { }

        if (newCom.IsOpen)
            newCom.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying calling Close() after calling Open() then create a new instance of SerialPort and call Open() again FAILED");
        }

        return retValue;
    }


    public bool Open_BaseStreamClose_Open()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        Console.WriteLine("Verifying Properites after calling Open(), BaseStream.Close(), then Open() again");

        com.Open();
        com.BaseStream.Close();
        com.Open();

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying Properites after calling Open(), BaseStream.Close(), then Open() again FAILED");
        }

        return retValue;
    }


    public bool Open_BaseStreamClose_Close()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        Console.WriteLine("Verifying Properites after calling Open(), BaseStream.Close(), then Close()");

        com.Open();
        com.BaseStream.Close();
        com.Close();

        serPortProp.SetAllPropertiesToDefaults();

        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying Properites after calling Open(), BaseStream.Close(), then Close() FAILED");
        }

        return retValue;
    }


    public bool Open_MultipleBaseStreamClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying Properites after calling Open(), BaseStream.Close() multiple times");

        com.Open();
        serialStream = com.BaseStream;
        serialStream.Close();
        serialStream.Close();
        serialStream.Close();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying Properites after calling Open(), BaseStream.Close() multiple times FAILED");
        }

        return retValue;
    }
}

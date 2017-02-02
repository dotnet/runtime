// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class DiscardNull_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.DiscardNull";
    public static readonly String s_strTFName = "DiscardNull.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default number of bytes to read/write to verify the DiscardNull
    public static readonly int DEFAULT_NUM_CHARS_TO_WRITE = 8;

    //The default number of null characters to be inserted into the characters written  
    public static readonly int DEFUALT_NUM_NULL_CHAR = 1;

    //The default number of chars to write with when testing timeout with Read(char[], int, int)
    public static readonly int DEFAULT_READ_CHAR_ARRAY_SIZE = 8;

    //The default number of bytes to write with when testing timeout with Read(byte[], int, int)
    public static readonly int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;

    //The maximum time to wait for an event to occur
    public static readonly int MAX_WAIT_TIME = 750;

    private delegate char[] ReadMethodDelegate(SerialPort com);

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        DiscardNull_Property objTest = new DiscardNull_Property();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_Default_Read_byte_int_int), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_Default_Read_char_int_int), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_Default_ReadByte), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_Default_ReadChar), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_Default_ReadLine), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_Default_ReadTo), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_Read_byte_int_int_Before), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_Read_char_int_int_After), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_ReadByte_Before), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_ReadChar_After), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_ReadLine_Before), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_ReadTo_After), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_false_Read_byte_int_int_Before), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_true_true_Read_char_int_int_After), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DiscardNull_false_flase_Default_ReadByte), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool DiscardNull_Default_Read_byte_int_int()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with Read_byte_int_int");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(Read_byte_int_int), false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default DiscardNull with Read_byte_int_int FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DiscardNull_Default_Read_char_int_int()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with Read_char_int_int");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(Read_char_int_int), false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying default DiscardNull with Read_char_int_int FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DiscardNull_Default_ReadByte()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with ReadByte");
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(ReadByte), false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying default DiscardNull with ReadByte FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DiscardNull_Default_ReadChar()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with ReadChar");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(ReadChar), false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying default DiscardNull with ReadChar FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DiscardNull_Default_ReadLine()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with ReadLine");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(ReadLine), true);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying default DiscardNull with ReadLine FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DiscardNull_Default_ReadTo()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with ReadTo");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(ReadTo), true);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying default DiscardNull with ReadTo FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool DiscardNull_true_Read_byte_int_int_Before()
    {
        Console.WriteLine("Verifying true DiscardNull with Read_byte_int_int before open");
        if (!VerifyDiscardNullBeforeOpen(true, new ReadMethodDelegate(Read_byte_int_int), false))
        {
            Console.WriteLine("Err_007!!! Verifying true DiscardNull with Read_byte_int_int before open FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardNull_true_Read_char_int_int_After()
    {
        Console.WriteLine("Verifying true DiscardNull with Read_char_int_int after open");
        if (!VerifyDiscardNullAfterOpen(true, new ReadMethodDelegate(Read_char_int_int), false))
        {
            Console.WriteLine("Err_008!!! Verifying true DiscardNull with Read_char_int_int after open FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardNull_true_ReadByte_Before()
    {
        Console.WriteLine("Verifying true DiscardNull with ReadByte before open");
        if (!VerifyDiscardNullBeforeOpen(true, new ReadMethodDelegate(ReadByte), false))
        {
            Console.WriteLine("Err_009!!! Verifying true DiscardNull with ReadByte before open FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardNull_true_ReadChar_After()
    {
        Console.WriteLine("Verifying true DiscardNull with ReadChar after open");
        if (!VerifyDiscardNullAfterOpen(true, new ReadMethodDelegate(ReadChar), false))
        {
            Console.WriteLine("Err_009!!! Verifying true DiscardNull with ReadChar after open FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardNull_true_ReadLine_Before()
    {
        Console.WriteLine("Verifying true DiscardNull with ReadLine before open");
        if (!VerifyDiscardNullBeforeOpen(true, new ReadMethodDelegate(ReadLine), true))
        {
            Console.WriteLine("Err_009!!! Verifying true DiscardNull with ReadLine before open FAILED");
            return false;
        }

        return true;
    }


    public bool DiscardNull_true_ReadTo_After()
    {
        Console.WriteLine("Verifying true DiscardNull with ReadTo after open");
        if (!VerifyDiscardNullAfterOpen(true, new ReadMethodDelegate(ReadTo), true))
        {
            Console.WriteLine("Err_009!!! Verifying true DiscardNull with ReadTo after open FAILED");
            return false;
        }

        return true;
    }

    public bool DiscardNull_true_false_Read_byte_int_int_Before()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with Read_byte_int_int");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        com1.DiscardNull = true;
        com1.DiscardNull = false;

        serPortProp.SetProperty("DiscardNull", false);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(Read_byte_int_int), false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_151548aheid!!! Verifying default DiscardNull with Read_byte_int_int FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool DiscardNull_true_true_Read_char_int_int_After()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with Read_char_int_int");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        com1.DiscardNull = true;
        com1.DiscardNull = true;

        serPortProp.SetProperty("DiscardNull", true);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(Read_char_int_int), false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_015618aid!!! Verifying default DiscardNull with Read_char_int_int FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }

    public bool DiscardNull_false_flase_Default_ReadByte()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default DiscardNull with ReadByte");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        com1.DiscardNull = false;
        com1.DiscardNull = false;

        serPortProp.SetProperty("DiscardNull", false);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, new ReadMethodDelegate(ReadByte), false);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_4088ahjied!!! Verifying default DiscardNull with ReadByte FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyDiscardNullBeforeOpen(bool discardNull, ReadMethodDelegate readMethod, bool sendNewLine)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.DiscardNull = discardNull;
        com1.Open();
        serPortProp.SetProperty("DiscardNull", discardNull);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, readMethod, sendNewLine);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyDiscardNullAfterOpen(bool discardNull, ReadMethodDelegate readMethod, bool sendNewLine)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com1.DiscardNull = discardNull;
        serPortProp.SetProperty("DiscardNull", discardNull);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyDiscardNull(com1, readMethod, sendNewLine);
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool VerifyDiscardNull(SerialPort com1, ReadMethodDelegate readMethod, bool sendNewLine)
    {
        bool retValue = true;
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        char[] xmitChars = new char[DEFAULT_NUM_CHARS_TO_WRITE];
        byte[] xmitBytes;
        char[] expectedChars;
        char[] rcvChars;
        byte[] expectedBytes;
        Random rndGen = new Random();
        int numNulls = 0;
        int expectedIndex;
        int origReadTimeout = com1.ReadTimeout;

        //Generate some random chars to transfer
        for (int i = 0; i < xmitChars.Length; i++)
        {
            //xmitChars[i] = (char)rndGen.Next(1, UInt16.MaxValue);
            xmitChars[i] = (char)rndGen.Next(60, 80);
        }

        //Inject the null char randomly 
        for (int i = 0; i < DEFUALT_NUM_NULL_CHAR; i++)
        {
            int nullIndex = rndGen.Next(0, xmitChars.Length);

            if ('\0' != xmitChars[nullIndex])
            {
                numNulls++;
            }

            xmitChars[nullIndex] = '\0';
        }

        xmitBytes = com1.Encoding.GetBytes(xmitChars);

        if (com1.DiscardNull)
        {
            expectedIndex = 0;
            expectedChars = new char[xmitChars.Length - numNulls];
            for (int i = 0; i < xmitChars.Length; i++)
            {
                if ('\0' != xmitChars[i])
                {
                    expectedChars[expectedIndex] = xmitChars[i];
                    expectedIndex++;
                }
            }
        }
        else
        {
            expectedChars = new char[xmitChars.Length];
            Array.Copy(xmitChars, 0, expectedChars, 0, expectedChars.Length);
        }

        expectedBytes = com1.Encoding.GetBytes(expectedChars);
        expectedChars = com1.Encoding.GetChars(expectedBytes);

        com2.Open();
        com2.Write(xmitBytes, 0, xmitBytes.Length);

        int timeElapsed = 0;

        while (expectedBytes.Length > com1.BytesToRead && timeElapsed < MAX_WAIT_TIME)
        {
            System.Threading.Thread.Sleep(50);
            timeElapsed += 50;
        }

        if (sendNewLine)
            com2.WriteLine("");

        com1.ReadTimeout = 250;

        rcvChars = readMethod(com1);

        if (0 != com1.BytesToRead)
        {
            Console.WriteLine("ERROR!!! Expected BytesToRead=0 actual={0}", com1.BytesToRead);
            retValue = false;
        }

        if (expectedChars.Length != rcvChars.Length)
        {
            Console.WriteLine("ERROR!!! Expected to read {0} chars actually read {1} chars", expectedChars.Length, rcvChars.Length);
            retValue = false;
        }
        else
        {
            for (int i = 0; i < expectedChars.Length; i++)
            {
                if (expectedChars[i] != rcvChars[i])
                {
                    Console.WriteLine("Expected to read {0} actually read {1} at {2}", (int)expectedChars[i], (int)rcvChars[i], i);
                    retValue = false;
                }
            }
        }

        com1.ReadTimeout = origReadTimeout;
        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private char[] Read_byte_int_int(SerialPort com)
    {
        System.Collections.ArrayList receivedBytes = new System.Collections.ArrayList();
        byte[] buffer = new byte[DEFAULT_READ_BYTE_ARRAY_SIZE];
        int totalBytesRead = 0;
        int numBytes;

        while (true)
        {
            try
            {
                numBytes = com.Read(buffer, 0, buffer.Length);
            }
            catch (TimeoutException)
            {
                break;
            }

            receivedBytes.InsertRange(totalBytesRead, buffer);
            totalBytesRead += numBytes;
        }

        if (totalBytesRead < receivedBytes.Count)
            receivedBytes.RemoveRange(totalBytesRead, receivedBytes.Count - totalBytesRead);

        return com.Encoding.GetChars((byte[])receivedBytes.ToArray(typeof(byte)));
    }


    private char[] Read_char_int_int(SerialPort com)
    {
        System.Collections.ArrayList receivedChars = new System.Collections.ArrayList();
        char[] buffer = new char[DEFAULT_READ_CHAR_ARRAY_SIZE];
        int totalCharsRead = 0;
        int numChars;

        while (true)
        {
            try
            {
                numChars = com.Read(buffer, 0, buffer.Length);
            }
            catch (TimeoutException)
            {
                break;
            }

            receivedChars.InsertRange(totalCharsRead, buffer);
            totalCharsRead += numChars;
        }

        if (totalCharsRead < receivedChars.Count)
            receivedChars.RemoveRange(totalCharsRead, receivedChars.Count - totalCharsRead);

        return (char[])receivedChars.ToArray(typeof(char));
    }


    private char[] ReadByte(SerialPort com)
    {
        System.Collections.ArrayList receivedBytes = new System.Collections.ArrayList();
        int rcvByte;

        while (true)
        {
            try
            {
                rcvByte = com.ReadByte();
            }
            catch (TimeoutException)
            {
                break;
            }

            receivedBytes.Add((byte)rcvByte);
        }

        return com.Encoding.GetChars((byte[])receivedBytes.ToArray(typeof(byte)));
    }


    private char[] ReadChar(SerialPort com)
    {
        System.Collections.ArrayList receivedChars = new System.Collections.ArrayList();
        int rcvChar;

        while (true)
        {
            try
            {
                rcvChar = com.ReadChar();
            }
            catch (TimeoutException)
            {
                break;
            }
            receivedChars.Add((char)rcvChar);
        }

        return (char[])receivedChars.ToArray(typeof(char));
    }


    private char[] ReadLine(SerialPort com)
    {
        System.Text.StringBuilder rcvStringBuilder = new System.Text.StringBuilder();
        string rcvString;

        while (true)
        {
            try
            {
                rcvString = com.ReadLine();
            }
            catch (TimeoutException)
            {
                break;
            }

            rcvStringBuilder.Append(rcvString);
        }

        return rcvStringBuilder.ToString().ToCharArray();
    }


    private char[] ReadTo(SerialPort com)
    {
        System.Text.StringBuilder rcvStringBuilder = new System.Text.StringBuilder();
        string rcvString;

        while (true)
        {
            try
            {
                rcvString = com.ReadTo(com.NewLine);
            }
            catch (TimeoutException)
            {
                break;
            }

            rcvStringBuilder.Append(rcvString);
        }

        return rcvStringBuilder.ToString().ToCharArray();
    }
    #endregion
}

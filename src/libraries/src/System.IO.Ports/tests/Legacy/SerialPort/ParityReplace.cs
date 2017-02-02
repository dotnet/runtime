// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class ParityReplace_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ParityReplace";
    public static readonly String s_strTFName = "ParityReplace.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The default number of chars to write with when testing timeout with Read(char[], int, int)
    public static readonly int DEFAULT_READ_CHAR_ARRAY_SIZE = 8;

    //The default number of bytes to write with when testing timeout with Read(byte[], int, int)
    public static readonly int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;
    private static readonly int s_numRndBytesPairty = 8;

    public delegate char[] ReadMethodDelegate(SerialPort com);

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ParityReplace_Property objTest = new ParityReplace_Property();
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

        //See individual read methods for further testing
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ParityReplace_Default_BeforeOpen), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ParityReplace_Default_AfterOpen), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_byte_int_int_RNDParityReplace), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_char_int_int_RNDParityReplace), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadByte_RNDParityReplace), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadChar_RNDParityReplace), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadLine_RNDParityReplace), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadTo_str_RNDParityReplace), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_byte_int_int_RNDParityReplace), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ParityReplace_After_Parity), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ParityReplace_After_ParityReplace), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ParityReplace_After_ParityReplaceAndParity), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ParityReplace_Default_BeforeOpen()
    {
        SerialPort com1 = new SerialPort();
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default ParityReplace before Open");

        serPortProp.SetAllPropertiesToDefaults();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying default ParityReplace before Open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    public bool ParityReplace_Default_AfterOpen()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying default ParityReplace after Open");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        com1.Open();
        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying default ParityReplace after Open FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        return retValue;
    }


    private bool Read_byte_int_int_RNDParityReplace()
    {
        bool retValue = true;

        Console.WriteLine("Verifying random ParityReplace with Read(byte[], int, int)");
        retValue &= VerifyParityReplaceByte(new ReadMethodDelegate(Read_byte_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying random ParityReplace with Read(byte[], int, int) FAILED");
        }

        return retValue;
    }


    private bool Read_char_int_int_RNDParityReplace()
    {
        bool retValue = true;

        Console.WriteLine("Verifying random ParityReplace with Read(char[], int, int)");
        retValue &= VerifyParityReplaceByte(new ReadMethodDelegate(Read_char_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying random ParityReplace with Read(char[], int, int) FAILED");
        }

        return retValue;
    }


    private bool ReadByte_RNDParityReplace()
    {
        bool retValue = true;

        Console.WriteLine("Verifying random ParityReplace with ReadByte()");
        retValue &= VerifyParityReplaceByte(new ReadMethodDelegate(ReadByte), false);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying random ParityReplace with ReadByte() FAILED");
        }

        return retValue;
    }


    private bool ReadChar_RNDParityReplace()
    {
        bool retValue = true;

        Console.WriteLine("Verifying random ParityReplace with ReadChar()");
        retValue &= VerifyParityReplaceByte(new ReadMethodDelegate(ReadChar), false);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying random ParityReplace with ReadChar() FAILED");
        }

        return retValue;
    }


    private bool ReadLine_RNDParityReplace()
    {
        bool retValue = true;

        Console.WriteLine("Verifying random ParityReplace with ReadLine()");

        retValue &= VerifyParityReplaceByte(17, new ReadMethodDelegate(ReadLine), true);

        if (!retValue)
        {
            Console.WriteLine("Err_007!!! Verifying random ParityReplace with ReadLine() FAILED");
        }

        return retValue;
    }


    private bool ReadTo_str_RNDParityReplace()
    {
        bool retValue = true;

        Console.WriteLine("Verifying random ParityReplace with ReadTo(string)");
        retValue &= VerifyParityReplaceByte(new ReadMethodDelegate(ReadTo), true);

        if (!retValue)
        {
            Console.WriteLine("Err_008!!! Verifying random ParityReplace with ReadTo(string) FAILED");
        }

        return retValue;
    }

    public bool ParityReplace_After_Parity()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying setting ParityReplace after Parity has been set");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com2.Open();

        com1.Parity = Parity.Even;
        com1.ParityReplace = 1;

        serPortProp.SetProperty("Parity", Parity.Even);
        serPortProp.SetProperty("ParityReplace", (byte)1);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParityReplaceByte(com1, com2, new ReadMethodDelegate(Read_byte_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_04748ajoied!!! Verifying setting ParityReplace after Parity has been set FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool ParityReplace_After_ParityReplace()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying setting ParityReplace after ParityReplace has aready been set");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com2.Open();

        com1.ParityReplace = 1;
        com1.ParityReplace = 2;
        com1.Parity = Parity.Odd;

        serPortProp.SetProperty("Parity", Parity.Odd);
        serPortProp.SetProperty("ParityReplace", (byte)2);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParityReplaceByte(com1, com2, new ReadMethodDelegate(Read_byte_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_51848ajhied!!! Verifying setting ParityReplace after ParityReplace has aready been set FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool ParityReplace_After_ParityReplaceAndParity()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();
        bool retValue = true;

        Console.WriteLine("Verifying setting ParityReplace after ParityReplace and Parity have aready been set");

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com1.Open();
        com2.Open();

        com1.Parity = Parity.Mark;
        com1.ParityReplace = 1;
        com1.ParityReplace = 2;

        serPortProp.SetProperty("Parity", Parity.Mark);
        serPortProp.SetProperty("ParityReplace", (byte)2);

        retValue &= serPortProp.VerifyPropertiesAndPrint(com1);
        retValue &= VerifyParityReplaceByte(com1, com2, new ReadMethodDelegate(Read_byte_int_int), false);

        if (!retValue)
        {
            Console.WriteLine("Err_50848ajoied!!! Verifying setting ParityReplace after ParityReplace and Parity have aready been set FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    #endregion

    #region Verification for Test Cases


    private bool VerifyParityReplaceByte(ReadMethodDelegate readMethod, bool newLine)
    {
        Random rndGen = new Random();

        return VerifyParityReplaceByte(rndGen.Next(1, 128), readMethod, newLine);
    }


    private bool VerifyParityReplaceByte(int parityReplace, ReadMethodDelegate readMethod, bool newLine)
    {
        bool retValue = true;

        retValue &= VerifyParityReplaceByteBeforeOpen(parityReplace, readMethod, newLine);
        retValue &= VerifyParityReplaceByteAfterOpen(parityReplace, readMethod, newLine);

        return retValue;
    }


    private bool VerifyParityReplaceByteBeforeOpen(int parityReplace, ReadMethodDelegate readMethod, bool newLine)
    {
        bool retValue = true;
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        com1.ParityReplace = (byte)parityReplace;
        com1.Open();
        com2.Open();

        retValue &= VerifyParityReplaceByte(com1, com2, readMethod, newLine);

        com1.Close();
        com2.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_2509pqzhz Verifying setting ParityReplaceByte BEFORE calling Open failed ParityReplace={0}", parityReplace);
        }

        return retValue;
    }


    private bool VerifyParityReplaceByteAfterOpen(int parityReplace, ReadMethodDelegate readMethod, bool newLine)
    {
        bool retValue = true;
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        com1.Open();
        com2.Open();

        com1.ParityReplace = (byte)parityReplace;
        retValue &= VerifyParityReplaceByte(com1, com2, readMethod, newLine);

        com1.Close();
        com2.Close();

        if (!retValue)
        {
            Console.WriteLine("Err_0825qnza Verifying setting ParityReplaceByte AFTER calling Open failed ParityReplace={0}", parityReplace);
        }

        return retValue;
    }


    private bool VerifyParityReplaceByte(SerialPort com1, SerialPort com2, ReadMethodDelegate readMethod, bool newLine)
    {
        byte[] bytesToWrite = new byte[s_numRndBytesPairty];
        char[] expectedChars = new char[s_numRndBytesPairty];
        Random rndGen = new Random();
        int parityErrorIndex = rndGen.Next(0, s_numRndBytesPairty - 1);
        byte newLineByte = (byte)com1.NewLine[0];

        com1.Parity = Parity.Space;
        com1.DataBits = 7;
        com1.ReadTimeout = 500;

        //Genrate random characters without an parity error
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            byte randByte;

            do
            {
                randByte = (byte)rndGen.Next(0, 128);
            } while (randByte == newLineByte);

            bytesToWrite[i] = randByte;
            expectedChars[i] = (char)randByte;
        }

        bytesToWrite[parityErrorIndex] |= (byte)0x80;
        expectedChars[parityErrorIndex] = (char)com1.ParityReplace;

        return VerifyRead(com1, com2, bytesToWrite, expectedChars, readMethod, newLine);
    }


    private bool VerifyRead(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars, ReadMethodDelegate readMethod, bool newLine)
    {
        bool retValue = true;
        char[] actualChars;

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);

        if (newLine)
        {
            com2.Write(com1.NewLine);
            while (bytesToWrite.Length + com1.NewLine.Length > com1.BytesToRead) ;
        }
        else
        {
            while (bytesToWrite.Length > com1.BytesToRead) ;
        }

        actualChars = readMethod(com1);

        //Compare the chars that were written with the ones we expected to read
        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (actualChars.Length <= i)
            {
                System.Console.WriteLine("ERROR!!!: Expected more characters then were actually read");
                retValue = false;
                break;
            }
            else if (expectedChars[i] != actualChars[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1} at {2}", (int)expectedChars[i], (int)actualChars[i], i);
                retValue = false;
            }
        }

        if (actualChars.Length > expectedChars.Length)
        {
            System.Console.WriteLine("ERROR!!!: Read in more characters then expected");
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

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


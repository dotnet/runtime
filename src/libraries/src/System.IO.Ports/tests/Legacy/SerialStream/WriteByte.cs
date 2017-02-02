// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class WriteByte
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/17 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.WriteByte()";
    public static readonly String s_strTFName = "Write_byte_int_int.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The large number of times the write method is called when verifying write
    public static readonly int LARGE_NUM_WRITES = 2048;

    //The default number of times the write method is called when verifying write
    public static readonly int DEFAULT_NUM_WRITES = 8;

    //Delegate to start asynchronous write on the SerialPort com with string of size strSize
    public delegate void AsyncWriteDelegate(SerialPort com, int strSize);

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        WriteByte objTest = new WriteByte();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding), TCSupport.SerialPortRequirements.NullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UnicodeEncoding), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeBuffer), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBreak), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ASCIIEncoding()
    {
        if (!VerifyWrite(new System.Text.ASCIIEncoding(), DEFAULT_NUM_WRITES))
        {
            Console.WriteLine("Err_001!!! Verifying write method with count=buffer.length and ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        if (!VerifyWrite(new System.Text.UTF7Encoding(), DEFAULT_NUM_WRITES))
        {
            Console.WriteLine("Err_002!!! Verifying write method with count=buffer.length and UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        if (!VerifyWrite(new System.Text.UTF8Encoding(), DEFAULT_NUM_WRITES))
        {
            Console.WriteLine("Err_003!!! Verifying write method with count=buffer.length and UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        if (!VerifyWrite(new System.Text.UTF32Encoding(), DEFAULT_NUM_WRITES))
        {
            Console.WriteLine("Err_004!!! Verifying write method with count=buffer.length and UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool UnicodeEncoding()
    {
        if (!VerifyWrite(new System.Text.UnicodeEncoding(), DEFAULT_NUM_WRITES))
        {
            Console.WriteLine("Err_005!!! Verifying write method with count=buffer.length and UnicodeEncoding");
            return false;
        }

        return true;
    }


    public bool LargeBuffer()
    {
        if (!VerifyWrite(new System.Text.ASCIIEncoding(), LARGE_NUM_WRITES))
        {
            Console.WriteLine("Err_006!!! Verifying write method with large input buffer");
            return false;
        }

        return true;
    }

    public bool InBreak()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying WriteByte throws InvalidOperationException while in a Break");
        com1.Open();
        com1.BreakState = true;

        try
        {
            com1.BaseStream.WriteByte(1);
            retValue = false;
            Console.WriteLine("Err_2892ahei Expected BeginWrite to throw InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
        }

        if (!retValue)
            Console.WriteLine("Err_051848ajeoid Verifying WriteByte throws InvalidOperationException while in a Break FAILED");

        com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyWrite(System.Text.Encoding encoding, int numWrites)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        byte[] buffer = new byte[numWrites];

        Console.WriteLine("Verifying calling write method {0} timees with endocing={1}", numWrites, encoding.EncodingName);

        com1.Encoding = encoding;
        com2.Encoding = encoding;

        com1.Open();
        com2.Open();

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)rndGen.Next(0, 256);
        }

        return VerifyWriteByteArray(com1, com2, buffer);
    }


    public bool VerifyWriteByteArray(SerialPort com1, SerialPort com2, byte[] buffer)
    {
        bool retValue = true;
        int byteRead;
        int index = 0;
        byte[] actualBytes = new byte[buffer.Length];

        for (int i = 0; i < buffer.Length; i++)
        {
            com1.BaseStream.WriteByte(buffer[i]);
        }

        com2.ReadTimeout = 500;
        System.Threading.Thread.Sleep((int)(((buffer.Length * 10.0) / com1.BaudRate) * 1000) + 250);

        while (true)
        {
            try
            {
                byteRead = com2.ReadByte();
            }
            catch (TimeoutException)
            {
                break;
            }

            if (buffer.Length <= index)
            {
                //If we have read in more bytes then we expect
                Console.WriteLine("ERROR!!!: We have received more bytes then were sent");
                retValue = false;
                break;
            }

            actualBytes[index] = (byte)byteRead;
            index++;
            if (buffer.Length - index != com2.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", buffer.Length - index, com2.BytesToRead);
                retValue = false;
            }
        }

        //Compare the bytes that were read with the ones we expected to read
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != actualBytes[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read byte {0}  actual read {1} at {2}", (int)buffer[i], (int)actualBytes[i], i);
                retValue = false;
            }
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion
}

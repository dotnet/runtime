// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class ReadByte
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerailStream.ReadByte()";
    public static readonly String s_strTFName = "ReadByte.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Set bounds fore random timeout values.
    //If the min is to low read will not timeout accurately and the testcase will fail
    public static int minRandomTimeout = 100;

    //If the max is to large then the testcase will take forever to run
    public static int maxRandomTimeout = 2000;

    //The number of random bytes to receive
    public static int numRndByte = 8;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadByte objTest = new ReadByte();
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

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ASCIIEncoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with ASCIIEncoding");
        if (!VerifyRead(new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_001!!! Verifying read with bytes encoded with ASCIIEncoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with UTF7Encoding");
        if (!VerifyRead(new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_002!!! Verifying read with bytes encoded with UTF7Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with UTF8Encoding");
        if (!VerifyRead(new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_003!!! Verifying read with bytes encoded with UTF8Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        Console.WriteLine("Verifying read with bytes encoded with UTF32Encoding");
        if (!VerifyRead(new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_004!!! Verifying read with bytes encoded with UTF32Encoding FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyRead(System.Text.Encoding encoding)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        bool retValue = true;
        int bufferSize = numRndByte;
        byte[] byteXmitBuffer = new byte[bufferSize]; ;

        byte[] byteRcvBuffer = new byte[bufferSize];
        int readInt;
        int i;

        //Genrate random bytes
        for (i = 0; i < byteXmitBuffer.Length; i++)
        {
            byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
        }

        com1.ReadTimeout = 500;
        com1.Encoding = encoding;

        com1.Open();
        com2.Open();

        com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);
        System.Threading.Thread.Sleep((int)(((byteXmitBuffer.Length * 10.0) / com1.BaudRate) * 1000));

        i = 0;

        while (true)
        {
            try
            {
                readInt = com1.BaseStream.ReadByte();
            }
            catch (TimeoutException)
            {
                break;
            }

            //While their are more bytes to be read
            if (byteXmitBuffer.Length <= i)
            {
                //If we have read in more bytes then were actually sent
                Console.WriteLine("ERROR!!!: We have received more bytes then were sent");
                retValue = false;
                break;
            }

            byteRcvBuffer[i] = (byte)readInt;
            if (readInt != byteXmitBuffer[i])
            {
                //If the byte read is not the expected byte
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read byte {1}", (int)byteXmitBuffer[i], readInt);
                retValue = false;
            }

            i++;
            if (byteXmitBuffer.Length - i != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", byteXmitBuffer.Length - i, com1.BytesToRead);
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

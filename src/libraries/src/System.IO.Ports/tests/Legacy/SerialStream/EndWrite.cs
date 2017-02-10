// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class EndWrite
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.EndWrite(IAsyncResult)";
    public static readonly String s_strTFName = "EndWrite.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The number of random bytes to receive for read method testing
    public static readonly int numRndBytesToRead = 16;

    //The number of random bytes to receive for large input buffer testing
    public static readonly int largeNumRndBytesToRead = 2048;

    //When we test Read and do not care about actually reading anything we must still
    //create an byte array to pass into the method the following is the size of the 
    //byte array used in this situation
    public static readonly int defaultByteArraySize = 1;
    public static readonly int defaultByteOffset = 0;
    public static readonly int defaultByteCount = 1;

    //The maximum buffer size when a exception occurs
    public static readonly int maxBufferSizeForException = 255;

    //The maximum buffer size when a exception is not expected
    public static readonly int maxBufferSize = 8;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        EndWrite objTest = new EndWrite();
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

        //See BeginRead.cs for further testing of EndWrite
        retValue &= tcSupport.BeginTestcase(new TestDelegate(EndWriteAfterClose), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(EndWriteAfterSerialStreamClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_Null), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_MultipleSameResult), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_MultipleInOrder),
            TCSupport.SerialPortRequirements.OneSerialPort, TCSupport.OperatingSystemRequirements.NotWin9X);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_MultipleOutOfOrder),
            TCSupport.SerialPortRequirements.OneSerialPort, TCSupport.OperatingSystemRequirements.NotWin9X);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBreak), TCSupport.SerialPortRequirements.OneSerialPort);
        // Run this scenario last in case the port isn't closed cleanly - see Dev10 #591344
        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_ReadResult), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool EndWriteAfterClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        IAsyncResult asyncResult;
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying EndWrite method throws exception after a call to Close()");

        com.Open();
        serialStream = com.BaseStream;
        asyncResult = com.BaseStream.BeginWrite(new Byte[8], 0, 8, null, null);

        com.Close();

        if (!VerifyEndWriteException(serialStream, asyncResult, null))
        {
            Console.WriteLine("Err_003!!! Verifying EndWrite method throws exception after a call to Close() FAILED");
            return false;
        }

        return true;
    }


    public bool EndWriteAfterSerialStreamClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        IAsyncResult asyncResult;
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying EndWrite method throws exception after a call to BaseStream.Close()");

        com.Open();
        serialStream = com.BaseStream;
        asyncResult = com.BaseStream.BeginWrite(new Byte[8], 0, 8, null, null);

        serialStream.Close();

        if (!VerifyEndWriteException(serialStream, asyncResult, null))
        {
            Console.WriteLine("Err_004!!! Verifying EndWrite method throws exception after a call to BaseStream.Close() FAILED");
            return false;
        }

        com.Close();
        System.Threading.Thread.Sleep(200);  // Give the port time to finish closing since we have an unclosed BeginWrite - see  - see Dev10 #591344
        return true;
    }


    public bool AsyncResult_Null()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying EndWrite with null asyncResult");

        com.Open();
        retValue &= VerifyEndWriteException(com.BaseStream, null, typeof(ArgumentNullException));

        if (!retValue)
            Console.WriteLine("Err_001!!! Verifying EndWrite with null asyncResult FAILED");

        com.Close();
        return retValue;
    }


    public bool AsyncResult_ReadResult()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        IAsyncResult writeAsyncResult;

        Console.WriteLine("Verifying EndWrite with asyncResult returned from read");

        com.Open();


        com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);
        com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);


        writeAsyncResult = com.BaseStream.BeginRead(new byte[8], 0, 8, null, null);
        retValue &= VerifyEndWriteException(com.BaseStream, writeAsyncResult, typeof(ArgumentException));

        if (!retValue)
            Console.WriteLine("Err_002!!! Verifying EndWrite with asyncResult returned from read FAILED");

        com.Close();
        //Not needed if this scenario is run last.
        //System.Threading.Thread.Sleep(200);  // Give the port time to finish closing since we have an unclosed BeginWrite
        return retValue;
    }


    public bool AsyncResult_MultipleSameResult()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        int numBytesToWrite = 8;
        bool retValue = true;
        IAsyncResult writeAsyncResult;

        Console.WriteLine("Verifying calling EndWrite twice with the same asyncResult");

        com.Open();

        writeAsyncResult = com.BaseStream.BeginWrite(new byte[numBytesToWrite], 0, numBytesToWrite, null, null);
        com.BaseStream.EndWrite(writeAsyncResult);

        retValue &= VerifyEndWriteException(com.BaseStream, writeAsyncResult, typeof(ArgumentException));

        if (!retValue)
            Console.WriteLine("Err_004!!! Verifying calling EndWrite twice with the same asyncResult FAILED");

        com.Close();
        return retValue;
    }


    public bool AsyncResult_MultipleInOrder()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        IAsyncResult readAsyncResult1, readAsyncResult2, readAsyncResult3;
        int numBytesToWrite1 = 8, numBytesToWrite2 = 16, numBytesToWrite3 = 10;

        Console.WriteLine("Verifying EndWrite with multiple calls to BeginRead");

        com.Open();

        readAsyncResult1 = com.BaseStream.BeginWrite(new byte[numBytesToWrite1], 0, numBytesToWrite1, null, null);
        readAsyncResult2 = com.BaseStream.BeginWrite(new byte[numBytesToWrite2], 0, numBytesToWrite2, null, null);
        readAsyncResult3 = com.BaseStream.BeginWrite(new byte[numBytesToWrite3], 0, numBytesToWrite3, null, null);

        com.BaseStream.EndWrite(readAsyncResult1);
        com.BaseStream.EndWrite(readAsyncResult2);
        com.BaseStream.EndWrite(readAsyncResult3);

        if (!retValue)
            Console.WriteLine("Err_005!!! Verifying EndWrite with multiple calls to BeginRead FAILED");

        com.Close();
        return retValue;
    }


    public bool AsyncResult_MultipleOutOfOrder()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        IAsyncResult readAsyncResult1, readAsyncResult2, readAsyncResult3;
        int numBytesToWrite1 = 8, numBytesToWrite2 = 16, numBytesToWrite3 = 10;

        Console.WriteLine("Verifying calling EndWrite with different asyncResults out of order returned from BeginRead");

        com.Open();

        readAsyncResult1 = com.BaseStream.BeginWrite(new byte[numBytesToWrite1], 0, numBytesToWrite1, null, null);
        readAsyncResult2 = com.BaseStream.BeginWrite(new byte[numBytesToWrite2], 0, numBytesToWrite2, null, null);
        readAsyncResult3 = com.BaseStream.BeginWrite(new byte[numBytesToWrite3], 0, numBytesToWrite3, null, null);

        com.BaseStream.EndWrite(readAsyncResult2);
        com.BaseStream.EndWrite(readAsyncResult3);
        com.BaseStream.EndWrite(readAsyncResult1);

        if (!retValue)
            Console.WriteLine("Err_006!!! Verifying calling EndWrite with different asyncResults out of order returned from BeginRead FAILED");

        com.Close();
        return retValue;
    }

    public bool InBreak()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        IAsyncResult readAsyncResult;
        bool retValue = true;

        Console.WriteLine("Verifying EndWrite throws InvalidOperationException while in a Break");
        com1.Open();

        readAsyncResult = com1.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);
        com1.BreakState = true;

        try
        {
            com1.BaseStream.EndWrite(readAsyncResult);
            retValue = false;
            Console.WriteLine("Err_2892ahei Expected BeginWrite to throw InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
        }

        if (!retValue)
            Console.WriteLine("Err_051848ajeoid Verifying EndWrite throws InvalidOperationException while in a Break FAILED");

        com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyEndWriteException(System.IO.Stream serialStream, IAsyncResult asyncResult, Type expectedException)
    {
        bool retValue = true;

        try
        {
            serialStream.EndWrite(asyncResult);

            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!!: No Excpetion was thrown");
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!!: Expected no exception to be thrown and {0} was thrown", e);
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!!:  Expected {0} to be thrown and the following exception was thrown:\n{1}", expectedException, e);
                retValue = false;
            }
        }
        return retValue;
    }
    #endregion
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class EndRead
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.EndRead(IAsyncResult)";
    public static readonly String s_strTFName = "EndRead.cs";
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
        EndRead objTest = new EndRead();
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

        //See BeginRead.cs for further testing of EndRead
        retValue &= tcSupport.BeginTestcase(new TestDelegate(EndReadAfterClose), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(EndReadAfterSerialStreamClose), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_Null), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_WriteResult), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_MultipleSameResult), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_MultipleInOrder),
            TCSupport.SerialPortRequirements.NullModem, TCSupport.OperatingSystemRequirements.NotWin9X);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(AsyncResult_MultipleOutOfOrder),
            TCSupport.SerialPortRequirements.NullModem, TCSupport.OperatingSystemRequirements.NotWin9X);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool EndReadAfterClose()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        IAsyncResult asyncResult;
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying EndRead method throws exception after a call to Cloes()");

        com1.Open();
        com2.Open();

        serialStream = com1.BaseStream;
        asyncResult = com1.BaseStream.BeginRead(new Byte[8], 0, 8, null, null);

        com2.Write(new byte[16], 0, 16);
        while (com1.BytesToRead == 0)
        {
            System.Threading.Thread.Sleep(50);
        }


        com1.Close();

        if (!VerifyEndReadException(serialStream, asyncResult, null))
        {
            Console.WriteLine("Err_003!!! Verifying EndRead method throws exception after a call to Cloes() FAILED");
            return false;
        }

        com2.Close();

        return true;
    }


    public bool EndReadAfterSerialStreamClose()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        IAsyncResult asyncResult;
        System.IO.Stream serialStream;

        Console.WriteLine("Verifying EndRead method throws exception after a call to BaseStream.Close()");

        com1.Open();
        com2.Open();

        serialStream = com1.BaseStream;
        asyncResult = com1.BaseStream.BeginRead(new Byte[8], 0, 8, null, null);

        com2.Write(new byte[16], 0, 16);
        while (com1.BytesToRead == 0)
        {
            System.Threading.Thread.Sleep(50);
        }

        com1.BaseStream.Close();

        if (!VerifyEndReadException(serialStream, asyncResult, null))
        {
            Console.WriteLine("Err_004!!! Verifying EndRead method throws exception after a call to BaseStream.Close() FAILED");
            return false;
        }

        com2.Close();

        return true;
    }


    public bool AsyncResult_Null()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying EndRead with null asyncResult");

        com.Open();
        retValue &= VerifyEndReadException(com.BaseStream, null, typeof(ArgumentNullException));

        if (!retValue)
            Console.WriteLine("Err_001!!! Verifying EndRead with null asyncResult FAILED");

        com.Close();

        return retValue;
    }


    public bool AsyncResult_WriteResult()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        IAsyncResult writeAsyncResult;

        Console.WriteLine("Verifying EndRead with asyncResult returned from write");
        com.Open();

        com.BaseStream.BeginRead(new byte[8], 0, 8, null, null);

        writeAsyncResult = com.BaseStream.BeginWrite(new byte[8], 0, 8, null, null);
        retValue &= VerifyEndReadException(com.BaseStream, writeAsyncResult, typeof(ArgumentException));

        if (!retValue)
            Console.WriteLine("Err_002!!! Verifying EndRead with asyncResult returned from write FAILED");

        com.Close();

        return retValue;
    }


    public bool AsyncResult_MultipleSameResult()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        int numBytesToRead = 8;
        bool retValue = true;
        IAsyncResult readAsyncResult;
        int endReadReturnValue;

        Console.WriteLine("Verifying calling EndRead twice with the same asyncResult");

        com1.Open();
        com2.Open();

        com2.Write(new byte[numBytesToRead], 0, numBytesToRead);

        readAsyncResult = com1.BaseStream.BeginRead(new byte[numBytesToRead], 0, numBytesToRead, null, null);
        endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult);

        retValue &= VerifyEndReadException(com1.BaseStream, readAsyncResult, typeof(ArgumentException));

        if (!retValue)
            Console.WriteLine("Err_004!!! Verifying calling EndRead twice with the same asyncResult FAILED");

        com1.Close();
        com2.Close();

        return retValue;
    }


    public bool AsyncResult_MultipleInOrder()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        int endReadReturnValue;
        IAsyncResult readAsyncResult1, readAsyncResult2, readAsyncResult3;
        int numBytesToRead1 = 8, numBytesToRead2 = 16, numBytesToRead3 = 10;
        int totalBytesToRead = numBytesToRead1 + numBytesToRead2 + numBytesToRead3;

        Console.WriteLine("Verifying EndRead with multiple calls to BeginRead");

        com1.Open();
        com2.Open();

        com2.Write(new byte[totalBytesToRead], 0, totalBytesToRead);

        while (totalBytesToRead > com1.BytesToRead)
            System.Threading.Thread.Sleep(50);

        readAsyncResult1 = com1.BaseStream.BeginRead(new byte[numBytesToRead1], 0, numBytesToRead1, null, null);
        readAsyncResult2 = com1.BaseStream.BeginRead(new byte[numBytesToRead2], 0, numBytesToRead2, null, null);
        readAsyncResult3 = com1.BaseStream.BeginRead(new byte[numBytesToRead3], 0, numBytesToRead3, null, null);

        if (numBytesToRead1 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult1)))
        {
            Console.WriteLine("ERROR!!! Expected EndRead to return={0} actual={1} for first read", numBytesToRead1, endReadReturnValue);
            retValue = false;
        }

        if (numBytesToRead2 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult2)))
        {
            Console.WriteLine("ERROR!!! Expected EndRead to return={0} actual={1} for second read", numBytesToRead2, endReadReturnValue);
            retValue = false;
        }

        if (numBytesToRead3 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult3)))
        {
            Console.WriteLine("ERROR!!! Expected EndRead to return={0} actual={1} for third read", numBytesToRead3, endReadReturnValue);
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_005!!! Verifying EndRead with multiple calls to BeginRead FAILED");

        com1.Close();
        com2.Close();

        return retValue;
    }


    public bool AsyncResult_MultipleOutOfOrder()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        bool retValue = true;
        int endReadReturnValue;
        IAsyncResult readAsyncResult1, readAsyncResult2, readAsyncResult3;
        int numBytesToRead1 = 8, numBytesToRead2 = 16, numBytesToRead3 = 10;
        int totalBytesToRead = numBytesToRead1 + numBytesToRead2 + numBytesToRead3;

        Console.WriteLine("Verifying calling EndRead with different asyncResults out of order returned from BeginRead");

        com1.Open();
        com2.Open();

        com2.Write(new byte[totalBytesToRead], 0, totalBytesToRead);

        while (totalBytesToRead > com1.BytesToRead)
            System.Threading.Thread.Sleep(50);

        readAsyncResult1 = com1.BaseStream.BeginRead(new byte[numBytesToRead1], 0, numBytesToRead1, null, null);
        readAsyncResult2 = com1.BaseStream.BeginRead(new byte[numBytesToRead2], 0, numBytesToRead2, null, null);
        readAsyncResult3 = com1.BaseStream.BeginRead(new byte[numBytesToRead3], 0, numBytesToRead3, null, null);

        if (numBytesToRead2 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult2)))
        {
            Console.WriteLine("ERROR!!! Expected EndRead to return={0} actual={1} for second read", numBytesToRead2, endReadReturnValue);
            retValue = false;
        }

        if (numBytesToRead3 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult3)))
        {
            Console.WriteLine("ERROR!!! Expected EndRead to return={0} actual={1} for third read", numBytesToRead3, endReadReturnValue);
            retValue = false;
        }

        if (numBytesToRead1 != (endReadReturnValue = com1.BaseStream.EndRead(readAsyncResult1)))
        {
            Console.WriteLine("ERROR!!! Expected EndRead to return={0} actual={1} for first read", numBytesToRead1, endReadReturnValue);
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_006!!! Verifying calling EndRead with different asyncResults out of order returned from BeginRead FAILED");

        com1.Close();
        com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyEndReadException(System.IO.Stream serialStream, IAsyncResult asyncResult, Type expectedException)
    {
        bool retValue = true;

        try
        {
            serialStream.EndRead(asyncResult);

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

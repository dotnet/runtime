// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class Read_byte_int_int_Generic
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.Read(byte[], int, int,)";
    public static readonly String s_strTFName = "Read_byte_int_int.cs";
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
        Read_byte_int_int_Generic objTest = new Read_byte_int_int_Generic();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Buffer_Null), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_NEG1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_NEGRND), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_MinInt), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_NEG1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_NEGRND), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_MinInt), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OffsetCount_EQ_Length_Plus_1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(OffsetCount_GT_Length), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_GT_Length), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_GT_Length), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(OffsetCount_EQ_Length), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Offset_EQ_Length_Minus_1), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_EQ_Length), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeInputBuffer), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Buffer_Null()
    {
        if (!VerifyReadException(null, 0, 1, typeof(System.ArgumentNullException)))
        {
            Console.WriteLine("Err_001!!! Verifying read method throws exception with buffer equal to null FAILED");
            return false;
        }

        return true;
    }


    public bool Offset_NEG1()
    {
        if (!VerifyReadException(new byte[defaultByteArraySize], -1, defaultByteCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_002!!! Verifying read method throws exception with offset equal to -1");
            return false;
        }

        return true;
    }


    public bool Offset_NEGRND()
    {
        Random rndGen = new Random(-55);

        if (!VerifyReadException(new byte[defaultByteArraySize], rndGen.Next(Int32.MinValue, 0), defaultByteCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_003!!! Verifying read method throws exception with offset equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Offset_MinInt()
    {
        if (!VerifyReadException(new byte[defaultByteArraySize], Int32.MinValue, defaultByteCount, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_004!!! Verifying read method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool Count_NEG1()
    {
        if (!VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, -1, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_005!!! Verifying read method throws exception with count equal to -1");
            return false;
        }

        return true;
    }


    public bool Count_NEGRND()
    {
        Random rndGen = new Random(-55);

        if (!VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, rndGen.Next(Int32.MinValue, 0), typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_006!!! Verifying read method throws exception with count equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Count_MinInt()
    {
        if (!VerifyReadException(new byte[defaultByteArraySize], defaultByteOffset, Int32.MinValue, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_007!!! Verifying read method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length_Plus_1()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(0, bufferLength);
        int count = bufferLength + 1 - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_009!!! Verifying read method throws exception with offset+count=buffer.length+1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(0, bufferLength);
        int count = rndGen.Next(bufferLength + 1 - offset, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_010!!! Verifying read method throws exception with offset+count>buffer.length+1");
            return false;
        }

        return true;
    }


    public bool Offset_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = rndGen.Next(bufferLength, Int32.MaxValue);
        int count = defaultByteCount;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_011!!! Verifying read method throws exception with offset>buffer.length");
            return false;
        }

        return true;
    }


    public bool Count_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSizeForException);
        int offset = defaultByteOffset;
        int count = rndGen.Next(bufferLength + 1, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyReadException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_012!!! Verifying read method throws exception with count>buffer.length + 1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = bufferLength - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_013!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool Offset_EQ_Length_Minus_1()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = bufferLength - 1;
        int count = 1;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_014!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool Count_EQ_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, maxBufferSize);
        int offset = 0;
        int count = bufferLength;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_015!!! Verifying read method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool LargeInputBuffer()
    {
        Random rndGen = new Random(-55);
        int bufferLength = largeNumRndBytesToRead;
        int offset = 0;
        int count = bufferLength;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyRead(new byte[bufferLength], offset, count, largeNumRndBytesToRead))
        {
            Console.WriteLine("Err_016!!! Verifying read method with large input buffer");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyReadException(byte[] buffer, int offset, int count, Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        int bufferLength = null == buffer ? 0 : buffer.Length;

        Console.WriteLine("Verifying read method throws {0} buffer.Lenght={1}, offset={2}, count={3}", expectedException, bufferLength, offset, count);
        com.Open();

        try
        {
            com.BaseStream.Read(buffer, offset, count);

            Console.WriteLine("ERROR!!!: No Excpetion was thrown");
            retValue = false;
        }
        catch (System.Exception e)
        {
            if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!!: {0} exception was thrown expected {1}", e.GetType(), expectedException);
                retValue = false;
            }
        }
        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool VerifyRead(byte[] buffer, int offset, int count)
    {
        return VerifyRead(buffer, offset, count, numRndBytesToRead);
    }


    public bool VerifyRead(byte[] buffer, int offset, int count, int numberOfBytesToRead)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        byte[] bytesToWrite = new byte[numberOfBytesToRead];

        //Genrate random bytes
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 256);

            bytesToWrite[i] = randByte;
        }

        //Genrate some random bytes in the buffer
        for (int i = 0; i < buffer.Length; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 256);

            buffer[i] = randByte;
        }

        Console.WriteLine("Verifying read method buffer.Lenght={0}, offset={1}, count={2} with {3} random chars", buffer.Length, offset, count, bytesToWrite.Length);

        com1.ReadTimeout = 500;
        com1.Open();
        com2.Open();

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, buffer, offset, count);
    }


    private bool VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] rcvBuffer, int offset, int count)
    {
        bool retValue = true;
        byte[] buffer = new byte[bytesToWrite.Length];
        int bytesRead, totalBytesRead;
        int bytesToRead;
        byte[] oldRcvBuffer = (byte[])rcvBuffer.Clone();

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 500;
        System.Threading.Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

        totalBytesRead = 0;
        bytesToRead = com1.BytesToRead;

        while (true)
        {
            try
            {
                bytesRead = com1.BaseStream.Read(rcvBuffer, offset, count);
            }
            catch (TimeoutException)
            {
                break;
            }

            //While their are more characters to be read
            if ((bytesToRead > bytesRead && count != bytesRead) || (bytesToRead <= bytesRead && bytesRead != bytesToRead))
            {
                //If we have not read all of the characters that we should have
                Console.WriteLine("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                retValue = false;
            }

            if (bytesToWrite.Length < totalBytesRead + bytesRead)
            {
                //If we have read in more characters then we expect
                Console.WriteLine("ERROR!!!: We have received more characters then were sent");
                retValue = false;
                break;
            }

            if (!VerifyBuffer(rcvBuffer, oldRcvBuffer, offset, bytesRead))
                retValue = false;

            System.Array.Copy(rcvBuffer, offset, buffer, totalBytesRead, bytesRead);
            totalBytesRead += bytesRead;

            if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                retValue = false;
            }

            oldRcvBuffer = (byte[])rcvBuffer.Clone();
            bytesToRead = com1.BytesToRead;
        }

        //Compare the bytes that were written with the ones we read
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            if (bytesToWrite[i] != buffer[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1}", bytesToWrite[i], buffer[i]);
                retValue = false;
            }
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private bool VerifyBuffer(byte[] actualBuffer, byte[] expectedBuffer, int offset, int count)
    {
        bool retValue = true;

        //Verify all character before the offset
        for (int i = 0; i < offset; i++)
        {
            if (actualBuffer[i] != expectedBuffer[i])
            {
                Console.WriteLine("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                retValue = false;
            }
        }

        //Verify all character after the offset + count
        for (int i = offset + count; i < actualBuffer.Length; i++)
        {
            if (actualBuffer[i] != expectedBuffer[i])
            {
                Console.WriteLine("ERROR!!!: Expected {0} in buffer at {1} actual {2}", (int)expectedBuffer[i], i, (int)actualBuffer[i]);
                retValue = false;
            }
        }

        return retValue;
    }
    #endregion
}

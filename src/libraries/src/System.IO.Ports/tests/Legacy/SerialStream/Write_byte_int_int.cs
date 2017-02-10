// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class Write_byte_int_int
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/17 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialStream.Write(byte[], int, int)";
    public static readonly String s_strTFName = "Write_byte_int_int.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The string size used when veryifying encoding 
    public static readonly int ENCODING_BUFFER_SIZE = 4;

    //The string size used for large byte array testing
    public static readonly int LARGE_BUFFER_SIZE = 2048;

    //When we test Write and do not care about actually writing anything we must still
    //create an byte array to pass into the method the following is the size of the 
    //byte array used in this situation
    public static readonly int DEFAULT_BUFFER_SIZE = 1;
    public static readonly int DEFAULT_BUFFER_OFFSET = 0;
    public static readonly int DEFAULT_BUFFER_COUNT = 1;

    //The maximum buffer size when a exception occurs
    public static readonly int MAX_BUFFER_SIZE_FOR_EXCEPTION = 255;

    //The maximum buffer size when a exception is not expected
    public static readonly int MAX_BUFFER_SIZE = 8;

    //The default number of times the write method is called when verifying write
    public static readonly int DEFAULT_NUM_WRITES = 3;

    //Delegate to start asynchronous write on the SerialPort com with string of size strSize
    public delegate void AsyncWriteDelegate(SerialPort com, int strSize);

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Write_byte_int_int objTest = new Write_byte_int_int();
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
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding), TCSupport.SerialPortRequirements.NullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UnicodeEncoding), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeBuffer), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(InBreak), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Count_EQ_Zero), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool Buffer_Null()
    {
        if (!VerifyWriteException(null, 0, 1, typeof(System.ArgumentNullException)))
        {
            Console.WriteLine("Err_001!!! Verifying write method throws exception with buffer equal to null FAILED");
            return false;
        }

        return true;
    }


    public bool Offset_NEG1()
    {
        if (!VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], -1, DEFAULT_BUFFER_COUNT, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_002!!! Verifying write method throws exception with offset equal to -1");
            return false;
        }

        return true;
    }


    public bool Offset_NEGRND()
    {
        Random rndGen = new Random(-55);

        if (!VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], rndGen.Next(Int32.MinValue, 0), DEFAULT_BUFFER_COUNT, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_003!!! Verifying write method throws exception with offset equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Offset_MinInt()
    {
        if (!VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], Int32.MinValue, DEFAULT_BUFFER_COUNT, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_004!!! Verifying write method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool Count_NEG1()
    {
        if (!VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], DEFAULT_BUFFER_OFFSET, -1, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_005!!! Verifying write method throws exception with count equal to -1");
            return false;
        }

        return true;
    }


    public bool Count_NEGRND()
    {
        Random rndGen = new Random(-55);

        if (!VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], DEFAULT_BUFFER_OFFSET, rndGen.Next(Int32.MinValue, 0), typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_006!!! Verifying write method throws exception with count equal to negative random number");
            return false;
        }

        return true;
    }


    public bool Count_MinInt()
    {
        if (!VerifyWriteException(new byte[DEFAULT_BUFFER_SIZE], DEFAULT_BUFFER_OFFSET, Int32.MinValue, typeof(System.ArgumentOutOfRangeException)))
        {
            Console.WriteLine("Err_007!!! Verifying write method throws exception with count equal to MintInt");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length_Plus_1()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
        int offset = rndGen.Next(0, bufferLength);
        int count = bufferLength + 1 - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyWriteException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_009!!! Verifying write method throws exception with offset+count=buffer.length+1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
        int offset = rndGen.Next(0, bufferLength);
        int count = rndGen.Next(bufferLength + 1 - offset, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyWriteException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_010!!! Verifying write method throws exception with offset+count>buffer.length+1");
            return false;
        }

        return true;
    }


    public bool Offset_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
        int offset = rndGen.Next(bufferLength, Int32.MaxValue);
        int count = DEFAULT_BUFFER_COUNT;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyWriteException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_011!!! Verifying write method throws exception with offset>buffer.length");
            return false;
        }

        return true;
    }


    public bool Count_GT_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE_FOR_EXCEPTION);
        int offset = DEFAULT_BUFFER_OFFSET;
        int count = rndGen.Next(bufferLength + 1, Int32.MaxValue);
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyWriteException(new byte[bufferLength], offset, count, expectedException))
        {
            Console.WriteLine("Err_012!!! Verifying write method throws exception with count>buffer.length + 1");
            return false;
        }

        return true;
    }


    public bool OffsetCount_EQ_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = bufferLength - offset;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyWrite(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_013!!! Verifying write method with offset + count=buffer.length");
            return false;
        }

        return true;
    }


    public bool Offset_EQ_Length_Minus_1()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = bufferLength - 1;
        int count = 1;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyWrite(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_014!!! Verifying write method with offset=buffer.length - 1");
            return false;
        }

        return true;
    }


    public bool Count_EQ_Length()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = 0;
        int count = bufferLength;
        Type expectedException = typeof(System.ArgumentException);

        if (!VerifyWrite(new byte[bufferLength], offset, count))
        {
            Console.WriteLine("Err_015!!! Verifying write method with count=buffer.length");
            return false;
        }

        return true;
    }


    public bool ASCIIEncoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);

        if (!VerifyWrite(new byte[bufferLength], offset, count, new System.Text.ASCIIEncoding()))
        {
            Console.WriteLine("Err_016!!! Verifying write method with count=buffer.length and ASCIIEncoding");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);

        if (!VerifyWrite(new byte[bufferLength], offset, count, new System.Text.UTF7Encoding()))
        {
            Console.WriteLine("Err_017!!! Verifying write method with count=buffer.length and UTF7Encoding");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);

        if (!VerifyWrite(new byte[bufferLength], offset, count, new System.Text.UTF8Encoding()))
        {
            Console.WriteLine("Err_018!!! Verifying write method with count=buffer.length and UTF8Encoding");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);

        if (!VerifyWrite(new byte[bufferLength], offset, count, new System.Text.UTF32Encoding()))
        {
            Console.WriteLine("Err_019!!! Verifying write method with count=buffer.length and UTF32Encoding");
            return false;
        }

        return true;
    }


    public bool UnicodeEncoding()
    {
        Random rndGen = new Random(-55);
        int bufferLength = rndGen.Next(1, MAX_BUFFER_SIZE);
        int offset = rndGen.Next(0, bufferLength - 1);
        int count = rndGen.Next(1, bufferLength - offset);

        if (!VerifyWrite(new byte[bufferLength], offset, count, new System.Text.UnicodeEncoding()))
        {
            Console.WriteLine("Err_019!!! Verifying write method with count=buffer.length and UnicodeEncoding");
            return false;
        }

        return true;
    }


    public bool LargeBuffer()
    {
        Random rndGen = new Random(-55);
        int bufferLength = LARGE_BUFFER_SIZE;
        int offset = 0;
        int count = bufferLength;

        if (!VerifyWrite(new byte[bufferLength], offset, count, 1))
        {
            Console.WriteLine("Err_016!!! Verifying write method with large input buffer");
            return false;
        }

        return true;
    }

    public bool InBreak()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying Write throws InvalidOperationException while in a Break");
        com1.Open();
        com1.BreakState = true;

        try
        {
            com1.BaseStream.Write(new byte[8], 0, 8);
            retValue = false;
            Console.WriteLine("Err_2892ahei Expected Write to throw InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
        }

        if (!retValue)
            Console.WriteLine("Err_051848ajeoid Verifying Write throws InvalidOperationException while in a Break FAILED");

        com1.Close();

        return retValue;
    }

    public bool Count_EQ_Zero()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying Write with count=0 returns immediately");
        com1.Open();
        com1.Handshake = Handshake.RequestToSend;

        com1.BaseStream.Write(new byte[8], 0, 0);

        if (!retValue)
            Console.WriteLine("Err_051848ajeoid Verifying Write with count=0 returns immediately FAILED");

        com1.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public bool VerifyWriteException(byte[] buffer, int offset, int count, Type expectedException)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;
        int bufferLength = null == buffer ? 0 : buffer.Length;

        Console.WriteLine("Verifying write method throws {0} buffer.Lenght={1}, offset={2}, count={3}", expectedException, bufferLength, offset, count);
        com.Open();

        try
        {
            com.BaseStream.Write(buffer, offset, count);
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


    public bool VerifyWrite(byte[] buffer, int offset, int count)
    {
        return VerifyWrite(buffer, offset, count, new System.Text.ASCIIEncoding());
    }


    public bool VerifyWrite(byte[] buffer, int offset, int count, int numWrites)
    {
        return VerifyWrite(buffer, offset, count, new System.Text.ASCIIEncoding(), numWrites);
    }


    public bool VerifyWrite(byte[] buffer, int offset, int count, System.Text.Encoding encoding)
    {
        return VerifyWrite(buffer, offset, count, encoding, DEFAULT_NUM_WRITES);
    }


    public bool VerifyWrite(byte[] buffer, int offset, int count, System.Text.Encoding encoding, int numWrites)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);

        Console.WriteLine("Verifying write method buffer.Lenght={0}, offset={1}, count={2}, endocing={3}", buffer.Length, offset, count, encoding.EncodingName);

        com1.Encoding = encoding;
        com2.Encoding = encoding;

        com1.Open();
        com2.Open();

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)rndGen.Next(0, 256);
        }

        return VerifyWriteByteArray(buffer, offset, count, com1, com2, numWrites);
    }


    public bool VerifyWriteByteArray(byte[] buffer, int offset, int count, SerialPort com1, SerialPort com2)
    {
        return VerifyWriteByteArray(buffer, offset, count, com1, com2, DEFAULT_NUM_WRITES);
    }


    public bool VerifyWriteByteArray(byte[] buffer, int offset, int count, SerialPort com1, SerialPort com2, int numWrites)
    {
        bool retValue = true;
        byte[] oldBuffer, expectedBytes, actualBytes;
        int byteRead;
        int index = 0;

        oldBuffer = (byte[])buffer.Clone();
        expectedBytes = new byte[count];
        actualBytes = new byte[expectedBytes.Length * numWrites];

        for (int i = 0; i < count; i++)
        {
            expectedBytes[i] = buffer[i + offset];
        }

        for (int i = 0; i < numWrites; i++)
        {
            com1.BaseStream.Write(buffer, offset, count);
        }

        com2.ReadTimeout = 500;
        System.Threading.Thread.Sleep((int)(((expectedBytes.Length * numWrites * 10.0) / com1.BaudRate) * 1000) + 250);

        //Make sure buffer was not altered during the write call
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != oldBuffer[i])
            {
                System.Console.WriteLine("ERROR!!!: The contents of the buffer were changed from {0} to {1} at {2}", oldBuffer[i], buffer[i], i);
                retValue = false;
            }
        }

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

            if (actualBytes.Length <= index)
            {
                //If we have read in more bytes then we expect
                Console.WriteLine("ERROR!!!: We have received more bytes then were sent");
                retValue = false;
                break;
            }

            actualBytes[index] = (byte)byteRead;
            index++;
            if (actualBytes.Length - index != com2.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", actualBytes.Length - index, com2.BytesToRead);
                retValue = false;
            }
        }

        //Compare the bytes that were read with the ones we expected to read
        for (int j = 0; j < numWrites; j++)
        {
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (expectedBytes[i] != actualBytes[i + expectedBytes.Length * j])
                {
                    System.Console.WriteLine("ERROR!!!: Expected to read byte {0}  actual read {1} at {2}", (int)expectedBytes[i], (int)actualBytes[i + expectedBytes.Length * j], i);
                    retValue = false;
                }
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

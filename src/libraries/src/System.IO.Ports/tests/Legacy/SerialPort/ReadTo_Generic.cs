// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class ReadTo
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/19 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadTo(str)";
    public static readonly String s_strTFName = "ReadTo.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Set bounds fore random timeout values.
    //If the min is to low read will not timeout accurately and the testcase will fail
    public static readonly int minRandomTimeout = 250;

    //If the max is to large then the testcase will take forever to run
    public static readonly int maxRandomTimeout = 2000;

    //If the percentage difference between the expected timeout and the actual timeout
    //found through Stopwatch is greater then 10% then the timeout value was not correctly
    //to the read method and the testcase fails.
    public static readonly double maxPercentageDifference = .15;

    //The number of random bytes to receive for parity testing
    public static readonly int numRndBytesPairty = 8;

    //The number of characters to read at a time for parity testing
    public static readonly int numBytesReadPairty = 2;

    //The number of random bytes to receive for BytesToRead testing
    public static readonly int numRndBytesToRead = 16;

    //The number of new lines to insert into the string not including the one at the end
    //For BytesToRead testing
    public static readonly int DEFAULT_NUMBER_NEW_LINES = 2;
    public static readonly byte DEFAULT_NEW_LINE = (byte)'\n';

    //When we test Read and do not care about actually reading anything we must still
    //create an byte array to pass into the method the following is the size of the 
    //byte array used in this situation
    public static readonly int defaultCharArraySize = 1;
    public static readonly int NUM_TRYS = 5;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadTo objTest = new ReadTo();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadWithoutOpen), TCSupport.SerialPortRequirements.None);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadAfterFailedOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadAfterClose), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Timeout), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutNoData), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutSomeData), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(DefaultParityReplaceByte), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(NoParityReplaceByte), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RNDParityReplaceByte), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ParityErrorOnLastByte), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_RND_Buffer_Size), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_1_Buffer_Size), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(BytesToRead_Equal_Buffer_Size), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ReadWithoutOpen()
    {
        SerialPort com = new SerialPort();

        Console.WriteLine("Verifying read method throws exception without a call to Open()");
        if (!VerifyReadException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_001!!! Verifying read method throws exception without a call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool ReadAfterFailedOpen()
    {
        SerialPort com = new SerialPort("BAD_PORT_NAME");

        Console.WriteLine("Verifying read method throws exception with a failed call to Open()");

        //Since the PortName is set to a bad port name Open will thrown an exception
        //however we don't care what it is since we are verfifying a read method
        try
        {
            com.Open();
        }
        catch (System.Exception)
        {
        }
        if (!VerifyReadException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_002!!! Verifying read method throws exception with a failed call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool ReadAfterClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying read method throws exception after a call to Cloes()");
        com.Open();
        com.Close();

        if (!VerifyReadException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_003!!! Verifying read method throws exception after a call to Cloes() FAILED");
            return false;
        }

        return true;
    }


    public bool Timeout()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);

        com.ReadTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);

        Console.WriteLine("Verifying ReadTimeout={0}", com.ReadTimeout);
        com.Open();

        if (!VerifyTimeout(com))
        {
            Console.WriteLine("Err_004!!! Verifying timeout FAILED");
            return false;
        }

        return true;
    }


    public bool SuccessiveReadTimeoutNoData()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        bool retValue = true;

        com.ReadTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        //		com.Encoding = new System.Text.UTF7Encoding();
        com.Encoding = System.Text.Encoding.Unicode;

        Console.WriteLine("Verifying ReadTimeout={0} with successive call to read method and no data", com.ReadTimeout);
        com.Open();

        try
        {
            com.ReadTo(com.NewLine);
            Console.WriteLine("Err_702872ahps!!!: Read did not throw TimeoutException when it timed out");
            retValue = false;
        }
        catch (TimeoutException) { }

        retValue &= VerifyTimeout(com);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying with successive call to read method and no data FAILED");
        }

        return retValue;
    }


    public bool SuccessiveReadTimeoutSomeData()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(WriteToCom1));
        bool retValue = true;

        com1.ReadTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        com1.Encoding = new System.Text.UTF8Encoding();

        Console.WriteLine("Verifying ReadTimeout={0} with successive call to read method and some data being received in the first call", com1.ReadTimeout);
        com1.Open();

        //Call WriteToCom1 asynchronously this will write to com1 some time before the following call 
        //to a read method times out
        t.Start();

        try
        {
            com1.ReadTo(com1.NewLine);
        }
        catch (TimeoutException) { }

        //Wait for the thread to finish
        while (t.IsAlive)
            System.Threading.Thread.Sleep(50);

        //Make sure there is no bytes in the buffer so the next call to read will timeout
        com1.DiscardInBuffer();
        retValue &= VerifyTimeout(com1);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying with with successive call to read method and some data being received in the first call FAILED");
        }

        return retValue;
    }


    private void WriteToCom1()
    {
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        int sleepPeriod = rndGen.Next(minRandomTimeout, maxRandomTimeout / 2);

        //Sleep some random period with of a maximum duration of half the largest possible timeout value for a read method on COM1
        System.Threading.Thread.Sleep(sleepPeriod);
        com2.Open();
        com2.WriteLine("");
        if (com2.IsOpen)
            com2.Close();
    }


    public bool DefaultParityReplaceByte()
    {
        if (!VerifyParityReplaceByte(-1, numRndBytesPairty - 2))
        {
            Console.WriteLine("Err_007!!! Verifying default ParityReplace byte FAILED");
            return false;
        }

        return true;
    }


    public bool NoParityReplaceByte()
    {
        Random rndGen = new Random(-55);

        if (!VerifyParityReplaceByte((int)'\0', rndGen.Next(0, numRndBytesPairty - 1)))
        {
            Console.WriteLine("Err_008!!! Verifying no ParityReplace byte FAILED");
            return false;
        }

        return true;
    }


    public bool RNDParityReplaceByte()
    {
        Random rndGen = new Random(-55);

        if (!VerifyParityReplaceByte(rndGen.Next(0, 128), 0))
        {
            Console.WriteLine("Err_009!! Verifying random ParityReplace byte FAILED");
            return false;
        }

        return true;
    }


    public bool ParityErrorOnLastByte()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(15);
        byte[] bytesToWrite = new byte[numRndBytesPairty];
        char[] expectedChars = new char[numRndBytesPairty];
        char[] actualChars;
        string strRead;
        bool retValue = true;
        int waitTime;

        /* 1 Additional character gets added to the input buffer when the parity error occurs on the last byte of a stream
             We are verifying that besides this everything gets read in correctly. See NDP Whidbey: 24216 for more info on this */
        Console.WriteLine("Verifying default ParityReplace byte with a parity errro on the last byte");

        //Genrate random characters without an parity error
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 128);

            bytesToWrite[i] = randByte;
            expectedChars[i] = (char)randByte;
        }

        bytesToWrite[bytesToWrite.Length - 1] = (byte)(bytesToWrite[bytesToWrite.Length - 1] | 0x80); //Create a parity error on the last byte
        expectedChars[expectedChars.Length - 1] = (char)com1.ParityReplace; // Set the last expected char to be the ParityReplace Byte

        com1.Parity = Parity.Space;
        com1.DataBits = 7;
        com1.ReadTimeout = 250;

        com1.Open();
        com2.Open();

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com2.Write(com1.NewLine);

        waitTime = 0;

        while (bytesToWrite.Length + 2 > com1.BytesToRead && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        strRead = com1.ReadTo(com1.NewLine);
        actualChars = strRead.ToCharArray();

        //Compare the chars that were written with the ones we expected to read
        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (expectedChars[i] != actualChars[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1}", (int)expectedChars[i], (int)actualChars[i]);
                retValue = false;
            }
        }

        if (1 < com1.BytesToRead)
        {
            Console.WriteLine("ERROR!!!: Expected BytesToRead=0 actual={0}", com1.BytesToRead);
            Console.WriteLine("ByteRead={0}, {1}", com1.ReadByte(), bytesToWrite[bytesToWrite.Length - 1]);
            retValue = false;
        }

        com1.DiscardInBuffer();

        bytesToWrite[bytesToWrite.Length - 1] = (byte)'\n';
        expectedChars[expectedChars.Length - 1] = (char)bytesToWrite[bytesToWrite.Length - 1];

        retValue &= VerifyRead(com1, com2, bytesToWrite, expectedChars);

        com1.Close();
        com2.Close();

        if (!retValue)
            Console.WriteLine("Err_010!!! Verifying default ParityReplace byte with a parity errro on the last byte failed");

        return retValue;
    }


    public bool BytesToRead_RND_Buffer_Size()
    {
        Random rndGen = new Random(-55);

        if (!VerifyBytesToRead(rndGen.Next(1, 2 * numRndBytesToRead)))
        {
            Console.WriteLine("Err_010!! Verifying BytesToRead with a random buffer size FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_1_Buffer_Size()
    {
        if (!VerifyBytesToRead(1))
        {
            Console.WriteLine("Err_011!! Verifying BytesToRead with a buffer size of 1 FAILED");
            return false;
        }

        return true;
    }


    public bool BytesToRead_Equal_Buffer_Size()
    {
        Random rndGen = new Random(-55);

        if (!VerifyBytesToRead(numRndBytesToRead))
        {
            Console.WriteLine("Err_012!! Verifying BytesToRead with a buffer size equal to the number of byte written FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyTimeout(SerialPort com)
    {
        System.Diagnostics.Stopwatch timer = new Stopwatch();
        int expectedTime = com.ReadTimeout;
        int actualTime = 0;
        double percentageDifference;
        bool retValue = true;

        try
        {
            com.ReadTo(com.NewLine); //Warm up read method
            Console.WriteLine("Err_6941814ahbpa!!!: Read did not throw Timeout Exception when it timed out for the first time");
            retValue = false;
        }
        catch (TimeoutException) { }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();

            try
            {
                com.ReadTo(com.NewLine);
                Console.WriteLine("Err_17087ahps!!!: Read did not reuturn 0 when it timed out");
                retValue = false;
            }
            catch (TimeoutException) { }

            timer.Stop();
            actualTime += (int)timer.ElapsedMilliseconds;
            timer.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;
        percentageDifference = System.Math.Abs((expectedTime - actualTime) / (double)expectedTime);

        //Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
        if (maxPercentageDifference < percentageDifference)
        {
            Console.WriteLine("ERROR!!!: The read method timedout in {0} expected {1} percentage difference: {2}", actualTime, expectedTime, percentageDifference);
            retValue = false;
        }

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyReadException(SerialPort com, Type expectedException)
    {
        bool retValue = true;

        try
        {
            com.ReadTo(com.NewLine);
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
        return retValue;
    }


    public bool VerifyParityReplaceByte(int parityReplace, int parityErrorIndex)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        byte[] bytesToWrite = new byte[numRndBytesPairty + 1];//Plus one to accomidate the NewLineByte
        char[] expectedChars = new char[numRndBytesPairty + 1];//Plus one to accomidate the NewLineByte
        byte expectedByte;

        //Genrate random characters without an parity error
        for (int i = 0; i < numRndBytesPairty; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 128);

            bytesToWrite[i] = randByte;
            expectedChars[i] = (char)randByte;
        }

        if (-1 == parityReplace)
        {
            //If parityReplace is -1 and we should just use the default value
            expectedByte = com1.ParityReplace;
        }
        else if ('\0' == parityReplace)
        {
            //If parityReplace is the null charachater and parity replacement should not occur
            com1.ParityReplace = (byte)parityReplace;
            expectedByte = bytesToWrite[parityErrorIndex];
        }
        else
        {
            //Else parityReplace was set to a value and we should expect this value to be returned on a parity error
            com1.ParityReplace = (byte)parityReplace;
            expectedByte = (byte)parityReplace;
        }

        //Create an parity error by setting the highest order bit to true
        bytesToWrite[parityErrorIndex] = (byte)(bytesToWrite[parityErrorIndex] | 0x80);
        expectedChars[parityErrorIndex] = (char)expectedByte;

        Console.WriteLine("Verifying ParityReplace={0} with an ParityError at: {1} ", com1.ParityReplace, parityErrorIndex);

        com1.Parity = Parity.Space;
        com1.DataBits = 7;

        com1.Open();
        com2.Open();

        bytesToWrite[numRndBytesPairty] = DEFAULT_NEW_LINE;
        expectedChars[numRndBytesPairty] = (char)DEFAULT_NEW_LINE;

        return VerifyRead(com1, com2, bytesToWrite, expectedChars);
    }


    public bool VerifyBytesToRead(int numBytesRead)
    {
        return VerifyBytesToRead(numBytesRead, DEFAULT_NUMBER_NEW_LINES);
    }


    public bool VerifyBytesToRead(int numBytesRead, int numNewLines)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        byte[] bytesToWrite = new byte[numBytesRead + 1];//Plus one to accomidate the NewLineByte
        char[] expectedChars;
        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

        //Genrate random characters
        for (int i = 0; i < numBytesRead; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 256);

            bytesToWrite[i] = randByte;
        }

        expectedChars = encoding.GetChars(bytesToWrite, 0, bytesToWrite.Length);
        for (int i = 0; i < numNewLines; i++)
        {
            int newLineIndex;

            newLineIndex = rndGen.Next(0, numBytesRead);
            bytesToWrite[newLineIndex] = (byte)'\n';
            expectedChars[newLineIndex] = (char)'\n';
        }

        Console.WriteLine("Verifying BytesToRead with a buffer of: {0} ", numBytesRead);

        com1.Open();
        com2.Open();

        bytesToWrite[numBytesRead] = DEFAULT_NEW_LINE;
        expectedChars[numBytesRead] = (char)DEFAULT_NEW_LINE;

        return VerifyRead(com1, com2, bytesToWrite, expectedChars);
    }


    private bool VerifyRead(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars)
    {
        bool retValue = true;
        char[] rcvBuffer;
        char[] actualChars = new char[expectedChars.Length];
        int bytesRead, totalBytesRead, charsRead, totalCharsRead;
        int bytesToRead;
        string rcvString;
        int indexOfNewLine, lastIndexOfNewLine = -1;
        int waitTime = 0;

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 250;

        while (com1.BytesToRead < bytesToWrite.Length && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        totalBytesRead = 0;
        totalCharsRead = 0;
        bytesToRead = com1.BytesToRead;

        while (true)
        {
            try
            {
                rcvString = com1.ReadTo(com1.NewLine);
            }
            catch (TimeoutException)
            {
                break;
            }

            //While their are more characters to be read
            rcvBuffer = rcvString.ToCharArray();
            charsRead = rcvBuffer.Length;
            bytesRead = com1.Encoding.GetByteCount(rcvBuffer, 0, charsRead);

            indexOfNewLine = System.Array.IndexOf(expectedChars, (char)DEFAULT_NEW_LINE, lastIndexOfNewLine + 1);

            if (indexOfNewLine - (lastIndexOfNewLine + 1) != charsRead)
            {
                //If we have not read all of the characters that we should have
                Console.WriteLine("ERROR!!!: Read did not return all of the characters that were in SerialPort buffer");
                Console.WriteLine("indexOfNewLine={0} lastIndexOfNewLine={1} charsRead={2}", indexOfNewLine, lastIndexOfNewLine, charsRead);
                retValue = false;
            }

            if (expectedChars.Length < totalCharsRead + charsRead)
            {
                //If we have read in more characters then we expect
                Console.WriteLine("ERROR!!!: We have received more characters then were sent");
                retValue = false;
                break;
            }

            System.Array.Copy(rcvBuffer, 0, actualChars, totalCharsRead, charsRead);

            actualChars[totalCharsRead + charsRead] = (char)DEFAULT_NEW_LINE; //Add the NewLine char into actualChars
            totalBytesRead += bytesRead + 1; //Plus 1 because we read the NewLine char
            totalCharsRead += charsRead + 1; //Plus 1 because we read the NewLine char

            lastIndexOfNewLine = indexOfNewLine;

            if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                retValue = false;
            }

            bytesToRead = com1.BytesToRead;
        }//End while there are more characters to read

        //Compare the chars that were written with the ones we expected to read
        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (expectedChars[i] != actualChars[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1}", (int)expectedChars[i], (int)actualChars[i]);
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

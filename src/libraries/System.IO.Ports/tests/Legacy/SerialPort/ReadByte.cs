// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class ReadByte
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadByte()";
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

    public enum ReadDataFromEnum { NonBuffered, Buffered, BufferedAndNonBuffered };

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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_IterativeReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SerialPort_ReadBufferedAndNonBufferedData), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Read_DataReceivedBeforeTimeout), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

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


    public bool SerialPort_ReadBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_2507ajlsp!!! Verifying read method with reading all of the buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.Buffered))
        {
            Console.WriteLine("Err_1659akl!!! Verifying read method with reading the buffered data in several calls");
            return false;
        }

        return true;
    }


    public bool SerialPort_ReadBufferedAndNonBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.BufferedAndNonBuffered))
        {
            Console.WriteLine("Err_2082aspzh!!! Verifying read method with reading all of the buffered an non buffered data in one call");
            return false;
        }

        return true;
    }


    public bool SerialPort_IterativeReadBufferedAndNonBufferedData()
    {
        if (!VerifyRead(System.Text.Encoding.ASCII, ReadDataFromEnum.BufferedAndNonBuffered))
        {
            Console.WriteLine("Err_5687nhnhl!!! Verifying read method with reading the buffered and non buffereddata in several calls");
            return false;
        }

        return true;
    }

    public bool Read_DataReceivedBeforeTimeout()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        byte[] byteXmitBuffer = TCSupport.GetRandomBytes(512);
        byte[] byteRcvBuffer = new byte[byteXmitBuffer.Length];
        ASyncRead asyncRead = new ASyncRead(com1);
        System.Threading.Thread asyncReadThread = new System.Threading.Thread(new System.Threading.ThreadStart(asyncRead.Read));
        bool retValue = true;

        Console.WriteLine("Verifying that ReadByte() will read bytes that have been received after the call to Read was made");

        com1.Encoding = System.Text.Encoding.UTF8;
        com2.Encoding = System.Text.Encoding.UTF8;
        com1.ReadTimeout = 20000; // 20 seconds

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        asyncReadThread.Start();
        asyncRead.ReadStartedEvent.WaitOne(); //This only tells us that the thread has started to execute code in the method
        System.Threading.Thread.Sleep(2000); //We need to wait to guarentee that we are executing code in SerialPort
        com2.Write(byteXmitBuffer, 0, byteXmitBuffer.Length);

        asyncRead.ReadCompletedEvent.WaitOne();

        if (null != asyncRead.Exception)
        {
            retValue = false;
            Console.WriteLine("Err_04448ajhied Unexpected exception thrown from async read:\n{0}", asyncRead.Exception);
        }
        else if (asyncRead.Result != byteXmitBuffer[0])
        {
            retValue = false;
            Console.WriteLine("Err_0158ahei Expected ReadChar to read {0}({0:X}) actual {1}({1:X})", byteXmitBuffer[0], asyncRead.Result);
        }
        else
        {
            System.Threading.Thread.Sleep(1000); //We need to wait for all of the bytes to be received
            byteRcvBuffer[0] = (byte)asyncRead.Result;
            int readResult = com1.Read(byteRcvBuffer, 1, byteRcvBuffer.Length - 1);

            if (1 + readResult != byteXmitBuffer.Length)
            {
                retValue = false;
                Console.WriteLine("Err_051884ajoedo Expected Read to read {0} bytes actually read {1}",
                    byteXmitBuffer.Length - 1, readResult);
            }
            else
            {
                for (int i = 0; i < byteXmitBuffer.Length; ++i)
                {
                    if (byteRcvBuffer[i] != byteXmitBuffer[i])
                    {
                        retValue = false;
                        Console.WriteLine("Err_05188ahed Characters differ at {0} expected:{1}({1:X}) actual:{2}({2:X}) asyncRead.Result={3}",
                            i, byteXmitBuffer[i], byteRcvBuffer[i], asyncRead.Result);
                    }
                }
            }
        }

        if (!retValue)
            Console.WriteLine("Err_018068ajkid Verifying that ReadByte() will read bytes that have been received after the call to Read was made failed");

        com1.Close();
        com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyRead(System.Text.Encoding encoding)
    {
        return VerifyRead(encoding, ReadDataFromEnum.NonBuffered);
    }


    private bool VerifyRead(System.Text.Encoding encoding, ReadDataFromEnum readDataFrom)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        int bufferSize = numRndByte;
        byte[] byteXmitBuffer = new byte[bufferSize];

        //Genrate random bytes
        for (int i = 0; i < byteXmitBuffer.Length; i++)
        {
            byteXmitBuffer[i] = (byte)rndGen.Next(0, 256);
        }

        com1.ReadTimeout = 500;
        com1.Encoding = encoding;

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        switch (readDataFrom)
        {
            case ReadDataFromEnum.NonBuffered:
                return VerifyReadNonBuffered(com1, com2, byteXmitBuffer);

            case ReadDataFromEnum.Buffered:
                return VerifyReadBuffered(com1, com2, byteXmitBuffer);

            case ReadDataFromEnum.BufferedAndNonBuffered:
                return VerifyReadBufferedAndNonBuffered(com1, com2, byteXmitBuffer);
        }
        return false;
    }


    private bool VerifyReadNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, bytesToWrite);
    }


    private bool VerifyReadBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        BufferData(com1, com2, bytesToWrite);

        return PerformReadOnCom1FromCom2(com1, com2, bytesToWrite);
    }


    private bool VerifyReadBufferedAndNonBuffered(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        byte[] expectedBytes = new byte[(2 * bytesToWrite.Length)];

        BufferData(com1, com2, bytesToWrite);
        Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, 0, bytesToWrite.Length);
        Buffer.BlockCopy(bytesToWrite, 0, expectedBytes, bytesToWrite.Length, bytesToWrite.Length);

        return VerifyBytesReadOnCom1FromCom2(com1, com2, bytesToWrite, expectedBytes);
    }


    private bool BufferData(SerialPort com1, SerialPort com2, byte[] bytesToWrite)
    {
        bool retValue = true;

        com2.Write(bytesToWrite, 0, 1); // Write one byte at the begining because we are going to read this to buffer the rest of the data
        com2.Write(bytesToWrite, 0, bytesToWrite.Length);

        while (com1.BytesToRead < bytesToWrite.Length)
        {
            System.Threading.Thread.Sleep(50);
        }

        com1.Read(new char[1], 0, 1); // This should put the rest of the bytes in SerialPorts own internal buffer

        if (com1.BytesToRead != bytesToWrite.Length)
        {
            Console.WriteLine("Err_7083zaz Expected com1.BytesToRead={0} actual={1}", bytesToWrite.Length, com1.BytesToRead);
            retValue = false;
        }

        return retValue;
    }


    private bool VerifyBytesReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] bytesToWrite, byte[] expectedBytes)
    {
        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 500;

        System.Threading.Thread.Sleep((int)(((bytesToWrite.Length * 10.0) / com1.BaudRate) * 1000) + 250);

        return PerformReadOnCom1FromCom2(com1, com2, expectedBytes);
    }


    private bool PerformReadOnCom1FromCom2(SerialPort com1, SerialPort com2, byte[] expectedBytes)
    {
        bool retValue = true;
        byte[] byteRcvBuffer = new byte[expectedBytes.Length];
        int readInt;
        int i;

        i = 0;
        while (true)
        {
            try
            {
                readInt = com1.ReadByte();
            }
            catch (TimeoutException)
            {
                break;
            }

            //While their are more bytes to be read
            if (expectedBytes.Length <= i)
            {
                //If we have read in more bytes then were actually sent
                Console.WriteLine("ERROR!!!: We have received more bytes then were sent");
                retValue = false;
                break;
            }

            byteRcvBuffer[i] = (byte)readInt;

            if (readInt != expectedBytes[i])
            {
                //If the byte read is not the expected byte
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read byte {1}", (int)expectedBytes[i], readInt);
                retValue = false;
            }

            i++;

            if (expectedBytes.Length - i != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", expectedBytes.Length - i, com1.BytesToRead);
                retValue = false;
            }
        }

        if (0 != com1.BytesToRead)
        {
            System.Console.WriteLine("ERROR!!!: Expected BytesToRead=0  actual BytesToRead={0}", com1.BytesToRead);
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public class ASyncRead
    {
        private SerialPort _com;
        private int _result;

        private System.Threading.AutoResetEvent _readCompletedEvent;
        private System.Threading.AutoResetEvent _readStartedEvent;

        private Exception _exception;

        public ASyncRead(SerialPort com)
        {
            _com = com;
            _result = Int32.MinValue;

            _readCompletedEvent = new System.Threading.AutoResetEvent(false);
            _readStartedEvent = new System.Threading.AutoResetEvent(false);

            _exception = null;
        }

        public void Read()
        {
            try
            {
                _readStartedEvent.Set();
                _result = _com.ReadByte();
            }
            catch (Exception e)
            {
                _exception = e;
            }
            finally
            {
                _readCompletedEvent.Set();
            }
        }

        public System.Threading.AutoResetEvent ReadStartedEvent
        {
            get
            {
                return _readStartedEvent;
            }
        }

        public System.Threading.AutoResetEvent ReadCompletedEvent
        {
            get
            {
                return _readCompletedEvent;
            }
        }

        public int Result
        {
            get
            {
                return _result;
            }
        }

        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }
    }
    #endregion
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class ReceivedEvent
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReceivedBytesThreshold";
    public static readonly String s_strTFName = "ReceivedBytesThreshold.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Maximum random value to use for ReceivedBytesThreshold
    public static readonly int MAX_RND_THRESHOLD = 16;

    //Minimum random value to use for ReceivedBytesThreshold
    public static readonly int MIN_RND_THRESHOLD = 2;

    //Maximum time to wait for all of the expected events to be firered
    public static readonly int MAX_TIME_WAIT = 500;
    public static readonly int ITERATION_TIME_WAIT = 10;
    public static readonly int NUM_TRYS = 5;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReceivedEvent objTest = new ReceivedEvent();
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

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReceivedEvent_Chars), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReceivedEvent_Eof), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReceivedEvent_CharsEof), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReceivedEvent_CharsEof_ReadAllChars), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ReceivedEvent_Chars()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
        bool retValue = true;
        Random rndGen = new Random(-55);

        Console.WriteLine("Verifying ReceivedChars event");

        com1.Open();
        com2.Open();
        com1.DataReceived += new SerialDataReceivedEventHandler(rcvEventHandler.HandleEvent);

        for (int i = 0; i < NUM_TRYS; i++)
        {
            com2.Write(new byte[com1.ReceivedBytesThreshold], 0, com1.ReceivedBytesThreshold);
            rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

            if (!rcvEventHandler.Validate(SerialData.Chars, com1.ReceivedBytesThreshold))
            {
                Console.WriteLine("Err_2097asd!!! ReceivedChars Event not fired {0}", i);
                retValue = false;
            }

            if (0 != rcvEventHandler.NumberOfOccurencesOfType(SerialData.Eof))
            {
                Console.WriteLine("Err_21087qpua!!! Unexpected EofReceived event fireed {0}", i);
                retValue = false;
            }

            if (0 != rcvEventHandler.NumberOfOccurencesOfType(SerialData.Chars))
            {
                Console.WriteLine("Err_32417!!! Unexpected EofReceived event fireed {0}", i);
                retValue = false;
            }

            com1.DiscardInBuffer();
        }

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying ReceivedChars event FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool ReceivedEvent_Eof()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
        bool retValue = true;
        byte[] xmitBytes = new byte[1];

        Console.WriteLine("Verifying EofReceived event");
        com1.Open();
        com2.Open();
        com1.DataReceived += new SerialDataReceivedEventHandler(rcvEventHandler.HandleEvent);

        //EOF char
        xmitBytes[0] = 26;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            com2.Write(xmitBytes, 0, xmitBytes.Length);
            rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, 2);

            if (!rcvEventHandler.Validate(SerialData.Eof, i))
            {
                Console.WriteLine("Err_1048apqa!!! EofReceived Event not fired {0}", i);
                retValue = false;
            }

            if (!rcvEventHandler.Validate(SerialData.Chars, i + com1.ReceivedBytesThreshold))
            {
                Console.WriteLine("Err_16489qayas!!! ReceivedChars Event not fired {0}", i);
                retValue = false;
            }

            if (0 != rcvEventHandler.NumberOfOccurencesOfType(SerialData.Eof))
            {
                Console.WriteLine("Err_01278qaods!!! Unexpected EofReceived event fireed {0}", i);
                retValue = false;
            }

            if (1 < rcvEventHandler.NumberOfOccurencesOfType(SerialData.Chars))
            {
                Console.WriteLine("Err_2972qoypa!!! Unexpected ReceivedChars event fireed {0}", i);
                retValue = false;
            }
        }

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying EofReceived event FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool ReceivedEvent_CharsEof()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
        bool retValue = true;
        byte[] xmitBytes = new byte[3];

        Console.WriteLine("Verifying EofReceived event");

        com1.Open();
        com2.Open();
        com1.DataReceived += new SerialDataReceivedEventHandler(rcvEventHandler.HandleEvent);

        //EOF char
        xmitBytes[0] = 56;
        xmitBytes[1] = 26;
        xmitBytes[2] = 55;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            com2.Write(xmitBytes, 0, xmitBytes.Length);
            rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, SerialData.Eof);

            if (!rcvEventHandler.Validate(SerialData.Eof, i * xmitBytes.Length))
            {
                Console.WriteLine("Err_09727ahsp!!!EOF Event not fired {0}", i);
                retValue = false;
            }

            if (!rcvEventHandler.Validate(SerialData.Chars, (i * xmitBytes.Length) + com1.ReceivedBytesThreshold))
            {
                Console.WriteLine("Err_27928adshs !!!ReceivedChars Event not fired {0}", i);
                retValue = false;
            }

            if (0 != rcvEventHandler.NumberOfOccurencesOfType(SerialData.Eof))
            {
                Console.WriteLine("Err_20712asdfhow!!! Unexpected EofReceived event fired {0} iteration:{1}",
                    rcvEventHandler.NumberOfOccurencesOfType(SerialData.Eof), i);
                retValue = false;
            }

            rcvEventHandler.Clear();
        }

        if (!retValue)
        {
            Console.WriteLine("Err_3468eadhs!!! Verifying CharsReceived and EofReceived event FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool ReceivedEvent_CharsEof_ReadAllChars()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ReadInReceivedEventHandler rcvEventHandler = new ReadInReceivedEventHandler(com1);
        bool retValue = true;
        byte[] xmitBytes = new byte[3];

        Console.WriteLine("Verifying EofReceived and ReceivedChars events where all chars are read in the ReceivedChars event");

        com1.Open();
        com2.Open();
        com1.DataReceived += new SerialDataReceivedEventHandler(rcvEventHandler.HandleEvent);

        //EOF char
        xmitBytes[0] = 56;
        xmitBytes[1] = 26;
        xmitBytes[2] = 55;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            com2.Write(xmitBytes, 0, xmitBytes.Length);
            rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, SerialData.Eof);

            if (!rcvEventHandler.Validate(SerialData.Eof, 0))
            {
                Console.WriteLine("Err_09727ahsp!!!EOF Event not fired {0}", i);
                retValue = false;
            }

            if (!rcvEventHandler.Validate(SerialData.Chars, 1))
            {
                Console.WriteLine("Err_27928adshs !!!ReceivedChars Event not fired {0}", i);
                retValue = false;
            }

            if (0 != rcvEventHandler.NumberOfOccurencesOfType(SerialData.Eof))
            {
                Console.WriteLine("Err_20712asdfhow!!! Unexpected EofReceived event fired {0} iteration:{1}",
                    rcvEventHandler.NumberOfOccurencesOfType(SerialData.Eof), i);
                retValue = false;
            }

            rcvEventHandler.Clear();
        }

        if (rcvEventHandler.NumBytesRead != NUM_TRYS * xmitBytes.Length)
        {
            Console.WriteLine("Err_1298129ahnied!!! Expected to read {0} chars actually read {1}",
                NUM_TRYS * xmitBytes.Length, rcvEventHandler.NumBytesRead);
            retValue = false;
        }
        else
        {
            for (int i = 0; i < NUM_TRYS; ++i)
            {
                for (int j = 0; j < xmitBytes.Length; ++j)
                {
                    if (xmitBytes[j] != rcvEventHandler.BytesRead[(i * xmitBytes.Length) + j])
                    {
                        Console.WriteLine("Err_2829aneid Expected to Read '{0}'({0:X}) actually read {1}'({1:X})",
                            xmitBytes[j], rcvEventHandler.BytesRead[(i * xmitBytes.Length) + j]);
                        retValue = false;
                    }
                }
            }
        }

        if (!retValue)
        {
            Console.WriteLine("Err_3468eadhs!!! Verifying CharsReceived and EofReceived event FAILED");
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public class ReceivedEventHandler
    {
        public System.Collections.ArrayList EventType;
        public System.Collections.ArrayList BytesToRead;
        public System.Collections.ArrayList Source;
        public int NumEventsHandled;
        protected SerialPort com;


        public ReceivedEventHandler(SerialPort com)
        {
            this.com = com;
            NumEventsHandled = 0;

            EventType = new System.Collections.ArrayList();
            BytesToRead = new System.Collections.ArrayList();
            Source = new System.Collections.ArrayList();
        }


        public void HandleEvent(object source, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = com.BytesToRead;

            lock (this)
            {
                BytesToRead.Add(bytesToRead);
                EventType.Add(e.EventType);
                Source.Add(source);

                NumEventsHandled++;

                System.Threading.Monitor.Pulse(this);
            }
        }


        public void Clear()
        {
            lock (this)
            {
                EventType.Clear();
                BytesToRead.Clear();

                NumEventsHandled = 0;
            }
        }


        public bool WaitForEvent(int maxMilliseconds, int totalNumberOfEvents)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            lock (this)
            {
                sw.Start();

                while (maxMilliseconds > sw.ElapsedMilliseconds && NumEventsHandled < totalNumberOfEvents)
                {
                    System.Threading.Monitor.Wait(this, (int)(maxMilliseconds - sw.ElapsedMilliseconds));
                }

                return totalNumberOfEvents == NumEventsHandled;
            }
        }

        public bool WaitForEvent(int maxMilliseconds, SerialData eventType)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            lock (this)
            {
                sw.Start();

                while (maxMilliseconds > sw.ElapsedMilliseconds)
                {
                    System.Threading.Monitor.Wait(this, (int)(maxMilliseconds - sw.ElapsedMilliseconds));

                    for (int i = 0; i < EventType.Count; i++)
                    {
                        if (eventType == (SerialData)EventType[i])
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }


        //Since we can not garantee the order or the exact time that the event handler is called 
        //We wil look for an event that was firered that matches the type and that bytesToRead 
        //is greater then the parameter
        public bool Validate(SerialData eventType, int bytesToRead)
        {
            bool retValue = false;

            lock (this)
            {
                for (int i = 0; i < EventType.Count; i++)
                {
                    if (eventType == (SerialData)EventType[i] && bytesToRead <= (int)BytesToRead[i] && (SerialPort)Source[i] == com)
                    {
                        EventType.RemoveAt(i);
                        BytesToRead.RemoveAt(i);
                        Source.RemoveAt(i);

                        NumEventsHandled--;
                        retValue = true;

                        break;
                    }
                }
            }

            return retValue;
        }


        public int NumberOfOccurencesOfType(SerialData eventType)
        {
            int numOccurences = 0;

            lock (this)
            {
                for (int i = 0; i < EventType.Count; i++)
                {
                    if (eventType == (SerialData)EventType[i])
                    {
                        numOccurences++;
                    }
                }
            }

            return numOccurences;
        }
    }

    public class ReadInReceivedEventHandler : ReceivedEventHandler
    {
        private int _numBytesRead;
        private byte[] _bytesRead;

        public ReadInReceivedEventHandler(SerialPort com)
            : base(com)
        {
            _numBytesRead = 0;
            _bytesRead = new byte[4];
        }

        public int NumBytesRead
        {
            get
            {
                return _numBytesRead;
            }
        }

        public byte[] BytesRead
        {
            get
            {
                return _bytesRead;
            }
        }

        new public void HandleEvent(object source, SerialDataReceivedEventArgs e)
        {
            base.HandleEvent(source, e);

            if (e.EventType == SerialData.Chars)
            {
                if ((_bytesRead.Length - _numBytesRead) < com.BytesToRead)
                {
                    byte[] tempByteArray = new byte[Math.Max(_bytesRead.Length * 2, _bytesRead.Length + com.BytesToRead)];
                    Array.Copy(_bytesRead, 0, tempByteArray, 0, _numBytesRead);
                    _bytesRead = tempByteArray;
                }

                _numBytesRead += com.Read(_bytesRead, _numBytesRead, _bytesRead.Length - _numBytesRead);
            }
        }
    }
    #endregion
}

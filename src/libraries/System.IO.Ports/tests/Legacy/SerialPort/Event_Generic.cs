// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Threading;

public class Event_Generic
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.PinChangedEvent";
    public static readonly String s_strTFName = "PinChangedEvent.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Maximum time to wait for all of the expected events to be firered
    public static readonly int MAX_TIME_WAIT = 5000;

    //Time to wait inbetween trigering events
    public static readonly int TRIGERING_EVENTS_WAIT_TIME = 500;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        Event_Generic objTest = new Event_Generic();

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
        if (e.ExceptionObject is IgnoreException)
        { // Ignore Exceptions of the type IgnoreException
            return;
        }

        _numErrors++;
        Console.WriteLine("\nAn unhandled exception was thrown and not caught in the app domain: \n{0}", e.ExceptionObject);
        Console.WriteLine("Test FAILED!!!\n");

        Environment.ExitCode = 101;
    }

    public bool RunTest()
    {
        bool retValue = true;
        TCSupport tcSupport = new TCSupport();

        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(EventHandler_ThrowsException), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(EventHandlers_CalledSerially), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Thread_In_PinChangedEvent), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Thread_In_ReceivedEvent), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(Thread_In_ErrorEvent), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool EventHandler_ThrowsException()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        PinChangedEventHandler pinChangedEventHandler = new PinChangedEventHandler(com1, true);
        ReceivedEventHandler receivedEventHandler = new ReceivedEventHandler(com1, true);
        ErrorEventHandler errorEventHandler = new ErrorEventHandler(com1, true);
        bool retValue = true;

        /***************************************************************
        Scenario Description: All of the event handler are going to throw and we are 
        going to verify that the event handler will still get called.

        We want to verify that throwing does not cause the thread calling the event 
        handlers to die.		
        ***************************************************************/

        Console.WriteLine("Verifying where the event handlers throws");

        com1.Open();
        com2.Open();

        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
        com1.PinChanged += new SerialPinChangedEventHandler(pinChangedEventHandler.HandleEvent);
        com1.DataReceived += new SerialDataReceivedEventHandler(receivedEventHandler.HandleEvent);
        com1.ErrorReceived += new SerialErrorReceivedEventHandler(errorEventHandler.HandleEvent);

        //This should cause ErrorEvent to be fired with a parity error since the 
        //8th bit on com1 is the parity bit, com1 one expest this bit to be 1(Mark), 
        //and com2 is writing 0 for this bit
        com1.DataBits = 7;
        com1.Parity = Parity.Mark;
        com2.BaseStream.Write(new byte[1], 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause PinChangedEvent to be fired with SerialPinChanges.DsrChanged
        //since we are setting DtrEnable to true
        com2.DtrEnable = true;
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause ReceivedEvent to be fired with ReceivedChars
        //since we are writing some bytes
        com1.DataBits = 8;
        com1.Parity = Parity.None;
        com2.BaseStream.Write(new byte[] { 40 }, 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause a fame error since the 8th bit is not set,
        //and com1 is set to 7 data bits so the 8th bit will +12v where
        //com1 expects the stop bit at the 8th bit to be -12v
        com1.DataBits = 7;
        com1.Parity = Parity.None;
        com2.BaseStream.Write(new byte[] { 0x01 }, 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause PinChangedEvent to be fired with SerialPinChanges.CtsChanged
        //since we are setting RtsEnable to true
        com2.RtsEnable = true;
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause ReceivedEvent to be fired with EofReceived 
        //since we are writing the EOF char		
        com1.DataBits = 8;
        com1.Parity = Parity.None;
        com2.BaseStream.Write(new byte[] { 26 }, 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause PinChangedEvent to be fired with SerialPinChanges.Break
        //since we are setting BreakState to true
        com2.BreakState = true;
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);


        if (!pinChangedEventHandler.WaitForEvent(MAX_TIME_WAIT, 3))
        {
            Console.WriteLine("Err_28282haied Expected 3 PinChangedEvents to be fired and only {0} occured", pinChangedEventHandler.NumEventsHandled);
            retValue = false;
        }

        if (!receivedEventHandler.WaitForEvent(MAX_TIME_WAIT, 2))
        {
            Console.WriteLine("Err_2912hsie Expected 2 ReceivedEvents  to be fired and only {0} occured", receivedEventHandler.NumEventsHandled);
            retValue = false;
        }

        if (!errorEventHandler.WaitForEvent(MAX_TIME_WAIT, 2))
        {
            Console.WriteLine("Err_191291jaied Expected 3 ErrorEvents to be fired and only {0} occured", errorEventHandler.NumEventsHandled);
            retValue = false;
        }


        //[] Verify all PinChangedEvents should have occured
        if (!pinChangedEventHandler.Validate(SerialPinChange.DsrChanged, 0))
        {
            Console.WriteLine("Err_24597aqqoo!!! PinChangedEvent DsrChanged event not fired");
            retValue = false;
        }

        if (!pinChangedEventHandler.Validate(SerialPinChange.CtsChanged, 0))
        {
            Console.WriteLine("Err_144754ajied!!! PinChangedEvent CtsChanged event not fired");
            retValue = false;
        }

        if (!pinChangedEventHandler.Validate(SerialPinChange.Break, 0))
        {
            Console.WriteLine("Err_15488ahied!!! PinChangedEvent Break event not fired");
            retValue = false;
        }

        //[] Verify all ReceivedEvent should have occured
        if (!receivedEventHandler.Validate(SerialData.Chars, 0))
        {
            Console.WriteLine("Err_54552aheied!!! ReceivedEvent ReceivedChars event not fired");
            retValue = false;
        }

        if (!receivedEventHandler.Validate(SerialData.Eof, 0))
        {
            Console.WriteLine("Err_4588ajeod!!! ReceivedEvent EofReceived event not fired");
            retValue = false;
        }

        //[] Verify all ErrorEvents should have occured
        if (!errorEventHandler.Validate(SerialError.RXParity, 0))
        {
            Console.WriteLine("Err_1051ajheid!!! ErrorEvent RxParity event not fired");
            retValue = false;
        }

        if (!errorEventHandler.Validate(SerialError.Frame, 0))
        {
            Console.WriteLine("Err_61805aheud!!! ErrorEvent Frame event not fired");
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool EventHandlers_CalledSerially()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        PinChangedEventHandler pinChangedEventHandler = new PinChangedEventHandler(com1, false, true);
        ReceivedEventHandler receivedEventHandler = new ReceivedEventHandler(com1, false, true);
        ErrorEventHandler errorEventHandler = new ErrorEventHandler(com1, false, true);
        bool retValue = true;
        int numPinChangedEvents = 0, numErrorEvents = 0, numReceivedEvents = 0;
        int iterationWaitTime = 100;
        bool threadFound;

        /***************************************************************
        Scenario Description: All of the event handlers should be called sequentiall never
        at the same time on multiple thread. Basically we will block a thread and verify 
        that no other thread is in another event handler

        ***************************************************************/

        Console.WriteLine("Verifying where the event handlers are called serially");

        com1.Open();
        com2.Open();

        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
        com1.PinChanged += new SerialPinChangedEventHandler(pinChangedEventHandler.HandleEvent);
        com1.DataReceived += new SerialDataReceivedEventHandler(receivedEventHandler.HandleEvent);
        com1.ErrorReceived += new SerialErrorReceivedEventHandler(errorEventHandler.HandleEvent);

        //This should cause ErrorEvent to be fired with a parity error since the 
        //8th bit on com1 is the parity bit, com1 one expest this bit to be 1(Mark), 
        //and com2 is writing 0 for this bit
        com1.DataBits = 7;
        com1.Parity = Parity.Mark;
        com2.BaseStream.Write(new byte[1], 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause PinChangedEvent to be fired with SerialPinChanges.DsrChanged
        //since we are setting DtrEnable to true
        com2.DtrEnable = true;
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause ReceivedEvent to be fired with ReceivedChars
        //since we are writing some bytes
        com1.DataBits = 8;
        com1.Parity = Parity.None;
        com2.BaseStream.Write(new byte[] { 40 }, 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause a fame error since the 8th bit is not set,
        //and com1 is set to 7 data bits so the 8th bit will +12v where
        //com1 expects the stop bit at the 8th bit to be -12v
        com1.DataBits = 7;
        com1.Parity = Parity.None;
        com2.BaseStream.Write(new byte[] { 0x01 }, 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause PinChangedEvent to be fired with SerialPinChanges.CtsChanged
        //since we are setting RtsEnable to true
        com2.RtsEnable = true;
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause ReceivedEvent to be fired with EofReceived 
        //since we are writing the EOF char		
        com1.DataBits = 8;
        com1.Parity = Parity.None;
        com2.BaseStream.Write(new byte[] { 26 }, 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        //This should cause PinChangedEvent to be fired with SerialPinChanges.Break
        //since we are setting BreakState to true
        com2.BreakState = true;
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        threadFound = true;
        while (threadFound)
        {
            threadFound = false;

            for (int i = 0; i < MAX_TIME_WAIT / iterationWaitTime; ++i)
            {
                if (pinChangedEventHandler.WaitForEvent(iterationWaitTime, numPinChangedEvents + 1))
                {//A thread is in PinChangedEvent verify that it is not in any other
                    if (receivedEventHandler.NumEventsHandled != numReceivedEvents)
                    {
                        Console.WriteLine("Err_191818ahied A thread is in PinChangedEvent and ReceivedEvent");
                        retValue = false;
                    }

                    if (errorEventHandler.NumEventsHandled != numErrorEvents)
                    {
                        Console.WriteLine("Err_198119hjaheid A thread is in PinChangedEvent and ErrorEvent");
                        retValue = false;
                    }

                    ++numPinChangedEvents;
                    pinChangedEventHandler.ResumeHandleEvent();
                    threadFound = true;
                    break;
                }

                if (receivedEventHandler.WaitForEvent(iterationWaitTime, numReceivedEvents + 1))
                {//A thread is in ReceivedEvent verify that it is not in any other
                    if (pinChangedEventHandler.NumEventsHandled != numPinChangedEvents)
                    {
                        Console.WriteLine("Err_2288ajed A thread is in ReceivedEvent and PinChangedEvent");
                        retValue = false;
                    }

                    if (errorEventHandler.NumEventsHandled != numErrorEvents)
                    {
                        Console.WriteLine("Err_25158ajeiod A thread is in ReceivedEvent and ErrorEvent");
                        retValue = false;
                    }

                    ++numReceivedEvents;
                    receivedEventHandler.ResumeHandleEvent();
                    threadFound = true;
                    break;
                }

                if (errorEventHandler.WaitForEvent(iterationWaitTime, numErrorEvents + 1))
                {//A thread is in ErrorEvent verify that it is not in any other
                    if (pinChangedEventHandler.NumEventsHandled != numPinChangedEvents)
                    {
                        Console.WriteLine("Err_01208akiehd A thread is in ErrorEvent and PinChangedEvent");
                        retValue = false;
                    }

                    if (receivedEventHandler.NumEventsHandled != numReceivedEvents)
                    {
                        Console.WriteLine("Err_1254847ajied A thread is in ErrorEvent and ReceivedEvent");
                        retValue = false;
                    }

                    ++numErrorEvents;
                    errorEventHandler.ResumeHandleEvent();
                    threadFound = true;
                    break;
                }
            }
        }

        if (!pinChangedEventHandler.WaitForEvent(MAX_TIME_WAIT, 3))
        {
            Console.WriteLine("Err_2288ajied Expected 3 PinChangedEvents to be fired and only {0} occured", pinChangedEventHandler.NumEventsHandled);
            retValue = false;
        }

        if (!receivedEventHandler.WaitForEvent(MAX_TIME_WAIT, 2))
        {
            Console.WriteLine("Err_122808aoeid Expected 2 ReceivedEvents  to be fired and only {0} occured", receivedEventHandler.NumEventsHandled);
            retValue = false;
        }

        if (!errorEventHandler.WaitForEvent(MAX_TIME_WAIT, 2))
        {
            Console.WriteLine("Err_215887ajeid Expected 3 ErrorEvents to be fired and only {0} occured", errorEventHandler.NumEventsHandled);
            retValue = false;
        }

        //[] Verify all PinChangedEvents should have occured
        if (!pinChangedEventHandler.Validate(SerialPinChange.DsrChanged, 0))
        {
            Console.WriteLine("Err_258087aieid!!! PinChangedEvent DsrChanged event not fired");
            retValue = false;
        }

        if (!pinChangedEventHandler.Validate(SerialPinChange.CtsChanged, 0))
        {
            Console.WriteLine("Err_5548ajhied!!! PinChangedEvent CtsChanged event not fired");
            retValue = false;
        }

        if (!pinChangedEventHandler.Validate(SerialPinChange.Break, 0))
        {
            Console.WriteLine("Err_25848ajiied!!! PinChangedEvent Break event not fired");
            retValue = false;
        }

        //[] Verify all ReceivedEvent should have occured
        if (!receivedEventHandler.Validate(SerialData.Chars, 0))
        {
            Console.WriteLine("Err_0211558ajoied!!! ReceivedEvent ReceivedChars event not fired");
            retValue = false;
        }

        if (!receivedEventHandler.Validate(SerialData.Eof, 0))
        {
            Console.WriteLine("Err_215588zahid!!! ReceivedEvent EofReceived event not fired");
            retValue = false;
        }

        //[] Verify all ErrorEvents should have occured
        if (!errorEventHandler.Validate(SerialError.RXParity, 0))
        {
            Console.WriteLine("Err_515188ahjid!!! ErrorEvent RxParity event not fired");
            retValue = false;
        }

        if (!errorEventHandler.Validate(SerialError.Frame, 0))
        {
            Console.WriteLine("Err_55874884ajie!!! ErrorEvent Frame event not fired");
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool Thread_In_PinChangedEvent()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        PinChangedEventHandler pinChangedEventHandler = new PinChangedEventHandler(com1, false, true);
        Thread closeThread = new Thread(delegate () { com1.Close(); });
        bool retValue = true;

        Console.WriteLine("Verifying that if a thread is blocked in a PinChangedEvent handler the port can still be closed");

        com1.Open();
        com2.Open();

        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
        com1.PinChanged += new SerialPinChangedEventHandler(pinChangedEventHandler.HandleEvent);

        //This should cause PinChangedEvent to be fired with SerialPinChanges.DsrChanged
        //since we are setting DtrEnable to true
        com2.DtrEnable = true;

        if (!pinChangedEventHandler.WaitForEvent(MAX_TIME_WAIT, 1))
        {
            Console.WriteLine("Err_32688ajoid Expected 1 PinChangedEvents to be fired and only {0} occured", pinChangedEventHandler.NumEventsHandled);
            retValue = false;
        }

        closeThread.Start();
        Thread.Sleep(5000);

        pinChangedEventHandler.ResumeHandleEvent();
        closeThread.Join();

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool Thread_In_ReceivedEvent()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ReceivedEventHandler receivedEventHandler = new ReceivedEventHandler(com1, false, true);
        Thread closeThread = new Thread(delegate () { com1.Close(); });
        bool retValue = true;

        Console.WriteLine("Verifying that if a thread is blocked in a RecevedEvent handler the port can still be closed");

        com1.Open();
        com2.Open();

        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
        com1.DataReceived += new SerialDataReceivedEventHandler(receivedEventHandler.HandleEvent);

        //This should cause ReceivedEvent to be fired with ReceivedChars
        //since we are writing some bytes
        com1.DataBits = 8;
        com1.Parity = Parity.None;
        com2.BaseStream.Write(new byte[] { 40 }, 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        if (!receivedEventHandler.WaitForEvent(MAX_TIME_WAIT, 1))
        {
            Console.WriteLine("Err_122808aoeid Expected 1 ReceivedEvents  to be fired and only {0} occured", receivedEventHandler.NumEventsHandled);
            retValue = false;
        }

        closeThread.Start();
        Thread.Sleep(5000);

        receivedEventHandler.ResumeHandleEvent();
        closeThread.Join();

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }

    public bool Thread_In_ErrorEvent()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ErrorEventHandler errorEventHandler = new ErrorEventHandler(com1, false, true);
        Thread closeThread = new Thread(delegate () { com1.Close(); });
        bool retValue = true;

        Console.WriteLine("Verifying that if a thread is blocked in a ErrorEvent handler the port can still be closed");

        com1.Open();
        com2.Open();

        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
        com1.ErrorReceived += new SerialErrorReceivedEventHandler(errorEventHandler.HandleEvent);

        //This should cause ErrorEvent to be fired with a parity error since the 
        //8th bit on com1 is the parity bit, com1 one expest this bit to be 1(Mark), 
        //and com2 is writing 0 for this bit
        com1.DataBits = 7;
        com1.Parity = Parity.Mark;
        com2.BaseStream.Write(new byte[1], 0, 1);
        Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

        if (!errorEventHandler.WaitForEvent(MAX_TIME_WAIT, 1))
        {
            Console.WriteLine("Err_215887ajeid Expected 1 ErrorEvents to be fired and only {0} occured", errorEventHandler.NumEventsHandled);
            retValue = false;
        }

        closeThread.Start();
        Thread.Sleep(5000);

        errorEventHandler.ResumeHandleEvent();
        closeThread.Join();

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases

    public class IgnoreException : Exception
    {
        public IgnoreException()
            : base()
        {
        }

        public IgnoreException(string message)
            : base(message)
        {
        }
    }


    public class PinChangedEventHandler
    {
        public System.Collections.ArrayList EventType;
        public System.Collections.ArrayList BytesToRead;
        public System.Collections.ArrayList Source;
        public int NumEventsHandled;
        private SerialPort _com;
        private bool _shouldThrow;
        private bool _shouldWait;
        private AutoResetEvent _eventHandlerWait;

        public PinChangedEventHandler(SerialPort com) : this(com, false, false) { }

        public PinChangedEventHandler(SerialPort com, bool shouldThrow) : this(com, shouldThrow, false) { }

        public PinChangedEventHandler(SerialPort com, bool shouldThrow, bool shouldWait)
        {
            if (shouldThrow && shouldWait)
                throw new ArgumentException("shouldThrow and shouldWait can not both be true");

            _com = com;
            NumEventsHandled = 0;

            EventType = new System.Collections.ArrayList();
            Source = new System.Collections.ArrayList();
            BytesToRead = new System.Collections.ArrayList();

            _shouldThrow = shouldThrow;
            _shouldWait = shouldWait;

            _eventHandlerWait = new AutoResetEvent(false);
        }


        public void HandleEvent(object source, SerialPinChangedEventArgs e)
        {
            int bytesToRead = _com.BytesToRead;

            lock (this)
            {
                BytesToRead.Add(bytesToRead);
                EventType.Add(e.EventType);
                Source.Add(source);

                NumEventsHandled++;
                System.Threading.Monitor.Pulse(this);
            }

            if (_shouldThrow)
            {
                throw new IgnoreException("I was told to throw");
            }

            if (_shouldWait)
            {
                _eventHandlerWait.WaitOne();
            }
        }

        public void ResumeHandleEvent()
        {
            _eventHandlerWait.Set();
        }


        public void RemoveAt(int index)
        {
            lock (this)
            {
                EventType.RemoveAt(index);
                BytesToRead.RemoveAt(index);
                Source.RemoveAt(index);

                NumEventsHandled--;
            }
        }


        public void Clear()
        {
            lock (this)
            {
                EventType.Clear();
                BytesToRead.Clear();
                Source.Clear();

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

                return totalNumberOfEvents <= NumEventsHandled;
            }
        }


        //Since we can not garantee the order or the exact time that the event handler is called 
        //We wil look for an event that was firered that matches the type and that bytesToRead 
        //is greater then the parameter    
        public bool Validate(SerialPinChange eventType, int bytesToRead)
        {
            bool retValue = false;

            lock (this)
            {
                for (int i = 0; i < EventType.Count; i++)
                {
                    if (eventType == (SerialPinChange)EventType[i] && bytesToRead <= (int)BytesToRead[i] && (SerialPort)Source[i] == _com)
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

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < EventType.Count; i++)
            {
                sb.Append(i);
                sb.Append(": Type: ");
                sb.Append((SerialData)EventType[i]);
                sb.Append(" BytesToRead: ");
                sb.Append(BytesToRead[i]);
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }

    public class ErrorEventHandler
    {
        public System.Collections.ArrayList EventType;
        public System.Collections.ArrayList Source;
        public System.Collections.ArrayList BytesToRead;
        public int NumEventsHandled;
        private SerialPort _com;
        private bool _shouldThrow;
        private bool _shouldWait;
        private AutoResetEvent _eventHandlerWait;


        public ErrorEventHandler(SerialPort com) : this(com, false, false) { }

        public ErrorEventHandler(SerialPort com, bool shouldThrow) : this(com, shouldThrow, false) { }

        public ErrorEventHandler(SerialPort com, bool shouldThrow, bool shouldWait)
        {
            if (shouldThrow && shouldWait)
                throw new ArgumentException("shouldThrow and shouldWait can not both be true");

            _com = com;
            NumEventsHandled = 0;

            EventType = new System.Collections.ArrayList();
            Source = new System.Collections.ArrayList();
            BytesToRead = new System.Collections.ArrayList();

            _shouldThrow = shouldThrow;
            _shouldWait = shouldWait;

            _eventHandlerWait = new AutoResetEvent(false);
        }


        public void HandleEvent(object source, SerialErrorReceivedEventArgs e)
        {
            int bytesToRead = _com.BytesToRead;

            lock (this)
            {
                BytesToRead.Add(bytesToRead);
                EventType.Add(e.EventType);
                Source.Add(source);
                NumEventsHandled++;
                System.Threading.Monitor.Pulse(this);
            }

            if (_shouldThrow)
            {
                throw new IgnoreException("I was told to throw");
            }

            if (_shouldWait)
            {
                _eventHandlerWait.WaitOne();
            }
        }

        public void ResumeHandleEvent()
        {
            _eventHandlerWait.Set();
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

                return totalNumberOfEvents <= NumEventsHandled;
            }
        }


        //Since we can not garantee the order or the exact time that the event handler is called 
        //We wil look for an event that was firered that matches the type and that bytesToRead 
        //is greater then the parameter    
        public bool Validate(SerialError eventType, int bytesToRead)
        {
            bool retValue = false;

            lock (this)
            {
                for (int i = 0; i < EventType.Count; i++)
                {
                    if (eventType == (SerialError)EventType[i] && bytesToRead <= (int)BytesToRead[i] && (SerialPort)Source[i] == _com)
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
    }

    public class ReceivedEventHandler
    {
        public System.Collections.ArrayList EventType;
        public System.Collections.ArrayList BytesToRead;
        public System.Collections.ArrayList Source;
        public int NumEventsHandled;
        private SerialPort _com;
        private bool _shouldThrow;
        private bool _shouldWait;
        private AutoResetEvent _eventHandlerWait;

        public ReceivedEventHandler(SerialPort com) : this(com, false, false) { }

        public ReceivedEventHandler(SerialPort com, bool shouldThrow) : this(com, shouldThrow, false) { }

        public ReceivedEventHandler(SerialPort com, bool shouldThrow, bool shouldWait)
        {
            if (shouldThrow && shouldWait)
                throw new ArgumentException("shouldThrow and shouldWait can not both be true");

            _com = com;
            NumEventsHandled = 0;

            EventType = new System.Collections.ArrayList();
            Source = new System.Collections.ArrayList();
            BytesToRead = new System.Collections.ArrayList();

            _shouldThrow = shouldThrow;
            _shouldWait = shouldWait;

            _eventHandlerWait = new AutoResetEvent(false);
        }

        public void HandleEvent(object source, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = _com.BytesToRead;

            lock (this)
            {
                BytesToRead.Add(bytesToRead);
                EventType.Add(e.EventType);
                Source.Add(source);

                NumEventsHandled++;

                System.Threading.Monitor.Pulse(this);
            }

            if (_shouldThrow)
            {
                throw new IgnoreException("I was told to throw");
            }

            if (_shouldWait)
            {
                _eventHandlerWait.WaitOne();
            }
        }

        public void ResumeHandleEvent()
        {
            _eventHandlerWait.Set();
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

                return totalNumberOfEvents <= NumEventsHandled;
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
                    if (eventType == (SerialData)EventType[i] && bytesToRead <= (int)BytesToRead[i] && (SerialPort)Source[i] == _com)
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
    #endregion
}


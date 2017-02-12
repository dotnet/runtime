// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Event_Generic : PortsTest
{
    // Maximum time to wait for all of the expected events to be firered
    private static readonly int MAX_TIME_WAIT = 5000;

    // Time to wait inbetween triggering events
    private static readonly int TRIGERING_EVENTS_WAIT_TIME = 500;

    #region Test Cases

    [OuterLoop("Slow Test")]
    [ConditionalFact(nameof(HasNullModem), Skip="Always hangs")]
    public void EventHandlers_CalledSerially()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            PinChangedEventHandler pinChangedEventHandler = new PinChangedEventHandler(com1, false, true);
            ReceivedEventHandler receivedEventHandler = new ReceivedEventHandler(com1, false, true);
            ErrorEventHandler errorEventHandler = new ErrorEventHandler(com1, false, true);
            int numPinChangedEvents = 0, numErrorEvents = 0, numReceivedEvents = 0;
            int iterationWaitTime = 100;

            /***************************************************************
            Scenario Description: All of the event handlers should be called sequentially never
            at the same time on multiple thread. Basically we will block each event handler caller thread and verify 
            that no other thread is in another event handler

            ***************************************************************/

            Debug.WriteLine("Verifying that event handlers are called serially");

            com1.WriteTimeout = 5000;
            com2.WriteTimeout = 5000;

            com1.Open();
            com2.Open();

            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
            com1.PinChanged += pinChangedEventHandler.HandleEvent;
            com1.DataReceived += receivedEventHandler.HandleEvent;
            com1.ErrorReceived += errorEventHandler.HandleEvent;

            //This should cause ErrorEvent to be fired with a parity error since the 
            //8th bit on com1 is the parity bit, com1 one expest this bit to be 1(Mark), 
            //and com2 is writing 0 for this bit
            com1.DataBits = 7;
            com1.Parity = Parity.Mark;
            com2.BaseStream.Write(new byte[1], 0, 1);
            Debug.Print("ERROREvent Triggered");
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            //This should cause PinChangedEvent to be fired with SerialPinChanges.DsrChanged
            //since we are setting DtrEnable to true
            com2.DtrEnable = true;
            Debug.WriteLine("PinChange Triggered");
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            //This should cause ReceivedEvent to be fired with ReceivedChars
            //since we are writing some bytes
            com1.DataBits = 8;
            com1.Parity = Parity.None;
            com2.BaseStream.Write(new byte[] {40}, 0, 1);
            Debug.WriteLine("RxEvent Triggered");
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            //This should cause a frame error since the 8th bit is not set,
            //and com1 is set to 7 data bits so the 8th bit will +12v where
            //com1 expects the stop bit at the 8th bit to be -12v
            com1.DataBits = 7;
            com1.Parity = Parity.None;
            com2.BaseStream.Write(new byte[] {0x01}, 0, 1);
            Debug.WriteLine("FrameError Triggered");
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            //This should cause PinChangedEvent to be fired with SerialPinChanges.CtsChanged
            //since we are setting RtsEnable to true
            com2.RtsEnable = true;
            Debug.WriteLine("PinChange Triggered");
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            //This should cause ReceivedEvent to be fired with EofReceived 
            //since we are writing the EOF char		
            com1.DataBits = 8;
            com1.Parity = Parity.None;
            com2.BaseStream.Write(new byte[] {26}, 0, 1);
            Debug.WriteLine("RxEOF Triggered");
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            //This should cause PinChangedEvent to be fired with SerialPinChanges.Break
            //since we are setting BreakState to true
            com2.BreakState = true;
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            bool threadFound = true;
            Stopwatch sw = Stopwatch.StartNew();
            while (threadFound && sw.ElapsedMilliseconds < MAX_TIME_WAIT)
            {
                threadFound = false;

                for (int i = 0; i < MAX_TIME_WAIT / iterationWaitTime; ++i)
                {
                    Debug.WriteLine("Event counts: PinChange {0}, Rx {1}, error {2}", numPinChangedEvents, numReceivedEvents, numErrorEvents);

                    Debug.WriteLine("Waiting for pinchange event {0}ms", iterationWaitTime);

                    if (pinChangedEventHandler.WaitForEvent(iterationWaitTime, numPinChangedEvents + 1))
                    {
                        // A thread is in PinChangedEvent: verify that it is not in any other handler at the same time
                        if (receivedEventHandler.NumEventsHandled != numReceivedEvents)
                        {
                            Fail("Err_191818ahied A thread is in PinChangedEvent and ReceivedEvent");
                        }

                        if (errorEventHandler.NumEventsHandled != numErrorEvents)
                        {
                            Fail("Err_198119hjaheid A thread is in PinChangedEvent and ErrorEvent");
                        }

                        ++numPinChangedEvents;
                        pinChangedEventHandler.ResumeHandleEvent();
                        threadFound = true;
                        break;
                    }

                    Debug.WriteLine("Waiting for rx event {0}ms", iterationWaitTime);

                    if (receivedEventHandler.WaitForEvent(iterationWaitTime, numReceivedEvents + 1))
                    {
                        // A thread is in ReceivedEvent: verify that it is not in any other handler at the same time
                        if (pinChangedEventHandler.NumEventsHandled != numPinChangedEvents)
                        {
                            Fail("Err_2288ajed A thread is in ReceivedEvent and PinChangedEvent");
                        }

                        if (errorEventHandler.NumEventsHandled != numErrorEvents)
                        {
                            Fail("Err_25158ajeiod A thread is in ReceivedEvent and ErrorEvent");
                        }

                        ++numReceivedEvents;
                        receivedEventHandler.ResumeHandleEvent();
                        threadFound = true;
                        break;
                    }

                    Debug.WriteLine("Waiting for error event {0}ms", iterationWaitTime);

                    if (errorEventHandler.WaitForEvent(iterationWaitTime, numErrorEvents + 1))
                    {
                        // A thread is in ErrorEvent: verify that it is not in any other handler at the same time
                        if (pinChangedEventHandler.NumEventsHandled != numPinChangedEvents)
                        {
                            Fail("Err_01208akiehd A thread is in ErrorEvent and PinChangedEvent");
                        }

                        if (receivedEventHandler.NumEventsHandled != numReceivedEvents)
                        {
                            Fail("Err_1254847ajied A thread is in ErrorEvent and ReceivedEvent");
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
                Fail("Err_2288ajied Expected 3 PinChangedEvents to be fired and only {0} occured",
                    pinChangedEventHandler.NumEventsHandled);
            }
            
            if (!receivedEventHandler.WaitForEvent(MAX_TIME_WAIT, 2))
            {
                Fail("Err_122808aoeid Expected 2 ReceivedEvents  to be fired and only {0} occured",
                    receivedEventHandler.NumEventsHandled);
            }
            
            if (!errorEventHandler.WaitForEvent(MAX_TIME_WAIT, 2))
            {
                Fail("Err_215887ajeid Expected 3 ErrorEvents to be fired and only {0} occured",
                    errorEventHandler.NumEventsHandled);
            }

            //[] Verify all PinChangedEvents should have occured
            pinChangedEventHandler.Validate(SerialPinChange.DsrChanged, 0);
            pinChangedEventHandler.Validate(SerialPinChange.CtsChanged, 0);
            pinChangedEventHandler.Validate(SerialPinChange.Break, 0);

            //[] Verify all ReceivedEvent should have occured
            receivedEventHandler.Validate(SerialData.Chars, 0);
            receivedEventHandler.Validate(SerialData.Eof, 0);
            
            //[] Verify all ErrorEvents should have occured
            errorEventHandler.Validate(SerialError.RXParity, 0);
            errorEventHandler.Validate(SerialError.Frame, 0);
            
            // It's important that we close com1 BEFORE com2 (the using() block would do this the other way around normally)
            // This is because we have our special blocking event handlers hooked onto com1, and closing com2 is likely to 
            // cause a pin-change event which then hangs and prevents com1 from closing.
            // An alternative approach would be to unhook all the event-handlers before leaving the using() block.
            com1.Close();
        }
    }

    [OuterLoop("Slow Test")]
    [ConditionalFact(nameof(HasNullModem))]
    public void Thread_In_PinChangedEvent()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            PinChangedEventHandler pinChangedEventHandler = new PinChangedEventHandler(com1, false, true);

            Debug.WriteLine(
                "Verifying that if a thread is blocked in a PinChangedEvent handler the port can still be closed");

            com1.Open();
            com2.Open();

            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
            com1.PinChanged += pinChangedEventHandler.HandleEvent;

            //This should cause PinChangedEvent to be fired with SerialPinChanges.DsrChanged
            //since we are setting DtrEnable to true
            com2.DtrEnable = true;

            if (!pinChangedEventHandler.WaitForEvent(MAX_TIME_WAIT, 1))
            {
                Fail("Err_32688ajoid Expected 1 PinChangedEvents to be fired and only {0} occured",
                    pinChangedEventHandler.NumEventsHandled);
            }

            Task task = Task.Run(() => com1.Close());
            Thread.Sleep(5000);

            pinChangedEventHandler.ResumeHandleEvent();

            Assert.True(task.Wait(2000), "Waiting for Close task completion");
        }
    }

    [OuterLoop("Slow Test")]
    [ConditionalFact(nameof(HasNullModem))]
    public void Thread_In_ReceivedEvent()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            ReceivedEventHandler receivedEventHandler = new ReceivedEventHandler(com1, false, true);

            Debug.WriteLine(
                "Verifying that if a thread is blocked in a RecevedEvent handler the port can still be closed");

            com1.Open();
            com2.Open();

            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
            com1.DataReceived += receivedEventHandler.HandleEvent;

            //This should cause ReceivedEvent to be fired with ReceivedChars
            //since we are writing some bytes
            com1.DataBits = 8;
            com1.Parity = Parity.None;
            com2.BaseStream.Write(new byte[] {40}, 0, 1);
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            if (!receivedEventHandler.WaitForEvent(MAX_TIME_WAIT, 1))
            {
                Fail("Err_122808aoeid Expected 1 ReceivedEvents  to be fired and only {0} occured",
                    receivedEventHandler.NumEventsHandled);
            }

            Task task = Task.Run(() => com1.Close());
            Thread.Sleep(5000);

            receivedEventHandler.ResumeHandleEvent();

            Assert.True(task.Wait(2000), "Waiting for Close task completion");
        }
    }

    [OuterLoop("Slow Test")]
    [ConditionalFact(nameof(HasNullModem))]
    public void Thread_In_ErrorEvent()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            ErrorEventHandler errorEventHandler = new ErrorEventHandler(com1, false, true);

            Debug.WriteLine("Verifying that if a thread is blocked in a ErrorEvent handler the port can still be closed");

            com1.Open();
            com2.Open();

            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);
            com1.ErrorReceived += errorEventHandler.HandleEvent;

            //This should cause ErrorEvent to be fired with a parity error since the 
            //8th bit on com1 is the parity bit, com1 one expest this bit to be 1(Mark), 
            //and com2 is writing 0 for this bit
            com1.DataBits = 7;
            com1.Parity = Parity.Mark;
            com2.BaseStream.Write(new byte[1], 0, 1);
            Thread.Sleep(TRIGERING_EVENTS_WAIT_TIME);

            if (!errorEventHandler.WaitForEvent(MAX_TIME_WAIT, 1))
            {
                Fail("Err_215887ajeid Expected 1 ErrorEvents to be fired and only {0} occured",errorEventHandler.NumEventsHandled);
            }

            Task task = Task.Run(() => com1.Close());
            Thread.Sleep(5000);

            errorEventHandler.ResumeHandleEvent();
            Assert.True(task.Wait(2000), "Waiting for Close task completion");
        }
    }
    #endregion

    #region Verification for Test Cases

    private class IgnoreException : Exception
    {
        public IgnoreException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Base class for all test event handlers
    /// </summary>
    /// <typeparam name="T">The type of the EventType object which is passed to the event handler</typeparam>
    private class TestEventHandler<T>
    {
        private readonly List<T> _eventTypes = new List<T>();
        private readonly List<int> _bytesToRead = new List<int>();
        private readonly List<SerialPort> _sources = new List<SerialPort>();
        private readonly SerialPort _com;
        private readonly bool _shouldThrow;
        private readonly bool _shouldWait;
        private readonly AutoResetEvent _eventHandlerWait = new AutoResetEvent(false);
        private readonly object _lock = new object();

        public int NumEventsHandled { get; private set; }

        protected TestEventHandler(SerialPort com, bool shouldThrow, bool shouldWait)
        {
            if (shouldThrow && shouldWait)
            {
                throw new ArgumentException("shouldThrow and shouldWait can not both be true");
            }

            _com = com;
            _shouldThrow = shouldThrow;
            _shouldWait = shouldWait;
        }

        protected void HandleEvent(object source, T eventType)
        {
            int bytesToRead = _com.BytesToRead;

            lock (_lock)
            {
                _bytesToRead.Add(bytesToRead);
                _eventTypes.Add(eventType);
                _sources.Add((SerialPort)source);

                NumEventsHandled++;
                Monitor.Pulse(_lock);
            }

            if (_shouldThrow)
            {
                throw new IgnoreException("I was told to throw");
            }

            if (_shouldWait)
            {
                Assert.True(_eventHandlerWait.WaitOne(10000));
            }
        }

        public void ResumeHandleEvent()
        {
            _eventHandlerWait.Set();
        }

        private void RemoveAt(int index)
        {
            lock (_lock)
            {
                _eventTypes.RemoveAt(index);
                _bytesToRead.RemoveAt(index);
                _sources.RemoveAt(index);
                NumEventsHandled--;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _eventTypes.Clear();
                _bytesToRead.Clear();
                _sources.Clear();

                NumEventsHandled = 0;
            }
        }

        public bool WaitForEvent(int maxMilliseconds, int totalNumberOfEvents)
        {
            Stopwatch sw = new Stopwatch();

            lock (_lock)
            {
                sw.Start();
                long remaining;
                while ((remaining = (maxMilliseconds - sw.ElapsedMilliseconds)) > 0 && NumEventsHandled < totalNumberOfEvents)
                {
                    Monitor.Wait(_lock, (int)remaining);
                }
                return totalNumberOfEvents <= NumEventsHandled;
            }
        }

        // Since we can not garantee the order or the exact time that the event handler is called 
        // We will look for an event that was fired that matches the type and that bytesToRead 
        // is greater then the parameter    
        public void Validate(T eventType, int bytesToRead)
        {
            lock (_lock)
            {
                for (int i = 0; i < _eventTypes.Count; i++)
                {
                    if (Equals(eventType, _eventTypes[i]) && bytesToRead <= _bytesToRead[i] && _sources[i] == _com)
                    {
                        RemoveAt(i);
                        return;
                    }
                }
            }
            Assert.True(false, $"Failed to validate event type {eventType}");
        }

        public int NumberOfOccurencesOfType(T eventType)
        {
            lock (_lock)
            {
                return _eventTypes.Count(et => Equals(et, eventType));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _eventTypes.Count; i++)
            {
                sb.Append(i);
                sb.Append(": Type: ");
                sb.Append(_eventTypes[i]);
                sb.Append(" BytesToRead: ");
                sb.Append(_eventTypes[i]);
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }

    private class PinChangedEventHandler : TestEventHandler<SerialPinChange>
    {
        public PinChangedEventHandler(SerialPort com, bool shouldThrow, bool shouldWait) : base(com, shouldThrow, shouldWait)
        {
        }

        public void HandleEvent(object source, SerialPinChangedEventArgs e)
        {
            HandleEvent(source, e.EventType);
        }
    }

    private class ErrorEventHandler : TestEventHandler<SerialError>
    { 
        public ErrorEventHandler(SerialPort com, bool shouldThrow, bool shouldWait) : base(com, shouldThrow, shouldWait)
        {
        }

        public void HandleEvent(object source, SerialErrorReceivedEventArgs e)
        {
            HandleEvent(source, e.EventType);
        }
    }

    private class ReceivedEventHandler : TestEventHandler<SerialData>
    {
        public ReceivedEventHandler(SerialPort com, bool shouldThrow, bool shouldWait) : base(com, shouldThrow, shouldWait)
        {
        }

        public void HandleEvent(object source, SerialDataReceivedEventArgs e)
        {
            HandleEvent(source, e.EventType);
        }
    }
    #endregion
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class PinChangedEvent : PortsTest
    {
        // Maximum time to wait for all of the expected events to be fired
        private const int MAX_TIME_WAIT = 1000;
        private const int NUM_TRYS = 5;

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void PinChangedEvent_CtsChanged()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                PinChangedEventHandler eventHandler = new PinChangedEventHandler(com1);

                Debug.WriteLine("Verifying CtsChanged event");

                com1.PinChanged += eventHandler.HandleEvent;
                com1.Open();
                com2.Open();

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    Debug.WriteLine("Verifying when RtsEnable set to true on remote port try: {0}", i);

                    com2.RtsEnable = true;
                    Assert.True(eventHandler.WaitForEvent(MAX_TIME_WAIT, 1), "Initial event missing");

                    eventHandler.Validate(SerialPinChange.CtsChanged, 0);

                    Assert.Equal(0, eventHandler.NumEventsHandled);

                    com2.RtsEnable = false;
                    eventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                    eventHandler.Validate(SerialPinChange.CtsChanged, 0);

                    Assert.Equal(0, eventHandler.NumEventsHandled);
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void PinChangedEvent_DsrChanged()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                PinChangedEventHandler eventHandler = new PinChangedEventHandler(com1);

                // Some null-modem cables have a connection between CD and CSR/CTR, so we need to discard CDChanged events
                eventHandler.EventFilter = eventType => eventType != SerialPinChange.CDChanged;

                Debug.WriteLine("Verifying DsrChanged event");
                com1.PinChanged += eventHandler.HandleEvent;
                com1.Open();
                com2.Open();

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    Debug.WriteLine("Verifying when DtrEnable set to true on remote port {0}", i);

                    com2.DtrEnable = true;
                    Assert.True(eventHandler.WaitForEvent(MAX_TIME_WAIT, 1), "Initial event missing");

                    eventHandler.Validate(SerialPinChange.DsrChanged, 0);

                    Assert.Equal(0, eventHandler.NumEventsHandled);

                    com2.DtrEnable = false;
                    eventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                    eventHandler.Validate(SerialPinChange.DsrChanged, 0);

                    Assert.Equal(0, eventHandler.NumEventsHandled);
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void PinChangedEvent_Break()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                PinChangedEventHandler eventHandler = new PinChangedEventHandler(com1);

                Debug.WriteLine("Verifying Break event");

                com1.PinChanged += eventHandler.HandleEvent;
                com1.Open();
                com2.Open();

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    Debug.WriteLine("Verifying when Break set to true on remote port try: {0}", i);

                    com2.BreakState = true;
                    Assert.True(eventHandler.WaitForEvent(MAX_TIME_WAIT, 1), "Initial event missing");

                    eventHandler.Validate(SerialPinChange.Break, 0);

                    Assert.Equal(0, eventHandler.NumEventsHandled);

                    com2.BreakState = false;
                    // This will always time-out, because we don't expect an event here
                    eventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                    Assert.Equal(0, eventHandler.NumEventsHandled);
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void PinChangedEvent_Multiple()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                PinChangedEventHandler eventHandler = new PinChangedEventHandler(com1);

                // Some null-modem cables have a connection between CD and CSR/CTR, so we need to discard CDChanged events
                eventHandler.EventFilter = eventType => eventType != SerialPinChange.CDChanged;

                SerialPinChangedEventHandler pinchangedEventHandler = eventHandler.HandleEvent;

                Debug.WriteLine("Verifying multiple PinChangedEvents");

                com1.PinChanged += pinchangedEventHandler;

                com1.Open();
                com2.Open();

                com2.BreakState = true;
                Thread.Sleep(100);
                com2.DtrEnable = true;
                Thread.Sleep(100);
                com2.RtsEnable = true;

                eventHandler.WaitForEvent(MAX_TIME_WAIT, 3);

                eventHandler.Validate(SerialPinChange.Break, 0);
                eventHandler.Validate(SerialPinChange.DsrChanged, 0);
                eventHandler.Validate(SerialPinChange.CtsChanged, 0);

                com1.PinChanged -= pinchangedEventHandler;
                com2.BreakState = false;
                com2.DtrEnable = false;
                com2.RtsEnable = false;
            }
        }
        #endregion

        #region Verification for Test Cases

        private class PinChangedEventHandler : TestEventHandler<SerialPinChange>
        {
            public PinChangedEventHandler(SerialPort com) : base(com, false, false)
            {
            }

            public void HandleEvent(object source, SerialPinChangedEventArgs e)
            {
                HandleEvent(source, e.EventType);
            }
        }
        #endregion
    }
}

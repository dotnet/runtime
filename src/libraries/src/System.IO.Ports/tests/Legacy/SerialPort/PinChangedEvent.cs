// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO.Ports;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

public class PinChangedEvent : PortsTest
{
    //Maximum random value to use for ReceivedBytesThreshold
    public static readonly int MAX_RND_THRESHOLD = 16;

    //Minimum random value to use for ReceivedBytesThreshold
    public static readonly int MIN_RND_THRESHOLD = 2;

    //Maximum time to wait for all of the expected events to be firered
    public static readonly int MAX_TIME_WAIT = 5000;
    public static readonly int NUM_TRYS = 5;


/*    public bool RunTest()
    {
        
        TCSupport tcSupport = new TCSupport();

        tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_CtsChanged), TCSupport.SerialPortRequirements.NullModem);
        tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_DsrChanged), TCSupport.SerialPortRequirements.NullModem);

        tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_Break), TCSupport.SerialPortRequirements.NullModem);

        tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_Multiple), TCSupport.SerialPortRequirements.NullModem);

        /*	  retValue &= tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_CDChanged), true);
              retValue &= tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_Break), true);
              retValue &= tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_Multiple), true);*/
        ///retValue &= tcSupport.BeginTestcase(new TestDelegate(PinChangedEvent_Ring), true);
        //We have no way to set change the value of this pin and thus we can not test it
//    }


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
                com2.RtsEnable = true;
                Debug.WriteLine("Verifying when RtsEnable set to true on remote port try: {0}", i);
                eventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                if (!eventHandler.Validate(SerialPinChange.CtsChanged, 0))
                {
                    Fail("Err_1351adsf!!! CtsChanged event not fired");
                }

                if (0 != eventHandler.NumEventsHandled)
                {
                    Fail("Err_4217qyza!!! unexpected event fired");
                }

                com2.RtsEnable = false;
                eventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                if (!eventHandler.Validate(SerialPinChange.CtsChanged, 0))
                {
                    Fail("Err_24597aqqoo!!! CtsChanged event not fired");
                }

                if (0 != eventHandler.NumEventsHandled)
                {
                    Fail("Err_4309714qaoya!!! unexpected event fired");
                }
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
            
            Debug.WriteLine("Verifying DsrChanged event");
            com1.PinChanged += eventHandler.HandleEvent;
            com1.Open();
            com2.Open();

            for (int i = 0; i < NUM_TRYS; i++)
            {
                com2.DtrEnable = true;

                Debug.WriteLine("Verifying when DtrEnable set to true on remote port {0}", i);
                eventHandler.WaitForEvent(MAX_TIME_WAIT, 2);

                if (!eventHandler.Validate(SerialPinChange.DsrChanged, 0))
                {
                    Fail("Err_5239aopz!!! DsrChanged event not fired");
                }

                eventHandler.Validate(SerialPinChange.CDChanged, 0);
                    //This is necessary becuase DSR pin is connected CTS pin in my null cable

                Assert.Equal(0, eventHandler.NumEventsHandled);

                com2.DtrEnable = false;
                eventHandler.WaitForEvent(MAX_TIME_WAIT, 2);

                if (!eventHandler.Validate(SerialPinChange.DsrChanged, 0))
                {
                    Fail("Err_1520qhoa!!! DsrChanged event not fired");
                }

                eventHandler.Validate(SerialPinChange.CDChanged, 0);
                //This is necessary becuase DSR pin is connected CTS pin in my null cable

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
                com2.BreakState = true;
                Debug.WriteLine("Verifying when Break set to true on remote port try: {0}", i);
                eventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                if (!eventHandler.Validate(SerialPinChange.Break, 0))
                {
                    Fail("Err_67894ahlead!!! Break event not fired");
                }

                Assert.Equal(0, eventHandler.NumEventsHandled);

                com2.BreakState = false;
                eventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                Assert.Equal(0, eventHandler.NumEventsHandled);
            }
        }
    }
    /*
        public bool PinChangedEvent_CDChanged()
        {
            SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
            SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
            PinChangedEventHandler eventHandler = new PinChangedEventHandler(com1);
            
            int elapsedTime;

            Debug.WriteLine("Verifying CDChanged event");

            com1.PinChangedEvent += new SerialPinChangedEventHandler(eventHandler.HandleEvent);

            com1.Open();
            com2.Open();
            com2.DtrEnable = true;
            elapsedTime = 0;

            Debug.WriteLine("Verifying when DtrEnable set to true on remote port");
            while(1 > eventHandler.NumEventsHandled && elapsedTime < MAX_TIME_WAIT) {
                System.Threading.Thread.Sleep(ITERATION_TIME_WAIT);
                elapsedTime += ITERATION_TIME_WAIT;
            }

            retValue &= eventHandler.Validate(SerialPinChange.CDChanged, 0, 0);

            eventHandler.Clear();
            com2.DtrEnable = false;
            elapsedTime = 0;

            Debug.WriteLine("Verifying when DtrEnable set to false on remote port");
            while(1 > eventHandler.NumEventsHandled && elapsedTime < MAX_TIME_WAIT) {
                System.Threading.Thread.Sleep(ITERATION_TIME_WAIT);
                elapsedTime += ITERATION_TIME_WAIT;
            }

            retValue &= eventHandler.Validate(SerialPinChange.CDChanged, 0, 0);	  

            if(!retValue) {
                Debug.WriteLine("Err_003!!! Verifying CDChanged event FAILED");
            }

            if(com1.IsOpen)
                com1.Close();

            if(com2.IsOpen)
                com2.Close();      

            return retValue;  
        }

        public bool PinChangedEvent_Break()
        {
            SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
            SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
            PinChangedEventHandler eventHandler = new PinChangedEventHandler(com1);
            
            int elapsedTime;

            Debug.WriteLine("Verifying Break event");

            com1.PinChangedEvent += new SerialPinChangedEventHandler(eventHandler.HandleEvent);

            com1.Open();
            com2.Open();
            com2.BreakState = true;
            elapsedTime = 0;

            Debug.WriteLine("Verifying when Break set to true on remote port");
            while(1 > eventHandler.NumEventsHandled && elapsedTime < MAX_TIME_WAIT) {
                System.Threading.Thread.Sleep(ITERATION_TIME_WAIT);
                elapsedTime += ITERATION_TIME_WAIT;
            }

            retValue &= eventHandler.Validate(SerialPinChange.Break, 0, 0);

            if(!retValue) {
                Debug.WriteLine("Err_004!!! Verifying Break event FAILED");
            }

            if(com1.IsOpen)
                com1.Close();

            if(com2.IsOpen)
                com2.Close();      

            return retValue;  
        }
    */

    [ConditionalFact(nameof(HasNullModem))]
    public void PinChangedEvent_Multiple()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            PinChangedEventHandler eventHandler = new PinChangedEventHandler(com1);
            SerialPinChangedEventHandler pinchangedEventHandler = eventHandler.HandleEvent;

            Debug.WriteLine("Verifying multiple PinChangedEvents");

            com1.PinChanged += pinchangedEventHandler;

            com1.Open();
            com2.Open();

            com2.BreakState = true;
            System.Threading.Thread.Sleep(100);
            com2.DtrEnable = true;
            System.Threading.Thread.Sleep(100);
            com2.RtsEnable = true;

            //On my machine it looks like the CDChanged event is getting fired as
            //well as the three that we expect here. So we will wait until 4 events 
            //are fired. If other machines do not fire this event the test will 
            //still pass we will just wait MAX_TIME_WAITms.
            eventHandler.WaitForEvent(MAX_TIME_WAIT, 4);

            if (!eventHandler.Validate(SerialPinChange.Break, 0))
            {
                Fail("Verifying Break State FAILED");
            }

            if (!eventHandler.Validate(SerialPinChange.DsrChanged, 0))
            {
                Fail("Verifying DsrChanged FAILED");
            }

            if (!eventHandler.Validate(SerialPinChange.CtsChanged, 0))
            {
                Fail("Verifying CtsCahnged FAILED");
            }

            com1.PinChanged -= pinchangedEventHandler;
            com2.BreakState = false;
            com2.DtrEnable = false;
            com2.RtsEnable = false;
        }
    }
    #endregion

    #region Verification for Test Cases
    public class PinChangedEventHandler
    {
        public System.Collections.ArrayList EventType;
        public System.Collections.ArrayList BytesToRead;
        public System.Collections.ArrayList Source;
        public int NumEventsHandled;
        private SerialPort _com;


        public PinChangedEventHandler(SerialPort com)
        {
            _com = com;
            NumEventsHandled = 0;
            EventType = new System.Collections.ArrayList();
            BytesToRead = new System.Collections.ArrayList();
            Source = new System.Collections.ArrayList();
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


        public void WaitForEvent(int maxMilliseconds, int totalNumberOfEvents)
        {
            Stopwatch sw = new Stopwatch();

            lock (this)
            {
                sw.Start();

                while (maxMilliseconds > sw.ElapsedMilliseconds && NumEventsHandled < totalNumberOfEvents)
                {
                    System.Threading.Monitor.Wait(this, (int)(maxMilliseconds - sw.ElapsedMilliseconds));
                }

                // TODO: Consider changing this to  totalNumberOfEvents <= NumEventsHandled
                Assert.Equal(totalNumberOfEvents,NumEventsHandled);
            }
        }

        // Since we can not garantee the order or the exact time that the event handler is called 
        // We will look for an event that was fired that matches the type and that bytesToRead 
        // is greater then the parameter    
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
    #endregion
}


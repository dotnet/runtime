// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class ReceivedBytesThreshold_Property : PortsTest
    {
        //Maximum random value to use for ReceivedBytesThreshold
        private const int MAX_RND_THRESHOLD = 16;

        //Minimum random value to use for ReceivedBytesThreshold
        private const int MIN_RND_THRESHOLD = 2;

        //Maximum time to wait for all of the expected events to be firered
        private const int MAX_TIME_WAIT = 2000;

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
                SerialPortProperties serPortProp = new SerialPortProperties();

                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying default ReceivedBytesThreshold");

                com2.Write(new byte[1], 0, 1);

                rcvEventHandler.WaitForEvent(SerialData.Chars, MAX_TIME_WAIT);

                com1.DiscardInBuffer();

                serPortProp.VerifyPropertiesAndPrint(com1);
                rcvEventHandler.Validate(SerialData.Chars, 1, 0);
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Rnd_ExactWrite()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                const int thresh = 8;

                com1.ReceivedBytesThreshold = thresh;
                com1.ReadTimeout = 200;
                com1.Open();
                com2.Open();

                int timesCalled = 0;
                com1.DataReceived += (s, args) => {
                    if (args.EventType == SerialData.Chars)
                        timesCalled++;
                };

                com2.Write(new byte[thresh], 0, thresh);
                Thread.Sleep(200);
                com1.Read(new byte[thresh], 0, thresh);
                Assert.Equal(1, timesCalled);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Rnd_MultipleWrite()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
                SerialPortProperties serPortProp = new SerialPortProperties();

                Random rndGen = new Random(-55);
                int receivedBytesThreshold = rndGen.Next(MIN_RND_THRESHOLD, MAX_RND_THRESHOLD);

                com1.ReceivedBytesThreshold = receivedBytesThreshold;
                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("ReceivedBytesThreshold", receivedBytesThreshold);
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying writing the number of bytes of ReceivedBytesThreshold after several write calls");

                com2.Write(new byte[(int)Math.Floor(com1.ReceivedBytesThreshold / 2.0)], 0,
                    (int)Math.Floor(com1.ReceivedBytesThreshold / 2.0));
                com2.Write(new byte[(int)Math.Ceiling(com1.ReceivedBytesThreshold / 2.0)], 0,
                    (int)Math.Ceiling(com1.ReceivedBytesThreshold / 2.0));

                rcvEventHandler.WaitForEvent(SerialData.Chars, MAX_TIME_WAIT);

                com1.DiscardInBuffer();

                serPortProp.VerifyPropertiesAndPrint(com1);
                rcvEventHandler.Validate(SerialData.Chars, com1.ReceivedBytesThreshold, 0);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Above_Exact()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
                SerialPortProperties serPortProp = new SerialPortProperties();

                Random rndGen = new Random(-55);
                int receivedBytesThreshold = rndGen.Next(MIN_RND_THRESHOLD, MAX_RND_THRESHOLD);

                com1.ReceivedBytesThreshold = receivedBytesThreshold + 1;
                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("ReceivedBytesThreshold", receivedBytesThreshold);
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying writing less then number of bytes of ReceivedBytesThreshold then setting " +
                                "ReceivedBytesThreshold to the number of bytes written");

                com2.Write(new byte[receivedBytesThreshold], 0, receivedBytesThreshold);

                TCSupport.WaitForReadBufferToLoad(com1, receivedBytesThreshold);

                if (0 != rcvEventHandler.NumEventsHandled)
                {
                    Fail("ERROR!!! Unexpected ReceivedEvent was fired NumEventsHandled={0}", rcvEventHandler.NumEventsHandled);
                }
                else
                {
                    com1.ReceivedBytesThreshold = receivedBytesThreshold;

                    rcvEventHandler.WaitForEvent(SerialData.Chars, MAX_TIME_WAIT);
                }

                com1.DiscardInBuffer();

                serPortProp.VerifyPropertiesAndPrint(com1);
                rcvEventHandler.Validate(SerialData.Chars, com1.ReceivedBytesThreshold, 0);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Above_Below()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
                SerialPortProperties serPortProp = new SerialPortProperties();

                Random rndGen = new Random(-55);
                int receivedBytesThreshold = rndGen.Next(MIN_RND_THRESHOLD, MAX_RND_THRESHOLD);

                com1.ReceivedBytesThreshold = receivedBytesThreshold + 1;
                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("ReceivedBytesThreshold", receivedBytesThreshold - 1);
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying writing less then number of bytes of ReceivedBytesThreshold then setting " +
                                "ReceivedBytesThreshold to less then the number of bytes written");

                com2.Write(new byte[receivedBytesThreshold], 0, receivedBytesThreshold);

                TCSupport.WaitForReadBufferToLoad(com1, receivedBytesThreshold);

                if (0 != rcvEventHandler.NumEventsHandled)
                {
                    Fail("ERROR!!! Unexpected ReceivedEvent was firered NumEventsHandled={0}", rcvEventHandler.NumEventsHandled);
                }
                else
                {
                    com1.ReceivedBytesThreshold = receivedBytesThreshold - 1;

                    rcvEventHandler.WaitForEvent(SerialData.Chars, MAX_TIME_WAIT);
                }

                com1.DiscardInBuffer();

                serPortProp.VerifyPropertiesAndPrint(com1);
                rcvEventHandler.Validate(SerialData.Chars, com1.ReceivedBytesThreshold, 0);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Above_1()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);
                SerialPortProperties serPortProp = new SerialPortProperties();

                Random rndGen = new Random(-55);
                int receivedBytesThreshold = rndGen.Next(MIN_RND_THRESHOLD, MAX_RND_THRESHOLD);

                com1.ReceivedBytesThreshold = receivedBytesThreshold + 1;
                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("ReceivedBytesThreshold", 1);
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying writing less then number of bytes of ReceivedBytesThreshold then " +
                                "setting ReceivedBytesThreshold to 1");

                com2.Write(new byte[receivedBytesThreshold], 0, receivedBytesThreshold);

                TCSupport.WaitForReadBufferToLoad(com1, receivedBytesThreshold);

                if (0 != rcvEventHandler.NumEventsHandled)
                {
                    Fail("ERROR!!! Unexpected ReceivedEvent was firered NumEventsHandled={0}", rcvEventHandler.NumEventsHandled);
                }
                else
                {
                    com1.ReceivedBytesThreshold = 1;
                    rcvEventHandler.WaitForEvent(SerialData.Chars, MAX_TIME_WAIT);
                }

                com1.DiscardInBuffer();

                serPortProp.VerifyPropertiesAndPrint(com1);
                rcvEventHandler.Validate(SerialData.Chars, com1.ReceivedBytesThreshold, 0);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue ReceivedBytesThreshold");
            VerifyException(int.MinValue, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_Neg1()
        {
            Debug.WriteLine("Verifying -1 ReceivedBytesThreshold");
            VerifyException(-1, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedBytesThreshold_0()
        {
            Debug.WriteLine("Verifying 0 ReceivedBytesThreshold");
            VerifyException(0, typeof(ArgumentOutOfRangeException));
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyException(int receivedBytesThreshold, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, receivedBytesThreshold, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, receivedBytesThreshold, expectedException);
            }
        }

        private void VerifyExceptionAtOpen(SerialPort com, int receivedBytesThreshold, Type expectedException)
        {
            int origReceivedBytesThreshold = com.ReceivedBytesThreshold;

            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.ReceivedBytesThreshold = receivedBytesThreshold;

                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);

            com.ReceivedBytesThreshold = origReceivedBytesThreshold;
        }


        private void VerifyExceptionAfterOpen(SerialPort com, int receivedBytesThreshold, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.ReceivedBytesThreshold = receivedBytesThreshold;

                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the ReceivedBytesThreshold after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the ReceivedBytesThreshold after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the ReceivedBytesThreshold after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }
            serPortProp.VerifyPropertiesAndPrint(com);
        }

        private class ReceivedEventHandler
        {
            private readonly List<SerialData> _eventType = new List<SerialData>();
            private readonly List<SerialPort> Source = new List<SerialPort>();
            private readonly List<int> _bytesToRead = new List<int>();
            public int NumEventsHandled;
            private readonly SerialPort _com;

            public ReceivedEventHandler(SerialPort com)
            {
                _com = com;
                NumEventsHandled = 0;
            }

            public void HandleEvent(object source, SerialDataReceivedEventArgs e)
            {
                lock (this)
                {
                    try
                    {
                        _bytesToRead.Add(_com.BytesToRead);
                        _eventType.Add(e.EventType);
                        Source.Add((SerialPort)source);

                        NumEventsHandled++;
                    }
                    catch (Exception exp)
                    {
                        Debug.WriteLine(exp);
                        Debug.WriteLine(exp.StackTrace);
                    }
                    Monitor.Pulse(this);
                }
            }

            public void Validate(SerialData eventType, int bytesToRead, int eventIndex)
            {
                lock (this)
                {
                    if (eventIndex >= NumEventsHandled)
                    {
                        Fail("ERROR!!! Expected EvenIndex={0} is greater then the number of events handled {1}", eventIndex, NumEventsHandled);
                    }

                    Assert.Equal(eventType, _eventType[eventIndex]);

                    if (bytesToRead > _bytesToRead[eventIndex])
                    {
                        Fail("ERROR!!! Expected BytesToRead={0} actual={1}", bytesToRead, _bytesToRead[eventIndex]);
                    }

                    if (_com != Source[eventIndex])
                    {
                        Fail("ERROR!!! Expected {0} source actual={1}", _com.BaseStream, Source[eventIndex]);
                    }
                }
            }

            public void WaitForEvent(SerialData eventType, int timeout)
            {
                WaitForEvent(eventType, 1, timeout);
            }

            public void WaitForEvent(SerialData eventType, int numEvents, int timeout)
            {
                lock (this)
                {
                    if (EventExists(eventType, numEvents))
                    {
                        return;
                    }

                    Stopwatch sw = new Stopwatch();

                    sw.Start();
                    do
                    {
                        Monitor.Wait(this, (int)(timeout - sw.ElapsedMilliseconds));

                        if (EventExists(eventType, numEvents))
                        {
                            return;
                        }
                    } while (sw.ElapsedMilliseconds < timeout);

                    sw.Stop();
                }

                Fail("Event wait timeout ({0})", eventType);
            }

            public bool EventExists(SerialData eventType, int numEvents)
            {
                int numOccurrences = 0;

                for (int i = 0; i < NumEventsHandled; i++)
                {
                    if (eventType == _eventType[i])
                    {
                        numOccurrences++;
                    }
                }

                return numOccurrences >= numEvents;
            }
        }
        #endregion
    }
}

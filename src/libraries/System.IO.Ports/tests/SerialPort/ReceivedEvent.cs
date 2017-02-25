// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class ReceivedEvent : PortsTest
    {
        //Maximum time to wait for all of the expected events to be firered
        private const int MAX_TIME_WAIT = 500;
        private const int NUM_TRYS = 5;

        #region Test Cases
        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedEvent_Chars()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);

                Debug.WriteLine("Verifying ReceivedChars event");

                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    com2.Write(new byte[com1.ReceivedBytesThreshold], 0, com1.ReceivedBytesThreshold);
                    rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                    rcvEventHandler.Validate(SerialData.Chars, com1.ReceivedBytesThreshold);

                    if (0 != rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Eof))
                    {
                        Fail("Err_21087qpua!!! Unexpected EofReceived event fireed {0}", i);
                    }

                    if (0 != rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Chars))
                    {
                        Fail("Err_32417!!! Unexpected EofReceived event fireed {0}", i);
                    }

                    com1.DiscardInBuffer();
                }
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedEvent_Eof()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);

                byte[] xmitBytes = new byte[1];

                Debug.WriteLine("Verifying EofReceived event");
                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                //EOF char
                xmitBytes[0] = 26;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    com2.Write(xmitBytes, 0, xmitBytes.Length);
                    rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, 2);

                    rcvEventHandler.Validate(SerialData.Eof, i);

                    rcvEventHandler.Validate(SerialData.Chars, i + com1.ReceivedBytesThreshold);

                    if (0 != rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Eof))
                    {
                        Fail("Err_01278qaods!!! Unexpected EofReceived event fireed {0}", i);
                    }

                    if (1 < rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Chars))
                    {
                        Fail("Err_2972qoypa!!! Unexpected ReceivedChars event fireed {0}", i);
                    }
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedEvent_CharsEof()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReceivedEventHandler rcvEventHandler = new ReceivedEventHandler(com1);

                byte[] xmitBytes = new byte[3];

                Debug.WriteLine("Verifying EofReceived event");

                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                //EOF char
                xmitBytes[0] = 56;
                xmitBytes[1] = 26;
                xmitBytes[2] = 55;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    com2.Write(xmitBytes, 0, xmitBytes.Length);
                    rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, SerialData.Eof);

                    rcvEventHandler.Validate(SerialData.Eof, i * xmitBytes.Length);

                    rcvEventHandler.Validate(SerialData.Chars, (i * xmitBytes.Length) + com1.ReceivedBytesThreshold);

                    if (0 != rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Eof))
                    {
                        Fail("Err_20712asdfhow!!! Unexpected EofReceived event fired {0} iteration:{1}",
                            rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Eof), i);
                    }

                    rcvEventHandler.Clear();
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReceivedEvent_CharsEof_ReadAllChars()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ReadInReceivedEventHandler rcvEventHandler = new ReadInReceivedEventHandler(com1);
                byte[] xmitBytes = new byte[3];

                Debug.WriteLine(
                    "Verifying EofReceived and ReceivedChars events where all chars are read in the ReceivedChars event");

                com1.Open();
                com2.Open();
                com1.DataReceived += rcvEventHandler.HandleEvent;

                //EOF char
                xmitBytes[0] = 56;
                xmitBytes[1] = 26;
                xmitBytes[2] = 55;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    com2.Write(xmitBytes, 0, xmitBytes.Length);
                    rcvEventHandler.WaitForEvent(MAX_TIME_WAIT, SerialData.Eof);

                    rcvEventHandler.Validate(SerialData.Eof, 0);

                    rcvEventHandler.Validate(SerialData.Chars, 1);

                    if (0 != rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Eof))
                    {
                        Fail("Err_20712asdfhow!!! Unexpected EofReceived event fired {0} iteration:{1}",
                            rcvEventHandler.NumberOfOccurrencesOfType(SerialData.Eof), i);
                    }

                    rcvEventHandler.Clear();
                }

                if (rcvEventHandler.NumBytesRead != NUM_TRYS * xmitBytes.Length)
                {
                    Fail("Err_1298129ahnied!!! Expected to read {0} chars actually read {1}",
                        NUM_TRYS * xmitBytes.Length, rcvEventHandler.NumBytesRead);
                }
                else
                {
                    for (int i = 0; i < NUM_TRYS; ++i)
                    {
                        for (int j = 0; j < xmitBytes.Length; ++j)
                        {
                            if (xmitBytes[j] != rcvEventHandler.BytesRead[(i * xmitBytes.Length) + j])
                            {
                                Fail("Err_2829aneid Expected to Read '{0}'({0:X}) actually read {1}'({1:X})",
                                    xmitBytes[j], rcvEventHandler.BytesRead[(i * xmitBytes.Length) + j]);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Verification for Test Cases
        public class ReceivedEventHandler
        {
            public ArrayList EventType;
            public ArrayList BytesToRead;
            public ArrayList Source;
            public int NumEventsHandled;
            protected SerialPort com;

            public ReceivedEventHandler(SerialPort com)
            {
                this.com = com;
                NumEventsHandled = 0;

                EventType = new ArrayList();
                BytesToRead = new ArrayList();
                Source = new ArrayList();
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

                    Monitor.Pulse(this);
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


            public void WaitForEvent(int maxMilliseconds, int totalNumberOfEvents)
            {
                Stopwatch sw = new Stopwatch();

                lock (this)
                {
                    sw.Start();

                    while (maxMilliseconds > sw.ElapsedMilliseconds && NumEventsHandled < totalNumberOfEvents)
                    {
                        Monitor.Wait(this, (int)(maxMilliseconds - sw.ElapsedMilliseconds));
                    }

                    Assert.Equal(totalNumberOfEvents, NumEventsHandled);
                }
            }

            public void WaitForEvent(int maxMilliseconds, SerialData eventType)
            {
                Stopwatch sw = new Stopwatch();

                lock (this)
                {
                    sw.Start();

                    while (maxMilliseconds > sw.ElapsedMilliseconds)
                    {
                        Monitor.Wait(this, (int)(maxMilliseconds - sw.ElapsedMilliseconds));

                        for (int i = 0; i < EventType.Count; i++)
                        {
                            if (eventType == (SerialData)EventType[i])
                            {
                                return;
                            }
                        }
                    }

                    Assert.True(false, "Wait for event failure");
                }
            }

            //Since we can not guarantee the order or the exact time that the event handler is called 
            //We wil look for an event that was firered that matches the type and that bytesToRead 
            //is greater then the parameter
            public void Validate(SerialData eventType, int bytesToRead)
            {
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
                            return;
                        }
                    }
                }

                Assert.True(false, $"Validate {eventType} failed");
            }

            public int NumberOfOccurrencesOfType(SerialData eventType)
            {
                int numOccurrences = 0;

                lock (this)
                {
                    for (int i = 0; i < EventType.Count; i++)
                    {
                        if (eventType == (SerialData)EventType[i])
                        {
                            numOccurrences++;
                        }
                    }
                }

                return numOccurrences;
            }
        }

        private class ReadInReceivedEventHandler : ReceivedEventHandler
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

            public new void HandleEvent(object source, SerialDataReceivedEventArgs e)
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
}

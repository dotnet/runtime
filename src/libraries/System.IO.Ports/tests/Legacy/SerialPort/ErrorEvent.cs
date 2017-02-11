// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

public class ErrorEvent : PortsTest
{
    //Maximum time to wait for all of the expected events to be firered
    static readonly int MAX_TIME_WAIT = 5000;
    static readonly int NUM_TRYS = 5;

    #region Test Cases
    /*	
     This was already commented-out in the code which came to CoreFx from legacy 	
	public bool ErrorEvent_TXFull() 
	{
		SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
		SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
		ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
		
		int elapsedTime;

		Debug.WriteLine("Verifying TXFull event");
	  
		com1.Handshake = Handshake.RequestToSend;
		com1.Open();
		com2.Open();

		com1.ErrorEvent += new SerialErrorEventHandler(errEventHandler.HandleEvent);
		com1.BaseStream.BeginWrite(new byte[32767], 0, 32767, null, null);
		elapsedTime = 0;

		while(1 > errEventHandler.NumEventsHandled && elapsedTime < MAX_TIME_WAIT) {
			System.Threading.Thread.Sleep(ITERATION_TIME_WAIT);
			elapsedTime += ITERATION_TIME_WAIT;
		}	  
	  
		retValue &= errEventHandler.Validate(SerialErrors.TxFull, com1.ReceivedBytesThreshold, 0);
	  		
		if(!retValue) {
			Debug.WriteLine("Err_001!!! Verifying TXFull event FAILED");
		}
		
		if(com1.IsOpen)
			com1.Close();
      
		if(com2.IsOpen)
			com2.Close();      
		
		return retValue;  
	}
*/

    [ConditionalFact(nameof(HasNullModem))]
    public void ErrorEvent_RxOver()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);

            Debug.WriteLine("Verifying RxOver event");

            com1.Handshake = Handshake.RequestToSend;
            com1.BaudRate = 115200;
            com2.BaudRate = 115200;
            com1.Open();
            com2.Open();

            //This might not be necessary but it will clear the RTS pin when the buffer is too full
            com1.Handshake = Handshake.RequestToSend;

            com1.ErrorReceived += errEventHandler.HandleEvent;

            //This is overkill should find a more reasonable amount of bytes to write
            com2.BaseStream.Write(new byte[32767], 0, 32767);

            Assert.True(errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1), "Event never occurred");

            while (0 < errEventHandler.NumEventsHandled)
            {
                errEventHandler.Validate(SerialError.RXOver, -1);
            }

            lock (com1)
            {
                if (com1.IsOpen)
                    com1.Close();
            }
        }
    }

    /*	
     This was already commented-out in the code which came to CoreFx from legacy 	
            public bool ErrorEvent_Overrun() 
            {
                SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
                ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
                
                int elapsedTime;
    
                Debug.WriteLine("Verifying Overrun event");
          
                com1.Handshake = Handshake.RequestToSend;
                com1.BaudRate = 115200;
                com2.BaudRate = 115200;
                com1.Open();
                com2.Open();
    
                com1.ErrorEvent += new SerialErrorEventHandler(errEventHandler.HandleEvent);
                com2.BaseStream.Write(new byte[32767], 0, 32767);
    
                elapsedTime = 0;
    
                while(1 > errEventHandler.NumEventsHandled && elapsedTime < MAX_TIME_WAIT) {
                    System.Threading.Thread.Sleep(ITERATION_TIME_WAIT);
                    elapsedTime += ITERATION_TIME_WAIT;
                }	  
          
                retValue &= errEventHandler.Validate(SerialErrors.Overrun, com1.ReceivedBytesThreshold, 0);
                  
                if(!retValue) {
                    Debug.WriteLine("Err_003!!! Verifying Overrun event FAILED");
                }
            
                if(com1.IsOpen)
                    com1.Close();
          
                if(com2.IsOpen)
                    com2.Close();      
            
                return retValue;  
            }
        */

    [ConditionalFact(nameof(HasNullModem))]
    public void ErrorEvent_RxParity()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);

            Debug.WriteLine("Verifying RxParity event");

            com1.DataBits = 7;
            com1.Parity = Parity.Mark;

            com1.Open();
            com2.Open();

            com1.ErrorReceived += errEventHandler.HandleEvent;

            for (int i = 0; i < NUM_TRYS; i++)
            {
                Debug.WriteLine("Verifying RxParity event try: {0}", i);

                com2.BaseStream.Write(new byte[8], 0, 8);
                Assert.True(errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1));

                while (0 < errEventHandler.NumEventsHandled)
                {
                    errEventHandler.Validate(SerialError.RXParity, -1);
                }
            }

            lock (com1)
            {
                if (com1.IsOpen)
                    com1.Close();
            }
        }
    }


    [ConditionalFact(nameof(HasNullModem))]
    public void ErrorEvent_Frame()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
            byte[] frameErrorBytes = new byte[1];
            Random rndGen = new Random();

            Debug.WriteLine("Verifying Frame event");
            com1.DataBits = 7;

            //com1.StopBits = StopBits.Two;
            com1.Open();
            com2.Open();

            com1.ErrorReceived += errEventHandler.HandleEvent;

            for (int i = 0; i < frameErrorBytes.Length; i++)
            {
                frameErrorBytes[i] = (byte)rndGen.Next(0, 256);
            }

            //This should cause a frame error since the 8th bit is not set 
            //and com1 is set to 7 data bits ao the 8th bit will +12v where
            //com1 expects the stop bit at the 8th bit to be -12v
            frameErrorBytes[0] = 0x01;

            for (int i = 0; i < NUM_TRYS; i++)
            {
                Debug.WriteLine("Verifying Frame event try: {0}", i);

                com2.BaseStream.Write(frameErrorBytes, 0, 1);
                errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                while (0 < errEventHandler.NumEventsHandled)
                {
                    errEventHandler.Validate(SerialError.Frame, -1);
                }
            }

            lock (com1)
            {
                if (com1.IsOpen)
                    com1.Close();
            }
        }
    }
    #endregion

    #region Verification for Test Cases
    public class ErrorEventHandler
    {
        public readonly List<SerialError> EventType = new List<SerialError>();
        public readonly List<SerialPort> Source = new List<SerialPort>();
        public readonly List<int> BytesToRead = new List<int>();
        public int NumEventsHandled;
        private readonly SerialPort _com;

        public ErrorEventHandler(SerialPort com)
        {
            _com = com;
            NumEventsHandled = 0;
        }

        public void HandleEvent(object source, SerialErrorReceivedEventArgs e)
        {
            int bytesToRead;

            lock (_com)
            {
                bytesToRead = _com.BytesToRead;
            }

            lock (this)
            {
                BytesToRead.Add(bytesToRead);
                EventType.Add(e.EventType);
                Source.Add((SerialPort)source);
                NumEventsHandled++;
                System.Threading.Monitor.Pulse(this);
            }
        }

        public bool WaitForEvent(int maxMilliseconds, int totalNumberOfEvents)
        {
            Stopwatch sw = new Stopwatch();

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


        //Since we can not guarantee the order or the exact time that the event handler is called 
        //We will look for an event that was fired that matches the type and that bytesToRead 
        //is greater then the parameter    
        public void Validate(SerialError eventType, int bytesToRead)
        {
            lock (this)
            {
                for (int i = 0; i < EventType.Count; i++)
                {
                    if (eventType == EventType[i] && bytesToRead <= BytesToRead[i] && Source[i] == _com)
                    {
                        EventType.RemoveAt(i);
                        BytesToRead.RemoveAt(i);
                        Source.RemoveAt(i);
                        NumEventsHandled--;
                        return;
                    }
                }
            }
            Assert.True(false, $"Failed to validate expected type {eventType}, bytesToRead {bytesToRead}, actualTypes {string.Join(",", EventType)}");
        }
    }
    #endregion
}

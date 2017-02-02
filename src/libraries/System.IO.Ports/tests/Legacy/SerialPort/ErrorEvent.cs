// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class ErrorEvent
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ErrorEvent";
    public static readonly String s_strTFName = "ErrorEvent.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Maximum random value to use for ReceivedBytesThreshold
    public static readonly int MAX_RND_THRESHOLD = 16;

    //Minimum random value to use for ReceivedBytesThreshold
    public static readonly int MIN_RND_THRESHOLD = 2;

    //Maximum time to wait for all of the expected events to be firered
    public static readonly int MAX_TIME_WAIT = 5000;
    public static readonly int NUM_TRYS = 5;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ErrorEvent objTest = new ErrorEvent();
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

        //The following are commented out because I can determine a good strategy to make the event type occur
        //	  retValue &= tcSupport.BeginTestcase(new TestDelegate(ErrorEvent_TXFull), , TCSupport.SerialPortRequirements.NullModem);
        //	  retValue &= tcSupport.BeginTestcase(new TestDelegate(ErrorEvent_Overrun), TCSupport.SerialPortRequirements.NullModem);


        retValue &= tcSupport.BeginTestcase(new TestDelegate(ErrorEvent_RxOver), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ErrorEvent_RxParity), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ErrorEvent_Frame), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    /*	
	public bool ErrorEvent_TXFull() 
	{
		SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
		SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
		ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
		bool retValue = true;
		int elapsedTime;

		Console.WriteLine("Verifying TXFull event");
	  
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
			Console.WriteLine("Err_001!!! Verifying TXFull event FAILED");
		}
		
		if(com1.IsOpen)
			com1.Close();
      
		if(com2.IsOpen)
			com2.Close();      
		
		return retValue;  
	}
*/
    public bool ErrorEvent_RxOver()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
        bool retValue = true;

        Console.WriteLine("Verifying RxOver event");

        com1.Handshake = Handshake.RequestToSend;
        com1.BaudRate = 115200;
        com2.BaudRate = 115200;
        com1.Open();
        com2.Open();

        //This might not be necessary but it will clear the RTS pin when the buffer is too full
        com1.Handshake = Handshake.RequestToSend;

        com1.ErrorReceived += new SerialErrorReceivedEventHandler(errEventHandler.HandleEvent);

        //This is overkill should find a more reasonable ammount of bytes to write
        com2.BaseStream.Write(new byte[32767], 0, 32767);

        if (!errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1))
        {
            Console.WriteLine("Err_298292haid Event never occured");
            retValue = false;
        }

        while (0 < errEventHandler.NumEventsHandled)
        {
            if (!errEventHandler.Validate(SerialError.RXOver, -1))
            {
                Console.WriteLine("Err_2929ajidz!!! Expected all errors to be RXOver but at least one is:{0}", errEventHandler.EventType[0]);
                retValue = false;
            }
        }

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying RxOver event FAILED");
        }

        lock (com1)
        {
            if (com1.IsOpen)
                com1.Close();
        }

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    /*	
        public bool ErrorEvent_Overrun() 
        {
            SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
            SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
            ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
            bool retValue = true;
            int elapsedTime;

            Console.WriteLine("Verifying Overrun event");
	  
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
                Console.WriteLine("Err_003!!! Verifying Overrun event FAILED");
            }
		
            if(com1.IsOpen)
                com1.Close();
      
            if(com2.IsOpen)
                com2.Close();      
		
            return retValue;  
        }
    */
    public bool ErrorEvent_RxParity()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
        bool retValue = true;

        Console.WriteLine("Verifying RxParity event");

        com1.DataBits = 7;
        com1.Parity = Parity.Mark;

        com1.Open();
        com2.Open();

        com1.ErrorReceived += new SerialErrorReceivedEventHandler(errEventHandler.HandleEvent);

        for (int i = 0; i < NUM_TRYS; i++)
        {
            Console.WriteLine("Verifying RxParity event try: {0}", i);

            com2.BaseStream.Write(new byte[8], 0, 8);
            errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

            while (0 < errEventHandler.NumEventsHandled)
            {
                if (!errEventHandler.Validate(SerialError.RXParity, -1))
                {
                    Console.WriteLine("Err_2929ajidz!!! Expected all errors to be RXParity but at least one is:{0}", errEventHandler.EventType[0]);
                    retValue = false;
                }
            }
        }

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying RxParity event FAILED");
        }

        lock (com1)
        {
            if (com1.IsOpen)
                com1.Close();
        }

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    public bool ErrorEvent_Frame()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);
        bool retValue = true;
        byte[] frameErrorBytes = new byte[1];
        Random rndGen = new Random();

        Console.WriteLine("Verifying Frame event");
        com1.DataBits = 7;

        //com1.StopBits = StopBits.Two;
        com1.Open();
        com2.Open();

        com1.ErrorReceived += new SerialErrorReceivedEventHandler(errEventHandler.HandleEvent);

        for (int i = 0; i < frameErrorBytes.Length; i++)
        {
            frameErrorBytes[i] = (byte)rndGen.Next(0, 256);
        }

        //This should cause a fame error since the 8th bit is not set 
        //and com1 is set to 7 data bits ao the 8th bit will +12v where
        //com1 expects the stop bit at the 8th bit to be -12v
        frameErrorBytes[0] = 0x01;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            Console.WriteLine("Verifying Frame event try: {0}", i);

            com2.BaseStream.Write(frameErrorBytes, 0, 1);
            errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1);


            while (0 < errEventHandler.NumEventsHandled)
            {
                if (!errEventHandler.Validate(SerialError.Frame, -1))
                {
                    Console.WriteLine("Err_25097qpaua!!! Expected all errors to be Frame but at least one is:{0}", errEventHandler.EventType[0]);
                    retValue = false;
                }
            }
        }

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying Frame event FAILED");
        }

        lock (com1)
        {
            if (com1.IsOpen)
                com1.Close();
        }

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    public class ErrorEventHandler
    {
        public System.Collections.ArrayList EventType;
        public System.Collections.ArrayList Source;
        public System.Collections.ArrayList BytesToRead;
        public int NumEventsHandled;
        private SerialPort _com;

        public ErrorEventHandler(SerialPort com)
        {
            _com = com;
            NumEventsHandled = 0;
            EventType = new System.Collections.ArrayList();
            Source = new System.Collections.ArrayList();
            BytesToRead = new System.Collections.ArrayList();
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
                Source.Add(source);
                NumEventsHandled++;
                System.Threading.Monitor.Pulse(this);
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
    #endregion
}

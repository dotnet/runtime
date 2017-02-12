// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO.Ports;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

public class Event_Close_Stress : PortsTest
{
    //Maximum time to wait for all of the expected events to be firered
    private static readonly TimeSpan TestDuration = TCSupport.RunShortStressTests ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(3);

    #region Test Cases

    [ConditionalFact(nameof(HasNullModem))]
    public void PinChanged_Close_Stress()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            Stopwatch stopwatch = new Stopwatch();
            int count = 0;

            com1.PinChanged += CatchPinChangedEvent;
            com2.Open();

            stopwatch.Start();
            while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < TestDuration.TotalMilliseconds)
            {
                com1.Open();

                for (int j = 0; j < 10; ++j)
                {
                    com2.RtsEnable = !com2.RtsEnable;
                }

                com1.Close();

                ++count;
            }

            Debug.WriteLine("PinChanged={0}", _pinChangedCount);
        }
    }

    private int _pinChangedCount = 0;

    public void CatchPinChangedEvent(object sender, SerialPinChangedEventArgs e)
    {
        ++_pinChangedCount;
    }

    [ConditionalFact(nameof(HasNullModem))]
    public void DataReceived_Close_Stress()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            Stopwatch stopwatch = new Stopwatch();
            int count = 0;

            com1.DataReceived += CatchDataReceivedEvent;
            com2.Open();

            stopwatch.Start();
            while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < TestDuration.TotalMilliseconds)
            {
                com1.Open();

                for (int j = 0; j < 10; ++j)
                {
                    com2.WriteLine("foo");
                }

                com1.Close();

                ++count;
            }

            com2.Close();

            Debug.WriteLine("DataReceived={0}", _dataReceivedCount);

        }
    }

    private int _dataReceivedCount = 0;

    public void CatchDataReceivedEvent(object sender, SerialDataReceivedEventArgs e)
    {
        ++_dataReceivedCount;
    }

    [ConditionalFact(nameof(HasNullModem))]
    public void ErrorReceived_Close_Stress()
    {
        using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
        using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
        {
            byte[] frameErrorBytes = new byte[1];
            Stopwatch stopwatch = new Stopwatch();
            int count = 0;

            com1.DataBits = 7;
            com1.ErrorReceived += CatchErrorReceivedEvent;
            com2.Open();

            //This should cause a fame error since the 8th bit is not set 
            //and com1 is set to 7 data bits ao the 8th bit will +12v where
            //com1 expects the stop bit at the 8th bit to be -12v
            frameErrorBytes[0] = 0x01;

            stopwatch.Start();
            while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < TestDuration.TotalMilliseconds)
            {
                com1.Open();

                for (int j = 0; j < 10; ++j)
                {
                    com2.Write(frameErrorBytes, 0, 1);
                }

                com1.Close();

                ++count;
            }

            com2.Close();

            Debug.WriteLine("ERRORReceived={0}", _errorReceivedCount);
        }
    }

    private int _errorReceivedCount = 0;

    public void CatchErrorReceivedEvent(object sender, SerialErrorReceivedEventArgs e)
    {
        ++_errorReceivedCount;
    }
    #endregion

    #region Verification for Test Cases
    #endregion
}


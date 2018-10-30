// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Event_Close_Stress : PortsTest
    {
        //Maximum time to wait for all of the expected events to be firered
        private static readonly TimeSpan s_testDuration = TCSupport.RunShortStressTests ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(3);

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void PinChanged_Close_Stress()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Stopwatch stopwatch = new Stopwatch();
                int count = 0;
                int pinChangedCount = 0;

                com1.PinChanged += (sender, e) => { ++pinChangedCount; };
                com2.Open();

                stopwatch.Start();
                while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < s_testDuration.TotalMilliseconds)
                {
                    com1.Open();

                    for (int j = 0; j < 10; ++j)
                    {
                        com2.RtsEnable = !com2.RtsEnable;
                    }

                    com1.Close();

                    ++count;
                }

                Debug.WriteLine("PinChanged={0}", pinChangedCount);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DataReceived_Close_Stress()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Stopwatch stopwatch = new Stopwatch();
                int dataReceivedCount = 0;
                int count = 0;

                com1.DataReceived += (sender, e) => { ++dataReceivedCount; };
                com2.Open();

                stopwatch.Start();
                while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < s_testDuration.TotalMilliseconds)
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

                Debug.WriteLine("DataReceived={0}", dataReceivedCount);
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void ErrorReceived_Close_Stress()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte[] frameErrorBytes = new byte[1];
                Stopwatch stopwatch = new Stopwatch();
                int errorReceivedCount = 0;
                int count = 0;

                com1.DataBits = 7;
                com1.ErrorReceived += (sender, e) => { ++errorReceivedCount; };
                com2.Open();

                //This should cause a fame error since the 8th bit is not set
                //and com1 is set to 7 data bits ao the 8th bit will +12v where
                //com1 expects the stop bit at the 8th bit to be -12v
                frameErrorBytes[0] = 0x01;

                stopwatch.Start();
                while (count % 100 != 0 || stopwatch.ElapsedMilliseconds < s_testDuration.TotalMilliseconds)
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

                Debug.WriteLine("ERRORReceived={0}", errorReceivedCount);
            }
        }

        #endregion

        #region Verification for Test Cases
        #endregion
    }
}

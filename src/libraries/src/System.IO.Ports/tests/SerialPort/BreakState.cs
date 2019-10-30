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
    public class BreakState_Property : PortsTest
    {
        //The maximum time we will wait for the pin changed event to get firered for the break state
        private const int MAX_WAIT_FOR_BREAK = 800;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BreakState_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default BreakState");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BreakState_BeforeOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying setting BreakState before open");
                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Assert.Throws<InvalidOperationException>(() => com1.BreakState = true);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BreakState_true()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying true BreakState");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                SetBreakStateandVerify(com1);

                serPortProp.SetProperty("BreakState", true);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void BreakState_false()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying false BreakState");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                SetBreakStateandVerify(com1);
                serPortProp.SetProperty("BreakState", false);

                com1.BreakState = false;
                serPortProp.VerifyPropertiesAndPrint(com1);

                Assert.False(GetCurrentBreakState());
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void BreakState_true_false()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying setting BreakState to true then false");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                serPortProp.VerifyPropertiesAndPrint(com1);

                SetBreakStateandVerify(com1);
                serPortProp.SetProperty("BreakState", true);
                serPortProp.VerifyPropertiesAndPrint(com1);

                serPortProp.SetProperty("BreakState", false);
                com1.BreakState = false;
                serPortProp.VerifyPropertiesAndPrint(com1);
            }

            Assert.False(GetCurrentBreakState());
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void BreakState_true_false_true()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying setting BreakState to true then false then true again");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                SetBreakStateandVerify(com1);

                serPortProp.SetProperty("BreakState", true);
                serPortProp.VerifyPropertiesAndPrint(com1);

                serPortProp.SetProperty("BreakState", false);
                com1.BreakState = false;
                serPortProp.VerifyPropertiesAndPrint(com1);

                Assert.False(GetCurrentBreakState());

                serPortProp.VerifyPropertiesAndPrint(com1);
                SetBreakStateandVerify(com1);
                serPortProp.SetProperty("BreakState", true);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases

        private void SetBreakStateandVerify(SerialPort com1)
        {
            BreakStateEventHandler breakState;
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                breakState = new BreakStateEventHandler();

                com2.PinChanged += breakState.HandleEvent;
                com2.Open();

                com1.BreakState = true;
                Assert.True(breakState.WaitForBreak(MAX_WAIT_FOR_BREAK));
            }
        }


        private bool GetCurrentBreakState()
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                BreakStateEventHandler breakState = new BreakStateEventHandler();

                com2.PinChanged += breakState.HandleEvent;
                com2.Open();
                return breakState.WaitForBreak(MAX_WAIT_FOR_BREAK);
            }
        }

        private class BreakStateEventHandler
        {
            private bool _breakOccurred;

            public void HandleEvent(object source, SerialPinChangedEventArgs e)
            {
                lock (this)
                {
                    if (SerialPinChange.Break == e.EventType)
                    {
                        _breakOccurred = true;
                        Monitor.Pulse(this);
                    }
                }
            }

            public void WaitForBreak()
            {
                lock (this)
                {
                    if (!_breakOccurred)
                    {
                        Monitor.Wait(this);
                    }
                }
            }


            public bool WaitForBreak(int timeout)
            {
                lock (this)
                {
                    if (!_breakOccurred)
                    {
                        return Monitor.Wait(this, timeout);
                    }
                    return true;
                }
            }
        }

        #endregion
    }
}

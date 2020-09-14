// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class RtsEnable_Property : PortsTest
    {
        #region Test Cases
        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default RtsEnable");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyRtsEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_true_BeforeOpen()
        {
            Debug.WriteLine("Verifying true RtsEnable before open");
            VerifyRtsEnableBeforeOpen(true);
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_false_BeforeOpen()
        {
            Debug.WriteLine("Verifying false RtsEnable before open");
            VerifyRtsEnableBeforeOpen(false);
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_true_false_BeforeOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying seting RtsEnable to true then false before open");

                com1.RtsEnable = true;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("RtsEnable", false);
                com1.RtsEnable = false;

                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyRtsEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_true_AfterOpen()
        {
            Debug.WriteLine("Verifying true RtsEnable after open");
            VerifyRtsEnableAfterOpen(true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_false_AfterOpen()
        {
            Debug.WriteLine("Verifying false RtsEnable after open");
            VerifyRtsEnableAfterOpen(false);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_true_Handshake_XOnXOff()
        {
            Debug.WriteLine("Verifying true RtsEnable after setting Handshake to XOnXOff");


            VerifyRtsEnableWithHandshake(true, Handshake.XOnXOff);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void RtsEnable_false_Handshake_XOnXOff()
        {
            Debug.WriteLine("Verifying false RtsEnable after setting Handshake to XOnXOff");


            VerifyRtsEnableWithHandshake(false, Handshake.XOnXOff);
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void RtsEnable_true_Handshake_RequestToSend()
        {
            Debug.WriteLine("Verifying true RtsEnable after setting Handshake to RequestToSend");
            VerifyRtsEnableWithHandshake(true, Handshake.RequestToSend);
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void RtsEnable_false_Handshake_RequestToSend()
        {
            Debug.WriteLine("Verifying false RtsEnable after setting Handshake to RequestToSend");
            VerifyRtsEnableWithHandshake(false, Handshake.RequestToSend);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void RtsEnable_true_Handshake_RequestToSendXOnXOff()
        {
            Debug.WriteLine("Verifying true RtsEnable after setting Handshake to RequestToSendXOnXOff");
            VerifyRtsEnableWithHandshake(true, Handshake.RequestToSendXOnXOff);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void RtsEnable_false_Handshake_RequestToSendXOnXOff()
        {
            Debug.WriteLine("Verifying false RtsEnable after setting Handshake to RequestToSendXOnXOff");
            VerifyRtsEnableWithHandshake(false, Handshake.RequestToSendXOnXOff);
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void RtsEnable_true_false_AfterOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying seting RtsEnable to true then false after open");

                com1.RtsEnable = true;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("RtsEnable", false);
                com1.RtsEnable = false;

                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyRtsEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void RtsEnable_Get_Handshake_None()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying getting RtsEnable with Handshake set to None");

                com1.Open();
                com1.Handshake = Handshake.None;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("Handshake", Handshake.None);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void RtsEnable_Get_Handshake_RequestToSend()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying getting RtsEnable with Handshake set to RequestToSend");

                com1.Open();
                com1.Handshake = Handshake.RequestToSend;

                Assert.Throws<InvalidOperationException>(() =>
                {
                    _ = com1.RtsEnable;
                });
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void RtsEnable_Get_Handshake_RequestToSendXOnXOff()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying getting RtsEnable with Handshake set to RequestToSendXOnXOff");

                com1.Open();
                com1.Handshake = Handshake.RequestToSendXOnXOff;

                Assert.Throws<InvalidOperationException>(() =>
                {
                    _ = com1.RtsEnable;
                });
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void RtsEnable_Get_Handshake_XOnXOff()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying getting RtsEnable with Handshake set to XOnXOff");

                com1.Open();
                com1.Handshake = Handshake.XOnXOff;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("Handshake", Handshake.XOnXOff);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyRtsEnableBeforeOpen(bool rtsEnable)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.RtsEnable = rtsEnable;
                com1.Open();
                serPortProp.SetProperty("RtsEnable", rtsEnable);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyRtsEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        private void VerifyRtsEnableAfterOpen(bool rtsEnable)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.RtsEnable = rtsEnable;
                serPortProp.SetProperty("RtsEnable", rtsEnable);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyRtsEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyRtsEnableWithHandshake(bool rtsEnable, Handshake handshake)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();
                Handshake originalHandshake;
                bool expetectedRtsEnable;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.RtsEnable = rtsEnable;
                com1.Open();
                originalHandshake = com1.Handshake;
                serPortProp.SetProperty("RtsEnable", rtsEnable);

                serPortProp.VerifyPropertiesAndPrint(com1);

                VerifyRtsEnable(com1, rtsEnable);

                com1.Handshake = handshake;

                if (IsRequestToSend(com1))
                {
                    try
                    {
                        com1.RtsEnable = !rtsEnable;
                    }
                    catch (InvalidOperationException) { }
                }
                else
                {
                    com1.RtsEnable = !rtsEnable;
                    com1.RtsEnable = rtsEnable;
                }

                expetectedRtsEnable = handshake == Handshake.RequestToSend || handshake == Handshake.RequestToSendXOnXOff || rtsEnable;

                VerifyRtsEnable(com1, expetectedRtsEnable);

                com1.Handshake = originalHandshake;

                expetectedRtsEnable = rtsEnable;

                VerifyRtsEnable(com1, expetectedRtsEnable);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyRtsEnable(SerialPort com1)
        {
            VerifyRtsEnable(com1, com1.RtsEnable);
        }

        private void VerifyRtsEnable(SerialPort com1, bool expectedRtsEnable)
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                com2.Open();

                // Allow tiny bit of time for state of the line to change in case Rts was just set
                System.Threading.Thread.Sleep(30);
                Assert.Equal(expectedRtsEnable, com2.CtsHolding);
            }
        }

        private bool IsRequestToSend(SerialPort com)
        {
            return com.Handshake == Handshake.RequestToSend || com.Handshake == Handshake.RequestToSendXOnXOff;
        }

        #endregion
    }
}

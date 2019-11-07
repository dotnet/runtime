// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class DtrEnable_Property : PortsTest
    {
        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void DtrEnable_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DtrEnable");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDtrEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DtrEnable_true_BeforeOpen()
        {
            Debug.WriteLine("Verifying true DtrEnable before open");
            VerifyDtrEnableBeforeOpen(true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DtrEnable_false_BeforeOpen()
        {
            Debug.WriteLine("Verifying false DtrEnable before open");
            VerifyDtrEnableBeforeOpen(false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DtrEnable_true_false_BeforeOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying seting DtrEnable to true then false before open");

                com1.DtrEnable = true;

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                serPortProp.SetProperty("DtrEnable", false);
                com1.DtrEnable = false;
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDtrEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DtrEnable_true_AfterOpen()
        {
            Debug.WriteLine("Verifying true DtrEnable after open");
            VerifyDtrEnableAfterOpen(true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DtrEnable_false_AfterOpen()
        {
            Debug.WriteLine("Verifying false DtrEnable after open");
            VerifyDtrEnableAfterOpen(false);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DtrEnable_true_false_AfterOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying seting DtrEnable to true then false after open");

                com1.Open();

                com1.DtrEnable = true;
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                serPortProp.SetProperty("DtrEnable", false);
                com1.DtrEnable = false;

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDtrEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyDtrEnableBeforeOpen(bool dtrEnable)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.DtrEnable = dtrEnable;
                com1.Open();

                serPortProp.SetProperty("DtrEnable", dtrEnable);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDtrEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyDtrEnableAfterOpen(bool dtrEnable)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                com1.DtrEnable = dtrEnable;

                serPortProp.SetProperty("DtrEnable", dtrEnable);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDtrEnable(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyDtrEnable(SerialPort com1)
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                com2.Open();
                Assert.True((com1.DtrEnable && com2.DsrHolding) || (!com1.DtrEnable && !com2.DsrHolding));
            }
        }

        #endregion
    }
}

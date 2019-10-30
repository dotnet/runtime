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
    public class CtsHolding_Property : PortsTest
    {
        #region Test Cases

        [Fact]
        public void CtsHolding_Default()
        {
            using (SerialPort com1 = new SerialPort())
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default CtsHolding before Open");

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void CtsHolding_Default_AfterOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default CtsHolding after Open");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void CtsHolding_true()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying CtsHolding=true on com1 when com2.RtsEnable=true");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("CtsHolding", true);

                com1.Open();
                com2.Open();
                com2.RtsEnable = true;

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void CtsHolding_true_false()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then false");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("CtsHolding", true);

                com1.Open();
                com2.Open();
                com2.RtsEnable = true;

                serPortProp.VerifyPropertiesAndPrint(com1);

                com2.RtsEnable = false;
                serPortProp.SetProperty("CtsHolding", false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void CtsHolding_true_local_close()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine(
                    "Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then com1 is closed");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("CtsHolding", true);

                com1.Open();
                com2.Open();
                com2.RtsEnable = true;

                serPortProp.VerifyPropertiesAndPrint(com1);

                if (com1.IsOpen)
                    com1.Close();

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void CtsHolding_true_remote_close()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine(
                    "Verifying CtsHolding=true then false on com1 when com2.RtsEnable=true then com2 is closed");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("CtsHolding", true);

                com1.Open();
                com2.Open();
                com2.RtsEnable = true;

                serPortProp.VerifyPropertiesAndPrint(com1);

                if (com2.IsOpen)
                    com2.Close();

                Thread.Sleep(100);
                serPortProp.SetProperty("CtsHolding", false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases

        #endregion
    }
}

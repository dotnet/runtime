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
    public class DsrHolding_Property : PortsTest
    {
        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DsrHolding_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DsrHolding before Open");

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DsrHolding_Default_AfterOpen()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DsrHolding after Open");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DsrHolding_true()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying DsrHolding=true on com1 when com2.DtrEnable=true");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("DsrHolding", true);

                com1.Open();
                com2.Open();

                com2.DtrEnable = true;
                serPortProp.SetProperty("CDHolding", com1.CDHolding);
                //We dont care what this is set to since some serial cables loop CD to CTS
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DsrHolding_true_false()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying DsrHolding=true then false on com1 when com2.DtrEnable=true then false");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("DsrHolding", true);

                com1.Open();
                com2.Open();

                com2.DtrEnable = true;
                serPortProp.SetProperty("CDHolding", com1.CDHolding);
                //We dont care what this is set to since some serial cables loop CD to CTS
                serPortProp.VerifyPropertiesAndPrint(com1);

                com2.DtrEnable = false;
                serPortProp.SetProperty("CDHolding", com1.CDHolding);
                //We dont care what this is set to since some serial cables loop CD to CTS
                serPortProp.SetProperty("DsrHolding", false);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void DsrHolding_true_local_close()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying DsrHolding=true then false on com1 when com2.DtrEnable=true then com1 is closed");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("DsrHolding", true);

                com1.Open();
                com2.Open();
                com2.DtrEnable = true;

                serPortProp.SetProperty("CDHolding", com1.CDHolding);
                //We dont care what this is set to since some serial cables loop CD to CTS
                serPortProp.VerifyPropertiesAndPrint(com1);

                if (com1.IsOpen)
                    com1.Close();

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void DsrHolding_true_remote_close()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying DsrHolding=true then false on com1 when com2.DtrEnable=true then com2 is closed");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("DsrHolding", true);

                com1.Open();
                com2.Open();
                com2.DtrEnable = true;

                serPortProp.SetProperty("CDHolding", com1.CDHolding);
                //We dont care what this is set to since some serial cables loop CD to CTS
                serPortProp.VerifyPropertiesAndPrint(com1);

                if (com2.IsOpen)
                    com2.Close();

                Thread.Sleep(100);

                serPortProp.SetProperty("DsrHolding", false);
                serPortProp.SetProperty("CDHolding", com1.CDHolding);
                //We dont care what this is set to since some serial cables loop CD to CTS
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases

        #endregion
    }
}

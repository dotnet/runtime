// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Open : PortsTest
    {
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenDefault()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                com.Open();
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("BytesToWrite={0}", com.BytesToWrite);

                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenTwice()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying after calling Open() twice");

                com.Open();

                Assert.Throws<InvalidOperationException>(() => com.Open());
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenTwoInstances()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp1 = new SerialPortProperties();
                SerialPortProperties serPortProp2 = new SerialPortProperties();

                Debug.WriteLine("Verifying calling Open() on two instances of SerialPort");
                serPortProp1.SetAllPropertiesToOpenDefaults();
                serPortProp1.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                serPortProp2.SetAllPropertiesToDefaults();
                serPortProp2.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                Assert.Throws<UnauthorizedAccessException>(() => com2.Open());

                serPortProp1.VerifyPropertiesAndPrint(com1);
                serPortProp2.VerifyPropertiesAndPrint(com2);
            }
        }
    }
}

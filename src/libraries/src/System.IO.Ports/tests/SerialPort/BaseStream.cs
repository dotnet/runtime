// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class BaseStream_Property : PortsTest
    {
        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaseStream_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default BaseStream");
                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaseStream_Open()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying BaseStream after Open() has been called");
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                Assert.NotNull(com1.BaseStream);
                Assert.True(com1.BaseStream.GetType().BaseType == typeof(Stream));

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaseStream_Open_Close()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying BaseStream after Open() and Close have been called");

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.Close();

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaseStream_Open_BaseStreamClose()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying BaseStream after Open() and BaseStream.Close() have been called");

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.BaseStream.Close();

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases

        #endregion
    }
}

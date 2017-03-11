// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_CanTimeout : PortsTest
    {
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void CanTimeout_Open()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();

                Debug.WriteLine("Verifying CanTimeout property returns true after a call to Open()");

                Assert.Equal(true, com.BaseStream.CanTimeout);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void CanTimeout_Open_Close()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.Close();

                Debug.WriteLine("Verifying CanTimeout property retunrs false After Open() then Close()");

                Assert.Equal(false, serialStream.CanTimeout);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void CanTimeout_Open_Close_Open()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                com.Close();
                com.Open();
                Stream serialStream = com.BaseStream;

                Debug.WriteLine("Verifying CanTimeout property returns false After Open() then Close()");

                Assert.Equal(true, serialStream.CanTimeout);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void CanTimeout_Open_BaseStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.BaseStream.Close();

                Debug.WriteLine("Verifying CanTimeout property returns false After Open() then BaseStream.Close()");

                Assert.Equal(false, serialStream.CanTimeout);
            }
        }
    }
}
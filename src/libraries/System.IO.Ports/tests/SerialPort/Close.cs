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
    public class Close : PortsTest
    {
        // The number of the bytes that should read/write buffers
        private const int numReadBytes = 32;
        private static readonly int s_numWriteBytes = TCSupport.MinimumBlockingByteCount;

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void CloseWithoutOpen()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Calling Close() without first calling Open()");

                com.Close();

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Calling Close() after calling Open()");
                com.Open();
                com.Close();

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void OpenFillBuffersClose()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Calling Open(), fill both transmit and receive buffers, call Close()");
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                // Setting com1 to use Handshake so we can fill read buffer
                com1.Handshake = Handshake.RequestToSend;
                com1.Open();
                com2.Open();

                // BeginWrite is used so we can fill the read buffer then go onto to verify
                com1.BaseStream.BeginWrite(new byte[s_numWriteBytes], 0, s_numWriteBytes, null, null);
                com2.Write(new byte[numReadBytes], 0, numReadBytes);
                Thread.Sleep(500);

                serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
                serPortProp.SetProperty("BytesToWrite", s_numWriteBytes - TCSupport.HardwareTransmitBufferSize);
                serPortProp.SetProperty("BytesToRead", numReadBytes);

                Debug.WriteLine("Verifying properties after port is open and buffers have been filled");
                serPortProp.VerifyPropertiesAndPrint(com1);

                com1.Handshake = Handshake.None;
                com1.Close();

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying properties after port has been closed");
                serPortProp.VerifyPropertiesAndPrint(com1);

                com1.Open();
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying properties after port has been opened again");
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenCloseNewInstanceOpen()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Calling Close() after calling Open() then create a new instance of SerialPort and call Open() again");
                com.Open();
                com.Close();

                using (var newCom = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
                {
                    newCom.Open();

                    serPortProp.SetAllPropertiesToOpenDefaults();
                    serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                    serPortProp.VerifyPropertiesAndPrint(newCom);
                }
            }
        }
    }
}

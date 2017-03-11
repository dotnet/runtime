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
    public class SerialStream_Close : PortsTest
    {
        // The number of the bytes that should read/write buffers
        private const int numReadBytes = 32;
        private const int numWriteBytes = 32;

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenClose_WriteMethods()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying calling Write methods after calling Open() and BaseStream.Close()");
                com.Open();
                com.BaseStream.Close();

                try
                {
                    com.Write(new byte[8], 0, 8);
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.Write(new char[8], 0, 8);
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.Write("A");
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.WriteLine("A");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenClose_ReadMethods()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying calling Read methods after calling Open() and BaseStream.Close()");
                com.Open();
                com.BaseStream.Close();

                try
                {
                    com.Read(new byte[8], 0, 8);
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.Read(new char[8], 0, 8);
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.ReadByte();
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.ReadExisting();
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.ReadLine();
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.ReadTo("A");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenClose_DiscardMethods()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying calling Discard methods after calling Open() and BaseStream.Close()");
                com.Open();
                com.BaseStream.Close();

                try
                {
                    com.DiscardInBuffer();
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    com.DiscardOutBuffer();
                }
                catch (InvalidOperationException)
                {
                }
            }
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenClose_OpenClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying calling Open/Close methods after calling Open() and BaseStream.Close()");
                com.Open();
                com.BaseStream.Close();

                try
                {
                    com.Close();
                }
                catch (ObjectDisposedException)
                {
                }

                com.Open();

                try
                {
                    if (com.IsOpen)
                        com.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenClose_Properties()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying Properites after calling Open() and BaseStream.Close()");

                com.Open();
                com.BaseStream.Close();

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void OpenFillBuffersClose()
        {
            using (var com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (var com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Calling Open(), fill both trasmit and receive buffers, call Close()");
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                // Setting com1 to use Handshake so we can fill read buffer
                com1.Handshake = Handshake.RequestToSend;
                com1.Open();
                com2.Open();

                // BeginWrite is used so we can fill the read buffer then go onto to verify
                com1.BaseStream.BeginWrite(new byte[numWriteBytes], 0, numWriteBytes, null, null);
                com2.Write(new byte[numReadBytes], 0, numReadBytes);
                Thread.Sleep(500);

                serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
                serPortProp.SetProperty("BytesToWrite", numWriteBytes);
                serPortProp.SetProperty("BytesToRead", numReadBytes);

                Debug.WriteLine("Verifying properties after port is open and bufferes have been filled");
                serPortProp.VerifyPropertiesAndPrint(com1);

                com1.Handshake = Handshake.None;
                com1.BaseStream.Close();
                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying properties after port has been closed");
                serPortProp.VerifyPropertiesAndPrint(com1);

                com1.Open();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                Debug.WriteLine("Verifying properties after port has been opened again");
                serPortProp.VerifyPropertiesAndPrint(com1);

                try
                {
                    if (com1.IsOpen)
                        com1.Close();
                }
                catch (Exception)
                {
                }
            }
            // Give the port time to finish closing since we potentially have an unclosed BeginRead/BeginWrite
            Thread.Sleep(200);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void OpenCloseNewInstanceOpen()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine(
                    "Calling Close() after calling Open() then create a new instance of SerialPort and call Open() again");
                com.Open();
                com.BaseStream.Close();
                using (var newCom = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
                {
                    try
                    {
                        newCom.Open();

                        serPortProp.SetAllPropertiesToOpenDefaults();
                        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                        serPortProp.VerifyPropertiesAndPrint(newCom);
                    }
                    catch (Exception e)
                    {
                        Fail("ERROR!!! Open() threw {0}", e.GetType());
                    }

                    try
                    {
                        if (com.IsOpen)
                            com.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Open_BaseStreamClose_Open()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying Properites after calling Open(), BaseStream.Close(), then Open() again");

                com.Open();
                com.BaseStream.Close();
                com.Open();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Open_BaseStreamClose_Close()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying Properites after calling Open(), BaseStream.Close(), then Close()");

                com.Open();
                com.BaseStream.Close();
                com.Close();

                serPortProp.SetAllPropertiesToDefaults();

                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Open_MultipleBaseStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying Properites after calling Open(), BaseStream.Close() multiple times");

                com.Open();
                Stream serialStream = com.BaseStream;
                serialStream.Close();
                serialStream.Close();
                serialStream.Close();

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }
    }
}

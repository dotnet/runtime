// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Legacy.Support;
using System.Diagnostics;
using System.IO.Ports;
using System.IO.PortsTests;
using Xunit;

public class Close : PortsTest
{
    // The number of the bytes that should read/write buffers
    public static readonly int numReadBytes = 32;
    public static readonly int numWriteBytes = 32;

    [ConditionalFact(nameof(HasOneSerialPort))]
    public void CloseWithoutOpen()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();

        Debug.WriteLine("Calling Close() without first calling Open()");

        com.Close();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();
    }

    [ConditionalFact(nameof(HasOneSerialPort))]
    public void OpenClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPortProperties serPortProp = new SerialPortProperties();

        Debug.WriteLine("Calling Close() after calling Open()");
        com.Open();
        com.Close();

        serPortProp.SetAllPropertiesToDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.VerifyPropertiesAndPrint(com);

        if (com.IsOpen)
            com.Close();
    }

    [ConditionalFact(nameof(HasNullModem))]
    public void OpenFillBuffersClose()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
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
        System.Threading.Thread.Sleep(500);

        serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
        serPortProp.SetProperty("BytesToWrite", numWriteBytes);
        serPortProp.SetProperty("BytesToRead", numReadBytes);

        Debug.WriteLine("Verifying properties after port is open and bufferes have been filled");
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

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();
    }

    [ConditionalFact(nameof(HasOneSerialPort))]
    public void OpenCloseNewInstanceOpen()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort newCom;
        SerialPortProperties serPortProp = new SerialPortProperties();

        Debug.WriteLine("Calling Close() after calling Open() then create a new instance of SerialPort and call Open() again");
        com.Open();
        com.Close();

        newCom = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        newCom.Open();

        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        serPortProp.VerifyPropertiesAndPrint(newCom);

        if (com.IsOpen)
            com.Close();

        if (newCom.IsOpen)
            newCom.Close();
    }
}

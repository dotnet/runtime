// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Legacy.Support;
using System.Diagnostics;
using System.IO.Ports;
using System.IO.PortsTests;
using Xunit;

public class ctor : PortsTest
{
    [Fact]
    public void Verify()
    {
        SerialPortProperties serPortProp = new SerialPortProperties();
        SerialPort com = new SerialPort();

        serPortProp.SetAllPropertiesToDefaults();
        Debug.WriteLine("Verifying properties is called");
        serPortProp.VerifyPropertiesAndPrint(com);
    }
}

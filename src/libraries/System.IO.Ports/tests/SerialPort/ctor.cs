// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class ctor : PortsTest
    {
        [Fact]
        public void Verify()
        {
            SerialPortProperties serPortProp = new SerialPortProperties();
            using (SerialPort com = new SerialPort())
            {
                serPortProp.SetAllPropertiesToDefaults();
                Debug.WriteLine("Verifying properties is called");
                serPortProp.VerifyPropertiesAndPrint(com);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Ports;
using Xunit;

namespace System.IO.PortsTests
{
    public class SmokeTest
    {
        [Fact]
        public void EnumeratePorts()
        {
            Assert.NotNull(SerialPort.GetPortNames());
        }
    }
}

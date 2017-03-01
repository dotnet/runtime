// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class GetPortNames : PortsTest
    {
        #region Test Cases

        [Fact]
        private void OpenEveryPortName()
        {
            string[] portNames = SerialPort.GetPortNames();

            for (int i = 0; i < portNames.Length; ++i)
            {
                Debug.WriteLine("Opening port " + portNames[i]);
                bool portExists = false;
                foreach (string str in PortHelper.GetPorts())
                {
                    if (str == portNames[i])
                    {
                        portExists = true;
                        break;
                    }
                }
                if (!portExists)
                {
                    Debug.WriteLine("Real Port does not exist. Ignore the output from SerialPort.GetPortNames()");
                    continue;
                }
                using (SerialPort serialPort = new SerialPort(portNames[i]))
                {
                    try
                    {
                        serialPort.Open();
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
        #endregion
    }
}


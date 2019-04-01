// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Linq;
using System.Text;
using Legacy.Support;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class GetPortNames : PortsTest
    {
        #region Test Cases

        /// <summary>
        /// Check that all ports either open correctly or fail with UnauthorizedAccessException (which implies they're already open)
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop] // Occasionally flaky on UAP: https://github.com/dotnet/corefx/issues/32077
        public void OpenEveryPortName()
        {
            foreach (string portName in SerialPort.GetPortNames())
            {
                Debug.WriteLine("Opening port " + portName);
                using (SerialPort serialPort = new SerialPort(portName))
                {
                    try
                    {
                        serialPort.Open();
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        /// <summary>
        /// Test that SerialPort.GetPortNames finds every port that the test helpers have found.
        /// (On Windows, the latter uses a different technique to SerialPort to find ports).
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void AllHelperPortsAreInGetPortNames()
        {
            if (PlatformDetection.IsWindows && PlatformDetection.IsArmOrArm64Process)
            {
                // ActiveIssue: 35722
                throw new SkipTestException("Port detection broken on Windows IoT");
            }

            string[] serialPortNames = SerialPort.GetPortNames();
            foreach (string helperPortName in PortHelper.GetPorts())
            {
                Assert.True(serialPortNames.Contains(helperPortName),
                    $"{helperPortName} is not present in SerialPort.GetPortNames result\r\n{PortInformationString}");
            }
        }

        /// <summary>
        /// Test that the test helpers have found every port that SerialPort.GetPortNames has found
        /// This catches regressions in the test helpers, eg GH #18928 / #20668
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void AllGetPortNamesAreInHelperPorts()
        {
            string[] helperPortNames = PortHelper.GetPorts();
            foreach (string serialPortName in SerialPort.GetPortNames())
            {
                Assert.True(helperPortNames.Contains(serialPortName),
                    $"{serialPortName} is not present in PortHelper.GetPorts result\r\n{PortInformationString}");
            }
        }

        #endregion

        static string PortInformationString
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("PortHelper Ports: " + string.Join(",", PortHelper.GetPorts()));
                sb.AppendLine("SerialPort Ports: " + string.Join(",", SerialPort.GetPortNames()));
                return sb.ToString();
            }
        }
    }
}


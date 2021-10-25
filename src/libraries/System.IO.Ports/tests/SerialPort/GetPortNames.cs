// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34490", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/28851")]
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
        /// This catches regressions in the test helpers,
        /// eg https://github.com/dotnet/corefx/pull/18928 / https://github.com/dotnet/corefx/pull/20668
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
                sb.AppendLine($"PortHelper Ports: {string.Join(",", PortHelper.GetPorts())}");
                sb.AppendLine($"SerialPort Ports: {string.Join(",", SerialPort.GetPortNames())}");
                return sb.ToString();
            }
        }
    }
}

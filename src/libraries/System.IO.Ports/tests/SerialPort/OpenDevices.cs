// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Linq;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class OpenDevices : PortsTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // see https://github.com/dotnet/runtime/issues/26199#issuecomment-390338721
        public void OpenDevices01()
        {
            DosDevices dosDevices = new DosDevices();
            foreach (var (value, debugLine) in dosDevices.SelectMany(kv => new[] {
                (kv.Key, $"Checking exception thrown with Key {kv.Key}"),
                (kv.Value, $"Checking exception thrown with Value {kv.Value}")
            }))
            {
                if (!string.IsNullOrEmpty(value) && !PortHelper.ComPortRegex().IsMatch(value))
                {
                    using (SerialPort com1 = new SerialPort(value))
                    {
                        Debug.WriteLine(debugLine);
                        Assert.ThrowsAny<Exception>(() => com1.Open());
                    }
                }
            }
        }
    }
}

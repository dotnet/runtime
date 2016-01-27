// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class Environment_MachineName
    {
        [Fact]
        public void TestMachineNameProperty()
        {
            string computerName = GetComputerName();
            Assert.Equal(computerName, Environment.MachineName);
        }

        internal static string GetComputerName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetEnvironmentVariable("COMPUTERNAME");
            }
            else
            {
                return Interop.Sys.GetNodeName();
            }
        }
    }
}
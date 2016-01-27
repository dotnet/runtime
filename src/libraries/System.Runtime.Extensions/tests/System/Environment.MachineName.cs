// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class Environment_MachineName
    {
        [ActiveIssue(5732, PlatformID.OSX)]
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
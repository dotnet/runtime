// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
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
#if !Unix
            return Environment.GetEnvironmentVariable("COMPUTERNAME");
#else
            if (PlatformDetection.IsBrowser)
                return "localhost";
            string temp = Interop.Sys.GetHostName();
            int index = temp.IndexOf('.');
            return index < 0 ? temp : temp.Substring(0, index);
#endif
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Net.Primitives.PalTests
{
    public class InterfaceInfoPalTests
    {
        public static IEnumerable<object[]> InterfaceNames()
        {
            // By default, Linux will have a network interface named "lo" or "lo0".
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                yield return new object[] { "lo0", NetworkInterface.IPv6LoopbackInterfaceIndex };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                yield return new object[] { "lo", NetworkInterface.IPv6LoopbackInterfaceIndex };
            }
        }

        [Theory]
        [MemberData(nameof(InterfaceNames))]
        public void Interface_Name_Round_Trips_To_Index(string ifName, uint ifIdx)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(ifName);

            Assert.Equal(ifIdx, InterfaceInfoPal.InterfaceNameToIndex(ifName.AsSpan()));
            Assert.Equal(ifIdx, InterfaceInfoPal.InterfaceNameToIndex<byte>(utf8Bytes));
        }
    }
}

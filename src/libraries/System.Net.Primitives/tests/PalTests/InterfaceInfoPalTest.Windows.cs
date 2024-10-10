// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using Xunit;

namespace System.Net.Primitives.PalTests
{
    public class InterfaceInfoPalTests
    {
        private static string GetInterfaceName(uint ifIdx)
        {
            ulong loopbackLuid = 0;

            if (Interop.IpHlpApi.ConvertInterfaceIndexToLuid(ifIdx, ref loopbackLuid) == 0)
            {
                Span<char> ifNameBuffer = stackalloc char[256];

                if (Interop.IpHlpApi.ConvertInterfaceLuidToName(loopbackLuid, ifNameBuffer, ifNameBuffer.Length) == 0)
                {
                    int nullTerminatorIdx = ifNameBuffer.IndexOf('\0');

                    if (nullTerminatorIdx != -1)
                    {
                        Span<char> ifName = ifNameBuffer.Slice(0, nullTerminatorIdx);

                        return ifName.ToString();
                    }
                }
            }

            return null;
        }

        public static IEnumerable<object[]> InterfaceNames()
        {
            // Windows will usually name its loopback interface "loopback_0", except when it runs in a container.
            string loopbackInterface = GetInterfaceName((uint)NetworkInterface.IPv6LoopbackInterfaceIndex);

            if (loopbackInterface != null)
            {
                yield return new object[] { loopbackInterface, NetworkInterface.IPv6LoopbackInterfaceIndex };
            }

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                uint ifIdx;
                string ifName;

                if (ni.Supports(NetworkInterfaceComponent.IPv6))
                {
                    ifIdx = (uint)ni.GetIPProperties().GetIPv6Properties().Index;
                }
                else if (ni.Supports(NetworkInterfaceComponent.IPv4))
                {
                    ifIdx = (uint)ni.GetIPProperties().GetIPv4Properties().Index;
                }
                else
                {
                    continue;
                }

                ifName = GetInterfaceName(ifIdx);
                if (ifName != null)
                {
                    yield return new object[] { ifName, ifIdx };
                }
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

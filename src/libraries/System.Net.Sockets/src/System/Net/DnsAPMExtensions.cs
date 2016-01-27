// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Net.Sockets
{
    internal static class DnsAPMExtensions
    {
        public static IAsyncResult BeginGetHostAddresses(string hostNameOrAddress, AsyncCallback requestCallback, object state)
        {
            return TaskToApm.Begin(Dns.GetHostAddressesAsync(hostNameOrAddress), requestCallback, state);
        }

        public static IPAddress[] EndGetHostAddresses(IAsyncResult asyncResult)
        {
            return TaskToApm.End<IPAddress[]>(asyncResult);
        }
    }
}

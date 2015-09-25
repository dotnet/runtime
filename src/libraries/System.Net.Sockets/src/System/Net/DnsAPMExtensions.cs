// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

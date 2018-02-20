// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal sealed class DnsResolveAsyncResult : ContextAwareResult
    {
        internal string HostName { get; }        
        internal IPAddress IpAddress { get; }

        // Forward lookup
        internal DnsResolveAsyncResult(string hostName, object myObject, object myState, AsyncCallback myCallBack)
            : base(myObject, myState, myCallBack)
        {
            HostName = hostName;            
        }

        // Reverse lookup
        internal DnsResolveAsyncResult(IPAddress ipAddress, object myObject, object myState, AsyncCallback myCallBack)
            : base(myObject, myState, myCallBack)
        {
            IpAddress = ipAddress;
        }
    }
}

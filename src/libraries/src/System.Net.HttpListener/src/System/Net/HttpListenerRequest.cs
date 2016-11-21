// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    public sealed unsafe partial class HttpListenerRequest
    {
        public Guid RequestTraceIdentifier
        {
            get
            {
                Guid guid = new Guid();
                *(1 + (ulong*)&guid) = RequestId;
                return guid;
            }
        }
    }
}

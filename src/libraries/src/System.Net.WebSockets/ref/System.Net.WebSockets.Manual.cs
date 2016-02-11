// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.WebSockets
{
    public sealed partial class WebSocketException : Exception
    {
        // Following property was added after removing the Win32Exception base class.
        public int ErrorCode { get { return default(int); } }
    }
}


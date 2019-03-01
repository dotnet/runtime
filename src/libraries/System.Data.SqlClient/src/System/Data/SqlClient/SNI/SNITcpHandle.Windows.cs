// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace System.Data.SqlClient.SNI
{
    internal partial class SNITcpHandle
    {
        internal static void SetKeepAliveValues(ref Socket socket)
        {
            //This method will later be setting the KeepAlive, TcpKeepAliveInterval and TcpKeepAliveTime based on Windows platform specific checks. 
            // Link to issue: https://github.com/dotnet/corefx/issues/33209
        }
    }
}

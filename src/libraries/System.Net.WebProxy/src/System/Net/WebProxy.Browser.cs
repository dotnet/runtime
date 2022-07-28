// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Net
{
    public partial class WebProxy : IWebProxy, ISerializable
    {
        private static bool IsLocal(Uri host)
        {
            if (host.IsLoopback)
            {
                return true;
            }

            string hostString = host.Host;
            return
                !IPAddress.TryParse(hostString, out _) &&
                !hostString.Contains('.');
        }
    }
}

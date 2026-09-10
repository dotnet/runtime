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

            // Use IdnHost rather than Host so that any non-ASCII dot separators (e.g. U+3002) are normalized to '.'.
            string hostString = host.IdnHost;
            return
                !IPAddress.IsValid(hostString) &&
                !hostString.Contains('.');
        }
    }
}

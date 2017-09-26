// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net
{
    internal static class IPAddressFakeExtensions
    {
        public static bool TryWriteBytes(this IPAddress address, Span<byte> destination, out int bytesWritten)
        {
            byte[] bytes = address.GetAddressBytes();
            if (bytes.Length >= destination.Length)
            {
                new ReadOnlySpan<byte>(bytes).CopyTo(destination);
                bytesWritten = bytes.Length;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }
    }
}

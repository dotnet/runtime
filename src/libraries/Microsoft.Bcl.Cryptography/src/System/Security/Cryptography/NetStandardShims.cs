// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace System.Security.Cryptography
{
    internal static class NetStandardShims
    {
        internal static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> str, Span<byte> destination)
        {
            if (str.IsEmpty)
            {
                return 0;
            }

            fixed (char* pStr = str)
            fixed (byte* pDestination = destination)
            {
                return encoding.GetBytes(pStr, str.Length, pDestination, destination.Length);
            }
        }
    }

    internal static class CryptographicOperations
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ZeroMemory(Span<byte> buffer)
        {
            buffer.Clear();
        }
    }
}

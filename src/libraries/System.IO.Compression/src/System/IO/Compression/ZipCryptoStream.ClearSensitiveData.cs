// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        // NoOptimize to prevent the optimizer from deciding this call is unnecessary.
        // NoInlining to prevent the inliner from forgetting that the method was no-optimize.
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void ClearSensitiveData(Span<byte> buffer) => buffer.Clear();
    }
}

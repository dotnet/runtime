// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Utf8Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text) => text.DangerousGetMutableSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start) =>
            text.DangerousGetMutableSpan().Slice(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start, int length) =>
            text.DangerousGetMutableSpan().Slice(start, length);
    }
}

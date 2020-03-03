// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Utf8Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text) =>
            new ReadOnlySpan<byte>(ref text.DangerousGetMutableReference(), text.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start) =>
            new ReadOnlySpan<byte>(ref text.DangerousGetMutableReference(start), text.Length - start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> CreateSpan(Utf8String text, int start, int length) =>
            new ReadOnlySpan<byte>(ref text.DangerousGetMutableReference(start), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlyMemory<byte> CreateMemoryBytes(Utf8String text, int start, int length) =>
            new ReadOnlyMemory<byte>(text, start, length);
    }
}

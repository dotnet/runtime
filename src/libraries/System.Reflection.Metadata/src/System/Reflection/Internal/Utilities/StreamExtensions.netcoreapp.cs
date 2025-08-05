// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Reflection.Internal
{
    internal static partial class StreamExtensions
    {
        internal static unsafe void ReadExactly(this Stream stream, byte* buffer, int size)
            => stream.ReadExactly(new Span<byte>(buffer, size));
    }
}

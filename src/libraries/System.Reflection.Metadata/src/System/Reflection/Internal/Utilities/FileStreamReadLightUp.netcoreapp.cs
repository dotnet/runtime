// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Reflection.Internal
{
    internal static class FileStreamReadLightUp
    {
        internal static bool IsFileStream(Stream stream) => stream is FileStream;

        internal static unsafe int ReadFile(Stream stream, byte* buffer, int size)
            => stream.Read(new Span<byte>(buffer, size));
    }
}

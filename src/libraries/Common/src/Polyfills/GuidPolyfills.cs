// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="Guid"/>.</summary>
internal static class GuidPolyfills
{
    extension(Guid self)
    {
        public bool TryWriteBytes(Span<byte> destination)
        {
            if (destination.Length < 16)
                return false;

            ref Guid selfRef = ref Unsafe.AsRef(in self);
            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.Write(destination, ref selfRef);
            }
            else
            {
                // slower path for BigEndian
                self.ToByteArray().AsSpan().CopyTo(destination);
            }

            return true;
        }
    }
}

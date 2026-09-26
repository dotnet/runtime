// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Implements the throwing <see cref="IBinaryInteger{TSelf}" /> write methods for types that implement them explicitly.</summary>
    /// <remarks>
    /// Calling a default interface implementation on a value type boxes it unless the JIT removes the box,
    /// so value types implement the methods explicitly and forward here, where the call to the
    /// <c>TryWrite</c> method is constrained and does not box.
    /// </remarks>
    internal static class BinaryIntegerWriter
    {
        public static int WriteBigEndian<TSelf>(TSelf value, Span<byte> destination)
            where TSelf : IBinaryInteger<TSelf>
        {
            if (!value.TryWriteBigEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }

        public static int WriteLittleEndian<TSelf>(TSelf value, Span<byte> destination)
            where TSelf : IBinaryInteger<TSelf>
        {
            if (!value.TryWriteLittleEndian(destination, out int bytesWritten))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
            return bytesWritten;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    /// <summary>
    /// Allows creating <see cref="Utf8Span"/> instances that wrap <see cref="BoundedMemory{Byte}"/>.
    /// Useful for ensuring an API under test doesn't read past the end of the span.
    /// </summary>
    public sealed class BoundedUtf8Span : IDisposable
    {
        private readonly BoundedMemory<byte> _boundedMemory;

        public BoundedUtf8Span(ReadOnlySpan<char> utf16Data, PoisonPagePlacement placement = PoisonPagePlacement.After)
            : this(u8(utf16Data.ToString()).AsBytes(), placement)
        {
        }

        public BoundedUtf8Span(ReadOnlySpan<byte> utf8Data, PoisonPagePlacement placement = PoisonPagePlacement.After)
        {
            _boundedMemory = BoundedMemory.AllocateFromExistingData(utf8Data, placement);
        }

        public Utf8Span Span => Utf8Span.UnsafeCreateWithoutValidation(_boundedMemory.Span);

        public void Dispose()
        {
            _boundedMemory.Dispose();
        }
    }
}

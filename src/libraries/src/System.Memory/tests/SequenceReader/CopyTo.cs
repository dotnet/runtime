// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using Xunit;

namespace System.Memory.Tests.SequenceReader
{
    public class CopyTo
    {
        [Fact]
        public void TryCopyTo_Empty()
        {
            var reader = new SequenceReader<char>(ReadOnlySequence<char>.Empty);

            // Nothing to nothing is always possible
            Assert.True(reader.TryCopyTo(Span<char>.Empty));

            // Nothing to something doesn't work
            Assert.False(reader.TryCopyTo(new char[1]));
        }

        [Fact]
        public void TryCopyTo_Multisegment()
        {
            ReadOnlySequence<char> chars = SequenceFactory.Create(new char[][] {
                new char[] { 'A'           },
                new char[] { 'B', 'C'      },
                new char[] { 'D', 'E', 'F' }
            });

            ReadOnlySpan<char> linear = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };

            var reader = new SequenceReader<char>(chars);

            // Something to nothing is always possible
            Assert.True(reader.TryCopyTo(Span<char>.Empty));
            Span<char> buffer;

            // Read out ABCDEF, ABCDE, etc.
            for (int i = linear.Length; i > 0; i--)
            {
                buffer = new char[i];
                Assert.True(reader.TryCopyTo(buffer));
                Assert.True(buffer.SequenceEqual(linear.Slice(0, i)));
            }

            buffer = new char[1];

            // Read out one at a time and move through
            for (int i = 0; i < linear.Length; i++)
            {
                Assert.True(reader.TryCopyTo(buffer));
                Assert.True(reader.TryRead(out char value));
                Assert.Equal(buffer[0], value);
            }

            // Trying to get more data than there is will fail
            Assert.False(reader.TryCopyTo(new char[reader.Remaining + 1]));
        }
    }
}

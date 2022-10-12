// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class XxHash3Tests
    {
        [Fact]
        public void Hash_InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => XxHash3.Hash(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => XxHash3.Hash(null, 42));

            AssertExtensions.Throws<ArgumentException>("destination", () => XxHash3.Hash(new byte[] { 1, 2, 3 }, new byte[7]));
        }

        [Fact]
        public void Hash_OneShot_Expected()
        {
            byte[] destination = new byte[8];

            // Run each test case.  This doesn't use a Theory to avoid bloating the xunit output with thousands of cases.
            foreach ((ulong Hash, long Seed, string Ascii) test in TestCases())
            {
                byte[] input = Encoding.ASCII.GetBytes(test.Ascii);

                // Validate `byte[] XxHash3.Hash` with and without a seed
                if (test.Seed == 0)
                {
                    Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(XxHash3.Hash(input)));
                    Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(XxHash3.Hash((ReadOnlySpan<byte>)input)));
                }
                Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(XxHash3.Hash(input, test.Seed)));
                Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(XxHash3.Hash((ReadOnlySpan<byte>)input, test.Seed)));

                Assert.False(XxHash3.TryHash(input, destination.AsSpan(0, destination.Length - 1), out int bytesWritten, test.Seed));
                Assert.Equal(0, bytesWritten);

                // Validate `XxHash3.TryHash` with and without a seed
                if (test.Seed == 0)
                {
                    Array.Clear(destination, 0, destination.Length);
                    Assert.True(XxHash3.TryHash(input, destination, out bytesWritten));
                    Assert.Equal(8, bytesWritten);
                    Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(destination));
                }
                Array.Clear(destination, 0, destination.Length);
                Assert.True(XxHash3.TryHash(input, destination, out bytesWritten, test.Seed));
                Assert.Equal(8, bytesWritten);
                Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(destination));

                // Validate `XxHash3.Hash(span, out int)` with and without a seed
                if (test.Seed == 0)
                {
                    Array.Clear(destination, 0, destination.Length);
                    Assert.Equal(8, XxHash3.Hash(input, destination));
                    Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(destination));
                }
                Array.Clear(destination, 0, destination.Length);
                Assert.Equal(8, XxHash3.Hash(input, destination, test.Seed));
                Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(destination));
            }
        }

        [Fact]
        public void Hash_Streaming_Expected()
        {
            var rand = new Random(42);
            byte[] destination = new byte[8], destination2 = new byte[8];

            // Run each test case.  This doesn't use a Theory to avoid bloating the xunit output with thousands of cases.
            foreach ((ulong Hash, long Seed, string Ascii) test in TestCases().OrderBy(t => t.Seed))
            {
                // Validate the seeded constructor as well as the unseeded constructor if the seed is 0.
                int ctorIterations = test.Seed == 0 ? 2 : 1;
                for (int ctorIteration = 0; ctorIteration < ctorIterations; ctorIteration++)
                {
                    XxHash3 hash = ctorIteration == 0 ?
                        new XxHash3(test.Seed) :
                        new XxHash3();

                    byte[] asciiBytes = Encoding.ASCII.GetBytes(test.Ascii);

                    // Run the hashing twice, once with the initially-constructed object and once with it reset.
                    for (int trial = 0; trial < 2; trial++)
                    {
                        // Append the data from the source in randomly-sized chunks.
                        ReadOnlySpan<byte> input = asciiBytes;
                        int processed = 0;
                        while (!input.IsEmpty)
                        {
                            ReadOnlySpan<byte> slice = input.Slice(0, rand.Next(0, input.Length) + 1);
                            hash.Append(slice);
                            input = input.Slice(slice.Length);
                            processed += slice.Length;

                            // Validate that the hash we get from doing a one-shot of all the data up to this point
                            // matches the incremental hash for the data appended until now.
                            Assert.True(hash.TryGetCurrentHash(destination, out int bytesWritten));
                            Assert.Equal(8, XxHash3.Hash(asciiBytes.AsSpan(0, processed), destination2, test.Seed));
                            AssertExtensions.SequenceEqual(destination, destination2);
                            Assert.Equal(8, bytesWritten);
                        }

                        // Validate the final hash code.
                        Array.Clear(destination, 0, destination.Length);
                        Assert.Equal(8, hash.GetHashAndReset(destination));
                        Assert.Equal(test.Hash, BinaryPrimitives.ReadUInt64LittleEndian(destination));
                    }
                }
            }
        }

        private static IEnumerable<(ulong Hash, long Seed, string Ascii)> TestCases()
        {
            yield return (Hash: 0x2d06800538d394c2UL, Seed: 0x0000000000000000L, Ascii: "");
        }
    }
}

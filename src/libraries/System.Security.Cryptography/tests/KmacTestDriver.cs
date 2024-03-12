// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public interface IKmacTrait<TKmac> where TKmac : IDisposable
    {
        static abstract TKmac Create(ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString);
        static abstract TKmac Create(byte[] key, byte[] customizationString);
        static abstract bool IsSupported { get; }
        static abstract void AppendData(TKmac kmac, byte[] data);
        static abstract void AppendData(TKmac kmac, ReadOnlySpan<byte> data);
        static abstract byte[] GetHashAndReset(TKmac kmac, int outputLength);
        static abstract void GetHashAndReset(TKmac kmac, Span<byte> destination);
        static abstract byte[] GetCurrentHash(TKmac kmac, int outputLength);
        static abstract void GetCurrentHash(TKmac kmac, Span<byte> destination);

        static abstract byte[] HashData(byte[] key, byte[] source, int outputLength, byte[] customizationString);
        static abstract byte[] HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, int outputLength, ReadOnlySpan<byte> customizationString);
        static abstract void HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, ReadOnlySpan<byte> customizationString);

        static abstract byte[] HashData(byte[] key, Stream source, int outputLength, byte[] customizationString);
        static abstract byte[] HashData(ReadOnlySpan<byte> key, Stream source, int outputLength, ReadOnlySpan<byte> customizationString);
        static abstract void HashData(ReadOnlySpan<byte> key, Stream source, Span<byte> destination, ReadOnlySpan<byte> customizationString);

        static abstract ValueTask HashDataAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            ReadOnlyMemory<byte> customizationString,
            CancellationToken cancellationToken);

        static abstract ValueTask<byte[]> HashDataAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            int outputLength,
            ReadOnlyMemory<byte> customizationString,
            CancellationToken cancellationToken);

        static abstract ValueTask<byte[]> HashDataAsync(
            byte[] key,
            Stream source,
            int outputLength,
            byte[] customizationString,
            CancellationToken cancellationToken);
    }

    public abstract class KmacTestDriver<TKmacTrait, TKmac>
        where TKmacTrait : IKmacTrait<TKmac>
        where TKmac : IDisposable
    {
        // Test vectors can be generated with the following shell script. Change the algorithm and xof variables as needed.
        //
        // #!/usr/bin/env zsh
        //
        // algorithm="KMAC128" #KMAC128 or KMAC256
        // xof="0" #0 or 1

        // for keylen in 4 16 32
        // do
        //     key=$(seq 1 $keylen | awk '{ printf "%02x", $1 }')
        //
        //     for size in 1 {8..264..16}
        //     do
        //         for cs in 0 8
        //         do
        //             if [ $cs -eq 0 ]; then
        //                 custom=""
        //             else
        //                 custom=$(seq 1 $cs | awk '{ printf "%02x", $1 }')
        //             fi
        //
        //             for msg in "" "hello" "goodbye"
        //             do
        //                 msghex=$(echo -n $msg | xxd -p)
        //                 mac=$(echo -e -n $msg | openssl mac -macopt hexkey:$key -macopt size:$size -macopt hexcustom:$custom -macopt xof:$xof $algorithm | awk '{print tolower($0)}')
        //                 echo -n "yield return new(Key: \"$key\", Msg: \"$msghex\", Custom: \"$custom\", Mac: \"$mac\");\n"
        //             done
        //         done
        //     done
        // done
        protected abstract IEnumerable<KmacTestVector> TestVectors { get; }

        public static bool IsSupported => TKmacTrait.IsSupported;
        public static bool IsNotSupported => !IsSupported;
        public static KeySizes? PlatformKeySizeRequirements { get; } =
            PlatformDetection.IsOpenSslSupported ? new KeySizes(4, 512, 1) : null;

        public static int? PlatformMaxOutputSize { get; } = PlatformDetection.IsOpenSslSupported ? 0xFFFFFF / 8 : null;
        public static int? PlatformMaxCustomizationStringSize { get; } = PlatformDetection.IsOpenSslSupported ? 512 : null;

        public static byte[] MinimalKey { get; } =
            PlatformKeySizeRequirements?.MinSize is int min ? new byte[min] : Array.Empty<byte>();

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_AllAtOnce()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (TKmac kmac = TKmacTrait.Create(testVector.KeyBytes, testVector.CustomBytes))
                {
                    TKmacTrait.AppendData(kmac, testVector.MsgBytes);
                    byte[] mac = TKmacTrait.GetHashAndReset(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }

                using (TKmac kmac = TKmacTrait.Create(testVector.KeyBytes, testVector.CustomBytes))
                {
                    TKmacTrait.AppendData(kmac, new ReadOnlySpan<byte>(testVector.MsgBytes));
                    byte[] mac = TKmacTrait.GetHashAndReset(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_Chunks()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (TKmac kmac = TKmacTrait.Create(testVector.KeyBytes, testVector.CustomBytes))
                {
                    foreach (byte[] chunk in testVector.MsgBytes.Chunk(3))
                    {
                        TKmacTrait.AppendData(kmac, chunk);
                    }

                    byte[] mac = TKmacTrait.GetHashAndReset(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_Reused()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (TKmac kmac = TKmacTrait.Create(testVector.KeyBytes, testVector.CustomBytes))
                {
                    TKmacTrait.AppendData(kmac, testVector.MsgBytes);
                    byte[] mac = TKmacTrait.GetHashAndReset(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);

                    TKmacTrait.AppendData(kmac, testVector.MsgBytes);
                    mac = TKmacTrait.GetHashAndReset(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_GetCurrentHash_ByteArray()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (TKmac kmac = TKmacTrait.Create(testVector.KeyBytes, testVector.CustomBytes))
                {
                    TKmacTrait.AppendData(kmac, testVector.MsgBytes);
                    byte[] mac = TKmacTrait.GetCurrentHash(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);

                    mac = TKmacTrait.GetCurrentHash(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);

                    mac = TKmacTrait.GetHashAndReset(kmac, testVector.Mac.Length / 2);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_Hash_Destination()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                byte[] mac = new byte[testVector.MacBytes.Length];

                using (TKmac kmac = TKmacTrait.Create(testVector.KeyBytes, testVector.CustomBytes))
                {
                    TKmacTrait.AppendData(kmac, testVector.MsgBytes);
                    TKmacTrait.GetCurrentHash(kmac, mac);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);

                    TKmacTrait.GetCurrentHash(kmac, mac);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);

                    TKmacTrait.GetHashAndReset(kmac, mac);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void Create_CustomizationStringNullIsEmpty()
        {
            int OutputLength = 32;
            byte[] macWithNullCustomizationString;
            byte[] macWithEmptyCustomizationString;

            using (TKmac kmac = TKmacTrait.Create(MinimalKey, (byte[])null))
            {
                TKmacTrait.AppendData(kmac, "habaneros"u8);
                macWithNullCustomizationString = TKmacTrait.GetHashAndReset(kmac, OutputLength);
            }

            using (TKmac kmac = TKmacTrait.Create(MinimalKey, Array.Empty<byte>()))
            {
                TKmacTrait.AppendData(kmac, "habaneros"u8);
                macWithEmptyCustomizationString = TKmacTrait.GetHashAndReset(kmac, OutputLength);
            }

            Assert.Equal(macWithEmptyCustomizationString, macWithNullCustomizationString);
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetHashAndReset_PerformsReset_Span()
        {
            const int OutputLength = 32;
            ReadOnlySpan<byte> customizationString = [];
            ReadOnlySpan<byte> data = "habaneros"u8;
            ReadOnlySpan<byte> noData = [];
            ReadOnlySpan<byte> expected = TKmacTrait.HashData(MinimalKey, data, OutputLength, customizationString);
            ReadOnlySpan<byte> expectedNoData = TKmacTrait.HashData(MinimalKey, noData, OutputLength, customizationString);

            using (TKmac kmac = TKmacTrait.Create(new ReadOnlySpan<byte>(MinimalKey), customizationString))
            {
                Span<byte> mac = stackalloc byte[OutputLength];
                TKmacTrait.AppendData(kmac, data);
                TKmacTrait.GetHashAndReset(kmac, mac);
                AssertExtensions.SequenceEqual(expected, mac);

                TKmacTrait.GetCurrentHash(kmac, mac);
                AssertExtensions.SequenceEqual(expectedNoData, mac);

                TKmacTrait.GetHashAndReset(kmac, mac);
                AssertExtensions.SequenceEqual(expectedNoData, mac);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetHashAndReset_PerformsReset_Array()
        {
            const int OutputLength = 32;
            byte[] customizationString = [];
            byte[] data = "habaneros"u8.ToArray();
            byte[] noData = [];
            byte[] expected = TKmacTrait.HashData(MinimalKey, data, OutputLength, customizationString);
            byte[] expectedNoData = TKmacTrait.HashData(MinimalKey, noData, OutputLength, customizationString);

            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString))
            {
                byte[] mac;
                TKmacTrait.AppendData(kmac, data);
                mac = TKmacTrait.GetHashAndReset(kmac, OutputLength);
                Assert.Equal(expected, mac);

                mac = TKmacTrait.GetCurrentHash(kmac, OutputLength);
                Assert.Equal(expectedNoData, mac);

                mac = TKmacTrait.GetHashAndReset(kmac, OutputLength);
                Assert.Equal(expectedNoData, mac);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetCurrentHash_Minimal_Bytes()
        {
            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: Array.Empty<byte>()))
            {
                TKmacTrait.AppendData(kmac, Array.Empty<byte>());
                byte[] result = TKmacTrait.GetCurrentHash(kmac, outputLength: 0);
                Assert.Empty(result);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetCurrentHash_Minimal_Span()
        {
            using (TKmac kmac = TKmacTrait.Create(new ReadOnlySpan<byte>(MinimalKey), customizationString: default(ReadOnlySpan<byte>)))
            {
                TKmacTrait.AppendData(kmac, default(ReadOnlySpan<byte>));
                Span<byte> mac = Span<byte>.Empty;

                // Assert.NoThrow
                TKmacTrait.GetCurrentHash(kmac, mac);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetCurrentHash_ExistingStatePreserved_Span()
        {
            const int OutputLength = 32;
            ReadOnlySpan<byte> customizationString = [];
            ReadOnlySpan<byte> data = "habaneros"u8;
            ReadOnlySpan<byte> expected = TKmacTrait.HashData(MinimalKey, data, OutputLength, customizationString);

            using (TKmac kmac = TKmacTrait.Create(new ReadOnlySpan<byte>(MinimalKey), customizationString))
            {
                Span<byte> mac = stackalloc byte[OutputLength];

                int i = 0;
                for (; i < data.Length - 1; i++)
                {
                    TKmacTrait.AppendData(kmac, data.Slice(i, 1));
                    TKmacTrait.GetCurrentHash(kmac, mac);
                    Assert.False(expected.SequenceEqual(mac), "expected.SequenceEqual(mac)");
                }

                TKmacTrait.AppendData(kmac, data.Slice(i, 1));
                TKmacTrait.GetCurrentHash(kmac, mac);
                AssertExtensions.SequenceEqual(expected, mac);

                TKmacTrait.GetHashAndReset(kmac, mac);
                AssertExtensions.SequenceEqual(expected, mac);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetCurrentHash_ExistingStatePreserved_Bytes()
        {
            int OutputLength = 32;
            byte[] customizationString = [];
            byte[] data = "habaneros"u8.ToArray();
            byte[] expected = TKmacTrait.HashData(MinimalKey, data, OutputLength, customizationString);

            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString))
            {
                byte[] mac;
                int i = 0;
                for (; i < data.Length - 1; i++)
                {
                    TKmacTrait.AppendData(kmac, data.AsSpan(i, 1).ToArray());
                    mac = TKmacTrait.GetCurrentHash(kmac, OutputLength);
                    Assert.NotEqual(expected, mac);
                }

                TKmacTrait.AppendData(kmac, data.AsSpan(i, 1).ToArray());
                mac = TKmacTrait.GetCurrentHash(kmac, OutputLength);
                Assert.Equal(expected, mac);

                mac = TKmacTrait.GetHashAndReset(kmac, OutputLength);
                Assert.Equal(expected, mac);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetHashAndReset_Minimal_Bytes()
        {
            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: Array.Empty<byte>()))
            {
                TKmacTrait.AppendData(kmac, Array.Empty<byte>());
                byte[] result = TKmacTrait.GetHashAndReset(kmac, outputLength: 0);
                Assert.Empty(result);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetHashAndReset_Minimal_Span()
        {
            using (TKmac kmac = TKmacTrait.Create(new ReadOnlySpan<byte>(MinimalKey), customizationString: default(ReadOnlySpan<byte>)))
            {
                TKmacTrait.AppendData(kmac, default(ReadOnlySpan<byte>));
                Span<byte> mac = Span<byte>.Empty;

                // Assert.NoThrow
                TKmacTrait.GetHashAndReset(kmac, mac);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetHashAndReset_ResetWithEmpty()
        {
            const int OutputLength = 64;

            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: Array.Empty<byte>()))
            {
                TKmacTrait.AppendData(kmac, "habaneros"u8);
                byte[] expected = TKmacTrait.GetHashAndReset(kmac, OutputLength);
                Assert.Equal(OutputLength, expected.Length);

                TKmacTrait.AppendData(kmac, "habaneros"u8);
                byte[] mac = TKmacTrait.GetHashAndReset(kmac, outputLength: 0); // Reset with empty buffer should still reset.
                Assert.Empty(mac);

                TKmacTrait.AppendData(kmac, "habaneros"u8);
                mac = TKmacTrait.GetHashAndReset(kmac, OutputLength);
                Assert.Equal(expected, mac);
            }

            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: Array.Empty<byte>()))
            {
                byte[] expected = new byte[OutputLength];
                TKmacTrait.AppendData(kmac, "habaneros"u8);
                TKmacTrait.GetHashAndReset(kmac, expected);

                scoped Span<byte> mac = Span<byte>.Empty;
                TKmacTrait.AppendData(kmac, "habaneros"u8);
                TKmacTrait.GetHashAndReset(kmac, mac); // Reset with empty buffer should still reset.

                mac = stackalloc byte[OutputLength];
                TKmacTrait.AppendData(kmac, "habaneros"u8);
                TKmacTrait.GetHashAndReset(kmac, mac);
                AssertExtensions.SequenceEqual(expected, mac);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task OneShot_HashData_CustomizationStringNullIsEmpty()
        {
            const int OutputLength = 32;
            byte[] source = new byte[1];
            byte[] customizationString = null;
            byte[] expected = TKmacTrait.HashData(MinimalKey, source, OutputLength, customizationString: Array.Empty<byte>());

            byte[] mac = TKmacTrait.HashData(MinimalKey, source, OutputLength, customizationString);
            Assert.Equal(expected, mac);

            mac = TKmacTrait.HashData(MinimalKey, new MemoryStream(source), OutputLength, customizationString);
            Assert.Equal(expected, mac);

            mac = await TKmacTrait.HashDataAsync(
                MinimalKey,
                new MemoryStream(source),
                OutputLength,
                customizationString,
                default(CancellationToken));
            Assert.Equal(expected, mac);
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_ByteArray()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                byte[] mac = TKmacTrait.HashData(
                    testVector.KeyBytes,
                    testVector.MsgBytes,
                    testVector.MacBytes.Length,
                    testVector.CustomBytes);

                Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_ByteArray_SpanInput()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                byte[] mac = TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(testVector.KeyBytes),
                    new ReadOnlySpan<byte>(testVector.MsgBytes),
                    testVector.MacBytes.Length,
                    new ReadOnlySpan<byte>(testVector.CustomBytes));

                Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_JustRight()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                Span<byte> destination = new byte[testVector.MacBytes.Length];

                TKmacTrait.HashData(
                    testVector.KeyBytes,
                    testVector.MsgBytes,
                    destination,
                    testVector.CustomBytes);

                Assert.Equal(testVector.Mac, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_LargerWithOffset()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                Span<byte> buffer = new byte[testVector.MacBytes.Length + 2];
                buffer[0] = 0xFF;
                buffer[^1] = 0xFF;
                Span<byte> destination = buffer[1..^1];

                TKmacTrait.HashData(
                    testVector.KeyBytes,
                    testVector.MsgBytes,
                    destination,
                    testVector.CustomBytes);

                Assert.Equal(testVector.Mac, Convert.ToHexString(destination), ignoreCase: true);
                Assert.Equal(0xFF, buffer[0]);
                Assert.Equal(0xFF, buffer[^1]);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_OverlapExact()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                byte[] buffer = new byte[Math.Max(testVector.MsgBytes.Length, testVector.MacBytes.Length)];
                testVector.MsgBytes.AsSpan().CopyTo(buffer);

                Span<byte> destination = buffer.AsSpan(0, testVector.MacBytes.Length);
                ReadOnlySpan<byte> source = buffer.AsSpan(0, testVector.MsgBytes.Length);
                TKmacTrait.HashData(testVector.KeyBytes, source, destination, testVector.CustomBytes);
                Assert.Equal(testVector.Mac, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_OverlapPartial_MessageBefore()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                byte[] buffer = new byte[Math.Max(testVector.MsgBytes.Length, testVector.MacBytes.Length) + 10];
                testVector.MsgBytes.AsSpan().CopyTo(buffer);

                Span<byte> destination = buffer.AsSpan(10, testVector.MacBytes.Length);
                ReadOnlySpan<byte> source = buffer.AsSpan(0, testVector.MsgBytes.Length);
                TKmacTrait.HashData(testVector.KeyBytes, source, destination, testVector.CustomBytes);
                Assert.Equal(testVector.Mac, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_OverlapPartial_MessageAfter()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                byte[] buffer = new byte[Math.Max(testVector.MsgBytes.Length, testVector.MacBytes.Length) + 10];
                testVector.MsgBytes.AsSpan().CopyTo(buffer.AsSpan(10));

                Span<byte> destination = buffer.AsSpan(0, testVector.MacBytes.Length);
                ReadOnlySpan<byte> source = buffer.AsSpan(10, testVector.MsgBytes.Length);
                TKmacTrait.HashData(testVector.KeyBytes, source, destination, testVector.CustomBytes);
                Assert.Equal(testVector.Mac, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_Stream_ByteArray()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (MemoryStream source = new MemoryStream(testVector.MsgBytes))
                {
                    byte[] mac = TKmacTrait.HashData(
                        testVector.KeyBytes,
                        source,
                        testVector.MacBytes.Length,
                        testVector.CustomBytes);

                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }

                using (MemoryStream source = new MemoryStream(testVector.MsgBytes))
                {
                    byte[] mac = TKmacTrait.HashData(
                        new ReadOnlySpan<byte>(testVector.KeyBytes),
                        source,
                        testVector.MacBytes.Length,
                        new ReadOnlySpan<byte>(testVector.CustomBytes));

                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_Stream_Destination()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (MemoryStream source = new MemoryStream(testVector.MsgBytes))
                {
                    byte[] destination = new byte[testVector.MacBytes.Length];
                    TKmacTrait.HashData(testVector.KeyBytes, source, destination, testVector.CustomBytes);
                    Assert.Equal(testVector.Mac, Convert.ToHexString(destination), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task KnownAnswerTests_OneShot_HashData_StreamAsync_ByteArray()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (MemoryStream source = new MemoryStream(testVector.MsgBytes))
                {
                    byte[] mac = await TKmacTrait.HashDataAsync(
                        testVector.KeyBytes,
                        source,
                        testVector.MacBytes.Length,
                        testVector.CustomBytes,
                        default(CancellationToken));

                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }

                using (MemoryStream source = new MemoryStream(testVector.MsgBytes))
                {
                    byte[] mac = await TKmacTrait.HashDataAsync(
                        new ReadOnlyMemory<byte>(testVector.KeyBytes),
                        source,
                        testVector.MacBytes.Length,
                        new ReadOnlyMemory<byte>(testVector.CustomBytes),
                        default(CancellationToken));

                    Assert.Equal(testVector.Mac, Convert.ToHexString(mac), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task KnownAnswerTests_OneShot_HashData_StreamAsync_Destination()
        {
            foreach (KmacTestVector testVector in TestVectors)
            {
                using (MemoryStream source = new MemoryStream(testVector.MsgBytes))
                {
                    byte[] destination = new byte[testVector.MacBytes.Length];

                    await TKmacTrait.HashDataAsync(
                        testVector.KeyBytes,
                        source, destination,
                        testVector.CustomBytes,
                        default(CancellationToken));

                    Assert.Equal(testVector.Mac, Convert.ToHexString(destination), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_OneShot_HashData_OutputLengthNegative()
        {
            byte[] source = new byte[1];
            byte[] customizationString = [];

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TKmacTrait.HashData(MinimalKey, source, outputLength: -1, customizationString));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    new ReadOnlySpan<byte>(source),
                    outputLength: -1,
                    new ReadOnlySpan<byte>(customizationString)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    Stream.Null,
                    outputLength: -1,
                    new ReadOnlySpan<byte>(customizationString)));

            // These asserts are not async - argument validation should occur synchronously.
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    Stream.Null,
                    outputLength: -1,
                    new ReadOnlyMemory<byte>(customizationString),
                    default(CancellationToken)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TKmacTrait.HashDataAsync(
                    MinimalKey,
                    Stream.Null,
                    outputLength: -1,
                    customizationString,
                    default(CancellationToken)));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_OneShot_HashData_StreamNotReadable()
        {
            byte[] buffer = new byte[1];
            byte[] customizationString = [];

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TKmacTrait.HashData(
                    MinimalKey,
                    UntouchableStream.Instance,
                    buffer,
                    customizationString));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TKmacTrait.HashData(
                    MinimalKey,
                    UntouchableStream.Instance,
                    outputLength: 1,
                    customizationString));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    UntouchableStream.Instance,
                    outputLength: 1,
                    new ReadOnlySpan<byte>(customizationString)));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    UntouchableStream.Instance,
                    new Memory<byte>(buffer),
                    new ReadOnlyMemory<byte>(customizationString),
                    default(CancellationToken)));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TKmacTrait.HashDataAsync(
                    MinimalKey,
                    UntouchableStream.Instance,
                    outputLength: 1,
                    customizationString,
                    default(CancellationToken)));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    UntouchableStream.Instance,
                    outputLength: 1,
                    new ReadOnlyMemory<byte>(customizationString),
                    default(CancellationToken)));
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task ArgValidation_OneShot_HashDataAsync_Cancelled()
        {
            byte[] buffer = new byte[1];
            byte[] customizationString = [];
            CancellationToken cancelledToken = new CancellationToken(canceled: true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    Stream.Null,
                    new Memory<byte>(buffer),
                    new ReadOnlyMemory<byte>(customizationString),
                    cancelledToken));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await TKmacTrait.HashDataAsync(
                    MinimalKey,
                    Stream.Null,
                    outputLength: 1,
                    customizationString,
                    cancelledToken));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    Stream.Null,
                    outputLength: 1,
                    new ReadOnlyMemory<byte>(customizationString),
                    cancelledToken));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_OneShot_HashData_SourceNull()
        {
            byte[] customizationString = [];
            byte[] destination = new byte[1];

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TKmacTrait.HashData(MinimalKey, (byte[])null, outputLength: 1, customizationString));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TKmacTrait.HashData(MinimalKey, (Stream)null, outputLength: 1, customizationString));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    (Stream)null,
                    outputLength: 1,
                    customizationString));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    (Stream)null,
                    destination,
                    customizationString));

            // async is not awaited because argument validation should happen synchronously.
            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TKmacTrait.HashDataAsync(
                    MinimalKey,
                    (Stream)null,
                    outputLength: 1,
                    customizationString,
                    default(CancellationToken)));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    (Stream)null,
                    outputLength: 1,
                    customizationString,
                    default(CancellationToken)));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    (Stream)null,
                    destination,
                    customizationString,
                    default(CancellationToken)));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_OneShot_HashData_KeyNull()
        {
            byte[] source = new byte[8];
            byte[] customizationString = [];
            byte[] destination = new byte[1];

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => TKmacTrait.HashData((byte[])null, source, outputLength: 1, customizationString));

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => TKmacTrait.HashData((byte[])null, Stream.Null, outputLength: 1, customizationString));

            // async is not awaited because argument validation should happen synchronously.
            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => TKmacTrait.HashDataAsync(
                    (byte[])null,
                    Stream.Null,
                    outputLength: 1,
                    customizationString,
                    default(CancellationToken)));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_GetCurrentHash_OutputLengthNegative()
        {
            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: null))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => TKmacTrait.GetCurrentHash(kmac, outputLength: -1));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_GetHashAndReset_OutputLengthNegative()
        {
            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: null))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => TKmacTrait.GetHashAndReset(kmac, outputLength: -1));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_AppendData_DataNull()
        {
            using (TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: null))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "data",
                    () => TKmacTrait.AppendData(kmac, (byte[])null));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_UseAfterDispose()
        {
            byte[] buffer = new byte[1];
            TKmac kmac = TKmacTrait.Create(MinimalKey, customizationString: null);
            kmac.Dispose();
            kmac.Dispose(); // Assert.NoThrow

            Assert.Throws<ObjectDisposedException>(() => TKmacTrait.AppendData(kmac, buffer));
            Assert.Throws<ObjectDisposedException>(() => TKmacTrait.AppendData(kmac, new ReadOnlySpan<byte>(buffer)));
            Assert.Throws<ObjectDisposedException>(() => TKmacTrait.GetHashAndReset(kmac, outputLength: 1));
            Assert.Throws<ObjectDisposedException>(() => TKmacTrait.GetHashAndReset(kmac, buffer.AsSpan()));
            Assert.Throws<ObjectDisposedException>(() => TKmacTrait.GetCurrentHash(kmac, outputLength: 1));
            Assert.Throws<ObjectDisposedException>(() => TKmacTrait.GetCurrentHash(kmac, buffer.AsSpan()));
        }

        [ConditionalFact(nameof(IsNotSupported))]
        public void NotSupported_ThrowsPlatformNotSupportedException()
        {
            byte[] source = new byte[1];
            byte[] destination = [];
            byte[] customizationString = [];

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.Create(MinimalKey, customizationString));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.Create(new ReadOnlySpan<byte>(MinimalKey), customizationString));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashData(MinimalKey, source, outputLength: 0, customizationString));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    new ReadOnlySpan<byte>(source),
                    outputLength: 1,
                    new ReadOnlySpan<byte>(customizationString)));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    new ReadOnlySpan<byte>(source),
                    destination,
                    new ReadOnlySpan<byte>(customizationString)));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashData(
                    MinimalKey,
                    Stream.Null,
                    outputLength: 1,
                    customizationString));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    Stream.Null,
                    outputLength: 1,
                    customizationString));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(MinimalKey),
                    Stream.Null,
                    destination,
                    customizationString));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashDataAsync(
                    MinimalKey,
                    Stream.Null,
                    outputLength: 1,
                    customizationString,
                    default(CancellationToken)));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    Stream.Null,
                    outputLength: 1,
                    new ReadOnlyMemory<byte>(customizationString),
                    default(CancellationToken)));

            Assert.Throws<PlatformNotSupportedException>(
                () => TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(MinimalKey),
                    Stream.Null,
                    new Memory<byte>(destination),
                    new ReadOnlyMemory<byte>(customizationString),
                    default(CancellationToken)));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void CryptographicException_Allocated_KeySize()
        {
            if (PlatformKeySizeRequirements?.MinSize - 1 is int smallKey and > 0)
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => TKmacTrait.Create(new byte[smallKey], Array.Empty<byte>()));

                Assert.ThrowsAny<CryptographicException>(
                    () => TKmacTrait.Create(new ReadOnlySpan<byte>(new byte[smallKey]), default(ReadOnlySpan<byte>)));
            }

            if (PlatformKeySizeRequirements?.MaxSize + 1 is int largeKey)
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => TKmacTrait.Create(new byte[largeKey], Array.Empty<byte>()));

                Assert.ThrowsAny<CryptographicException>(
                    () => TKmacTrait.Create(new ReadOnlySpan<byte>(new byte[largeKey]), default(ReadOnlySpan<byte>)));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task CryptographicException_OneShot_KeySize()
        {
            if (PlatformKeySizeRequirements?.MinSize - 1 is int smallKeySize and > 0)
            {
                await AssertOneShotsThrowAnyAsync<CryptographicException>(keySize: smallKeySize);
            }

            if (PlatformKeySizeRequirements?.MaxSize + 1 is int largeKeySize)
            {
                await AssertOneShotsThrowAnyAsync<CryptographicException>(keySize: largeKeySize);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void CryptographicException_Instance_CustomizationStringSize()
        {
            if (PlatformMaxCustomizationStringSize + 1 is int tooBigCustomizationString)
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => TKmacTrait.Create(MinimalKey, new byte[tooBigCustomizationString]));

                Assert.ThrowsAny<CryptographicException>(() =>
                    TKmacTrait.Create(
                        new ReadOnlySpan<byte>(MinimalKey),
                        new ReadOnlySpan<byte>(new byte[tooBigCustomizationString])));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task CryptographicException_OneShot_CustomizationStringSize()
        {
            if (PlatformMaxCustomizationStringSize + 1 is int tooBigCustomizationString)
            {
                await AssertOneShotsThrowAnyAsync<CryptographicException>(
                    customizationStringSize: tooBigCustomizationString);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task CryptographicException_OneShot_OutputSize()
        {
            if (PlatformMaxOutputSize + 1 is int tooBigOutputSize)
            {
                await AssertOneShotsThrowAnyAsync<CryptographicException>(outputSize: tooBigOutputSize);
            }
        }

        [Fact]
        public void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(TKmacTrait.IsSupported, PlatformSupportsKmac());
        }

        private static async Task AssertOneShotsThrowAnyAsync<TException>(
            int? keySize = null,
            int? customizationStringSize = null,
            int outputSize = 1) where TException : Exception
        {
            byte[] key = keySize is int ks ? new byte[ks] : MinimalKey;
            byte[] source = [1];
            byte[] destination = new byte[outputSize];
            byte[] customizationString = new byte[customizationStringSize.GetValueOrDefault()];

            Assert.ThrowsAny<TException>(
                () => TKmacTrait.HashData(key, source, outputSize, customizationString));

            Assert.ThrowsAny<TException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(key),
                    new ReadOnlySpan<byte>(source),
                    outputSize,
                    new ReadOnlySpan<byte>(customizationString)));

            Assert.ThrowsAny<TException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(key),
                    new ReadOnlySpan<byte>(source),
                    destination,
                    new ReadOnlySpan<byte>(customizationString)));

            Assert.ThrowsAny<TException>(
                () => TKmacTrait.HashData(
                    key,
                    Stream.Null,
                    outputSize,
                    customizationString));

            Assert.ThrowsAny<TException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(key),
                    Stream.Null,
                    outputSize,
                    customizationString));

            Assert.ThrowsAny<TException>(
                () => TKmacTrait.HashData(
                    new ReadOnlySpan<byte>(key),
                    Stream.Null,
                    destination,
                    customizationString));

            await Assert.ThrowsAnyAsync<TException>(
                async () => await TKmacTrait.HashDataAsync(
                    key,
                    Stream.Null,
                    outputSize,
                    customizationString,
                    default(CancellationToken)));

            await Assert.ThrowsAnyAsync<TException>(
                async () => await TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(key),
                    Stream.Null,
                    outputSize,
                    new ReadOnlyMemory<byte>(customizationString),
                    default(CancellationToken)));

            await Assert.ThrowsAnyAsync<TException>(
                async () => await TKmacTrait.HashDataAsync(
                    new ReadOnlyMemory<byte>(key),
                    Stream.Null,
                    new Memory<byte>(destination),
                    new ReadOnlyMemory<byte>(customizationString),
                    default(CancellationToken)));
        }

        private static bool PlatformSupportsKmac()
        {
            // This uses the platform version to determine if the platform supports KMAC. The actual implementation
            // uses feature detection, so use a different means of determining support here to make sure the platform
            // version and feature detection agree.

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26016))
            {
                // See HashProviderDispenser.KmacSupported for background on this Windows version requirement.
                return true;
            }

            if (PlatformDetection.IsOpenSslSupported && PlatformDetection.OpenSslVersion.Major >= 3)
            {
                // KMAC on OpenSSL was introduced in OpenSSL 3
                // KMAC on macOS is not supported with an OpenSSL fallback.
                return true;
            }

            return false;
        }
    }

    public record KmacTestVector(string Key, string Msg, string Custom, string Mac)
    {
        public byte[] KeyBytes { get; } = Convert.FromHexString(Key);
        public byte[] MsgBytes { get; } = Convert.FromHexString(Msg);
        public byte[] CustomBytes { get; } = Convert.FromHexString(Custom);
        public byte[] MacBytes { get; } = Convert.FromHexString(Mac);
    }

    public record NistKmacTestVector(string Name, string Key, string Msg, string CustomText, string Mac)
        : KmacTestVector(Key, Msg, Convert.ToHexString(Encoding.UTF8.GetBytes(CustomText)), Mac)
    {
    }
}

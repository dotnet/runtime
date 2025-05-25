// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public interface IShakeTrait<TShake> where TShake : class, IDisposable, new()
    {
        static abstract TShake Create();
        static abstract bool IsSupported { get; }
        static abstract void AppendData(TShake shake, byte[] data);
        static abstract void AppendData(TShake shake, ReadOnlySpan<byte> data);
        static abstract byte[] GetHashAndReset(TShake shake, int outputLength);
        static abstract void GetHashAndReset(TShake shake, Span<byte> destination);
        static abstract byte[] GetCurrentHash(TShake shake, int outputLength);
        static abstract void GetCurrentHash(TShake shake, Span<byte> destination);
        static abstract void Read(TShake shake, Span<byte> destination);
        static abstract byte[] Read(TShake shake, int outputLength);
        static abstract void Reset(TShake shake);
        static abstract TShake Clone(TShake shake);

        static abstract byte[] HashData(byte[] source, int outputLength);
        static abstract byte[] HashData(ReadOnlySpan<byte> source, int outputLength);
        static abstract void HashData(ReadOnlySpan<byte> source, Span<byte> destination);

        static abstract byte[] HashData(Stream source, int outputLength);
        static abstract void HashData(Stream source, Span<byte> destination);
        static abstract ValueTask HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken);
        static abstract ValueTask<byte[]> HashDataAsync(Stream source, int outputLength, CancellationToken cancellationToken);
        static abstract ValueTask HashDataAsync(Stream source, Memory<byte> destination);
        static abstract ValueTask<byte[]> HashDataAsync(Stream source, int outputLength);
    }

    public abstract class ShakeTestDriver<TShakeTrait, TShake>
        where TShakeTrait : IShakeTrait<TShake>
        where TShake : class, IDisposable, new()
    {
        protected abstract IEnumerable<(string Msg, string Output)> Fips202Kats { get; }
        public static bool IsSupported => TShakeTrait.IsSupported;
        public static bool IsNotSupported => !IsSupported;

        public static bool IsReadSupported
        {
            get
            {
                const long OpenSsl_3_3_0 = 0x30300000L;
                return IsSupported && (PlatformDetection.IsWindows || SafeEvpPKeyHandle.OpenSslVersion >= OpenSsl_3_3_0);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_AllAtOnce()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, message);
                    byte[] hash = TShakeTrait.GetHashAndReset(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, new ReadOnlySpan<byte>(message));
                    byte[] hash = TShakeTrait.GetHashAndReset(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_Chunks()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);

                using (TShake shake = new TShake())
                {
                    foreach (byte[] chunk in message.Chunk(15))
                    {
                        TShakeTrait.AppendData(shake, chunk);
                    }

                    byte[] hash = TShakeTrait.GetHashAndReset(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_Reused()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, message);
                    byte[] hash = TShakeTrait.GetHashAndReset(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);

                    TShakeTrait.AppendData(shake, message);
                    hash = TShakeTrait.GetHashAndReset(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_GetCurrentHash_ByteArray()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, message);
                    byte[] hash = TShakeTrait.GetCurrentHash(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);

                    hash = TShakeTrait.GetCurrentHash(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);

                    hash = TShakeTrait.GetHashAndReset(shake, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Allocated_Hash_Destination()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);
                byte[] hash = new byte[kat.Output.Length / 2];

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, message);
                    TShakeTrait.GetCurrentHash(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);

                    TShakeTrait.GetCurrentHash(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);

                    TShakeTrait.GetHashAndReset(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsReadSupported))]
        public void KnownAnswerTests_Allocated_Read_Twice()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, message);
                    Span<byte> hash = new byte[kat.Output.Length / 2];
                    ReadChunked(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);

                    TShakeTrait.Reset(shake);
                    hash.Clear();

                    TShakeTrait.AppendData(shake, message);
                    ReadChunked(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsReadSupported))]
        public void KnownAnswerTests_Allocated_Read_GetHashAndReset()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, message);
                    Span<byte> hash = new byte[kat.Output.Length / 2];
                    ReadChunked(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);

                    TShakeTrait.Reset(shake);
                    hash.Clear();

                    TShakeTrait.AppendData(shake, message);
                    TShakeTrait.GetHashAndReset(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Clone_Independent_Unobserved()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);
                byte[] hash = new byte[kat.Output.Length / 2];

                using (TShake shake = new TShake())
                using (TShake clone = TShakeTrait.Clone(shake))
                {
                    TShakeTrait.AppendData(shake, "badbadbad"u8);

                    TShakeTrait.AppendData(clone, message);
                    TShakeTrait.GetCurrentHash(clone, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Clone_Independent_Disposed()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);
                byte[] hash = new byte[kat.Output.Length / 2];

                TShake shake = new TShake();
                using (TShake clone = TShakeTrait.Clone(shake))
                {
                    shake.Dispose();

                    TShakeTrait.AppendData(clone, message);
                    TShakeTrait.GetCurrentHash(clone, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_Reset()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);
                byte[] hash = new byte[kat.Output.Length / 2];

                using (TShake shake = new TShake())
                {
                    TShakeTrait.AppendData(shake, "badbadbad"u8);
                    TShakeTrait.Reset(shake);
                    TShakeTrait.AppendData(shake, message);
                    TShakeTrait.GetCurrentHash(shake, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_ByteArray()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);
                byte[] hash = TShakeTrait.HashData(message, kat.Output.Length / 2);
                Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_ByteArray_SpanInput()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                ReadOnlySpan<byte> message = Convert.FromHexString(kat.Msg);
                byte[] hash = TShakeTrait.HashData(message, kat.Output.Length / 2);
                Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_JustRight()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);
                Span<byte> destination = new byte[kat.Output.Length / 2];
                TShakeTrait.HashData(message, destination);
                Assert.Equal(kat.Output, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_LargerWithOffset()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] message = Convert.FromHexString(kat.Msg);
                Span<byte> buffer = new byte[kat.Output.Length / 2 + 2];
                buffer[0] = 0xFF;
                buffer[^1] = 0xFF;
                Span<byte> destination = buffer[1..^1];
                TShakeTrait.HashData(message, destination);
                Assert.Equal(kat.Output, Convert.ToHexString(destination), ignoreCase: true);
                Assert.Equal(0xFF, buffer[0]);
                Assert.Equal(0xFF, buffer[^1]);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_OverlapExact()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] buffer = new byte[Math.Max(kat.Msg.Length, kat.Output.Length) / 2];
                Span<byte> message = Convert.FromHexString(kat.Msg);
                message.CopyTo(buffer);

                Span<byte> destination = buffer.AsSpan(0, kat.Output.Length / 2);
                ReadOnlySpan<byte> source = buffer.AsSpan(0, kat.Msg.Length / 2);
                TShakeTrait.HashData(source, destination);
                Assert.Equal(kat.Output, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_OverlapPartial_MessageBefore()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] buffer = new byte[Math.Max(kat.Msg.Length, kat.Output.Length) / 2 + 10];
                Span<byte> message = Convert.FromHexString(kat.Msg);
                message.CopyTo(buffer);

                Span<byte> destination = buffer.AsSpan(10, kat.Output.Length / 2);
                ReadOnlySpan<byte> source = buffer.AsSpan(0, kat.Msg.Length / 2);
                TShakeTrait.HashData(source, destination);
                Assert.Equal(kat.Output, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_SpanBuffer_OverlapPartial_MessageAfter()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] buffer = new byte[Math.Max(kat.Msg.Length, kat.Output.Length) / 2 + 10];
                Span<byte> message = Convert.FromHexString(kat.Msg);
                message.CopyTo(buffer.AsSpan(10));

                Span<byte> destination = buffer.AsSpan(0, kat.Output.Length / 2);
                ReadOnlySpan<byte> source = buffer.AsSpan(10, kat.Msg.Length / 2);
                TShakeTrait.HashData(source, destination);
                Assert.Equal(kat.Output, Convert.ToHexString(destination), ignoreCase: true);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_Stream_ByteArray()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                using (MemoryStream message = new MemoryStream(Convert.FromHexString(kat.Msg)))
                {
                    byte[] hash = TShakeTrait.HashData(message, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void KnownAnswerTests_OneShot_HashData_Stream_Destination()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] hash = new byte[kat.Output.Length / 2];

                using (MemoryStream message = new MemoryStream(Convert.FromHexString(kat.Msg)))
                {
                    TShakeTrait.HashData(message, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task KnownAnswerTests_OneShot_HashDataAsync_Stream_ByteArray()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                using (MemoryStream message = new MemoryStream(Convert.FromHexString(kat.Msg)))
                {
                    byte[] hash = await TShakeTrait.HashDataAsync(message, kat.Output.Length / 2);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task KnownAnswerTests_OneShot_HashDataAsync_Stream_Destination()
        {
            foreach ((string Msg, string Output) kat in Fips202Kats)
            {
                byte[] hash = new byte[kat.Output.Length / 2];

                using (MemoryStream message = new MemoryStream(Convert.FromHexString(kat.Msg)))
                {
                    await TShakeTrait.HashDataAsync(message, hash);
                    Assert.Equal(kat.Output, Convert.ToHexString(hash), ignoreCase: true);
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HashData_Minimal()
        {
            byte[] source = Array.Empty<byte>();

            byte[] result = TShakeTrait.HashData(source, outputLength: 0);
            Assert.Empty(result);

            result = TShakeTrait.HashData(ReadOnlySpan<byte>.Empty, outputLength: 0);
            Assert.Empty(result);

            result = TShakeTrait.HashData(Stream.Null, outputLength: 0);
            Assert.Empty(result);

            TShakeTrait.HashData(Stream.Null, Span<byte>.Empty); // Assert.NoThrow
            TShakeTrait.HashData(source, Span<byte>.Empty); // Assert.NoThrow
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HashDataAsync_Minimal()
        {
            byte[] result = await TShakeTrait.HashDataAsync(Stream.Null, outputLength: 0);
            Assert.Empty(result);

            await TShakeTrait.HashDataAsync(Stream.Null, Memory<byte>.Empty); // Assert.NoThrow
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetCurrentHash_Minimal()
        {
            using (TShake shake = new TShake())
            {
                TShakeTrait.AppendData(shake, Array.Empty<byte>());
                TShakeTrait.AppendData(shake, ReadOnlySpan<byte>.Empty);
                byte[] result = TShakeTrait.GetCurrentHash(shake, outputLength: 0);
                Assert.Empty(result);

                TShakeTrait.GetCurrentHash(shake, Span<byte>.Empty); // Assert.NoThrow
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetHashAndReset_Minimal()
        {
            using (TShake shake = new TShake())
            {
                TShakeTrait.AppendData(shake, Array.Empty<byte>());
                TShakeTrait.AppendData(shake, ReadOnlySpan<byte>.Empty);
                byte[] result = TShakeTrait.GetHashAndReset(shake, outputLength: 0);
                Assert.Empty(result);

                TShakeTrait.GetHashAndReset(shake, Span<byte>.Empty); // Assert.NoThrow
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void GetHashAndReset_ResetWithEmpty()
        {
            const int OutputLength = 64;

            using (TShake shake = new TShake())
            {
                TShakeTrait.AppendData(shake, "habaneros"u8);
                byte[] expected = TShakeTrait.GetHashAndReset(shake, OutputLength);
                Assert.Equal(OutputLength, expected.Length);

                TShakeTrait.AppendData(shake, "habaneros"u8);
                byte[] hash = TShakeTrait.GetHashAndReset(shake, outputLength: 0); // Reset with empty buffer should still reset.
                Assert.Empty(hash);

                TShakeTrait.AppendData(shake, "habaneros"u8);
                hash = TShakeTrait.GetHashAndReset(shake, OutputLength);
                Assert.Equal(expected, hash);
            }

            using (TShake shake = new TShake())
            {
                byte[] expected = new byte[OutputLength];
                TShakeTrait.AppendData(shake, "habaneros"u8);
                TShakeTrait.GetHashAndReset(shake, expected);

                byte[] hash = new byte[0];
                TShakeTrait.AppendData(shake, "habaneros"u8);
                TShakeTrait.GetHashAndReset(shake, hash); // Reset with empty buffer should still reset.

                hash = new byte[OutputLength];
                TShakeTrait.AppendData(shake, "habaneros"u8);
                TShakeTrait.GetHashAndReset(shake, hash);
                Assert.Equal(expected, hash);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_OneShot_HashData_OutputLengthNegative()
        {
            byte[] source = new byte[1];

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TShakeTrait.HashData(source, outputLength: -1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TShakeTrait.HashData(new ReadOnlySpan<byte>(source), outputLength: -1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TShakeTrait.HashData(Stream.Null, outputLength: -1));

            // This assert is not async - argument validation should occur synchronously.
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "outputLength",
                () => TShakeTrait.HashDataAsync(Stream.Null, outputLength: -1));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_OneShot_HashData_StreamNotReadable()
        {
            byte[] buffer = new byte[1];

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TShakeTrait.HashData(UntouchableStream.Instance, buffer));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TShakeTrait.HashDataAsync(UntouchableStream.Instance, buffer));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TShakeTrait.HashData(UntouchableStream.Instance, outputLength: 1));

            AssertExtensions.Throws<ArgumentException>(
                "source",
                () => TShakeTrait.HashDataAsync(UntouchableStream.Instance, outputLength: 1));
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task ArgValidation_OneShot_HashDataAsync_Cancelled()
        {
            byte[] buffer = new byte[1];
            CancellationToken cancelledToken = new CancellationToken(canceled: true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await TShakeTrait.HashDataAsync(Stream.Null, outputLength: 1, cancelledToken));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await TShakeTrait.HashDataAsync(Stream.Null, buffer, cancelledToken));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_OneShot_HashData_SourceNull()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TShakeTrait.HashData((byte[])null, outputLength: 1));

            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => TShakeTrait.HashData((Stream)null, outputLength: 1));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_GetCurrentHash_OutputLengthNegative()
        {
            using (TShake shake = new TShake())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => TShakeTrait.GetCurrentHash(shake, outputLength: -1));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_GetHashAndReset_OutputLengthNegative()
        {
            using (TShake shake = new TShake())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => TShakeTrait.GetHashAndReset(shake, outputLength: -1));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_AppendData_DataNull()
        {
            using (TShake shake = new TShake())
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "data",
                    () => TShakeTrait.AppendData(shake, (byte[])null));
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void ArgValidation_Allocated_UseAfterDispose()
        {
            byte[] buffer = new byte[1];
            TShake shake = new TShake();
            shake.Dispose();
            shake.Dispose(); // Assert.NoThrow

            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.AppendData(shake, buffer));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.AppendData(shake, new ReadOnlySpan<byte>(buffer)));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.GetHashAndReset(shake, outputLength: 1));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.GetHashAndReset(shake, buffer.AsSpan()));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.GetCurrentHash(shake, outputLength: 1));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.GetCurrentHash(shake, buffer.AsSpan()));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.Clone(shake));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.Reset(shake));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.Read(shake, buffer.AsSpan()));
            Assert.Throws<ObjectDisposedException>(() => TShakeTrait.Read(shake, outputLength: 1));
        }

        [ConditionalFact(nameof(IsNotSupported))]
        public void NotSupported_ThrowsPlatformNotSupportedException()
        {
            byte[] source = new byte[1];
            byte[] destination = new byte[0];

            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.Create());
            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.HashData(source, outputLength: 0));
            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.HashData(new ReadOnlySpan<byte>(source), outputLength: 0));
            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.HashData(source, destination));
            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.HashData(Stream.Null, outputLength: 0));
            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.HashData(Stream.Null, destination));
            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.HashDataAsync(Stream.Null, destination));
            Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.HashDataAsync(Stream.Null, outputLength: 0));
        }

        [Fact]
        public void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(TShakeTrait.IsSupported, PlatformDetection.SupportsSha3);
        }

        [ConditionalFact(nameof(IsSupported))]
        public void Clone_DifferentInstance()
        {
            using (TShake shake = new TShake())
            using (TShake clone = TShakeTrait.Clone(shake))
            {
                Assert.NotSame(shake, clone);
            }
        }

        [ConditionalFact(nameof(IsReadSupported))]
        public void Read_MixedAppendAfterRead()
        {
            using (TShake shake = new TShake())
            {
                TShakeTrait.Read(shake, Span<byte>.Empty);

                Assert.Throws<InvalidOperationException>(() => TShakeTrait.AppendData(shake, ReadOnlySpan<byte>.Empty));
                Assert.Throws<InvalidOperationException>(() => TShakeTrait.AppendData(shake, Array.Empty<byte>()));

                TShakeTrait.Reset(shake);

                // Assert.NoThrow
                TShakeTrait.AppendData(shake, ReadOnlySpan<byte>.Empty);
                TShakeTrait.AppendData(shake, Array.Empty<byte>());
            }
        }

        [ConditionalFact(nameof(IsReadSupported))]
        public void Read_MixedCloneAfterRead()
        {
            using (TShake shake = new TShake())
            {
                TShakeTrait.Read(shake, Span<byte>.Empty);
                Assert.Throws<InvalidOperationException>(() => TShakeTrait.Clone(shake));

                TShakeTrait.Reset(shake);

                using (TShake clone = TShakeTrait.Clone(shake))
                {
                    Assert.NotNull(clone);
                }
            }
        }

        [ConditionalFact(nameof(IsReadSupported))]
        public void Read_MixedGetHashAndReset()
        {
            using (TShake shake = new TShake())
            {
                TShakeTrait.Read(shake, Span<byte>.Empty);
                Assert.Throws<InvalidOperationException>(() => TShakeTrait.GetHashAndReset(shake, Span<byte>.Empty));
                Assert.Throws<InvalidOperationException>(() => TShakeTrait.GetHashAndReset(shake, outputLength: 0));

                TShakeTrait.Reset(shake);

                // Assert.NoThrow
                TShakeTrait.GetHashAndReset(shake, Span<byte>.Empty);
                TShakeTrait.GetHashAndReset(shake, outputLength: 0);
            }
        }

        [ConditionalFact(nameof(IsReadSupported))]
        public void Read_MixedGetCurrentHash()
        {
            using (TShake shake = new TShake())
            {
                TShakeTrait.Read(shake, Span<byte>.Empty);

                // Cannot GetCurrentHash while reading.
                Assert.Throws<InvalidOperationException>(() => TShakeTrait.GetCurrentHash(shake, Span<byte>.Empty));
                Assert.Throws<InvalidOperationException>(() => TShakeTrait.GetCurrentHash(shake, outputLength: 0));

                TShakeTrait.Reset(shake);

                // Assert.NoThrow
                TShakeTrait.GetCurrentHash(shake, Span<byte>.Empty);
                TShakeTrait.GetCurrentHash(shake, outputLength: 0);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void Read_NotSupported()
        {
            // This is testing when a TShake can be created, but the platform does not have Read.
            if (IsReadSupported)
            {
                return;
            }

            using (TShake shake = new TShake())
            {
                Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.Read(shake, Span<byte>.Empty));
                Assert.Throws<PlatformNotSupportedException>(() => TShakeTrait.Read(shake, outputLength: 0));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetHashAndReset_ConcurrentUseDoesNotCrashProcess()
        {
            if (!IsSupported)
            {
                throw new SkipTestException("Algorithm is not supported on this platform.");
            }

            RemoteExecutor.Invoke(static () =>
            {
                using (TShake shake = TShakeTrait.Create())
                {
                    Thread thread1 = new(ThreadWork);
                    Thread thread2 = new(ThreadWork);
                    thread1.Start(shake);
                    thread2.Start(shake);
                    thread1.Join();
                    thread2.Join();
                }
            }).Dispose();

            static void ThreadWork(object obj)
            {
                TShake shake = (TShake)obj;

                try
                {
                    byte[] input = new byte[128];

                    for (int i = 0; i < 10_000; i++)
                    {
                        TShakeTrait.AppendData(shake, input);
                        TShakeTrait.GetHashAndReset(shake, 128);
                    }
                }
                catch
                {
                    // Ignore all managed exceptions. HashAlgorithm is not thread safe, but we don't want process crashes.
                }
            }
        }

        private static void ReadChunked(TShake shake, Span<byte> destination)
        {
            int read = 0;
            int outputLength = destination.Length;

            while (read < outputLength)
            {
                int size = Math.Min(Math.Max(outputLength / 4, 1), outputLength - read);
                TShakeTrait.Read(shake, destination.Slice(read, size));
                read += size;
            }
        }
    }
}

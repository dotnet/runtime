// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Tests
{
    public class StreamToStreamCopyTest
    {
        [Theory]
        [MemberData(nameof(TwoBooleansWithAdditionalArg), new object[] { new object[] { 256, 8192, 8231 } })]
        public async Task MemoryStream_To_MemoryStream(bool sourceIsExposable, bool disposeSource, int inputSize)
        {
            byte[] input = CreateByteArray(inputSize);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            var destination = new MemoryStream();

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            Assert.Equal(input, destination.ToArray());
            Assert.Equal(input.Length, destination.Position);
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(TwoBooleans))]
        public async Task NonSeekableMemoryStream_To_MemoryStream(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            var source = new NonSeekableMemoryStream(input, sourceIsExposable);
            var destination = new MemoryStream();

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            Assert.Equal(input, destination.ToArray());
            Assert.Equal(input.Length, destination.Position);
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(TwoBooleans))]
        public async Task MemoryStream_NonZeroPosition_To_MemoryStream(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            const int StartingPosition = 1024;
            source.Position = StartingPosition;

            var destination = new MemoryStream();

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            Assert.Equal(input.Skip(StartingPosition), destination.ToArray());
            Assert.Equal(input.Length - StartingPosition, destination.Position);
            Assert.Equal(input.Length - StartingPosition, destination.Length);
        }

        [Theory]
        [MemberData(nameof(TwoBooleans))]
        public async Task MemoryStream_PositionAtEnd_To_MemoryStream(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            int StartingPosition = input.Length;
            source.Position = StartingPosition;

            var destination = new MemoryStream();

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            Assert.Equal(input.Skip(StartingPosition), destination.ToArray());
            Assert.Equal(input.Length - StartingPosition, destination.Position);
            Assert.Equal(input.Length - StartingPosition, destination.Length);
        }

        [Theory]
        [MemberData(nameof(TwoBooleans))]
        public async Task MemoryStream_To_LimitMemoryStream_NoCapacity(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            using var destination = new HttpContent.LimitArrayPoolWriteStream(int.MaxValue, 0, getFinalSizeFromPool: false);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            destination.ReallocateIfPooled();
            Assert.Equal(input, destination.ToArray());
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(ThreeBooleans))]
        public async Task MemoryStream_To_LimitMemoryStream_EqualCapacity(bool sourceIsExposable, bool disposeSource, bool getFinalSizeFromPool)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            using var destination = new HttpContent.LimitArrayPoolWriteStream(int.MaxValue, input.Length, getFinalSizeFromPool);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            destination.ReallocateIfPooled();
            Assert.Equal(input, destination.GetFirstBuffer());
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(ThreeBooleans))]
        public async Task MemoryStream_To_LimitMemoryStream_BiggerCapacity(bool sourceIsExposable, bool disposeSource, bool getFinalSizeFromPool)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            var destination = new HttpContent.LimitArrayPoolWriteStream(int.MaxValue, input.Length * 2, getFinalSizeFromPool);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            destination.ReallocateIfPooled();
            Assert.Equal(input, destination.GetFirstBuffer());
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(ThreeBooleans))]
        public async Task MemoryStream_To_LimitMemoryStream_SmallerCapacity(bool sourceIsExposable, bool disposeSource, bool getFinalSizeFromPool)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            var destination = new HttpContent.LimitArrayPoolWriteStream(int.MaxValue, 1024, getFinalSizeFromPool);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            destination.ReallocateIfPooled();
            Assert.Equal(input, destination.GetFirstBuffer());
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(TwoBooleans))]
        public async Task NonMemoryStream_To_MemoryStream(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            var source = new WrapperStream(CreateSourceMemoryStream(sourceIsExposable, input));
            var destination = new MemoryStream();

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            Assert.Equal(input, destination.ToArray());
            Assert.Equal(input.Length, destination.Position);
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(TwoBooleans))]
        public async Task NonMemoryStream_To_LimitMemoryStream_NoCapacity(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            var source = new WrapperStream(CreateSourceMemoryStream(sourceIsExposable, input));
            var destination = new HttpContent.LimitArrayPoolWriteStream(int.MaxValue, 0, getFinalSizeFromPool: false);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            destination.ReallocateIfPooled();
            Assert.Equal(input, destination.ToArray());
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(ThreeBooleans))]
        public async Task NonMemoryStream_To_LimitMemoryStream_EqualCapacity(bool sourceIsExposable, bool disposeSource, bool getFinalSizeFromPool)
        {
            byte[] input = CreateByteArray(8192);
            var source = new WrapperStream(CreateSourceMemoryStream(sourceIsExposable, input));
            var destination = new HttpContent.LimitArrayPoolWriteStream(int.MaxValue, input.Length, getFinalSizeFromPool);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            destination.ReallocateIfPooled();
            Assert.Equal(input, destination.GetFirstBuffer());
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [MemberData(nameof(TwoBooleans))]
        public async Task NonMemoryStream_To_NonMemoryStream(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            var source = new WrapperStream(CreateSourceMemoryStream(sourceIsExposable, input));

            var underlyingDestination = new MemoryStream();
            var destination = new WrapperStream(underlyingDestination);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            Assert.Equal(input, underlyingDestination.ToArray());
            Assert.Equal(input.Length, destination.Position);
            Assert.Equal(input.Length, destination.Length);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LimitMemoryStream_ResizingLogicWorks(bool getFinalSizeFromPool)
        {
            byte[] input = CreateByteArray(32 * 1024 * 1024);
            var rng = new Random();

            for (int actualSize = 2; actualSize <= input.Length; actualSize = (int)Math.Ceiling(actualSize * 1.15))
            {
                ReadOnlySpan<byte> currentInput = input.AsSpan(0, actualSize);

                foreach (int expectedSize in (ReadOnlySpan<int>)[0, 42, actualSize / 4, actualSize / 2, actualSize - 1, actualSize, actualSize + 1])
                {
                    HttpRequestException capacityEx;
                    if (expectedSize >= actualSize)
                    {
                        capacityEx = Assert.Throws<HttpRequestException>(() => new HttpContent.LimitArrayPoolWriteStream(maxBufferSize: actualSize - 1, expectedSize, getFinalSizeFromPool));
                    }
                    else
                    {
                        using var smallDestination = new HttpContent.LimitArrayPoolWriteStream(maxBufferSize: actualSize - 1, expectedSize, getFinalSizeFromPool);
                        capacityEx = Assert.Throws<HttpRequestException>(() => WriteChunks(smallDestination, actualSize));
                    }

                    Assert.Equal(HttpRequestError.ConfigurationLimitExceeded, capacityEx.HttpRequestError);

                    using var destination = new HttpContent.LimitArrayPoolWriteStream(maxBufferSize: actualSize + 42, expectedSize, getFinalSizeFromPool);
                    WriteChunks(destination, actualSize);

                    if (!getFinalSizeFromPool && expectedSize == actualSize)
                    {
                        Assert.Equal(currentInput, destination.GetFirstBuffer());
                        Assert.Equal(actualSize, destination.GetSingleBuffer().Length);
                        Assert.Same(destination.ToArray(), destination.GetSingleBuffer());
                    }
                    else
                    {
                        Assert.True(currentInput.StartsWith(destination.GetFirstBuffer()));
                    }

                    destination.ReallocateIfPooled();
                    Assert.Equal(currentInput, destination.CreateCopy());

                    if (getFinalSizeFromPool || actualSize == expectedSize)
                    {
                        Assert.Equal(actualSize, destination.GetSingleBuffer().Length);
                    }
                    else
                    {
                        Assert.True(actualSize <= destination.GetSingleBuffer().Length);
                    }
                }
            }

            void WriteChunks(Stream destination, int totalSize)
            {
                ReadOnlySpan<byte> remaining = input.AsSpan(0, totalSize);

                while (!remaining.IsEmpty)
                {
                    int chunk = rng.Next(remaining.Length + 1);
                    destination.Write(remaining.Slice(0, chunk));
                    remaining = remaining.Slice(chunk);
                }
            }
        }

        private static MemoryStream CreateSourceMemoryStream(bool sourceIsExposable, byte[] input)
        {
            MemoryStream source;
            if (sourceIsExposable)
            {
                source = new MemoryStream();
                source.Write(input, 0, input.Length);
                source.Position = 0;
            }
            else
            {
                source = new MemoryStream(input);
            }
            return source;
        }

        private static byte[] CreateByteArray(int length)
        {
            byte[] data = new byte[length];
            new Random(1).NextBytes(data);
            return data;
        }

        public static IEnumerable<object[]> TwoBooleans = new object[][]
        {
            new object[] { false, false },
            new object[] { false, true },
            new object[] { true, false },
            new object[] { true, true },
        };

        public static IEnumerable<object[]> ThreeBooleans = TwoBooleansWithAdditionalArg([true, false]);

        public static IEnumerable<object[]> TwoBooleansWithAdditionalArg(object[] args)
        {
            bool[] bools = new[] { true, false };
            foreach (object arg in args)
                foreach (bool b1 in bools)
                    foreach (bool b2 in bools)
                        yield return new object[] { b1, b2, arg };
        }

        private sealed class WrapperStream : DelegatingStream
        {
            public WrapperStream(Stream wrapped) : base(wrapped) { }
        }

        private sealed class NonSeekableMemoryStream : MemoryStream
        {
            public NonSeekableMemoryStream(byte[] input, bool sourceIsExposable) : base(input, 0, input.Length, true, sourceIsExposable)
            {
            }

            public override bool CanSeek => false;
        }
    }
}

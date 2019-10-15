// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, 0);

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

            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, 0);

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
            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, 0);

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
        public async Task MemoryStream_To_LimitMemoryStream_EqualCapacity(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, input.Length);

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
        public async Task MemoryStream_To_LimitMemoryStream_BiggerCapacity(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, input.Length * 2);

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
        public async Task MemoryStream_To_LimitMemoryStream_SmallerCapacity(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, 1024);

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
        public async Task MemoryStream_To_LimitMemoryStream_BiggerLength(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            MemoryStream source = CreateSourceMemoryStream(sourceIsExposable, input);
            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, 0);
            destination.SetLength(input.Length * 2);

            await StreamToStreamCopy.CopyAsync(source, destination, 4096, disposeSource);

            Assert.NotEqual(disposeSource, source.CanRead);
            if (!disposeSource)
            {
                Assert.Equal(input.Length, source.Position);
            }

            Assert.Equal(input.Concat(new byte[input.Length]), destination.ToArray());
            Assert.Equal(input.Length, destination.Position);
            Assert.Equal(input.Length * 2, destination.Length);
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
            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, 0);

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
        public async Task NonMemoryStream_To_LimitMemoryStream_EqualCapacity(bool sourceIsExposable, bool disposeSource)
        {
            byte[] input = CreateByteArray(8192);
            var source = new WrapperStream(CreateSourceMemoryStream(sourceIsExposable, input));
            var destination = new HttpContent.LimitMemoryStream(int.MaxValue, input.Length);

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

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests for the constructors of AnonymousPipeServerStream
    /// </summary>
    public class AnonymousPipeTest_CreateServer : AnonymousPipeTestBase
    {
        [Fact]
        public static void InOutPipeDirection_Throws_NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut));
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, HandleInheritability.None));
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, HandleInheritability.None, 500));

            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, dummyserver.SafePipeHandle, null));
            }

            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.In))
            {
                Assert.Throws<NotSupportedException>(() => new AnonymousPipeServerStream(PipeDirection.InOut, dummyserver.SafePipeHandle, null));
            }
        }

        [Theory]
        [InlineData(PipeDirection.In, 999)]
        [InlineData(PipeDirection.Out, 999)]
        public static void ServerBadInheritabilityThrows(PipeDirection direction, HandleInheritability inheritability)
        {
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () => new AnonymousPipeServerStream(direction, inheritability));
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () => new AnonymousPipeServerStream(direction, inheritability, 500));
        }

        [Theory]
        [InlineData(PipeDirection.In, -500)]
        [InlineData(PipeDirection.Out, -500)]
        [InlineData(PipeDirection.InOut, -500)] //bufferSize will cause an exception before InOut will
        public static void InvalidBufferSize_Throws_ArgumentOutOfRangeException(PipeDirection direction, int bufferSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => new AnonymousPipeServerStream(direction, HandleInheritability.None, bufferSize));
        }

        [Fact]
        public static void InvalidPipeDirection_Throws_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, 500));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, (HandleInheritability)999, -500));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, - 500));
        }

        [Fact]
        public static void InvalidPipeHandle_Throws()
        {
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                Assert.Throws<ArgumentNullException>("serverSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, null, dummyserver.ClientSafePipeHandle));

                Assert.Throws<ArgumentNullException>("clientSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, null));

                SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
                Assert.Throws<ArgumentException>("serverSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, pipeHandle, dummyserver.ClientSafePipeHandle));

                Assert.Throws<ArgumentException>("clientSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, pipeHandle));
            }
        }

        [Fact]
        public static void ValidConstructors()
        {
            new AnonymousPipeServerStream().Dispose();
            new AnonymousPipeServerStream(PipeDirection.Out).Dispose();
            new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None).Dispose();
            new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, 0).Dispose();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new AnonymousPipeServerStream(direction, inheritability));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new AnonymousPipeServerStream(direction, inheritability, 500));
        }

        [Theory]
        [InlineData(PipeDirection.In, -500)]
        [InlineData(PipeDirection.Out, -500)]
        [InlineData(PipeDirection.InOut, -500)] //bufferSize will cause an exception before InOut will
        public static void InvalidBufferSize_Throws_ArgumentOutOfRangeException(PipeDirection direction, int bufferSize)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => new AnonymousPipeServerStream(direction, HandleInheritability.None, bufferSize));
        }

        [Fact]
        public static void InvalidPipeDirection_Throws_ArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, 500));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, (HandleInheritability)999, -500));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new AnonymousPipeServerStream((PipeDirection)123, HandleInheritability.None, - 500));
        }

        [Fact]
        public static void InvalidPipeHandle_Throws()
        {
            using (AnonymousPipeServerStream dummyserver = new AnonymousPipeServerStream(PipeDirection.Out))
            {
                AssertExtensions.Throws<ArgumentNullException>("serverSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, null, dummyserver.ClientSafePipeHandle));

                AssertExtensions.Throws<ArgumentNullException>("clientSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, null));

                SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
                AssertExtensions.Throws<ArgumentException>("serverSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, pipeHandle, dummyserver.ClientSafePipeHandle));

                AssertExtensions.Throws<ArgumentException>("clientSafePipeHandle", () => new AnonymousPipeServerStream(PipeDirection.Out, dummyserver.SafePipeHandle, pipeHandle));
            }
        }

        [Fact]
        public static void ValidConstructors()
        {
            new AnonymousPipeServerStream().Dispose();
            new AnonymousPipeServerStream(PipeDirection.Out).Dispose();
            new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None).Dispose();
            new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable).Dispose();
            new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, 0).Dispose();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests for the constructors of AnonymousPipeClientStream
    /// </summary>
    public class AnonymousPipeTest_CreateClient : AnonymousPipeTestBase
    {
        [Fact]
        public static void NullParameters_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("pipeHandleAsString", () => new AnonymousPipeClientStream((string)null));
            Assert.Throws<ArgumentNullException>("pipeHandleAsString", () => new AnonymousPipeClientStream(PipeDirection.Out, (string)null));
            Assert.Throws<ArgumentNullException>("safePipeHandle", () => new AnonymousPipeClientStream(PipeDirection.In, (SafePipeHandle)null));
        }

        [Fact]
        public static void CreateClientStreamFromStringHandle_Valid()
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (var client = new AnonymousPipeClientStream(server.GetClientHandleAsString()))
            { }
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("-1")]
        public static void InvalidStringParameters_Throws_ArgumentException(string handle)
        {
            // Parameters must be nonnegative numeric characters
            Assert.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream(handle));
            Assert.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream(PipeDirection.Out, handle));
        }

        [Fact]
        public static void InvalidPipeHandle_Throws_ArgumentException()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<ArgumentException>("safePipeHandle", () => new AnonymousPipeClientStream(PipeDirection.In, pipeHandle));
        }

        [Fact]
        public static void InOutPipeDirection_Throws_NotSupportedException()
        {
            // Anonymous pipes can't be made with PipeDirection.InOut
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeClientStream(PipeDirection.InOut, "123"));

            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<NotSupportedException>(() => new AnonymousPipeClientStream(PipeDirection.InOut, pipeHandle));
        }
    }
}

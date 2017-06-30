// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AssertExtensions.Throws<ArgumentNullException>("pipeHandleAsString", () => new AnonymousPipeClientStream((string)null));
            AssertExtensions.Throws<ArgumentNullException>("pipeHandleAsString", () => new AnonymousPipeClientStream(PipeDirection.Out, (string)null));
            AssertExtensions.Throws<ArgumentNullException>("safePipeHandle", () => new AnonymousPipeClientStream(PipeDirection.In, (SafePipeHandle)null));
        }

        [Fact]
        public static void CreateClientStreamFromStringHandle_Valid()
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (var client = new AnonymousPipeClientStream(server.GetClientHandleAsString()))
            {
                SuppressClientHandleFinalizationIfNetFramework(server);
            }
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("-1")]
        public static void InvalidStringParameters_Throws_ArgumentException(string handle)
        {
            // Parameters must be nonnegative numeric characters
            AssertExtensions.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream(handle));
            AssertExtensions.Throws<ArgumentException>("pipeHandleAsString", () => new AnonymousPipeClientStream(PipeDirection.Out, handle));
        }

        [Fact]
        public static void InvalidPipeHandle_Throws_ArgumentException()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            AssertExtensions.Throws<ArgumentException>("safePipeHandle", () => new AnonymousPipeClientStream(PipeDirection.In, pipeHandle));
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

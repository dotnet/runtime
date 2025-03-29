// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Unix-specific tests for the constructors for NamedPipeClientStream
    /// </summary>
    public partial class NamedPipeTest_CreateClient
    {
        [Fact]
        public static void NotSupportedPipeAccessRights_Throws_PlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeClientStream(".", "client1", PipeAccessRights.FullControl, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None));
        }

        [Fact]
        public static void NotSupportedPipePath_Throws_PlatformNotSupportedException()
        {
            string hostName;
            Assert.True(InteropTest.TryGetHostName(out hostName));

            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeClientStream("foobar" + hostName, "foobar"));
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeClientStream(hostName, "foobar" + Path.GetInvalidFileNameChars()[0]));
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeClientStream(hostName, "/tmp/foo\0bar"));
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeClientStream(hostName, "/tmp/foobar/"));
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeClientStream(hostName, "/"));
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeClientStream(hostName, "\0"));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests for the constructors for NamedPipeClientStream
    /// </summary>
    public partial class NamedPipeTest_CreateClient
    {
        [Fact]
        public static void NullPipeName_Throws_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeClientStream(null));
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeClientStream(".", null));
        }

        [Fact]
        public static void EmptyStringPipeName_Throws_ArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeClientStream(""));
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeClientStream(".", ""));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void NullServerName_Throws_ArgumentNullException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1"));
            AssertExtensions.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1", direction));
            AssertExtensions.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1", direction, PipeOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1", direction, PipeOptions.None, TokenImpersonationLevel.None));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void EmptyStringServerName_Throws_ArgumentException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new NamedPipeClientStream("", "client1"));
            AssertExtensions.Throws<ArgumentException>(null, () => new NamedPipeClientStream("", "client1", direction));
            AssertExtensions.Throws<ArgumentException>(null, () => new NamedPipeClientStream("", "client1", direction, PipeOptions.None));
            AssertExtensions.Throws<ArgumentException>(null, () => new NamedPipeClientStream("", "client1", direction, PipeOptions.None, TokenImpersonationLevel.None));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void ReservedPipeName_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            const string serverName = ".";
            const string reservedName = "anonymous";
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeClientStream(reservedName));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeClientStream(serverName, reservedName));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeClientStream(serverName, reservedName, direction));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeClientStream(serverName, reservedName, direction, PipeOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeClientStream(serverName, reservedName, direction, PipeOptions.None, TokenImpersonationLevel.Impersonation));
        }

        [Theory]
        [InlineData((PipeDirection)123)]
        public static void InvalidPipeDirection_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeClientStream(".", "client1", direction));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeClientStream(".", "client1", direction, PipeOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeClientStream(".", "client1", direction, PipeOptions.None, TokenImpersonationLevel.None));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void InvalidPipeOptions_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeClientStream(".", "client1", direction, (PipeOptions)255));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeClientStream(".", "client1", direction, (PipeOptions)255, TokenImpersonationLevel.None));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void InvalidImpersonationLevel_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("impersonationLevel", () => new NamedPipeClientStream(".", "client1", direction, PipeOptions.None, (TokenImpersonationLevel)999));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void NullHandle_Throws_ArgumentNullException(PipeDirection direction)
        {
#pragma warning disable SYSLIB0063 // Testing the obsolete constructor
            AssertExtensions.Throws<ArgumentNullException>("safePipeHandle", () => new NamedPipeClientStream(direction, false, true, null));
#pragma warning restore SYSLIB0063
            AssertExtensions.Throws<ArgumentNullException>("safePipeHandle", () => new NamedPipeClientStream(direction, false, null));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void InvalidHandle_Throws_ArgumentException(PipeDirection direction)
        {
            using SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
#pragma warning disable SYSLIB0063 // Testing the obsolete constructor
            AssertExtensions.Throws<ArgumentException>("safePipeHandle", () => new NamedPipeClientStream(direction, false, true, pipeHandle));
#pragma warning restore SYSLIB0063
            AssertExtensions.Throws<ArgumentException>("safePipeHandle", () => new NamedPipeClientStream(direction, false, pipeHandle));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void BadHandleKind_Throws_IOException(PipeDirection direction)
        {
            using (FileStream fs = new FileStream(Path.Combine(Path.GetTempPath(), "_BadHandleKind_Throws_IOException_" + Path.GetRandomFileName()), FileMode.Create, FileAccess.Write, FileShare.None, 8, FileOptions.DeleteOnClose))
            {
                SafeFileHandle safeHandle = fs.SafeFileHandle;

                bool gotRef = false;
                try
                {
                    safeHandle.DangerousAddRef(ref gotRef);
                    IntPtr handle = safeHandle.DangerousGetHandle();

                    SafePipeHandle fakePipeHandle = new SafePipeHandle(handle, ownsHandle: false);
#pragma warning disable SYSLIB0063 // Testing the obsolete constructor
                    Assert.Throws<IOException>(() => new NamedPipeClientStream(direction, false, true, fakePipeHandle));
#pragma warning restore SYSLIB0063
                    Assert.Throws<IOException>(() => new NamedPipeClientStream(direction, false, fakePipeHandle));
                }
                finally
                {
                    if (gotRef)
                        safeHandle.DangerousRelease();
                }
            }
        }

        [Fact]
        public static void NamedPipeClientStream_InvalidHandleInerhitability()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new NamedPipeClientStream("a", "b", PipeDirection.Out, 0, TokenImpersonationLevel.Delegation, HandleInheritability.None - 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new NamedPipeClientStream("a", "b", PipeDirection.Out, 0, TokenImpersonationLevel.Delegation, HandleInheritability.Inheritable + 1));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "iOS/tvOS path length exceeds character limit for domain sockets")]
        public static async Task ConnectOnPipeFromExistingHandle_Throws_InvalidOperationException()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using (var server1 = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 3, PipeTransmissionMode.Byte, PipeOptions.None))
            using (var client1 = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                client1.Connect();
                server1.WaitForConnection();

                var clientFromHandle = new NamedPipeClientStream(PipeDirection.InOut, false, client1.SafePipeHandle);
                Assert.Throws<InvalidOperationException>(clientFromHandle.Connect);
                await Assert.ThrowsAsync<InvalidOperationException>(clientFromHandle.ConnectAsync);
            }

#pragma warning disable SYSLIB0063
            using (var server2 = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 3, PipeTransmissionMode.Byte, PipeOptions.None))
            using (var client2 = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                client2.Connect();
                server2.WaitForConnection();

                var clientFromHandleTrue = new NamedPipeClientStream(PipeDirection.InOut, isAsync: false, isConnected: true, client2.SafePipeHandle);
                Assert.Throws<InvalidOperationException>(clientFromHandleTrue.Connect);
                await Assert.ThrowsAsync<InvalidOperationException>(clientFromHandleTrue.ConnectAsync);
            }

            using (var server3 = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 3, PipeTransmissionMode.Byte, PipeOptions.None))
            using (var client3 = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                client3.Connect();
                server3.WaitForConnection();

                var clientFromHandleFalse = new NamedPipeClientStream(PipeDirection.InOut, isAsync: false, isConnected: false, client3.SafePipeHandle);
                Assert.Throws<InvalidOperationException>(clientFromHandleFalse.Connect);
                await Assert.ThrowsAsync<InvalidOperationException>(clientFromHandleFalse.ConnectAsync);
            }
#pragma warning restore SYSLIB0063
        }
    }
}

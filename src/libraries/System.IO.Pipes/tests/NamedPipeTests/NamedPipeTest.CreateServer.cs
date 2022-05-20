// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests for the constructors for NamedPipeServerStream
    /// </summary>
    public class NamedPipeTest_CreateServer
    {
        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void NullPipeName_Throws_ArgumentNullException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null));
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, direction));
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, direction, 2));
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, direction, 3, PipeTransmissionMode.Byte));
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, direction, 3, PipeTransmissionMode.Byte, PipeOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, direction, 3, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void ZeroLengthPipeName_Throws_ArgumentException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeServerStream(""));
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeServerStream("", direction));
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeServerStream("", direction, 2));
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeServerStream("", direction, 3, PipeTransmissionMode.Byte));
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeServerStream("", direction, 3, PipeTransmissionMode.Byte, PipeOptions.None));
            AssertExtensions.Throws<ArgumentException>("pipeName", () => new NamedPipeServerStream("", direction, 3, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        [PlatformSpecific(TestPlatforms.Windows)] // "anonymous" only reserved on Windows
        public static void ReservedPipeName_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            const string reservedName = "anonymous";
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, direction));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, direction, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, direction, 1, PipeTransmissionMode.Byte));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, direction, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, direction, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));}

        [Fact]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks UNIX sockets")]
        public static void Create_PipeName()
        {
            new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName()).Dispose();
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks UNIX sockets")]
        public static void Create_PipeName_Direction_MaxInstances()
        {
            new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName(), PipeDirection.Out, 1).Dispose();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // can't access SafePipeHandle on Unix until after connection created
        public static void CreateWithNegativeOneServerInstances_DefaultsToMaxServerInstances()
        {
            // When passed -1 as the maxnumberofserverisntances, the NamedPipeServerStream.Windows class
            // will translate that to the platform specific maximum number (255)
            using (var server = new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName(), PipeDirection.InOut, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var server2 = new NamedPipeServerStream(PipeDirection.InOut, false, true, server.SafePipeHandle))
            using (var server3 = new NamedPipeServerStream(PipeDirection.InOut, false, true, server.SafePipeHandle))
            {
            }
        }

        [Fact]
        public static void InvalidPipeDirection_Throws_ArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123, 1, PipeTransmissionMode.Byte));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("tempx", (PipeDirection)123, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public static void InvalidServerInstances_Throws_ArgumentOutOfRangeException(int numberOfServerInstances)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, numberOfServerInstances));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, numberOfServerInstances, PipeTransmissionMode.Byte));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, numberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, numberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void ServerInstancesOver254_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", direction, 255));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", direction, 255, PipeTransmissionMode.Byte));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", direction, 255, PipeTransmissionMode.Byte, PipeOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", direction, 255, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void InvalidTransmissionMode_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("temp1", direction, 1, (PipeTransmissionMode)123));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("temp1", direction, 1, (PipeTransmissionMode)123, PipeOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("tempx", direction, 1, (PipeTransmissionMode)123, PipeOptions.None, 0, 0));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void InvalidPipeOptions_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeServerStream("temp1", direction, 1, PipeTransmissionMode.Byte, (PipeOptions)255));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeServerStream("tempx", direction, 1, PipeTransmissionMode.Byte, (PipeOptions)255, 0, 0));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void InvalidBufferSize_Throws_ArgumentOutOfRangeException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inBufferSize", () => new NamedPipeServerStream("temp2", direction, 1, PipeTransmissionMode.Byte, PipeOptions.None, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("outBufferSize", () => new NamedPipeServerStream("temp2", direction, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, -123));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void NullPipeHandle_Throws_ArgumentNullException(PipeDirection direction)
        {
            AssertExtensions.Throws<ArgumentNullException>("safePipeHandle", () => new NamedPipeServerStream(direction, false, true, null));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        public static void InvalidPipeHandle_Throws_ArgumentException(PipeDirection direction)
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            AssertExtensions.Throws<ArgumentException>("safePipeHandle", () => new NamedPipeServerStream(direction, false, true, pipeHandle));
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
                    Assert.Throws<IOException>(() => new NamedPipeServerStream(direction, false, true, fakePipeHandle));
                }
                finally
                {
                    if (gotRef)
                        safeHandle.DangerousRelease();
                }
            }
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        [PlatformSpecific(TestPlatforms.Windows)] // accessing SafePipeHandle on Unix fails for a non-connected stream
        public static void Windows_CreateFromDisposedServerHandle_Throws_ObjectDisposedException(PipeDirection direction)
        {
            // The pipe is closed when we try to make a new Stream with it
            var pipe = new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName(), direction, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            SafePipeHandle handle = pipe.SafePipeHandle;
            pipe.Dispose();
            Assert.Throws<ObjectDisposedException>(() => new NamedPipeServerStream(direction, true, true, pipe.SafePipeHandle).Dispose());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // accessing SafePipeHandle on Unix fails for a non-connected stream
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks UNIX sockets")]
        public static void Unix_GetHandleOfNewServerStream_Throws_InvalidOperationException()
        {
            using (var pipe = new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName(), PipeDirection.Out, 1, PipeTransmissionMode.Byte))
            {
                Assert.Throws<InvalidOperationException>(() => pipe.SafePipeHandle);
            }
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.InOut)]
        [InlineData(PipeDirection.Out)]
        [PlatformSpecific(TestPlatforms.Windows)] // accessing SafePipeHandle on Unix fails for a non-connected stream
        public static void Windows_CreateFromAlreadyBoundHandle_Throws_ArgumentException(PipeDirection direction)
        {
            // The pipe is already bound
            using (var pipe = new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName(), direction, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                AssertExtensions.Throws<ArgumentException>("handle", () => new NamedPipeServerStream(direction, true, true, pipe.SafePipeHandle));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // NumberOfServerInstances > 1 isn't supported and has undefined behavior on Unix
        public static void ServerCountOverMaxServerInstances_Throws_IOException()
        {
            string uniqueServerName = PipeStreamConformanceTests.GetUniquePipeName();
            using (NamedPipeServerStream server = new NamedPipeServerStream(uniqueServerName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Assert.Throws<IOException>(() => new NamedPipeServerStream(uniqueServerName));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // NumberOfServerInstances > 1 isn't supported and has undefined behavior on Unix
        public static void Windows_ServerCloneWithDifferentDirection_Throws_UnauthorizedAccessException()
        {
            string uniqueServerName = PipeStreamConformanceTests.GetUniquePipeName();
            using (NamedPipeServerStream server = new NamedPipeServerStream(uniqueServerName, PipeDirection.In, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Assert.Throws<UnauthorizedAccessException>(() => new NamedPipeServerStream(uniqueServerName, PipeDirection.Out));
            }
        }
    }
}

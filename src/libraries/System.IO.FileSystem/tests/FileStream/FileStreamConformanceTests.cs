// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStreamStandaloneConformanceTests : StandaloneStreamConformanceTests
    {
        protected abstract FileOptions Options { get; }
        protected abstract int BufferSize { get; }

        private Task<Stream> CreateStream(byte[] initialData, FileAccess access)
        {
            string path = GetTestFilePath();
            if (initialData != null)
            {
                File.WriteAllBytes(path, initialData);
            }

            return Task.FromResult<Stream>(new FileStream(path, FileMode.OpenOrCreate, access, FileShare.None, BufferSize, Options));
        }

        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Read);
        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.ReadWrite);
        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Write);

        protected override bool NopFlushCompletesSynchronously => OperatingSystem.IsWindows();

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public async Task FileOffsetIsPreservedWhenFileStreamIsCreatedFromSafeFileHandle_Reads(ReadWriteMode mode)
        {
            byte[] initialData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            using FileStream stream = (FileStream)await CreateReadOnlyStreamCore(initialData);
            byte[] buffer = new byte[5];
            int bytesRead = await ReadAsync(mode, stream, buffer, 0, buffer.Length);

            Assert.Equal(bytesRead, stream.Position);

            using FileStream createdFromHandle = new FileStream(stream.SafeFileHandle, FileAccess.Read);

            Assert.Equal(bytesRead, stream.Position); // accessing SafeFileHandle must not change the position
            Assert.Equal(stream.Position, createdFromHandle.Position); // but it should sync the offset with OS
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public async Task FileOffsetIsPreservedWhenFileStreamIsCreatedFromSafeFileHandle_Writes(ReadWriteMode mode)
        {
            using FileStream stream = (FileStream)await CreateWriteOnlyStreamCore(Array.Empty<byte>());
            byte[] buffer = new byte[] { 0, 1, 2, 3, 4 };
            await WriteAsync(mode, stream, buffer, 0, buffer.Length);

            Assert.Equal(buffer.Length, stream.Position);

            using FileStream createdFromHandle = new FileStream(stream.SafeFileHandle, FileAccess.Write);

            Assert.Equal(buffer.Length, stream.Position); 
            Assert.Equal(stream.Position, createdFromHandle.Position);
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public async Task WriteAsyncStartsWherePreviousReadAsyncHasFinished(ReadWriteMode mode)
        {
            if (mode == ReadWriteMode.SyncByte)
            {
                // it reads a single byte even if buffer.Length > 1
                return;
            }

            byte[] initialData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] readBuffer = new byte[initialData.Length * 2]; // the edge case: reading more than available
            byte[] writeBuffer = new byte[] { 10, 11, 12, 13, 14, 15 };
            string filePath;

            using (FileStream stream = (FileStream)await CreateReadWriteStreamCore(initialData))
            {
                filePath = stream.Name;

                int bytesRead = await ReadAsync(mode, stream, readBuffer, 0, readBuffer.Length);

                Assert.Equal(bytesRead, initialData.Length);
                Assert.Equal(initialData.Length, stream.Position);
                Assert.Equal(stream.Position, stream.Length);

                await WriteAsync(mode, stream, writeBuffer, 0, writeBuffer.Length);

                Assert.Equal(initialData.Length + writeBuffer.Length, stream.Position);
                Assert.Equal(stream.Position, stream.Length);
            }

            byte[] allBytes = File.ReadAllBytes(filePath);
            Assert.Equal(initialData.Concat(writeBuffer), allBytes);
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public async Task NoDataIsLostWhenWritingToFile(ReadWriteMode mode)
        {
            string filePath;
            List<byte> writtenBytes = new List<byte>();

            using (FileStream stream = (FileStream)await CreateWriteOnlyStreamCore(Array.Empty<byte>()))
            {
                filePath = stream.Name;

                // the following buffer fits into internal FileStream buffer
                byte[] small = Enumerable.Repeat(byte.MinValue, Math.Max(1, BufferSize - 1)).ToArray();
                // the following buffer does not fit into internal FileStream buffer
                byte[] big = Enumerable.Repeat(byte.MaxValue, BufferSize + 1).ToArray();
                // in this test we are selecting a random buffer and write it to file
                // the goal is to cover all possible scenarios for the internal buffering logic
                Random random = new Random(12345);
                for (int i = 0; i < 1000; i++)
                {
                    byte[] bytes = random.Next() % 2 == 0 ? small : big;

                    await WriteAsync(mode, stream, bytes, 0, bytes.Length);

                    writtenBytes.AddRange(bytes);
                    Assert.Equal(writtenBytes.Count, stream.Length);
                    Assert.Equal(stream.Length, stream.Position);
                }
            }

            byte[] allBytes = File.ReadAllBytes(filePath);
            Assert.Equal(writtenBytes.ToArray(), allBytes);
        }

        [Theory]
        [InlineData(FileAccess.Write)]
        [InlineData(FileAccess.ReadWrite)] // FileAccess.Read does not allow for length manipulations
        public async Task LengthIsNotCachedAfterHandleHasBeenExposed(FileAccess fileAccess)
        {
            using FileStream stream = (FileStream)await CreateStream(Array.Empty<byte>(), fileAccess);
            using FileStream createdFromHandle = new FileStream(stream.SafeFileHandle, fileAccess);

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, createdFromHandle.Length);

            createdFromHandle.SetLength(1);
            Assert.Equal(1, createdFromHandle.Length);
            Assert.Equal(1, stream.Length);

            createdFromHandle.SetLength(2);
            Assert.Equal(2, createdFromHandle.Length);
            Assert.Equal(2, stream.Length);

            stream.SetLength(1);
            Assert.Equal(1, stream.Length);
            Assert.Equal(1, createdFromHandle.Length);

            stream.SetLength(2);
            Assert.Equal(2, stream.Length);
            Assert.Equal(2, createdFromHandle.Length);
        }

        [Fact]
        public async Task WriteByteFlushesTheBufferWhenItBecomesFull()
        {
            string filePath;
            List<byte> writtenBytes = new List<byte>();

            using (FileStream stream = (FileStream)await CreateWriteOnlyStreamCore(Array.Empty<byte>()))
            {
                filePath = stream.Name;

                stream.WriteByte(0);
                writtenBytes.Add(0);

                byte[] bytes = new byte[Math.Max(0, BufferSize - 1)];
                stream.Write(bytes.AsSpan());
                writtenBytes.AddRange(bytes);

                stream.WriteByte(1);
                writtenBytes.Add(1);
            }

            byte[] allBytes = File.ReadAllBytes(filePath);
            Assert.Equal(writtenBytes.ToArray(), allBytes);
        }
    }

    public class UnbufferedSyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.None;

#if RELEASE // since buffering can be now disabled by setting the buffer size to 0 or 1, let's test 0 in one config and 1 in the other
        protected override int BufferSize => 0;
#else
        protected override int BufferSize => 1;
#endif
    }

    public class BufferedSyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.None;
        protected override int BufferSize => 10;
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [SkipOnPlatform(TestPlatforms.Browser, "lots of operations aren't supported on browser")] // copied from StreamConformanceTests base class due to https://github.com/xunit/xunit/issues/2186
    public class UnbufferedAsyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.Asynchronous;

#if RELEASE
        protected override int BufferSize => 0;
#else
        protected override int BufferSize => 1;
#endif
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [SkipOnPlatform(TestPlatforms.Browser, "lots of operations aren't supported on browser")] // copied from StreamConformanceTests base class due to https://github.com/xunit/xunit/issues/2186
    public class BufferedAsyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.Asynchronous;
        protected override int BufferSize => 10;
    }

    public class AnonymousPipeFileStreamConnectedConformanceTests : ConnectedStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            var server = new AnonymousPipeServerStream(PipeDirection.Out);

            var fs1 = new FileStream(new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Write);
            var fs2 = new FileStream(new SafeFileHandle(server.ClientSafePipeHandle.DangerousGetHandle(), true), FileAccess.Read);

            server.SafePipeHandle.SetHandleAsInvalid();
            server.ClientSafePipeHandle.SetHandleAsInvalid();

            return Task.FromResult<StreamPair>((fs1, fs2));
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool FullyCancelableOperations => false;
        protected override bool BlocksOnZeroByteReads => OperatingSystem.IsWindows();
        protected override bool SupportsConcurrentBidirectionalUse => false;
    }

    public class NamedPipeFileStreamConnectedConformanceTests : ConnectedStreamConformanceTests
    {
        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            string name = FileSystemTest.GetNamedPipeServerStreamName();

            var server = new NamedPipeServerStream(name, PipeDirection.In);
            var client = new NamedPipeClientStream(".", name, PipeDirection.Out);

            await WhenAllOrAnyFailed(server.WaitForConnectionAsync(), client.ConnectAsync());

            var fs1 = new FileStream(new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Read);
            var fs2 = new FileStream(new SafeFileHandle(client.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Write);

            server.SafePipeHandle.SetHandleAsInvalid();
            client.SafePipeHandle.SetHandleAsInvalid();

            return (fs1, fs2);
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool FullyCancelableOperations => false;
        protected override bool BlocksOnZeroByteReads => OperatingSystem.IsWindows();
        protected override bool SupportsConcurrentBidirectionalUse => false;
    }

    [PlatformSpecific(TestPlatforms.Windows)] // DOS device paths (\\.\ and \\?\) are a Windows concept
    public class UnseekableDeviceFileStreamConnectedConformanceTests : ConnectedStreamConformanceTests
    {
        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            string pipeName = FileSystemTest.GetNamedPipeServerStreamName();
            string pipePath = Path.GetFullPath($@"\\.\pipe\{pipeName}");

            var server = new NamedPipeServerStream(pipeName, PipeDirection.In);
            var clienStream = new FileStream(File.OpenHandle(pipePath, FileMode.Open, FileAccess.Write, FileShare.None), FileAccess.Write);

            await server.WaitForConnectionAsync();

            var serverStrean = new FileStream(new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Read);

            server.SafePipeHandle.SetHandleAsInvalid();

            return (serverStrean, clienStream);
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool FullyCancelableOperations => false;
        protected override bool BlocksOnZeroByteReads => OperatingSystem.IsWindows();
        protected override bool SupportsConcurrentBidirectionalUse => false;
    }

    [PlatformSpecific(TestPlatforms.Windows)] // DOS device paths (\\.\ and \\?\) are a Windows concept
    public class SeekableDeviceFileStreamStandaloneConformanceTests : UnbufferedAsyncFileStreamStandaloneConformanceTests
    {
        protected override string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string filePath = Path.GetFullPath(base.GetTestFilePath(index, memberName, lineNumber));
            string drive = Path.GetPathRoot(filePath);
            StringBuilder volumeNameBuffer = new StringBuilder(filePath.Length + 1024);

            // the following method maps drive letter like "C:\" to a DeviceID (a DOS device path)
            // example: "\\?\Volume{724edb31-eaa5-4728-a4e3-f2474fd34ae2}\"
            if (!GetVolumeNameForVolumeMountPoint(drive, volumeNameBuffer, volumeNameBuffer.Capacity))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "GetVolumeNameForVolumeMountPoint failed");
            }

            // instead of:
            // 'C:\Users\x\AppData\Local\Temp\y\z
            // we want sth like:
            // '\\.\Volume{724edb31-eaa5-4728-a4e3-f2474fd34ae2}\Users\x\AppData\Local\Temp\y\z
            string devicePath = filePath.Replace(drive, volumeNameBuffer.ToString());
            Assert.StartsWith(@"\\?\", devicePath);
#if DEBUG
            devicePath = devicePath.Replace(@"\\?\", @"\\.\"); // we want to test both \\.\ and \\?\
#endif

            return devicePath;
        }

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "GetVolumeNameForVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
        private static extern bool GetVolumeNameForVolumeMountPoint(string volumeName, StringBuilder uniqueVolumeName, int uniqueNameBufferCapacity);
    }

    [PlatformSpecific(TestPlatforms.Windows)] // the test setup is Windows-specifc
    [Collection("NoParallelTests")] // don't run in parallel, as file sharing logic is not thread-safe
    [OuterLoop("Requires admin privileges to create a file share")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsAdmin_IsNotNano))]
    public class UncFilePathFileStreamStandaloneConformanceTests : UnbufferedAsyncFileStreamStandaloneConformanceTests
    {
        protected override string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string testDirectoryPath = Path.GetFullPath(TestDirectory);
            string shareName = new DirectoryInfo(testDirectoryPath).Name;
            string fileName = GetTestFileName(index, memberName, lineNumber);

            SHARE_INFO_502 shareInfo = default;
            shareInfo.shi502_netname = shareName;
            shareInfo.shi502_path = testDirectoryPath;
            shareInfo.shi502_remark = "folder created to test UNC file paths";
            shareInfo.shi502_max_uses = -1;

            int infoSize = Marshal.SizeOf(shareInfo);
            IntPtr infoBuffer = Marshal.AllocCoTaskMem(infoSize);

            try
            {
                Marshal.StructureToPtr(shareInfo, infoBuffer, false);

                int shareResult = NetShareAdd(string.Empty, 502, infoBuffer, IntPtr.Zero);

                if (shareResult != 0 && shareResult != 2118) // is a failure that is not a NERR_DuplicateShare
                {
                    throw new Exception($"Failed to create a file share, NetShareAdd returned {shareResult}");
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(infoBuffer);
            }

            // now once the folder has been shared we can use "localhost" to access it:
            return @$"\\localhost\{shareName}\{fileName}";
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHARE_INFO_502
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_netname;
            public uint shi502_type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_remark;
            public int shi502_permissions;
            public int shi502_max_uses;
            public int shi502_current_uses;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_path;
            public IntPtr shi502_passwd;
            public int shi502_reserved;
            public IntPtr shi502_security_descriptor;
        }

        [DllImport(Interop.Libraries.Netapi32)]
        public static extern int NetShareAdd([MarshalAs(UnmanagedType.LPWStr)]string servername, int level, IntPtr buf, IntPtr parm_err);
    }
}

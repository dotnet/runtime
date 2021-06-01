// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;
using static Interop;
using static Interop.Kernel32;

namespace System.IO.Tests
{
    public class RandomAccess_NoBuffering : FileSystemTest
    {
        private const FileOptions NoBuffering = (FileOptions)0x20000000;

        // https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-createfile2
        // we need it to open a device handle (File.OpenHandle does not allow for opening directories or devices)
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern SafeFileHandle CreateFile2(
            ref char lpFileName,
            int dwDesiredAccess,
            int dwShareMode,
            int dwCreationDisposition,
            ref CREATEFILE2_EXTENDED_PARAMETERS pCreateExParams);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363216.aspx
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        private static unsafe extern bool DeviceIoControl(
            SafeHandle hDevice,
            uint dwIoControlCode,
            void* lpInBuffer,
            uint nInBufferSize,
            void* lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            void* lpOverlapped);

        [Fact]
        public async Task ReadAsyncUsingSingleBuffer()
        {
            const int fileSize = 1_000_000; // 1 MB
            string filePath = GetTestFilePath();
            byte[] expected = new byte[fileSize];
            new Random().NextBytes(expected);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: FileOptions.Asynchronous | NoBuffering))
            using (SectorAlignedMemory<byte> buffer = SectorAlignedMemory<byte>.Allocate(GetBufferSize(filePath)))
            {
                int current = 0;
                int total = 0;

                do
                {
                    current = await RandomAccess.ReadAsync(handle, buffer.Memory, fileOffset: total);

                    Assert.True(expected.AsSpan(total, current).SequenceEqual(buffer.GetSpan().Slice(0, current)));

                    total += current;
                }
                // From https://docs.microsoft.com/en-us/windows/win32/fileio/file-buffering:
                // "File access sizes, including the optional file offset in the OVERLAPPED structure,
                // if specified, must be for a number of bytes that is an integer multiple of the volume sector size."
                // So if buffer and physical sector size is 4096 and the file size is 4097:
                // the read from offset=0 reads 4096 bytes
                // the read from offset=4096 reads 1 byte
                // the read from offset=4097 THROWS (Invalid argument, offset is not a multiple of sector size!)
                // That is why we stop at the first incomplete read (the next one would throw).
                // It's possible to get 0 if we are lucky and file size is a multiple of physical sector size.
                while (current == buffer.Memory.Length);

                Assert.Equal(fileSize, total);
            }
        }

        private int GetBufferSize(string filePath)
        {
            // From https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfilescatter:
            // "Each buffer must be at least the size of a system memory page and must be aligned on a system memory page size boundary"
            // "Because the file must be opened with FILE_FLAG_NO_BUFFERING, the number of bytes must be a multiple of the sector size
            // of the file system where the file is located."
            // Sector size is typically 512 to 4,096 bytes for direct-access storage devices (hard drives) and 2,048 bytes for CD-ROMs.
            int physicalSectorSize = GetPhysicalSectorSize(filePath);

            // From https://docs.microsoft.com/en-us/windows/win32/fileio/file-buffering:
            // "VirtualAlloc allocates memory that is aligned on addresses that are integer multiples of the system's page size.
            // Page size is 4,096 bytes on x64 and x86 or 8,192 bytes for Itanium-based systems. For additional information, see the GetSystemInfo function."
            int systemPageSize = Environment.SystemPageSize;

            // the following assumption is crucial for all NoBuffering tests and should always be true
            Assert.True(systemPageSize % physicalSectorSize == 0);

            return systemPageSize;
        }

        [Fact]
        public async Task ReadAsyncUsingMultipleBuffers()
        {
            const int fileSize = 1_000_000; // 1 MB
            string filePath = GetTestFilePath();
            byte[] expected = new byte[fileSize];
            new Random().NextBytes(expected);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: FileOptions.Asynchronous | NoBuffering))
            using (SectorAlignedMemory<byte> buffer_1 = SectorAlignedMemory<byte>.Allocate(GetBufferSize(filePath)))
            using (SectorAlignedMemory<byte> buffer_2 = SectorAlignedMemory<byte>.Allocate(GetBufferSize(filePath)))
            {
                long current = 0;
                long total = 0;

                do
                {
                    current = await RandomAccess.ReadAsync(
                        handle,
                        new Memory<byte>[]
                        {
                            buffer_1.Memory,
                            buffer_2.Memory,
                        },
                        fileOffset: total);

                    int takeFromFirst = Math.Min(buffer_1.Memory.Length, (int)current);
                    Assert.True(expected.AsSpan((int)total, takeFromFirst).SequenceEqual(buffer_1.GetSpan().Slice(0, takeFromFirst)));
                    int takeFromSecond = (int)current - takeFromFirst;
                    Assert.True(expected.AsSpan((int)total + takeFromFirst, takeFromSecond).SequenceEqual(buffer_2.GetSpan().Slice(0, takeFromSecond)));

                    total += current;
                } while (current == buffer_1.Memory.Length + buffer_2.Memory.Length);

                Assert.Equal(fileSize, total);
            }
        }

        [Fact]
        public async Task WriteAsyncUsingSingleBuffer()
        {
            string filePath = GetTestFilePath();
            int bufferSize = GetBufferSize(filePath);
            int fileSize = bufferSize * 10;
            byte[] content = new byte[fileSize];
            new Random().NextBytes(content);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.Asynchronous | NoBuffering))
            using (SectorAlignedMemory<byte> buffer = SectorAlignedMemory<byte>.Allocate(bufferSize))
            {
                int total = 0;

                while (total != fileSize)
                {
                    int take = Math.Min(content.Length - total, bufferSize);
                    content.AsSpan(total, take).CopyTo(buffer.GetSpan());

                    total += await RandomAccess.WriteAsync(
                        handle,
                        buffer.Memory,
                        fileOffset: total);
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }

        [Fact]
        public async Task WriteAsyncUsingMultipleBuffers()
        {
            string filePath = GetTestFilePath();
            int bufferSize = GetBufferSize(filePath);
            int fileSize = bufferSize * 10;
            byte[] content = new byte[fileSize];
            new Random().NextBytes(content);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.Asynchronous | NoBuffering))
            using (SectorAlignedMemory<byte> buffer_1 = SectorAlignedMemory<byte>.Allocate(bufferSize))
            using (SectorAlignedMemory<byte> buffer_2 = SectorAlignedMemory<byte>.Allocate(bufferSize))
            {
                long total = 0;

                while (total != fileSize)
                {
                    content.AsSpan((int)total, bufferSize).CopyTo(buffer_1.GetSpan());
                    content.AsSpan((int)total + bufferSize, bufferSize).CopyTo(buffer_2.GetSpan());

                    total += await RandomAccess.WriteAsync(
                        handle,
                        new ReadOnlyMemory<byte>[]
                        {
                            buffer_1.Memory,
                            buffer_2.Memory,
                        },
                        fileOffset: total);
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }

        private static unsafe int GetPhysicalSectorSize(string fullPath)
        {
            string devicePath = @"\\.\" + Path.GetPathRoot(Path.GetFullPath(fullPath));
            CREATEFILE2_EXTENDED_PARAMETERS extended = new CREATEFILE2_EXTENDED_PARAMETERS()
            {
                dwSize = (uint)sizeof(CREATEFILE2_EXTENDED_PARAMETERS),
                dwFileAttributes = 0,
                dwFileFlags = 0,
                dwSecurityQosFlags = 0
            };

            ReadOnlySpan<char> span = Path.EndsInDirectorySeparator(devicePath)
                ? devicePath.Remove(devicePath.Length - 1) // CreateFile2 does not like a `\` at the end of device path..
                : devicePath;

            using (SafeFileHandle deviceHandle = CreateFile2(
                lpFileName: ref MemoryMarshal.GetReference(span),
                dwDesiredAccess: 0,
                dwShareMode: (int)FileShare.ReadWrite,
                dwCreationDisposition: (int)FileMode.Open,
                pCreateExParams: ref extended))
            {
                Assert.False(deviceHandle.IsInvalid);

                STORAGE_PROPERTY_QUERY input = new STORAGE_PROPERTY_QUERY
                {
                    PropertyId = 6, // StorageAccessAlignmentProperty
                    QueryType = 0 // PropertyStandardQuery
                };
                STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR output = default;

                Assert.True(DeviceIoControl(
                    hDevice: deviceHandle,
                    dwIoControlCode: 0x2D1400, // IOCTL_STORAGE_QUERY_PROPERTY
                    lpInBuffer: &input,
                    nInBufferSize: (uint)Marshal.SizeOf(input),
                    lpOutBuffer: &output,
                    nOutBufferSize: (uint)Marshal.SizeOf<STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR>(),
                    lpBytesReturned: out _,
                    lpOverlapped: null));

                return (int)output.BytesPerPhysicalSector;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR
        {
            internal uint Version;
            internal uint Size;
            internal uint BytesPerCacheLine;
            internal uint BytesOffsetForCacheAlignment;
            internal uint BytesPerLogicalSector;
            internal uint BytesPerPhysicalSector;
            internal uint BytesOffsetForSectorAlignment;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct STORAGE_PROPERTY_QUERY
        {
            internal int PropertyId;
            internal int QueryType;
            internal byte AdditionalParameters;
        }
    }
}

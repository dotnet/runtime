// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>
    /// Tests for MemoryMappedViewAccessor.
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49104", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public class MemoryMappedViewAccessorTests : MemoryMappedFilesTestBase
    {
        /// <summary>
        /// Test to validate the offset, size, and access parameters to MemoryMappedFile.CreateViewAccessor.
        /// </summary>
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51375", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void InvalidArguments()
        {
            int mapLength = s_pageSize.Value;
            foreach (MemoryMappedFile mmf in CreateSampleMaps(mapLength))
            {
                using (mmf)
                {
                    // Offset
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => mmf.CreateViewAccessor(-1, mapLength));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => mmf.CreateViewAccessor(-1, mapLength, MemoryMappedFileAccess.ReadWrite));

                    // Size
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewAccessor(0, -1));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewAccessor(0, -1, MemoryMappedFileAccess.ReadWrite));
                    if (IntPtr.Size == 4)
                    {
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewAccessor(0, 1 + (long)uint.MaxValue));
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewAccessor(0, 1 + (long)uint.MaxValue, MemoryMappedFileAccess.ReadWrite));
                    }
                    else
                    {
                        Assert.Throws<IOException>(() => mmf.CreateViewAccessor(0, long.MaxValue));
                        Assert.Throws<IOException>(() => mmf.CreateViewAccessor(0, long.MaxValue, MemoryMappedFileAccess.ReadWrite));
                    }

                    // Offset + Size
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(0, mapLength + 1));
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(0, mapLength + 1, MemoryMappedFileAccess.ReadWrite));
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(mapLength, 1));
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(mapLength, 1, MemoryMappedFileAccess.ReadWrite));

                    // Access
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => mmf.CreateViewAccessor(0, mapLength, (MemoryMappedFileAccess)(-1)));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => mmf.CreateViewAccessor(0, mapLength, (MemoryMappedFileAccess)(42)));
                }
            }
        }

        [ConditionalTheory]
        [InlineData(MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileAccess.Write)]
        [InlineData(MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileAccess.CopyOnWrite)]
        [InlineData(MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.CopyOnWrite)]
        [InlineData(MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite, MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite, MemoryMappedFileAccess.CopyOnWrite)]
        [InlineData(MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.Write)]
        [InlineData(MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.CopyOnWrite)]
        [InlineData(MemoryMappedFileAccess.Read, MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.Read, MemoryMappedFileAccess.CopyOnWrite)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/53601", runtimes: TestRuntimes.Mono, platforms: TestPlatforms.MacCatalyst)]
        public void ValidAccessLevelCombinations(MemoryMappedFileAccess mapAccess, MemoryMappedFileAccess viewAccess)
        {
            const int Capacity = 4096;
            AssertExtensions.ThrowsIf<IOException>(PlatformDetection.IsInAppContainer && mapAccess == MemoryMappedFileAccess.ReadWriteExecute && viewAccess == MemoryMappedFileAccess.ReadWriteExecute,
            () =>
            {
                try
                {
                    using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, Capacity, mapAccess))
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, Capacity, viewAccess))
                    {
                        ValidateMemoryMappedViewAccessor(acc, Capacity, viewAccess);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if ((OperatingSystem.IsMacOS() || PlatformDetection.IsInContainer) &&
                       (viewAccess == MemoryMappedFileAccess.ReadExecute || viewAccess == MemoryMappedFileAccess.ReadWriteExecute))
                    {
                        // Containers and OSX with SIP enabled do not have execute permissions by default.
                        throw new SkipTestException("Insufficient execute permission.");
                    }

                    throw;
                }
            });
        }

        [Theory]
        [InlineData(MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.Write)]
        [InlineData(MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite, MemoryMappedFileAccess.Write)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(MemoryMappedFileAccess.Read, MemoryMappedFileAccess.Write)]
        [InlineData(MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadExecute)]
        public void InvalidAccessLevelsCombinations(MemoryMappedFileAccess mapAccess, MemoryMappedFileAccess viewAccess)
        {
            const int Capacity = 4096;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, Capacity, mapAccess))
            {
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(0, Capacity, viewAccess));
            }
        }

        [Theory]
        [InlineData(MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadWriteExecute)]
        public void InvalidAccessLevels_ReadWrite_NonUwp(MemoryMappedFileAccess mapAccess, MemoryMappedFileAccess viewAccess)
        {
            const int Capacity = 4096;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, Capacity, mapAccess))
            {
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor(0, Capacity, viewAccess));
            }
        }

        /// <summary>
        /// Test to verify the accessor's PointerOffset at the beginning of a view.
        /// </summary>
        [Theory]
        [InlineData(1)] // smallest possible non-zero size
        [InlineData(4095)] // just under typical page size
        [InlineData(4096)] // typical page size
        [InlineData(4099)] // just larger than typical page size
        [InlineData(12290)] // just larger than a few typical page sizes
        [InlineData(70_000)] // larger than typical Windows allocation granularity (64K)
        public void PointerOffsetMatchesViewStart(int mapLength)
        {
            foreach (MemoryMappedFile mmf in CreateSampleMaps(mapLength))
            {
                using (mmf)
                {
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor())
                    {
                        Assert.Equal(0, acc.PointerOffset);
                    }

                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, mapLength))
                    {
                        Assert.Equal(0, acc.PointerOffset);
                    }
                }
            }
        }

        /// <summary>
        /// Test all of the Read/Write accessor methods against a variety of maps and accessors.
        /// </summary>
        [Theory]
        [InlineData(8192, 0, 8192)]
        [InlineData(8192, 8100, 92)]
        [InlineData(8192, 0, 20)]
        [InlineData(8192, 1, 8191)]
        [InlineData(8192, 17, 8175)]
        [InlineData(8192, 17, 20)]
        [InlineData(70_000, 69_900, 16)]
        public void AllReadWriteMethods(int mapSize, int offset, int size)
        {
            foreach (MemoryMappedFile mmf in CreateSampleMaps(mapSize))
            {
                using (mmf)
                using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(offset, size))
                {
                    // Validate all of the read/write methods
                    AssertWritesReads(acc);

                    // Make sure that the internal offsets used for reading/writing are correct.
                    // This is the only meaningful way to validate PointerOffset, as its value's
                    // correctness is based entirely on the address selected by the OS to start
                    // the view and the distance from there to the user-supplied offset.
                    PopulateWithRandomData(mmf);
                    unsafe
                    {
                        byte* ptr = null;
                        try
                        {
                            acc.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                            for (int i = 0; i < size; i++)
                            {
                                Assert.Equal(acc.ReadByte(i), *(ptr + acc.PointerOffset + i));
                                acc.Write(i, (byte)i);
                                Assert.Equal((byte)i, acc.ReadByte(i));
                            }
                        }
                        finally
                        {
                            acc.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }
        }

        /// <summary>Performs many reads and writes of various data types against the accessor.</summary>
        private static void AssertWritesReads(MemoryMappedViewAccessor acc)
        {
            // Successful reads and writes at the beginning for each data type
            AssertWriteRead<bool>(false, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadBoolean(pos));
            AssertWriteRead<bool>(true, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadBoolean(pos));
            AssertWriteRead<byte>(42, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadByte(pos));
            AssertWriteRead<char>('c', 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadChar(pos));
            AssertWriteRead<decimal>(9, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadDecimal(pos));
            AssertWriteRead<double>(10, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadDouble(pos));
            AssertWriteRead<short>(11, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadInt16(pos));
            AssertWriteRead<int>(12, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadInt32(pos));
            AssertWriteRead<long>(13, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadInt64(pos));
            AssertWriteRead<sbyte>(14, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadSByte(pos));
            AssertWriteRead<float>(15, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadSingle(pos));
            AssertWriteRead<ushort>(16, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadUInt16(pos));
            AssertWriteRead<uint>(17, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadUInt32(pos));
            AssertWriteRead<ulong>(17, 0, (pos, value) => acc.Write(pos, value), pos => acc.ReadUInt64(pos));

            // Successful reads and writes at the end for each data type
            long end = acc.Capacity;
            AssertWriteRead<bool>(false, end - sizeof(bool), (pos, value) => acc.Write(pos, value), pos => acc.ReadBoolean(pos));
            AssertWriteRead<bool>(true, end - sizeof(bool), (pos, value) => acc.Write(pos, value), pos => acc.ReadBoolean(pos));
            AssertWriteRead<byte>(42, end - sizeof(byte), (pos, value) => acc.Write(pos, value), pos => acc.ReadByte(pos));
            AssertWriteRead<char>('c', end - sizeof(char), (pos, value) => acc.Write(pos, value), pos => acc.ReadChar(pos));
            AssertWriteRead<decimal>(9, end - sizeof(decimal), (pos, value) => acc.Write(pos, value), pos => acc.ReadDecimal(pos));
            AssertWriteRead<double>(10, end - sizeof(double), (pos, value) => acc.Write(pos, value), pos => acc.ReadDouble(pos));
            AssertWriteRead<short>(11, end - sizeof(short), (pos, value) => acc.Write(pos, value), pos => acc.ReadInt16(pos));
            AssertWriteRead<int>(12, end - sizeof(int), (pos, value) => acc.Write(pos, value), pos => acc.ReadInt32(pos));
            AssertWriteRead<long>(13, end - sizeof(long), (pos, value) => acc.Write(pos, value), pos => acc.ReadInt64(pos));
            AssertWriteRead<sbyte>(14, end - sizeof(sbyte), (pos, value) => acc.Write(pos, value), pos => acc.ReadSByte(pos));
            AssertWriteRead<float>(15, end - sizeof(float), (pos, value) => acc.Write(pos, value), pos => acc.ReadSingle(pos));
            AssertWriteRead<ushort>(16, end - sizeof(ushort), (pos, value) => acc.Write(pos, value), pos => acc.ReadUInt16(pos));
            AssertWriteRead<uint>(17, end - sizeof(uint), (pos, value) => acc.Write(pos, value), pos => acc.ReadUInt32(pos));
            AssertWriteRead<ulong>(17, end - sizeof(ulong), (pos, value) => acc.Write(pos, value), pos => acc.ReadUInt64(pos));

            // Failed reads and writes just at the border of the end. This triggers different exception types
            // for some types than when we're completely beyond the end.
            long beyondEnd = acc.Capacity + 1;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadBoolean(beyondEnd - sizeof(bool)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadByte(beyondEnd - sizeof(byte)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadSByte(beyondEnd - sizeof(sbyte)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadChar(beyondEnd - sizeof(char)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadDecimal(beyondEnd - sizeof(decimal)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadDouble(beyondEnd - sizeof(double)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadInt16(beyondEnd - sizeof(short)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadInt32(beyondEnd - sizeof(int)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadInt64(beyondEnd - sizeof(long)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadSingle(beyondEnd - sizeof(float)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadUInt16(beyondEnd - sizeof(ushort)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadUInt32(beyondEnd - sizeof(uint)));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.ReadUInt64(beyondEnd - sizeof(ulong)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(bool), false));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(byte), (byte)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(sbyte), (sbyte)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(char), 'c'));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(decimal), (decimal)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(double), (double)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(short), (short)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(int), (int)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(long), (long)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(float), (float)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(ushort), (ushort)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(uint), (uint)0));
            AssertExtensions.Throws<ArgumentException>("position", () => acc.Write(beyondEnd - sizeof(ulong), (ulong)0));

            // Failed reads and writes well past the end
            beyondEnd = acc.Capacity + 20;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadBoolean(beyondEnd - sizeof(bool)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadByte(beyondEnd - sizeof(byte)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadSByte(beyondEnd - sizeof(sbyte)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadChar(beyondEnd - sizeof(char)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadDecimal(beyondEnd - sizeof(decimal)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadDouble(beyondEnd - sizeof(double)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadInt16(beyondEnd - sizeof(short)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadInt32(beyondEnd - sizeof(int)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadInt64(beyondEnd - sizeof(long)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadSingle(beyondEnd - sizeof(float)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadUInt16(beyondEnd - sizeof(ushort)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadUInt32(beyondEnd - sizeof(uint)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.ReadUInt64(beyondEnd - sizeof(ulong)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(bool), false));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(byte), (byte)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(sbyte), (sbyte)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(char), 'c'));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(decimal), (decimal)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(double), (double)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(short), (short)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(int), (int)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(long), (long)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(float), (float)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(ushort), (ushort)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(uint), (uint)0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("position", () => acc.Write(beyondEnd - sizeof(ulong), (ulong)0));
        }

        /// <summary>Performs and verifies a read and write against an accessor.</summary>
        /// <typeparam name="T">The type of data being read and written.</typeparam>
        /// <param name="expected">The data expected to be read.</param>
        /// <param name="position">The position of the read and write.</param>
        /// <param name="write">The function to perform the write, handed the position at which to write and the value to write.</param>
        /// <param name="read">The function to perform the read, handed the position from which to read and returning the read value.</param>
        private static void AssertWriteRead<T>(T expected, long position, Action<long, T> write, Func<long, T> read)
        {
            write(position, expected);
            Assert.Equal(expected, read(position));
        }

        /// <summary>
        /// Test to verify that Flush is supported regardless of the accessor's access level
        /// </summary>
        [Theory]
        [InlineData(MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.Write)]
        [InlineData(MemoryMappedFileAccess.ReadWrite)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite)]
        public void FlushSupportedOnBothReadAndWriteAccessors(MemoryMappedFileAccess access)
        {
            const int Capacity = 256;
            foreach (MemoryMappedFile mmf in CreateSampleMaps(Capacity))
            {
                using (mmf)
                using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, Capacity, access))
                {
                    acc.Flush();
                }
            }
        }

        /// <summary>
        /// Test to validate that multiple accessors over the same map share data appropriately.
        /// </summary>
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "the emscripten implementation doesn't share data")]
        public void ViewsShareData()
        {
            const int MapLength = 256;
            foreach (MemoryMappedFile mmf in CreateSampleMaps(MapLength))
            {
                using (mmf)
                {
                    // Create two views over the same map, and verify that data
                    // written to one is readable by the other.
                    using (MemoryMappedViewAccessor acc1 = mmf.CreateViewAccessor())
                    using (MemoryMappedViewAccessor acc2 = mmf.CreateViewAccessor())
                    {
                        for (int i = 0; i < MapLength; i++)
                        {
                            acc1.Write(i, (byte)i);
                        }
                        acc1.Flush();
                        for (int i = 0; i < MapLength; i++)
                        {
                            Assert.Equal(i, acc2.ReadByte(i));
                        }
                    }

                    // Then verify that after those views have been disposed of,
                    // we can create another view and still read the same data.
                    using (MemoryMappedViewAccessor acc3 = mmf.CreateViewAccessor())
                    {
                        for (int i = 0; i < MapLength; i++)
                        {
                            Assert.Equal(i, acc3.ReadByte(i));
                        }
                    }

                    // Finally, make sure such data is also visible to a stream view
                    // created subsequently from the same map.
                    using (MemoryMappedViewStream stream4 = mmf.CreateViewStream())
                    {
                        for (int i = 0; i < MapLength; i++)
                        {
                            Assert.Equal(i, stream4.ReadByte());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test to verify copy-on-write behavior of accessors.
        /// </summary>
        [Fact]
        public void CopyOnWrite()
        {
            const int MapLength = 256;
            foreach (MemoryMappedFile mmf in CreateSampleMaps(MapLength))
            {
                using (mmf)
                {
                    // Create a normal view, make sure the original data is there, then write some new data.
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, MapLength, MemoryMappedFileAccess.ReadWrite))
                    {
                        Assert.Equal(0, acc.ReadInt32(0));
                        acc.Write(0, 42);
                    }

                    // In a CopyOnWrite view, verify the previously written data is there, then write some new data
                    // and verify it's visible through this view.
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, MapLength, MemoryMappedFileAccess.CopyOnWrite))
                    {
                        Assert.Equal(42, acc.ReadInt32(0));
                        acc.Write(0, 84);
                        Assert.Equal(84, acc.ReadInt32(0));
                    }

                    // Finally, verify that the CopyOnWrite data is not visible to others using the map.
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, MapLength, MemoryMappedFileAccess.Read))
                    {
                        Assert.Equal(42, acc.ReadInt32(0));
                    }
                }
            }
        }

        /// <summary>
        /// Test to verify that we can dispose of an accessor multiple times.
        /// </summary>
        [Fact]
        public void DisposeMultipleTimes()
        {
            foreach (MemoryMappedFile mmf in CreateSampleMaps())
            {
                using (mmf)
                {
                    MemoryMappedViewAccessor acc = mmf.CreateViewAccessor();
                    acc.Dispose();
                    acc.Dispose();
                }
            }
        }

        /// <summary>
        /// Test to verify that a view becomes unusable after it's been disposed.
        /// </summary>
        [Fact]
        public void InvalidAfterDisposal()
        {
            foreach (MemoryMappedFile mmf in CreateSampleMaps())
            {
                using (mmf)
                {
                    MemoryMappedViewAccessor acc = mmf.CreateViewAccessor();
                    SafeMemoryMappedViewHandle handle = acc.SafeMemoryMappedViewHandle;

                    Assert.False(handle.IsClosed);
                    acc.Dispose();
                    Assert.True(handle.IsClosed);

                    Assert.Throws<ObjectDisposedException>(() => acc.ReadByte(0));
                    Assert.Throws<ObjectDisposedException>(() => acc.Write(0, (byte)0));
                    Assert.Throws<ObjectDisposedException>(() => acc.Flush());
                }
            }
        }

        /// <summary>
        /// Test to verify that we can still use a view after the associated map has been disposed.
        /// </summary>
        [Fact]
        public void UseAfterMMFDisposal()
        {
            foreach (MemoryMappedFile mmf in CreateSampleMaps(8192))
            {
                // Create the view, then dispose of the map
                MemoryMappedViewAccessor acc;
                using (mmf) acc = mmf.CreateViewAccessor();

                // Validate we can still use the view
                ValidateMemoryMappedViewAccessor(acc, 8192, MemoryMappedFileAccess.ReadWrite);
                acc.Dispose();
            }
        }

        /// <summary>
        /// Test to allow a map and view to be finalized, just to ensure we don't crash.
        /// </summary>
        [Fact]
        public void AllowFinalization()
        {
            // Explicitly do not dispose, to allow finalization to happen, just to try to verify
            // that nothing fails/throws when it does.

            WeakReference<MemoryMappedFile> mmfWeak;
            WeakReference<MemoryMappedViewAccessor> mmvaWeak;
            CreateWeakMmfAndMmva(out mmfWeak, out mmvaWeak);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (PlatformDetection.IsPreciseGcSupported)
            {
                Assert.False(mmfWeak.TryGetTarget(out _));
                Assert.False(mmvaWeak.TryGetTarget(out _));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateWeakMmfAndMmva(out WeakReference<MemoryMappedFile> mmfWeak, out WeakReference<MemoryMappedViewAccessor> mmvaWeak)
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, 4096);
            MemoryMappedViewAccessor acc = mmf.CreateViewAccessor();

            mmfWeak = new WeakReference<MemoryMappedFile>(mmf);
            mmvaWeak = new WeakReference<MemoryMappedViewAccessor>(acc);
        }
    }
}

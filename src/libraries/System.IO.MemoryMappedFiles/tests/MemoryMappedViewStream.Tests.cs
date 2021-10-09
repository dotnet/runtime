// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>
    /// Tests for MemoryMappedViewStream.
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49104", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public class MemoryMappedViewStreamTests : MemoryMappedFilesTestBase
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
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => mmf.CreateViewStream(-1, mapLength));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => mmf.CreateViewStream(-1, mapLength, MemoryMappedFileAccess.ReadWrite));

                    // Size
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewStream(0, -1));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewStream(0, -1, MemoryMappedFileAccess.ReadWrite));
                    if (IntPtr.Size == 4)
                    {
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewStream(0, 1 + (long)uint.MaxValue));
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => mmf.CreateViewStream(0, 1 + (long)uint.MaxValue, MemoryMappedFileAccess.ReadWrite));
                    }
                    else
                    {
                        Assert.Throws<IOException>(() => mmf.CreateViewStream(0, long.MaxValue));
                        Assert.Throws<IOException>(() => mmf.CreateViewStream(0, long.MaxValue, MemoryMappedFileAccess.ReadWrite));
                    }

                    // Offset + Size
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewStream(0, mapLength + 1));
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewStream(0, mapLength + 1, MemoryMappedFileAccess.ReadWrite));
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewStream(mapLength, 1));
                    Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewStream(mapLength, 1, MemoryMappedFileAccess.ReadWrite));

                    // Access
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => mmf.CreateViewStream(0, mapLength, (MemoryMappedFileAccess)(-1)));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => mmf.CreateViewStream(0, mapLength, (MemoryMappedFileAccess)(42)));
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
        public void ValidAccessLevelCombinations(MemoryMappedFileAccess mapAccess, MemoryMappedFileAccess viewAccess)
        {
            const int Capacity = 4096;
            AssertExtensions.ThrowsIf<IOException>(PlatformDetection.IsInAppContainer && mapAccess == MemoryMappedFileAccess.ReadWriteExecute && viewAccess == MemoryMappedFileAccess.ReadWriteExecute,
            () =>
            {
                try
                {
                    using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, Capacity, mapAccess))
                    using (MemoryMappedViewStream s = mmf.CreateViewStream(0, Capacity, viewAccess))
                    {
                        ValidateMemoryMappedViewStream(s, Capacity, viewAccess);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if ((OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || PlatformDetection.IsInContainer) &&
                        (viewAccess == MemoryMappedFileAccess.ReadExecute || viewAccess == MemoryMappedFileAccess.ReadWriteExecute))
                    {
                        // Containers and OSXlike platforms with SIP enabled do not have execute permissions by default.
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
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewStream(0, Capacity, viewAccess));
            }
        }

        [Theory]
        [InlineData(MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(MemoryMappedFileAccess.CopyOnWrite, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.ReadWriteExecute)]
        public void InvalidAccessLevels_ReadWriteExecute_NonUwp(MemoryMappedFileAccess mapAccess, MemoryMappedFileAccess viewAccess)
        {
            const int Capacity = 4096;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, Capacity, mapAccess))
            {
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewStream(0, Capacity, viewAccess));
            }
        }

        /// <summary>
        /// Test to verify the accessor's PointerOffset.
        /// </summary>
        [Fact]
        public void PointerOffsetMatchesViewStart()
        {
            const int MapLength = 4096;
            foreach (MemoryMappedFile mmf in CreateSampleMaps(MapLength))
            {
                using (mmf)
                {
                    using (MemoryMappedViewStream s = mmf.CreateViewStream())
                    {
                        Assert.Equal(0, s.PointerOffset);
                    }

                    using (MemoryMappedViewStream s = mmf.CreateViewStream(0, MapLength))
                    {
                        Assert.Equal(0, s.PointerOffset);
                    }
                    using (MemoryMappedViewStream s = mmf.CreateViewStream(1, MapLength - 1))
                    {
                        Assert.Equal(1, s.PointerOffset);
                    }
                    using (MemoryMappedViewStream s = mmf.CreateViewStream(MapLength - 1, 1))
                    {
                        Assert.Equal(MapLength - 1, s.PointerOffset);
                    }

                    // On Unix creating a view of size zero will result in an offset and capacity
                    // of 0 due to mmap behavior, whereas on Windows it's possible to create a
                    // zero-size view anywhere in the created file mapping.
                    using (MemoryMappedViewStream s = mmf.CreateViewStream(MapLength, 0))
                    {
                        Assert.Equal(
                            OperatingSystem.IsWindows() ? MapLength : 0,
                            s.PointerOffset);
                    }
                }
            }
        }

        /// <summary>
        /// Test all of the Read/Write accessor methods against a variety of maps and accessors.
        /// </summary>
        [Theory]
        [InlineData(0, 8192)]
        [InlineData(8100, 92)]
        [InlineData(0, 20)]
        [InlineData(1, 8191)]
        [InlineData(17, 8175)]
        [InlineData(17, 20)]
        public void AllReadWriteMethods(long offset, long size)
        {
            foreach (MemoryMappedFile mmf in CreateSampleMaps(8192))
            {
                using (mmf)
                using (MemoryMappedViewStream s = mmf.CreateViewStream(offset, size))
                {
                    // Write and read at the beginning
                    s.Position = 0;
                    s.WriteByte(42);
                    s.Position = 0;
                    Assert.Equal(42, s.ReadByte());

                    // Write and read at the end
                    byte[] data = new byte[] { 1, 2, 3 };
                    s.Position = s.Length - data.Length;
                    s.Write(data, 0, data.Length);
                    s.Position = s.Length - data.Length;
                    Array.Clear(data);
                    Assert.Equal(3, s.Read(data, 0, data.Length));
                    Assert.Equal(new byte[] { 1, 2, 3 }, data);

                    // Fail reading/writing past the end
                    s.Position = s.Length;
                    Assert.Equal(-1, s.ReadByte());
                    Assert.Throws<NotSupportedException>(() => s.WriteByte(42));
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
                    using (MemoryMappedViewStream s1 = mmf.CreateViewStream())
                    using (MemoryMappedViewStream s2 = mmf.CreateViewStream())
                    {
                        for (int i = 0; i < MapLength; i++)
                        {
                            s1.WriteByte((byte)i);
                        }
                        s1.Flush();
                        for (int i = 0; i < MapLength; i++)
                        {
                            Assert.Equal(i, s2.ReadByte());
                        }
                    }

                    // Then verify that after those views have been disposed of,
                    // we can create another view and still read the same data.
                    using (MemoryMappedViewStream s3 = mmf.CreateViewStream())
                    {
                        for (int i = 0; i < MapLength; i++)
                        {
                            Assert.Equal(i, s3.ReadByte());
                        }
                    }

                    // Finally, make sure such data is also visible to a stream view
                    // created subsequently from the same map.
                    using (MemoryMappedViewAccessor acc4 = mmf.CreateViewAccessor())
                    {
                        for (int i = 0; i < MapLength; i++)
                        {
                            Assert.Equal(i, acc4.ReadByte(i));
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
                    using (MemoryMappedViewStream s = mmf.CreateViewStream(0, MapLength, MemoryMappedFileAccess.ReadWrite))
                    {
                        Assert.Equal(0, s.ReadByte());
                        s.Position = 0;
                        s.WriteByte(42);
                    }

                    // In a CopyOnWrite view, verify the previously written data is there, then write some new data
                    // and verify it's visible through this view.
                    using (MemoryMappedViewStream s = mmf.CreateViewStream(0, MapLength, MemoryMappedFileAccess.CopyOnWrite))
                    {
                        Assert.Equal(42, s.ReadByte());
                        s.Position = 0;
                        s.WriteByte(84);
                        s.Position = 0;
                        Assert.Equal(84, s.ReadByte());
                    }

                    // Finally, verify that the CopyOnWrite data is not visible to others using the map.
                    using (MemoryMappedViewStream s = mmf.CreateViewStream(0, MapLength, MemoryMappedFileAccess.Read))
                    {
                        s.Position = 0;
                        Assert.Equal(42, s.ReadByte());
                    }
                }
            }
        }

        /// <summary>
        /// Test to verify that a view becomes unusable after it's been disposed.
        /// </summary>
        [Fact]
        public void HandleClosedOnDisposal()
        {
            foreach (MemoryMappedFile mmf in CreateSampleMaps())
            {
                using (mmf)
                {
                    MemoryMappedViewStream s = mmf.CreateViewStream();
                    SafeMemoryMappedViewHandle handle = s.SafeMemoryMappedViewHandle;

                    Assert.False(handle.IsClosed);
                    s.Dispose();
                    Assert.True(handle.IsClosed);
                }
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
            WeakReference<MemoryMappedViewStream> mmvsWeak;
            CreateWeakMmfAndMmvs(out mmfWeak, out mmvsWeak);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (PlatformDetection.IsPreciseGcSupported)
            {
                Assert.False(mmfWeak.TryGetTarget(out _));
                Assert.False(mmvsWeak.TryGetTarget(out _));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateWeakMmfAndMmvs(out WeakReference<MemoryMappedFile> mmfWeak, out WeakReference<MemoryMappedViewStream> mmvsWeak)
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, 4096);
            MemoryMappedViewStream s = mmf.CreateViewStream();

            mmfWeak = new WeakReference<MemoryMappedFile>(mmf);
            mmvsWeak = new WeakReference<MemoryMappedViewStream>(s);
        }

    }
}

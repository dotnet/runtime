// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    public class MemoryMappedFileTests_CreateOrOpen : MemoryMappedFilesTestBase
    {
        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen mapName parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_MapName()
        {
            // null is an invalid map name with CreateOrOpen (it's valid with CreateNew and CreateFromFile)
            AssertExtensions.Throws<ArgumentNullException>("mapName", () => MemoryMappedFile.CreateOrOpen(null, 4096));
            AssertExtensions.Throws<ArgumentNullException>("mapName", () => MemoryMappedFile.CreateOrOpen(null, 4096, MemoryMappedFileAccess.ReadWrite));
            AssertExtensions.Throws<ArgumentNullException>("mapName", () => MemoryMappedFile.CreateOrOpen(null, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));

            // Empty string is always an invalid map name
            AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateOrOpen(string.Empty, 4096));
            AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateOrOpen(string.Empty, 4096, MemoryMappedFileAccess.ReadWrite));
            AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateOrOpen(string.Empty, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Test to verify that map names are left unsupported on Unix.
        /// </summary>
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Map names not supported on Unix
        [Theory]
        [MemberData(nameof(CreateValidMapNames))]
        public void MapNamesNotSupported_Unix(string mapName)
        {
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateOrOpen(mapName, 4096));
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateOrOpen(mapName, 4096, MemoryMappedFileAccess.ReadWrite));
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateOrOpen(mapName, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen capacity parameter.
        /// </summary>
        [Theory]
        [InlineData(0)] // can't create a new map with a default capacity
        [InlineData(-1)] // negative capacities don't make sense
        public void InvalidArguments_Capacity(long capacity)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), capacity));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), capacity, MemoryMappedFileAccess.ReadWrite));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen capacity parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_Capacity_TooLarge()
        {
            // On 32-bit, values larger than uint.MaxValue aren't allowed
            if (IntPtr.Size == 4)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue, MemoryMappedFileAccess.ReadWrite));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
            }
        }

        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen access parameter.
        /// </summary>
        [Theory]
        [InlineData((MemoryMappedFileAccess)(-1))]
        [InlineData((MemoryMappedFileAccess)(42))]
        public void InvalidArgument_Access(MemoryMappedFileAccess access)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, access));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, access, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen options parameter.
        /// </summary>
        [Theory]
        [InlineData((MemoryMappedFileOptions)(-1))]
        [InlineData((MemoryMappedFileOptions)(42))]
        public void InvalidArgument_Options(MemoryMappedFileOptions options)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.ReadWrite, options, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen inheritability parameter.
        /// </summary>
        [Theory]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)(42))]
        public void InvalidArgument_Inheritability(HandleInheritability inheritability)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, inheritability));
        }

        /// <summary>
        /// Test various combinations of arguments to CreateOrOpen, validating the created maps each time they're created,
        /// focusing on accesses that don't involve execute permissions.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // Map names not supported on Unix
        [Theory]
        [MemberData(nameof(MemberData_ValidArgumentCombinations),
            new string[] { "CreateUniqueMapName()" },
            new long[] { 1, 256, -1 /*pagesize*/, 10000 },
            new MemoryMappedFileAccess[] { MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.CopyOnWrite },
            new MemoryMappedFileOptions[] { MemoryMappedFileOptions.None, MemoryMappedFileOptions.DelayAllocatePages },
            new HandleInheritability[] { HandleInheritability.None, HandleInheritability.Inheritable })]
        public void ValidArgumentCombinations_NonExecute(
            string mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability)
        {
            // Map doesn't exist
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                ValidateMemoryMappedFile(mmf, capacity, access);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, inheritability))
            {
                ValidateMemoryMappedFile(mmf, capacity, access, inheritability);
            }

            // Map does exist (CreateNew)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf2, capacity);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                ValidateMemoryMappedFile(mmf2, capacity, access);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access, options, inheritability))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, inheritability))
            {
                ValidateMemoryMappedFile(mmf2, capacity, access, inheritability);
            }

            // Map does exist (CreateFromFile)
            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, mapName))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf2, capacity);
            }
            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, mapName, capacity, access))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                ValidateMemoryMappedFile(mmf2, capacity, access);
            }
        }

        /// <summary>
        /// Test various combinations of arguments to CreateOrOpen, validating the created maps each time they're created,
        /// focusing on accesses that involve execute permissions.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // Map names not supported on Unix
        [Theory]
        [MemberData(nameof(MemberData_ValidArgumentCombinations),
            new string[] { "CreateUniqueMapName()" },
            new long[] { 1, 256, -1 /*pagesize*/, 10000 },
            new MemoryMappedFileAccess[] { MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.ReadWriteExecute },
            new MemoryMappedFileOptions[] { MemoryMappedFileOptions.None, MemoryMappedFileOptions.DelayAllocatePages },
            new HandleInheritability[] { HandleInheritability.None, HandleInheritability.Inheritable })]
        public void ValidArgumentCombinations_Execute(
            string mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability)
        {
            // Map doesn't exist
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                ValidateMemoryMappedFile(mmf, capacity, access);
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, inheritability))
            {
                ValidateMemoryMappedFile(mmf, capacity, access, inheritability);
            }

            // Map does exist (CreateNew)
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(mapName, capacity, access))
            {
                ValidateMemoryMappedFile(mmf2, capacity, access);
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access, options, inheritability))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateOrOpen(mapName, capacity, access, options, inheritability))
            {
                ValidateMemoryMappedFile(mmf2, capacity, access, inheritability);
            }

            // (Avoid testing with CreateFromFile when using execute permissions.)
        }

        /// <summary>
        /// Provides input data to the ValidArgumentCombinations tests, yielding the full matrix
        /// of combinations of input values provided, except for those that are known to be unsupported
        /// (e.g. non-null map names on Unix), and with appropriate values substituted in for placeholders
        /// listed in the MemberData attribute (e.g. actual system page size instead of -1).
        /// </summary>
        /// <param name="mapNames">
        /// The names to yield.  
        /// non-null may be excluded based on platform.
        /// "CreateUniqueMapName()" will be translated to an invocation of that method.
        /// </param>
        /// <param name="capacities">The capacities to yield.  -1 will be translated to system page size.</param>
        /// <param name="accesses">The accesses to yield</param>
        /// <param name="options">The options to yield.</param>
        /// <param name="inheritabilities">The inheritabilities to yield.</param>
        public static IEnumerable<object[]> MemberData_ValidArgumentCombinations(
            string[] mapNames, long[] capacities, MemoryMappedFileAccess[] accesses, MemoryMappedFileOptions[] options, HandleInheritability[] inheritabilities)
        {
            foreach (string tmpMapName in mapNames)
            {
                if (tmpMapName != null && !MapNamesSupported)
                {
                    continue;
                }

                foreach (long tmpCapacity in capacities)
                {
                    long capacity = tmpCapacity == -1 ?
                        s_pageSize.Value :
                        tmpCapacity;

                    foreach (MemoryMappedFileAccess access in accesses)
                    {
                        foreach (MemoryMappedFileOptions option in options)
                        {
                            foreach (HandleInheritability inheritability in inheritabilities)
                            {
                                string mapName = tmpMapName == "CreateUniqueMapName()" ? CreateUniqueMapName() : tmpMapName;
                                yield return new object[] { mapName, capacity, access, option, inheritability };
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test to validate behavior with MemoryMappedFileAccess.Write.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // Map names not supported on Unix
        [Fact]
        public void OpenWrite()
        {
            // Write-only access fails when the map doesn't exist
            AssertExtensions.Throws<ArgumentException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Write));
            AssertExtensions.Throws<ArgumentException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Write, MemoryMappedFileOptions.None, HandleInheritability.None));

            // Write-only access works when the map does exist
            const int Capacity = 4096;
            string name = CreateUniqueMapName();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(name, Capacity))
            using (MemoryMappedFile opened = MemoryMappedFile.CreateOrOpen(name, Capacity, MemoryMappedFileAccess.Write))
            {
                ValidateMemoryMappedFile(mmf, Capacity, MemoryMappedFileAccess.Write);
            }
        }

        /// <summary>
        /// Test the exceptional behavior when attempting to create a map so large it's not supported.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // Map names not supported on Unix
        [Fact]
        public void TooLargeCapacity()
        {
            if (IntPtr.Size == 4)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue));
            }
            else
            {
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), long.MaxValue));
            }
        }

        /// <summary>
        /// Test that the capacity of a view matches the original capacity, not that specified when opening the new map,
        /// and that the original capacity doesn't change from opening another map with a different capacity.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // Map names not supported on Unix
        [Fact]
        public void OpenedCapacityMatchesOriginal()
        {
            string name = CreateUniqueMapName();

            using (MemoryMappedFile original = MemoryMappedFile.CreateNew(name, 10000))
            {
                // Get the capacity of a view before we open any copies
                long capacity;
                using (MemoryMappedViewAccessor originalAcc = original.CreateViewAccessor())
                {
                    capacity = originalAcc.Capacity;
                }

                // Open additional maps
                using (MemoryMappedFile opened1 = MemoryMappedFile.CreateOrOpen(name, 1))     // smaller capacity
                using (MemoryMappedFile opened2 = MemoryMappedFile.CreateOrOpen(name, 20000)) // larger capacity
                {
                    // Verify that the original's capacity hasn't changed
                    using (MemoryMappedViewAccessor originalAcc = original.CreateViewAccessor())
                    {
                        Assert.Equal(capacity, originalAcc.Capacity);
                    }

                    // Verify that each map's capacity is the same as the original even though
                    // different values were provided
                    foreach (MemoryMappedFile opened in new[] { opened1, opened2 })
                    {
                        using (MemoryMappedViewAccessor acc = opened.CreateViewAccessor())
                        {
                            Assert.Equal(capacity, acc.Capacity);
                        }
                    }
                }
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Map names not supported on Unix
        [Fact]
        public void OpenedAccessibilityLimitedToOriginal()
        {
            const int Capacity = 4096;
            string name = CreateUniqueMapName();

            // Open the original as Read but the copy as ReadWrite
            using (MemoryMappedFile original = MemoryMappedFile.CreateNew(name, Capacity, MemoryMappedFileAccess.Read))
            using (MemoryMappedFile opened = MemoryMappedFile.CreateOrOpen(name, Capacity, MemoryMappedFileAccess.ReadWrite))
            {
                // Even though we passed ReadWrite to CreateOrOpen, trying to open a view accessor with ReadWrite should fail
                Assert.Throws<IOException>(() => opened.CreateViewAccessor());
                Assert.Throws<IOException>(() => opened.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite));

                // But Read should succeed
                opened.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.Read).Dispose();
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // Map names not supported on Unix
        [Fact]
        public void OpenedAccessibilityLimitedBeyondOriginal()
        {
            const int Capacity = 4096;
            string name = CreateUniqueMapName();

            // Open the original as ReadWrite but the copy as Read
            using (MemoryMappedFile original = MemoryMappedFile.CreateNew(name, Capacity, MemoryMappedFileAccess.ReadWrite))
            using (MemoryMappedFile opened = MemoryMappedFile.CreateOrOpen(name, Capacity, MemoryMappedFileAccess.Read))
            {
                // Even though we passed ReadWrite to CreateNew, trying to open a view accessor with ReadWrite should fail
                Assert.Throws<UnauthorizedAccessException>(() => opened.CreateViewAccessor());
                Assert.Throws<UnauthorizedAccessException>(() => opened.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite));

                // But Read should succeed
                opened.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.Read).Dispose();
            }
        }

    }
}

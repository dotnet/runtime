// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Assert.Throws<ArgumentNullException>("mapName", () => MemoryMappedFile.CreateOrOpen(null, 4096));
            Assert.Throws<ArgumentNullException>("mapName", () => MemoryMappedFile.CreateOrOpen(null, 4096, MemoryMappedFileAccess.ReadWrite));
            Assert.Throws<ArgumentNullException>("mapName", () => MemoryMappedFile.CreateOrOpen(null, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));

            // Empty string is always an invalid map name
            Assert.Throws<ArgumentException>(() => MemoryMappedFile.CreateOrOpen(string.Empty, 4096));
            Assert.Throws<ArgumentException>(() => MemoryMappedFile.CreateOrOpen(string.Empty, 4096, MemoryMappedFileAccess.ReadWrite));
            Assert.Throws<ArgumentException>(() => MemoryMappedFile.CreateOrOpen(string.Empty, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Test to verify that map names are left unsupported on Unix.
        /// </summary>
        [Theory, PlatformSpecific(PlatformID.AnyUnix)]
        [MemberData("CreateValidMapNames")]
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
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), capacity));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), capacity, MemoryMappedFileAccess.ReadWrite));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
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
                Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue));
                Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue, MemoryMappedFileAccess.ReadWrite));
                Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
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
            Assert.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, access));
            Assert.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, access, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen options parameter.
        /// </summary>
        [Theory]
        [InlineData((MemoryMappedFileOptions)(-1))]
        [InlineData((MemoryMappedFileOptions)(42))]
        public void InvalidArgument_Options(MemoryMappedFileOptions options)
        {
            Assert.Throws<ArgumentOutOfRangeException>("options", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.ReadWrite, options, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen inheritability parameter.
        /// </summary>
        [Theory]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)(42))]
        public void InvalidArgument_Inheritability(HandleInheritability inheritability)
        {
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, inheritability));
        }

        /// <summary>
        /// Test various combinations of arguments to CreateOrOpen, validating the created maps each time they're created,
        /// focusing on on accesses that don't involve execute permissions.
        /// </summary>
        [Theory, PlatformSpecific(PlatformID.Windows)]
        [MemberData("MemberData_ValidArgumentCombinations",
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
        /// focusing on on accesses that involve execute permissions.
        /// </summary>
        [Theory, PlatformSpecific(PlatformID.Windows)]
        [MemberData("MemberData_ValidArgumentCombinations",
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
        /// Test to validate behavior with MemoryMappedFileAccess.Write.
        /// </summary>
        [Fact, PlatformSpecific(PlatformID.Windows)]
        public void OpenWrite()
        {
            // Write-only access fails when the map doesn't exist
            Assert.Throws<ArgumentException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Write));
            Assert.Throws<ArgumentException>("access", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Write, MemoryMappedFileOptions.None, HandleInheritability.None));

            // Write-only access works when the map does exist
            string name = CreateUniqueMapName();
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(name, 4096))
            using (MemoryMappedFile opened = MemoryMappedFile.CreateOrOpen(name, 4096, MemoryMappedFileAccess.Write))
            {
                ValidateMemoryMappedFile(mmf, 4096, MemoryMappedFileAccess.Write);
            }
        }

        /// <summary>
        /// Test the exceptional behavior when attempting to create a map so large it's not supported.
        /// </summary>
        [Fact, PlatformSpecific(PlatformID.Windows)]
        public void TooLargeCapacity()
        {
            if (IntPtr.Size == 4)
            {
                Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateOrOpen(CreateUniqueMapName(), 1 + (long)uint.MaxValue));
            }
            else
            {
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateNew(CreateUniqueMapName(), long.MaxValue));
            }
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
            object[] mapNames, object[] capacities, object[] accesses, object[] options, object[] inheritabilities)
        {
            foreach (string tmpMapName in mapNames)
            {
                if (tmpMapName != null && !MapNamesSupported)
                {
                    continue;
                }
                string mapName = tmpMapName == "CreateUniqueMapName()" ? CreateUniqueMapName() : tmpMapName;

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
                                yield return new object[] { mapName, capacity, access, option, inheritability };
                            }
                        }
                    }
                }
            }
        }


    }
}

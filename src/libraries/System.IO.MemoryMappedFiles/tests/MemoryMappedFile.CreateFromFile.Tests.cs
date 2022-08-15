// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>
    /// Tests for MemoryMappedFile.CreateFromFile.
    /// </summary>
    public class MemoryMappedFileTests_CreateFromFile : MemoryMappedFilesTestBase
    {
        /// <summary>
        /// Tests invalid arguments to the CreateFromFile path parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_Path()
        {
            // null is an invalid path
            AssertExtensions.Throws<ArgumentNullException>("path", () => MemoryMappedFile.CreateFromFile(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => MemoryMappedFile.CreateFromFile(null, FileMode.Open));
            AssertExtensions.Throws<ArgumentNullException>("path", () => MemoryMappedFile.CreateFromFile(null, FileMode.Open, CreateUniqueMapName()));
            AssertExtensions.Throws<ArgumentNullException>("path", () => MemoryMappedFile.CreateFromFile(null, FileMode.Open, CreateUniqueMapName(), 4096));
            AssertExtensions.Throws<ArgumentNullException>("path", () => MemoryMappedFile.CreateFromFile(null, FileMode.Open, CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Read));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateFromFile fileStream parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_FileStream()
        {
            // null is an invalid stream
            AssertExtensions.Throws<ArgumentNullException>("fileStream", () => MemoryMappedFile.CreateFromFile(null, CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Read, HandleInheritability.None, true));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateFromFile mode parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_Mode()
        {
            // FileMode out of range
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), (FileMode)42));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), (FileMode)42, null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), (FileMode)42, null, 4096));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), (FileMode)42, null, 4096, MemoryMappedFileAccess.ReadWrite));

            // FileMode.Append never allowed
            AssertExtensions.Throws<ArgumentException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Append));
            AssertExtensions.Throws<ArgumentException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Append, null));
            AssertExtensions.Throws<ArgumentException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Append, null, 4096));
            AssertExtensions.Throws<ArgumentException>("mode", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Append, null, 4096, MemoryMappedFileAccess.ReadWrite));

            // FileMode.CreateNew/Create/OpenOrCreate can't be used with default capacity, as the file will be empty
            AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.CreateNew));
            AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Create));
            AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.OpenOrCreate));

            // FileMode.Truncate can't be used with default capacity, as resulting file will be empty
            using (TempFile file = new TempFile(GetTestFilePath()))
            {
                AssertExtensions.Throws<ArgumentException>("mode", null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Truncate));
            }
        }

        [Fact]
        public void InvalidArguments_Mode_Truncate()
        {
            // FileMode.Truncate never allowed
            AssertExtensions.Throws<ArgumentException>("mode", null, () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Truncate));
            AssertExtensions.Throws<ArgumentException>("mode", null, () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Truncate, null));
            AssertExtensions.Throws<ArgumentException>("mode", null, () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Truncate, null, 4096));
            AssertExtensions.Throws<ArgumentException>("mode", null, () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Truncate, null, 4096, MemoryMappedFileAccess.ReadWrite));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateFromFile access parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_Access()
        {
            // Out of range access values with a path
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Open, CreateUniqueMapName(), 4096, (MemoryMappedFileAccess)(-2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Open, CreateUniqueMapName(), 4096, (MemoryMappedFileAccess)(42)));

            //  Write-only access is not allowed on maps (only on views)
            AssertExtensions.Throws<ArgumentException>("access", () => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Open, CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Write));

            // Test the same things, but with a FileStream instead of a path
            using (TempFile file = new TempFile(GetTestFilePath()))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            {
                // Out of range values with a stream
                AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateFromFile(fs, CreateUniqueMapName(), 4096, (MemoryMappedFileAccess)(-2), HandleInheritability.None, true));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateFromFile(fs, CreateUniqueMapName(), 4096, (MemoryMappedFileAccess)(42), HandleInheritability.None, true));

                // Write-only access is not allowed
                AssertExtensions.Throws<ArgumentException>("access", () => MemoryMappedFile.CreateFromFile(fs, CreateUniqueMapName(), 4096, MemoryMappedFileAccess.Write, HandleInheritability.None, true));
            }
        }

        /// <summary>
        /// Tests various values of FileAccess used to construct a FileStream and MemoryMappedFileAccess used
        /// to construct a map over that stream.  The combinations should all be valid.
        /// </summary>
        [Theory]
        [InlineData(FileAccess.ReadWrite, MemoryMappedFileAccess.Read)]
        [InlineData(FileAccess.ReadWrite, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(FileAccess.ReadWrite, MemoryMappedFileAccess.CopyOnWrite)]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.Read)]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.CopyOnWrite)]
        public void FileAccessAndMapAccessCombinations_Valid(FileAccess fileAccess, MemoryMappedFileAccess mmfAccess)
        {
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath(), Capacity))
            using (FileStream fs = new FileStream(file.Path, FileMode.Open, fileAccess))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, Capacity, mmfAccess, HandleInheritability.None, true))
            {
                ValidateMemoryMappedFile(mmf, Capacity, mmfAccess);
            }
        }

        /// <summary>
        /// Tests various values of FileAccess used to construct a FileStream and MemoryMappedFileAccess used
        /// to construct a map over that stream on Windows.  The combinations should all be invalid, resulting in exception.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // On Windows, permission errors come from CreateFromFile
        [Theory]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.Read)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.CopyOnWrite)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(FileAccess.ReadWrite, MemoryMappedFileAccess.ReadExecute)] // this and the next are explicitly left off of the Unix test due to differences in Unix permissions
        [InlineData(FileAccess.ReadWrite, MemoryMappedFileAccess.ReadWriteExecute)]
        public void FileAccessAndMapAccessCombinations_Invalid_Windows(FileAccess fileAccess, MemoryMappedFileAccess mmfAccess)
        {
            // On Windows, creating the file mapping does the permissions checks, so the exception comes from CreateFromFile.
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath(), Capacity))
            using (FileStream fs = new FileStream(file.Path, FileMode.Open, fileAccess))
            {
                Assert.Throws<UnauthorizedAccessException>(() => MemoryMappedFile.CreateFromFile(fs, null, Capacity, mmfAccess, HandleInheritability.None, true));
            }
        }

        /// <summary>
        /// Tests various values of FileAccess used to construct a FileStream and MemoryMappedFileAccess used
        /// to construct a map over that stream on Unix.  The combinations should all be invalid, resulting in exception.
        /// </summary>
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // On Unix, permission errors come from CreateView*
        [Theory]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(FileAccess.Read, MemoryMappedFileAccess.ReadWriteExecute)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.Read)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.ReadWrite)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.CopyOnWrite)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.ReadExecute)]
        [InlineData(FileAccess.Write, MemoryMappedFileAccess.ReadWriteExecute)]
        public void FileAccessAndMapAccessCombinations_Invalid_Unix(FileAccess fileAccess, MemoryMappedFileAccess mmfAccess)
        {
            // On Unix we don't actually create the OS map until the view is created; this results in the permissions
            // error being thrown from CreateView* instead of from CreateFromFile.
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath(), Capacity))
            using (FileStream fs = new FileStream(file.Path, FileMode.Open, fileAccess))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, Capacity, mmfAccess, HandleInheritability.None, true))
            {
                Assert.Throws<UnauthorizedAccessException>(() => mmf.CreateViewAccessor());
            }
        }

        /// <summary>
        /// Tests invalid arguments to the CreateFromFile mapName parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_MapName()
        {
            using (TempFile file = new TempFile(GetTestFilePath()))
            {
                // Empty string is an invalid map name
                AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, string.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, string.Empty, 4096));
                AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, string.Empty, 4096, MemoryMappedFileAccess.Read));
                AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, string.Empty, 4096, MemoryMappedFileAccess.Read));
                using (FileStream fs = File.Open(file.Path, FileMode.Open))
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(fs, string.Empty, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true));
                }
            }
        }

        /// <summary>
        /// Test to verify that map names are left unsupported on Unix.
        /// </summary>
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Check map names are unsupported on Unix
        [Theory]
        [MemberData(nameof(CreateValidMapNames))]
        public void MapNamesNotSupported_Unix(string mapName)
        {
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath(), Capacity))
            {
                Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, mapName));
                Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, mapName, Capacity));
                Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, mapName, Capacity, MemoryMappedFileAccess.ReadWrite));
                Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, mapName, Capacity, MemoryMappedFileAccess.ReadWrite));
                using (FileStream fs = File.Open(file.Path, FileMode.Open))
                {
                    Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateFromFile(fs, mapName, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true));
                }
            }
        }

        /// <summary>
        /// Tests invalid arguments to the CreateFromFile capacity parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_Capacity()
        {
            using (TempFile file = new TempFile(GetTestFilePath()))
            {
                // Out of range values for capacity
                Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, CreateUniqueMapName(), -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, CreateUniqueMapName(), -1, MemoryMappedFileAccess.Read));

                // Positive capacity required when creating a map from an empty file
                AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read));
                AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, CreateUniqueMapName(), 0, MemoryMappedFileAccess.Read));

                // With Read, the capacity can't be larger than the backing file's size.
                AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, CreateUniqueMapName(), 1, MemoryMappedFileAccess.Read));

                // Now with a FileStream...
                using (FileStream fs = File.Open(file.Path, FileMode.Open))
                {
                    // The subsequent tests are only valid we if we start with an empty FileStream, which we should have.
                    // This also verifies the previous failed tests didn't change the length of the file.
                    Assert.Equal(0, fs.Length);

                    // Out of range values for capacity
                    Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMappedFile.CreateFromFile(fs, null, -1, MemoryMappedFileAccess.Read, HandleInheritability.None, true));

                    // Default (0) capacity with an empty file
                    AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true));
                    AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(fs, CreateUniqueMapName(), 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true));

                    // Larger capacity than the underlying file, but read-only such that we can't expand the file
                    fs.SetLength(4096);
                    AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(fs, null, 8192, MemoryMappedFileAccess.Read, HandleInheritability.None, true));
                    AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.CreateFromFile(fs, CreateUniqueMapName(), 8192, MemoryMappedFileAccess.Read, HandleInheritability.None, true));

                    // Capacity can't be less than the file size (for such cases a view can be created with the smaller size)
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateFromFile(fs, null, 1, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true));
                }

                // Capacity can't be less than the file size
                AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, CreateUniqueMapName(), 1, MemoryMappedFileAccess.Read));
            }
        }

        /// <summary>
        /// Tests invalid arguments to the CreateFromFile inheritability parameter.
        /// </summary>
        [Theory]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)(42))]
        public void InvalidArguments_Inheritability(HandleInheritability inheritability)
        {
            // Out of range values for inheritability
            using (TempFile file = new TempFile(GetTestFilePath()))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => MemoryMappedFile.CreateFromFile(fs, CreateUniqueMapName(), 4096, MemoryMappedFileAccess.ReadWrite, inheritability, true));
            }
        }

        /// <summary>
        /// Test various combinations of arguments to CreateFromFile, focusing on the Open and OpenOrCreate modes,
        /// and validating the creating maps each time they're created.
        /// </summary>
        [Theory]
        [MemberData(nameof(MemberData_ValidArgumentCombinationsWithPath),
            new FileMode[] { FileMode.Open, FileMode.OpenOrCreate },
            new string[] { null, "CreateUniqueMapName()" },
            new long[] { 1, 256, -1 /*pagesize*/, 10000 },
            new MemoryMappedFileAccess[] { MemoryMappedFileAccess.ReadWrite })]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51375", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void ValidArgumentCombinationsWithPath_ModesOpenOrCreate(
            FileMode mode, string mapName, long capacity, MemoryMappedFileAccess access)
        {
            _ = access;

            // Test each of the four path-based CreateFromFile overloads

            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }

            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, mode))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }

            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, mode, mapName))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }

            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, mode, mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }

            // Finally, re-test the last overload, this time with an empty file to start

            using (TempFile file = new TempFile(GetTestFilePath()))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, mode, mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }
        }

        /// <summary>
        /// Test various combinations of arguments to CreateFromFile, focusing on the CreateNew mode,
        /// and validating the creating maps each time they're created.
        /// </summary>
        [Theory]
        [MemberData(nameof(MemberData_ValidArgumentCombinationsWithPath),
            new FileMode[] { FileMode.CreateNew },
            new string[] { null, "CreateUniqueMapName()" },
            new long[] { 1, 256, -1 /*pagesize*/, 10000 },
            new MemoryMappedFileAccess[] { MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.CopyOnWrite })]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51375", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void ValidArgumentCombinationsWithPath_ModeCreateNew(
            FileMode mode, string mapName, long capacity, MemoryMappedFileAccess access)
        {
            // For FileMode.CreateNew, the file will be created new and thus be empty, so we can only use the overloads
            // that take a capacity, since the default capacity doesn't work with an empty file.

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(GetTestFilePath(), mode, mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(GetTestFilePath(), mode, mapName, capacity, access))
            {
                ValidateMemoryMappedFile(mmf, capacity, access);
            }
        }

        /// <summary>
        /// Test various combinations of arguments to CreateFromFile, focusing on the Create mode,
        /// and validating the creating maps each time they're created.
        /// </summary>
        [Theory]
        [MemberData(nameof(MemberData_ValidNameCapacityCombinationsWithPath),
            new string[] { null, "CreateUniqueMapName()" },
            new long[] { 1, 256, -1 /*pagesize*/, 10000 })]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51375", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void ValidArgumentCombinationsWithPath_ModeCreate(string mapName, long capacity)
        {
            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Create, mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }

            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Create, mapName, capacity, MemoryMappedFileAccess.ReadWrite))
            {
                ValidateMemoryMappedFile(mmf, capacity, MemoryMappedFileAccess.ReadWrite);
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Create, mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }
        }

        /// <summary>
        /// Provides input data to the ValidArgumentCombinationsWithPath tests, yielding the full matrix
        /// of combinations of input values provided, except for those that are known to be unsupported
        /// (e.g. non-null map names on Unix), and with appropriate values substituted in for placeholders
        /// listed in the MemberData attribute (e.g. actual system page size instead of -1).
        /// </summary>
        /// <param name="modes">The modes to yield.</param>
        /// <param name="mapNames">
        /// The names to yield.
        /// non-null may be excluded based on platform.
        /// "CreateUniqueMapName()" will be translated to an invocation of that method.
        /// </param>
        /// <param name="capacities">The capacities to yield.  -1 will be translated to system page size.</param>
        /// <param name="accesses">
        /// The accesses to yield.  Non-writable accesses will be skipped if the current mode doesn't support it.
        /// </param>
        public static IEnumerable<object[]> MemberData_ValidArgumentCombinationsWithPath(
            FileMode[] modes, string[] mapNames, long[] capacities, MemoryMappedFileAccess[] accesses)
        {
            foreach (object[] namesCaps in MemberData_ValidNameCapacityCombinationsWithPath(mapNames, capacities))
            {
                foreach (FileMode mode in modes)
                {
                    foreach (MemoryMappedFileAccess access in accesses)
                    {
                        if ((mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.Truncate) &&
                            !IsWritable(access))
                        {
                            continue;
                        }

                        yield return new object[] { mode, namesCaps[0], namesCaps[1], access };
                    }
                }
            }
        }

        public static IEnumerable<object[]> MemberData_ValidNameCapacityCombinationsWithPath(
            string[] mapNames, long[] capacities)
        {
            foreach (string tmpMapName in mapNames)
            {
                if (tmpMapName != null && !MapNamesSupported)
                {
                    continue;
                }

                foreach (long tmpCapacity in capacities)
                {
                    long capacity = tmpCapacity == -1 ? s_pageSize.Value : tmpCapacity;
                    string mapName = tmpMapName == "CreateUniqueMapName()" ? CreateUniqueMapName() : tmpMapName;
                    yield return new object[] { mapName, capacity, };
                }
            }
        }

        /// <summary>
        /// Test various combinations of arguments to CreateFromFile that accepts a FileStream.
        /// </summary>
        [Theory]
        [MemberData(nameof(MemberData_ValidArgumentCombinationsWithStream),
            new string[] { null, "CreateUniqueMapName()" },
            new long[] { 1, 256, -1 /*pagesize*/, 10000 },
            new MemoryMappedFileAccess[] { MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.CopyOnWrite },
            new HandleInheritability[] { HandleInheritability.None, HandleInheritability.Inheritable },
            new bool[] { false, true })]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51375", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void ValidArgumentCombinationsWithStream(
            string mapName, long capacity, MemoryMappedFileAccess access, HandleInheritability inheritability, bool leaveOpen)
        {
            // Create a file of the right size, then create the map for it.
            using (TempFile file = new TempFile(GetTestFilePath(), capacity))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, mapName, capacity, access, inheritability, leaveOpen))
            {
                ValidateMemoryMappedFile(mmf, capacity, access, inheritability);
            }

            // Start with an empty file and let the map grow it to the right size.  This requires write access.
            if (IsWritable(access))
            {
                using (FileStream fs = File.Create(GetTestFilePath()))
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, mapName, capacity, access, inheritability, leaveOpen))
                {
                    ValidateMemoryMappedFile(mmf, capacity, access, inheritability);
                }
            }
        }

        /// <summary>
        /// Provides input data to the ValidArgumentCombinationsWithStream tests, yielding the full matrix
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
        /// <param name="accesses">
        /// The accesses to yield.  Non-writable accesses will be skipped if the current mode doesn't support it.
        /// </param>
        /// <param name="inheritabilities">The inheritabilities to yield.</param>
        /// <param name="inheritabilities">The leaveOpen values to yield.</param>
        public static IEnumerable<object[]> MemberData_ValidArgumentCombinationsWithStream(
            string[] mapNames, long[] capacities, MemoryMappedFileAccess[] accesses, HandleInheritability[] inheritabilities, bool[] leaveOpens)
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
                        foreach (HandleInheritability inheritability in inheritabilities)
                        {
                            foreach (bool leaveOpen in leaveOpens)
                            {
                                string mapName = tmpMapName == "CreateUniqueMapName()" ? CreateUniqueMapName() : tmpMapName;
                                yield return new object[] { mapName, capacity, access, inheritability, leaveOpen };
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test that a map using the default capacity (0) grows to the size of the underlying file.
        /// </summary>
        [Fact]
        public void DefaultCapacityIsFileLength()
        {
            const int DesiredCapacity = 8192;
            const int DefaultCapacity = 0;

            // With path
            using (TempFile file = new TempFile(GetTestFilePath(), DesiredCapacity))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, DefaultCapacity))
            {
                ValidateMemoryMappedFile(mmf, DesiredCapacity);
            }

            // With stream
            using (TempFile file = new TempFile(GetTestFilePath(), DesiredCapacity))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, DefaultCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            {
                ValidateMemoryMappedFile(mmf, DesiredCapacity);
            }
        }

        /// <summary>
        /// Test that appropriate exceptions are thrown creating a map with a non-existent file and a mode
        /// that requires the file to exist.
        /// </summary>
        [Fact]
        public void FileDoesNotExist_OpenFileMode()
        {
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.CreateFromFile(GetTestFilePath()));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Open));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Open, null));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Open, null, 4096));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.Open, null, 4096, MemoryMappedFileAccess.ReadWrite));
        }

        /// <summary>
        /// Test that appropriate exceptions are thrown creating a map with an existing file and a mode
        /// that requires the file to not exist.
        /// </summary>
        [Fact]
        public void FileAlreadyExists()
        {
            using (TempFile file = new TempFile(GetTestFilePath()))
            {
                // FileMode.CreateNew invalid when the file already exists
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.CreateNew));
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.CreateNew, CreateUniqueMapName()));
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.CreateNew, CreateUniqueMapName(), 4096));
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.CreateNew, CreateUniqueMapName(), 4096, MemoryMappedFileAccess.ReadWrite));
            }
        }

        /// <summary>
        /// Test exceptional behavior when trying to create a map for a read-write file that's currently in use.
        /// </summary>
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // FileShare is limited on Unix, with None == exclusive, everything else == concurrent
        public void FileInUse_CreateFromFile_FailsWithExistingReadWriteFile()
        {
            // Already opened with a FileStream
            using (TempFile file = new TempFile(GetTestFilePath(), 4096))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            {
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(file.Path));
            }
        }

        /// <summary>
        /// Test exceptional behavior when trying to create a map for a non-shared file that's currently in use.
        /// </summary>
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // FileShare is limited on Unix, with None == exclusive, everything else == concurrent
        public void FileInUse_CreateFromFile_FailsWithExistingReadWriteMap()
        {
            // Already opened with another read-write map
            using (TempFile file = new TempFile(GetTestFilePath(), 4096))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path))
            {
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(file.Path));
            }
        }

        /// <summary>
        /// Test exceptional behavior when trying to create a map for a non-shared file that's currently in use.
        /// </summary>
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "the emscripten implementation ignores FileShare.None")]
        public void FileInUse_CreateFromFile_FailsWithExistingNoShareFile()
        {
            // Already opened with a FileStream
            using (TempFile file = new TempFile(GetTestFilePath(), 4096))
            using (FileStream fs = File.Open(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(file.Path));
            }
        }

        /// <summary>
        /// Test to validate we can create multiple concurrent read-only maps from the same file path.
        /// </summary>
        [Fact]
        public void FileInUse_CreateFromFile_SucceedsWithReadOnly()
        {
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath(), Capacity))
            using (MemoryMappedFile mmf1 = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, Capacity, MemoryMappedFileAccess.Read))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, Capacity, MemoryMappedFileAccess.Read))
            using (MemoryMappedViewAccessor acc1 = mmf1.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.Read))
            using (MemoryMappedViewAccessor acc2 = mmf2.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.Read))
            {
                Assert.Equal(acc1.Capacity, acc2.Capacity);
            }
        }

        /// <summary>
        /// Test the exceptional behavior of *Execute access levels.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)] // Unix model for executable differs from Windows
        [Theory]
        [InlineData(MemoryMappedFileAccess.ReadExecute)]
        [InlineData(MemoryMappedFileAccess.ReadWriteExecute)]
        public void FileNotOpenedForExecute(MemoryMappedFileAccess access)
        {
            using (TempFile file = new TempFile(GetTestFilePath(), 4096))
            {
                // The FileStream created by the map doesn't have GENERIC_EXECUTE set
                Assert.Throws<UnauthorizedAccessException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, 4096, access));

                // The FileStream opened explicitly doesn't have GENERIC_EXECUTE set
                using (FileStream fs = File.Open(file.Path, FileMode.Open))
                {
                    Assert.Throws<UnauthorizedAccessException>(() => MemoryMappedFile.CreateFromFile(fs, null, 4096, access, HandleInheritability.None, true));
                }
            }
        }

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed.
        /// </summary>
        private void WriteToReadOnlyFile(MemoryMappedFileAccess access, bool succeeds)
        {
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath(), Capacity))
            {
                FileAttributes original = File.GetAttributes(file.Path);
                File.SetAttributes(file.Path, FileAttributes.ReadOnly);
                try
                {
                    if (succeeds)
                        using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, Capacity, access))
                            ValidateMemoryMappedFile(mmf, Capacity, MemoryMappedFileAccess.Read);
                    else
                        Assert.Throws<UnauthorizedAccessException>(() => MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, Capacity, access));
                }
                finally
                {
                    File.SetAttributes(file.Path, original);
                }
            }
        }

        [Theory]
        [InlineData(MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.ReadWrite)]
        public void WriteToReadOnlyFile_ReadWrite(MemoryMappedFileAccess access)
        {
            WriteToReadOnlyFile(access, access == MemoryMappedFileAccess.Read ||
                            PlatformDetection.IsSuperUser);
        }

        [Fact]
        public void WriteToReadOnlyFile_CopyOnWrite()
        {
            WriteToReadOnlyFile(MemoryMappedFileAccess.CopyOnWrite, PlatformDetection.IsSuperUser);
        }

        /// <summary>
        /// Test to ensure that leaveOpen is appropriately respected, either leaving the FileStream open
        /// or closing it on disposal.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LeaveOpenRespected_Basic(bool leaveOpen)
        {
            const int Capacity = 4096;

            using (TempFile file = new TempFile(GetTestFilePath()))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            {
                // Handle should still be open
                SafeFileHandle handle = fs.SafeFileHandle;
                Assert.False(handle.IsClosed);

                // Create and close the map
                MemoryMappedFile.CreateFromFile(fs, null, Capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen).Dispose();

                // The handle should now be open iff leaveOpen
                Assert.NotEqual(leaveOpen, handle.IsClosed);
            }
        }

        /// <summary>
        /// Test to ensure that leaveOpen is appropriately respected, either leaving the FileStream open
        /// or closing it on disposal.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LeaveOpenRespected_OutstandingViews(bool leaveOpen)
        {
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath()))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            {
                // Handle should still be open
                SafeFileHandle handle = fs.SafeFileHandle;
                Assert.False(handle.IsClosed);

                // Create the map, create each of the views, then close the map
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, Capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen))
                using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, Capacity))
                using (MemoryMappedViewStream s = mmf.CreateViewStream(0, Capacity))
                {
                    // Explicitly close the map. The handle should now be open iff leaveOpen.
                    mmf.Dispose();
                    Assert.NotEqual(leaveOpen, handle.IsClosed);

                    // But the views should still be usable.
                    ValidateMemoryMappedViewAccessor(acc, Capacity, MemoryMappedFileAccess.ReadWrite);
                    ValidateMemoryMappedViewStream(s, Capacity, MemoryMappedFileAccess.ReadWrite);
                }
            }
        }

        /// <summary>
        /// Test to validate we can create multiple maps from the same FileStream.
        /// </summary>
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "the emscripten implementation doesn't share data")]
        public void MultipleMapsForTheSameFileStream()
        {
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath(), Capacity))
            using (FileStream fs = new FileStream(file.Path, FileMode.Open))
            using (MemoryMappedFile mmf1 = MemoryMappedFile.CreateFromFile(fs, null, Capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            using (MemoryMappedFile mmf2 = MemoryMappedFile.CreateFromFile(fs, null, Capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            using (MemoryMappedViewAccessor acc1 = mmf1.CreateViewAccessor())
            using (MemoryMappedViewAccessor acc2 = mmf2.CreateViewAccessor())
            {
                // The capacity of the two maps should be equal
                Assert.Equal(acc1.Capacity, acc2.Capacity);

                var rand = new Random();
                for (int i = 1; i <= 10; i++)
                {
                    // Write a value to one map, then read it from the other,
                    // ping-ponging between the two.
                    int pos = rand.Next((int)acc1.Capacity - 1);
                    MemoryMappedViewAccessor reader = acc1, writer = acc2;
                    if (i % 2 == 0)
                    {
                        reader = acc2;
                        writer = acc1;
                    }
                    writer.Write(pos, (byte)i);
                    writer.Flush();
                    Assert.Equal(i, reader.ReadByte(pos));
                }
            }
        }

        /// <summary>
        /// Test to verify that the map's size increases the underlying file size if the map's capacity is larger.
        /// </summary>
        [Fact]
        public void FileSizeExpandsToCapacity()
        {
            const int InitialCapacity = 256;
            using (TempFile file = new TempFile(GetTestFilePath(), InitialCapacity))
            {
                // Create a map with a larger capacity, and verify the file has expanded.
                MemoryMappedFile.CreateFromFile(file.Path, FileMode.Open, null, InitialCapacity * 2).Dispose();
                using (FileStream fs = File.OpenRead(file.Path))
                {
                    Assert.Equal(InitialCapacity * 2, fs.Length);
                }

                // Do the same thing again but with a FileStream.
                using (FileStream fs = File.Open(file.Path, FileMode.Open))
                {
                    MemoryMappedFile.CreateFromFile(fs, null, InitialCapacity  * 4, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true).Dispose();
                    Assert.Equal(InitialCapacity * 4, fs.Length);
                }
            }
        }

        /// <summary>
        /// Test the exceptional behavior when attempting to create a map so large it's not supported.
        /// </summary>
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51375", TestPlatforms.Browser)]
        [SkipOnPlatform(TestPlatforms.OSX, "Because of the file-based backing, OS X pops up a warning dialog about being out-of-space (even though we clean up immediately)")]
        [Fact]
        public void TooLargeCapacity()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.CreateNew))
            {
                try
                {
                    long length = long.MaxValue;
                    MemoryMappedFile.CreateFromFile(fs, null, length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true).Dispose();
                    Assert.Equal(length, fs.Length); // if it didn't fail to create the file, the length should be what was requested.
                }
                catch (IOException)
                {
                    // Expected exception for too large a capacity
                }
            }
        }

        /// <summary>
        /// Test to verify map names are handled appropriately, causing a conflict when they're active but
        /// reusable in a sequential manner.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests reusability of map names on Windows
        [Theory]
        [MemberData(nameof(CreateValidMapNames))]
        public void ReusingNames_Windows(string name)
        {
            const int Capacity = 4096;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.CreateNew, name, Capacity))
            {
                ValidateMemoryMappedFile(mmf, Capacity);
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.CreateNew, name, Capacity));
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(GetTestFilePath(), FileMode.CreateNew, name, Capacity))
            {
                ValidateMemoryMappedFile(mmf, Capacity);
            }
        }

        private void ValidateDeviceAccess(MemoryMappedFile memMap, long viewCapacity, MemoryMappedFileAccess access)
        {
            using (MemoryMappedViewAccessor view = memMap.CreateViewAccessor(0, viewCapacity, access))
            {
                if (access != MemoryMappedFileAccess.Write)
                {
                    byte b = view.ReadByte(0);
                    // /dev/zero return zeroes.
                    Assert.Equal(0, b);
                }

                if (access != MemoryMappedFileAccess.Read)
                {
                    view.Write(0, (byte)1);
                }
            }
        }

        /// <summary>
        /// Test that we can map special character devices on Unix using FileStream.
        /// </summary>
        [ConditionalTheory]
        [InlineData(MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.ReadWrite)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void OpenCharacterDeviceAsStream(MemoryMappedFileAccess access)
        {
            const string device = "/dev/zero";
            if (!File.Exists(device))
            {
                throw new SkipTestException($"'{device}' is not available.");
            }

            long viewCapacity = 0xFF;

            try
            {
                using (FileStream fs = new FileStream(device, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (MemoryMappedFile memMap = MemoryMappedFile.CreateFromFile(fs, null, viewCapacity, access, HandleInheritability.None, false))
                {
                    ValidateDeviceAccess(memMap, viewCapacity, access);
                }
            }
            catch (UnauthorizedAccessException) { }
            // ENODEV Operation not supported by device.
            catch (IOException ex) when (ex.HResult == 19) { };
        }

        /// <summary>
        /// Test that we can map special character devices on Unix using file name.
        /// </summary>
        [ConditionalTheory]
        [InlineData(MemoryMappedFileAccess.Read)]
        [InlineData(MemoryMappedFileAccess.ReadWrite)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void OpenCharacterDeviceAsFile(MemoryMappedFileAccess access)
        {
            const string device = "/dev/zero";
            if (!File.Exists(device))
            {
                throw new SkipTestException($"'{device}' is not available.");
            }

            long viewCapacity = 0xFF;

            try
            {
                using (MemoryMappedFile memMap = MemoryMappedFile.CreateFromFile(device, FileMode.Open, null, viewCapacity, access))
                {
                    ValidateDeviceAccess(memMap, viewCapacity, access);
                }
            }
            catch (UnauthorizedAccessException) { }
            // ENODEV Operation not supported by device.
            catch (IOException ex) when (ex.HResult == 19) { };
        }

        /// <summary>
        /// Test to verify that the MemoryMappedFile has the same underlying handle as the FileStream it's created from
        /// </summary>
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void MapHandleMatchesFileStreamHandle()
        {
            using (FileStream fs = File.OpenWrite(GetTestFilePath()))
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                {
                    SafeMemoryMappedFileHandle handle = mmf.SafeMemoryMappedFileHandle;
                    Assert.Equal(fs.SafeFileHandle.DangerousGetHandle(), handle.DangerousGetHandle());
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [SkipOnPlatform(TestPlatforms.Browser, "mkfifo is not supported on WASM")]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks fifo")]
        public async Task OpeningMemoryMappedFileFromFileStreamThatWrapsPipeThrowsNotSupportedException(long capacity)
        {
            (string pipePath, NamedPipeServerStream? serverStream) = CreatePipe();
            using FileStream clientStream = new (pipePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            if (serverStream is not null)
            {
                await serverStream.WaitForConnectionAsync();
            }

            Assert.Throws<NotSupportedException>(() => MemoryMappedFile.CreateFromFile(clientStream, null, capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false));

            serverStream?.Dispose();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [SkipOnPlatform(TestPlatforms.Browser, "mkfifo is not supported on WASM")]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks fifo")]
        public void OpeningMemoryMappedFileFromPipePathThrowsNotSupportedException(long capacity)
        {
            (string pipePath, NamedPipeServerStream? serverStream) = CreatePipe();

            Assert.Throws<NotSupportedException>(() => MemoryMappedFile.CreateFromFile(pipePath, FileMode.Open, null, capacity, MemoryMappedFileAccess.ReadWrite));

            serverStream?.Dispose();
        }

        private (string pipePath, NamedPipeServerStream? serverStream) CreatePipe()
        {
            if (OperatingSystem.IsWindows())
            {
                string pipeName = GetNamedPipeServerStreamName();
                string pipePath = Path.GetFullPath($@"\\.\pipe\{pipeName}");

                return (pipePath, new NamedPipeServerStream(pipeName, PipeDirection.InOut));
            }
            else
            {
                string fifoPath = GetTestFilePath();
                Assert.Equal(0, mkfifo(fifoPath, 438 /* 666 in octal */ ));

                return (fifoPath, null);
            }
        }
    }
}

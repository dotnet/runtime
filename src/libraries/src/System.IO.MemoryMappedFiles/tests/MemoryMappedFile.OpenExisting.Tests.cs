// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    public class MemoryMappedFileTests_OpenExisting : MemoryMappedFilesTestBase
    {
        /// <summary>
        /// Tests invalid arguments to the CreateOrOpen mapName parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_Name()
        {
            // null isn't valid when trying to OpenExistinga map
            AssertExtensions.Throws<ArgumentNullException>("mapName", () => MemoryMappedFile.OpenExisting(null));

            // Empty is never a valid map name
            AssertExtensions.Throws<ArgumentException>(null, () => MemoryMappedFile.OpenExisting(string.Empty));
        }

        /// <summary>
        /// Test to verify that map names are left unsupported on Unix.
        /// </summary>
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Map names unsupported on Unix
        [Theory]
        [MemberData(nameof(CreateValidMapNames))]
        public void MapNamesNotSupported_Unix(string mapName)
        {
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.OpenExisting(mapName));
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite));
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite, HandleInheritability.None));
        }

        /// <summary>
        /// Test to verify that non-existent map names result in exceptions.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)]  // Map names unsupported on Unix
        [Fact]
        public void InvalidArguments_Name_NonExistent()
        {
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(CreateUniqueMapName()));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(CreateUniqueMapName(), MemoryMappedFileRights.ReadWrite));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(CreateUniqueMapName(), MemoryMappedFileRights.ReadWrite, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the OpenExistinginheritability parameter.
        /// </summary>
        [Theory]
        [InlineData((HandleInheritability)(-1))]
        [InlineData((HandleInheritability)42)]
        public void InvalidArguments_Inheritability(HandleInheritability inheritability)
        {
            // Out of range values
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => MemoryMappedFile.OpenExisting(CreateUniqueMapName(), MemoryMappedFileRights.Read, inheritability));
        }

        /// <summary>
        /// Tests invalid arguments to the OpenExistingdesiredAccessRights parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_Rights()
        {
            // Out of range values
            AssertExtensions.Throws<ArgumentOutOfRangeException>("desiredAccessRights", () => MemoryMappedFile.OpenExisting(CreateUniqueMapName(), (MemoryMappedFileRights)0x800000));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("desiredAccessRights", () => MemoryMappedFile.OpenExisting(CreateUniqueMapName(), (MemoryMappedFileRights)0x800000, HandleInheritability.None));
        }

        /// <summary>
        /// Test various combinations of arguments to Open, opening maps created by CreateNew.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)] // Map names unsupported on Unix
        [Theory]
        [MemberData(nameof(MemberData_OpenCreated))]
        public void OpenCreatedNew(string mapName, MemoryMappedFileRights desiredAccessRights, HandleInheritability inheritability)
        {
            const int Capacity = 4096;
            using (MemoryMappedFile original = MemoryMappedFile.CreateNew(mapName, Capacity))
            {
                using (MemoryMappedFile opened = MemoryMappedFile.OpenExisting(mapName))
                {
                    Assert.Throws<IOException>(() => MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.AccessSystemSecurity));
                    Assert.Throws<IOException>(() => MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.AccessSystemSecurity, HandleInheritability.None));
                    ValidateMemoryMappedFile(opened, Capacity);
                }
                using (MemoryMappedFile opened = MemoryMappedFile.OpenExisting(mapName, desiredAccessRights | MemoryMappedFileRights.ReadPermissions)) // can't do anything if we don't have read permissions
                {
                    ValidateMemoryMappedFile(opened, Capacity, RightsToAccess(desiredAccessRights));
                }
                using (MemoryMappedFile opened = MemoryMappedFile.OpenExisting(mapName, desiredAccessRights | MemoryMappedFileRights.ReadPermissions, inheritability))
                {
                    ValidateMemoryMappedFile(opened, Capacity, RightsToAccess(desiredAccessRights), inheritability);
                }
            }

            // The map no longer exists
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(mapName));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite, HandleInheritability.None));
        }

        /// <summary>
        /// Test various combinations of arguments to Open, opening maps created by CreateFromFile.
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows)] // Map names unsupported on Unix
        [Theory]
        [MemberData(nameof(MemberData_OpenCreated))]
        public void OpenCreatedFromFile(string name, MemoryMappedFileRights rights, HandleInheritability inheritability)
        {
            const int Capacity = 4096;
            using (TempFile file = new TempFile(GetTestFilePath()))
            using (FileStream fs = File.Open(file.Path, FileMode.Open))
            using (MemoryMappedFile original = MemoryMappedFile.CreateFromFile(fs, name, Capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
                using (MemoryMappedFile opened = MemoryMappedFile.OpenExisting(name))
                {
                    Assert.Throws<IOException>(() => MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.AccessSystemSecurity));
                    Assert.Throws<IOException>(() => MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.AccessSystemSecurity, HandleInheritability.None));
                    ValidateMemoryMappedFile(opened, Capacity);
                }
                using (MemoryMappedFile opened = MemoryMappedFile.OpenExisting(name, rights | MemoryMappedFileRights.ReadPermissions))
                {
                    ValidateMemoryMappedFile(opened, Capacity, RightsToAccess(rights));
                }
                using (MemoryMappedFile opened = MemoryMappedFile.OpenExisting(name, rights | MemoryMappedFileRights.ReadPermissions, inheritability))
                {
                    ValidateMemoryMappedFile(opened, Capacity, RightsToAccess(rights), inheritability);
                }
            }

            // The map no longer exists
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(name));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.Read));
            Assert.Throws<FileNotFoundException>(() => MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.Read, HandleInheritability.None));
        }

        /// <summary>
        /// Member data from the OpenCreateNew and OpenCreatedFromFile tests.
        /// Yields map names, access rights, and inheritabilities.
        /// </summary>
        public static IEnumerable<object[]> MemberData_OpenCreated
        {
            get
            {
                foreach (object[] mapName in CreateValidMapNames())
                {
                    foreach (MemoryMappedFileRights rights in Enum.GetValues(typeof(MemoryMappedFileRights)))
                    {
                        if (rights == MemoryMappedFileRights.AccessSystemSecurity)
                        {
                            continue;
                        }

                        foreach (HandleInheritability inheritability in Enum.GetValues(typeof(HandleInheritability)))
                        {
                            yield return new object[] { mapName[0], rights, inheritability };
                        }
                    }
                }
            }
        }

        /// <summary>Converts file rights to minimum expected file accesses.</summary>
        private static MemoryMappedFileAccess RightsToAccess(MemoryMappedFileRights rights)
        {
            if ((rights & MemoryMappedFileRights.ReadWriteExecute) == MemoryMappedFileRights.ReadWriteExecute)
                return MemoryMappedFileAccess.ReadWriteExecute;

            if ((rights & MemoryMappedFileRights.ReadWrite) == MemoryMappedFileRights.ReadWrite)
                return MemoryMappedFileAccess.ReadWrite;

            if ((rights & MemoryMappedFileRights.Write) == MemoryMappedFileRights.Write)
                return MemoryMappedFileAccess.Write;

            if ((rights & MemoryMappedFileRights.ReadExecute) == MemoryMappedFileRights.ReadExecute)
                return MemoryMappedFileAccess.ReadExecute;

            if ((rights & MemoryMappedFileRights.Read) == MemoryMappedFileRights.Read)
                return MemoryMappedFileAccess.Read;

            return (MemoryMappedFileAccess)(-1); // no None, so just use -1
        }

    }
}

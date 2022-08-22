// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_TarEntry_ExtractToFile_Tests : TarTestsBase
    {
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.tvOS)] // https://github.com/dotnet/runtime/issues/68360
        [ConditionalFact(nameof(IsUnixButNotSuperUser), nameof(IsNotLinuxBionic))]
        public void SpecialFile_Unelevated_Throws()
        {
            using TempDirectory root = new TempDirectory();
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");

            using (TarReader reader = new TarReader(ms))
            {
                string path = Path.Join(root.Path, "output");

                // Block device requires elevation for writing
                PosixTarEntry blockDevice = reader.GetNextEntry() as PosixTarEntry;
                Assert.NotNull(blockDevice);
                Assert.Throws<UnauthorizedAccessException>(() => blockDevice.ExtractToFile(path, overwrite: false));
                Assert.False(File.Exists(path));

                // Character device requires elevation for writing
                PosixTarEntry characterDevice = reader.GetNextEntry() as PosixTarEntry;
                Assert.NotNull(characterDevice);
                Assert.Throws<UnauthorizedAccessException>(() => characterDevice.ExtractToFile(path, overwrite: false));
                Assert.False(File.Exists(path));

                // Fifo does not require elevation, should succeed
                PosixTarEntry fifo = reader.GetNextEntry() as PosixTarEntry;
                Assert.NotNull(fifo);
                fifo.ExtractToFile(path, overwrite: false);
                Assert.True(File.Exists(path));

                Assert.Null(reader.GetNextEntry());
            }
        }
    }
}

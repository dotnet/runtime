// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_TarEntry_ExtractToFile_Tests : TarTestsBase
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotPrivilegedProcess))]
        [MemberData(nameof(Get_Boolean_Data))]
        [SkipOnPlatform(TestPlatforms.tvOS, "https://github.com/dotnet/runtime/issues/68360")]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Not supported on Bionic")]
        public async Task SpecialFile_Unelevated_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");

            TarReader reader = await CreateTarReader(ms, leaveOpen: false, async: async);
            try
            {
                string path = Path.Join(root.Path, "output");

                PosixTarEntry blockDevice = await GetNextEntry(reader, async: async) as PosixTarEntry;
                Assert.NotNull(blockDevice);
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ExtractToFile(blockDevice, path, overwrite: false, async));
                Assert.False(File.Exists(path));

                PosixTarEntry characterDevice = await GetNextEntry(reader, async: async) as PosixTarEntry;
                Assert.NotNull(characterDevice);
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ExtractToFile(characterDevice, path, overwrite: false, async));
                Assert.False(File.Exists(path));

                PosixTarEntry fifo = await GetNextEntry(reader, async: async) as PosixTarEntry;
                Assert.NotNull(fifo);
                await ExtractToFile(fifo, path, overwrite: false, async);
                Assert.True(File.Exists(path));

                Assert.Null(await GetNextEntry(reader, async: async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }
    }
}

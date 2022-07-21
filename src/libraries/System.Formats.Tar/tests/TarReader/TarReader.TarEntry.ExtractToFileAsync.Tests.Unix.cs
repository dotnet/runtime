// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_TarEntry_ExtractToFileAsync_Tests : TarTestsBase
    {
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.tvOS)] // https://github.com/dotnet/runtime/issues/68360
        [ConditionalFact(nameof(IsUnixButNotSuperUser), nameof(IsNotLinuxBionic))]
        public async Task SpecialFile_Unelevated_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            await using (MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles"))
            await using (TarReader reader = new TarReader(ms))
            {
                string path = Path.Join(root.Path, "output");

                // Block device requires elevation for writing
                PosixTarEntry blockDevice = await reader.GetNextEntryAsync() as PosixTarEntry;
                Assert.NotNull(blockDevice);
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => blockDevice.ExtractToFileAsync(path, overwrite: false));
                Assert.False(File.Exists(path));

                // Character device requires elevation for writing
                PosixTarEntry characterDevice = await reader.GetNextEntryAsync() as PosixTarEntry;
                Assert.NotNull(characterDevice);
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => characterDevice.ExtractToFileAsync(path, overwrite: false));
                Assert.False(File.Exists(path));

                // Fifo does not require elevation, should succeed
                PosixTarEntry fifo = await reader.GetNextEntryAsync() as PosixTarEntry;
                Assert.NotNull(fifo);
                await fifo.ExtractToFileAsync(path, overwrite: false);
                Assert.True(File.Exists(path));

                Assert.Null(await reader.GetNextEntryAsync());
            }
        }
    }
}

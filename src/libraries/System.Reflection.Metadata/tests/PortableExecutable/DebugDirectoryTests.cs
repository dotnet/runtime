// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class DebugDirectoryTests
    {
        [Fact]
        public void NoDebugDirectory()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream))
            {
                var entries = reader.ReadDebugDirectory();
                Assert.Empty(entries);
            }
        }

        [Fact]
        public void CodeView()
        {
            var peStream = new MemoryStream(Misc.Debug);
            using (var reader = new PEReader(peStream))
            {
                // dumpbin:
                //
                // Debug Directories
                // 
                //     Time Type        Size RVA  Pointer
                // -------------- - ------------------------
                // 5670C4E6 cv           11C 0000230C      50C Format: RSDS, { 0C426227-31E6-4EC2-BD5F-712C4D96C0AB}, 1, C:\Temp\Debug.pdb

                var cvEntry = reader.ReadDebugDirectory().Single();
                Assert.Equal(DebugDirectoryEntryType.CodeView, cvEntry.Type);
                Assert.Equal(0x050c, cvEntry.DataPointer);
                Assert.Equal(0x230c, cvEntry.DataRelativeVirtualAddress);
                Assert.Equal(0x011c, cvEntry.DataSize); // includes NUL padding
                Assert.Equal(0, cvEntry.MajorVersion);
                Assert.Equal(0, cvEntry.MinorVersion);
                Assert.Equal(0x5670c4e6u, cvEntry.Stamp);

                var cv = reader.ReadCodeViewDebugDirectoryData(cvEntry);
                Assert.Equal(1, cv.Age);
                Assert.Equal(new Guid("0C426227-31E6-4EC2-BD5F-712C4D96C0AB"), cv.Guid);
                Assert.Equal(@"C:\Temp\Debug.pdb", cv.Path);
            }
        }

        [Fact]
        public void Deterministic()
        {
            var peStream = new MemoryStream(Misc.Deterministic);
            using (var reader = new PEReader(peStream))
            {
                // dumpbin:
                //
                // Debug Directories
                // 
                //       Time Type        Size      RVA  Pointer
                //   -------- ------- -------- -------- --------
                //   D2FC74D3 cv            32 00002338      538    Format: RSDS, {814C578F-7676-0263-4F8A-2D3E8528EAF1}, 1, C:\Temp\Deterministic.pdb
                //   00000000 repro          0 00000000        0

                var entries = reader.ReadDebugDirectory();

                var cvEntry = entries[0];
                Assert.Equal(DebugDirectoryEntryType.CodeView, cvEntry.Type);
                Assert.Equal(0x0538, cvEntry.DataPointer);
                Assert.Equal(0x2338, cvEntry.DataRelativeVirtualAddress);
                Assert.Equal(0x0032, cvEntry.DataSize); // no NUL padding
                Assert.Equal(0, cvEntry.MajorVersion);
                Assert.Equal(0, cvEntry.MinorVersion);
                Assert.Equal(0xD2FC74D3u, cvEntry.Stamp);

                var cv = reader.ReadCodeViewDebugDirectoryData(cvEntry);
                Assert.Equal(1, cv.Age);
                Assert.Equal(new Guid("814C578F-7676-0263-4F8A-2D3E8528EAF1"), cv.Guid);
                Assert.Equal(@"C:\Temp\Deterministic.pdb", cv.Path);

                var detEntry = entries[1];
                Assert.Equal(DebugDirectoryEntryType.Reproducible, detEntry.Type);
                Assert.Equal(0, detEntry.DataPointer);
                Assert.Equal(0, detEntry.DataRelativeVirtualAddress);
                Assert.Equal(0, detEntry.DataSize);
                Assert.Equal(0, detEntry.MajorVersion);
                Assert.Equal(0, detEntry.MinorVersion);
                Assert.Equal(0u, detEntry.Stamp);

                Assert.Equal(2, entries.Length);

                Assert.Throws<ArgumentException>("entry", () => reader.ReadCodeViewDebugDirectoryData(detEntry));
            }
        }
    }
}

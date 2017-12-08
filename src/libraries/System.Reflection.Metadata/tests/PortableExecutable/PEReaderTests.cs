// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEReaderTests
    {
        [Fact]
        public void Ctor()
        {
            Assert.Throws<ArgumentNullException>(() => new PEReader(null, PEStreamOptions.Default));

            var invalid = new MemoryStream(new byte[] { 1, 2, 3, 4 });

            // the stream should not be disposed if the arguments are bad
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEReader(invalid, (PEStreamOptions)int.MaxValue));
            Assert.True(invalid.CanRead);

            // no BadImageFormatException if we're prefetching the entire image:
            var peReader0 = new PEReader(invalid, PEStreamOptions.PrefetchEntireImage | PEStreamOptions.LeaveOpen);
            Assert.True(invalid.CanRead);
            Assert.Throws<BadImageFormatException>(() => peReader0.PEHeaders);
            invalid.Position = 0;

            // BadImageFormatException if we're prefetching the entire image and metadata:
            Assert.Throws<BadImageFormatException>(() => new PEReader(invalid, PEStreamOptions.PrefetchEntireImage | PEStreamOptions.PrefetchMetadata | PEStreamOptions.LeaveOpen));
            Assert.True(invalid.CanRead);
            invalid.Position = 0;

            // the stream should be disposed if the content is bad:
            Assert.Throws<BadImageFormatException>(() => new PEReader(invalid, PEStreamOptions.PrefetchMetadata));
            Assert.False(invalid.CanRead);

            // the stream should not be disposed if we specified LeaveOpen flag:
            invalid = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            Assert.Throws<BadImageFormatException>(() => new PEReader(invalid, PEStreamOptions.PrefetchMetadata | PEStreamOptions.LeaveOpen));
            Assert.True(invalid.CanRead);

            // valid metadata:
            var valid = new MemoryStream(Misc.Members);
            var peReader = new PEReader(valid, PEStreamOptions.Default);
            Assert.True(valid.CanRead);
            peReader.Dispose();
            Assert.False(valid.CanRead);
        }

        [Fact]
        public void Ctor_Streams()
        {
            AssertExtensions.Throws<ArgumentException>("peStream", () => new PEReader(new CustomAccessMemoryStream(canRead: false, canSeek: false, canWrite: false)));
            AssertExtensions.Throws<ArgumentException>("peStream", () => new PEReader(new CustomAccessMemoryStream(canRead: true, canSeek: false, canWrite: false)));

            var s = new CustomAccessMemoryStream(canRead: true, canSeek: true, canWrite: false);

            new PEReader(s);
            new PEReader(s, PEStreamOptions.Default, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEReader(s, PEStreamOptions.Default, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEReader(s, PEStreamOptions.Default, 1));
        }

        [Fact]
        public unsafe void Ctor_Loaded()
        {
            byte b = 1;
            Assert.True(new PEReader(&b, 1, isLoadedImage: true).IsLoadedImage);
            Assert.False(new PEReader(&b, 1, isLoadedImage: false).IsLoadedImage);

            Assert.True(new PEReader(new MemoryStream(), PEStreamOptions.IsLoadedImage).IsLoadedImage);
            Assert.False(new PEReader(new MemoryStream()).IsLoadedImage);
        }

        [Fact]
        public void FromEmptyStream()
        {
            Assert.Throws<BadImageFormatException>(() => new PEReader(new MemoryStream(), PEStreamOptions.PrefetchMetadata));
            Assert.Throws<BadImageFormatException>(() => new PEReader(new MemoryStream(), PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage));
        }

        [Fact(Skip = "https://github.com/dotnet/corefx/issues/7996")]
        [ActiveIssue(7996)]
        public void SubStream()
        {
            var stream = new MemoryStream();
            stream.WriteByte(0xff);
            stream.Write(Misc.Members, 0, Misc.Members.Length);
            stream.WriteByte(0xff);
            stream.WriteByte(0xff);

            stream.Position = 1;
            var peReader1 = new PEReader(stream, PEStreamOptions.LeaveOpen, Misc.Members.Length);

            Assert.Equal(Misc.Members.Length, peReader1.GetEntireImage().Length);
            peReader1.GetMetadataReader();

            stream.Position = 1;
            var peReader2 = new PEReader(stream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata, Misc.Members.Length);

            Assert.Equal(Misc.Members.Length, peReader2.GetEntireImage().Length);
            peReader2.GetMetadataReader();
            stream.Position = 1;

            var peReader3 = new PEReader(stream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchEntireImage, Misc.Members.Length);

            Assert.Equal(Misc.Members.Length, peReader3.GetEntireImage().Length);
            peReader3.GetMetadataReader();
        }

        // TODO: Switch to small checked in native image.
        /*
        [Fact]
        public void OpenNativeImage()
        {
            using (var reader = new PEReader(File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "kernel32.dll"))))
            {
                Assert.False(reader.HasMetadata);
                Assert.True(reader.PEHeaders.IsDll);
                Assert.False(reader.PEHeaders.IsExe);
                Assert.Throws<InvalidOperationException>(() => reader.GetMetadataReader());
            }
        }
        */

        [Fact]
        public void IL_LazyLoad()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                var md = reader.GetMetadataReader();
                var il = reader.GetMethodBody(md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1)).RelativeVirtualAddress);

                Assert.Equal(new byte[] { 0, 42 }, il.GetILBytes());
                Assert.Equal(8, il.MaxStack);
            }
        }

        [Fact]
        public void IL_EagerLoad()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage))
            {
                var md = reader.GetMetadataReader();
                var il = reader.GetMethodBody(md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1)).RelativeVirtualAddress);

                Assert.Equal(new byte[] { 0, 42 }, il.GetILBytes());
                Assert.Equal(8, il.MaxStack);
            }
        }

        [Fact]
        public void Metadata_LazyLoad()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                var md = reader.GetMetadataReader();
                var method = md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));

                Assert.Equal("MC1", md.GetString(method.Name));
            }
        }

        [Fact]
        public void Metadata_EagerLoad()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata))
            {
                var md = reader.GetMetadataReader();
                var method = md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
                Assert.Equal("MC1", md.GetString(method.Name));

                Assert.Throws<InvalidOperationException>(() => reader.GetEntireImage());
                Assert.Throws<InvalidOperationException>(() => reader.GetMethodBody(method.RelativeVirtualAddress));
            }
        }

        [Fact]
        public void EntireImage_LazyLoad()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                Assert.Equal(4608, reader.GetEntireImage().Length);
            }
        }

        [Fact]
        public void EntireImage_EagerLoad()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage))
            {
                Assert.Equal(4608, reader.GetEntireImage().Length);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get module handles
        public void GetMethodBody_Loaded()
        {
            LoaderUtilities.LoadPEAndValidate(Misc.Members, reader =>
            {
                var md = reader.GetMetadataReader();
                var il = reader.GetMethodBody(md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1)).RelativeVirtualAddress);

                Assert.Equal(new byte[] { 0, 42 }, il.GetILBytes());
                Assert.Equal(8, il.MaxStack);
            });
        }

        [Fact]
        public void GetSectionData()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream))
            {
                ValidateSectionData(reader);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get module handles
        public void GetSectionData_Loaded()
        {
            LoaderUtilities.LoadPEAndValidate(Misc.Members, ValidateSectionData);
        }

        private unsafe void ValidateSectionData(PEReader reader)
        {
            var relocBlob1 = reader.GetSectionData(".reloc").GetContent();
            var relocBlob2 = reader.GetSectionData(0x6000).GetContent();

            AssertEx.Equal(new byte[] 
            {
                0x00, 0x20, 0x00, 0x00,
                0x0C, 0x00, 0x00, 0x00,
                0xD0, 0x38, 0x00, 0x00
            }, relocBlob1);

            AssertEx.Equal(relocBlob1, relocBlob2);

            var data = reader.GetSectionData(0x5fff);
            Assert.True(data.Pointer == null);
            Assert.Equal(0, data.Length);
            AssertEx.Equal(new byte[0], data.GetContent());

            data = reader.GetSectionData(0x600B);
            Assert.True(data.Pointer != null);
            Assert.Equal(1, data.Length);
            AssertEx.Equal(new byte[] { 0x00 }, data.GetContent());

            data = reader.GetSectionData(0x600C);
            Assert.True(data.Pointer == null);
            Assert.Equal(0, data.Length);
            AssertEx.Equal(new byte[0], data.GetContent());

            data = reader.GetSectionData(0x600D);
            Assert.True(data.Pointer == null);
            Assert.Equal(0, data.Length);
            AssertEx.Equal(new byte[0], data.GetContent());

            data = reader.GetSectionData(int.MaxValue);
            Assert.True(data.Pointer == null);
            Assert.Equal(0, data.Length);
            AssertEx.Equal(new byte[0], data.GetContent());

            data = reader.GetSectionData(".nonexisting");
            Assert.True(data.Pointer == null);
            Assert.Equal(0, data.Length);
            AssertEx.Equal(new byte[0], data.GetContent());

            data = reader.GetSectionData("");
            Assert.True(data.Pointer == null);
            Assert.Equal(0, data.Length);
            AssertEx.Equal(new byte[0], data.GetContent());
        }

        [Fact]
        public void GetSectionData_Errors()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream))
            {
                Assert.Throws<ArgumentNullException>(() => reader.GetSectionData(null));
                Assert.Throws<ArgumentOutOfRangeException>(() => reader.GetSectionData(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => reader.GetSectionData(int.MinValue));
            }
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void TryOpenAssociatedPortablePdb_Args()
        {
            var peStream = new MemoryStream(PortablePdbs.DocumentsDll);
            using (var reader = new PEReader(peStream))
            {
                MetadataReaderProvider pdbProvider;
                string pdbPath;

                Assert.False(reader.TryOpenAssociatedPortablePdb(@"b.dll", _ => null, out pdbProvider, out pdbPath));
                Assert.Throws<ArgumentNullException>(() => reader.TryOpenAssociatedPortablePdb(@"b.dll", null, out pdbProvider, out pdbPath));
                Assert.Throws<ArgumentNullException>(() => reader.TryOpenAssociatedPortablePdb(null, _ => null, out pdbProvider, out pdbPath));
                AssertExtensions.Throws<ArgumentException>("peImagePath", () => reader.TryOpenAssociatedPortablePdb("C:\\a\\\0\\b", _ => null, out pdbProvider, out pdbPath));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void TryOpenAssociatedPortablePdb_Args_Core()
        {
            var peStream = new MemoryStream(PortablePdbs.DocumentsDll);
            using (var reader = new PEReader(peStream))
            {
                MetadataReaderProvider pdbProvider;
                string pdbPath;

                Assert.False(reader.TryOpenAssociatedPortablePdb(@"b.dll", _ => null, out pdbProvider, out pdbPath));
                Assert.Throws<ArgumentNullException>(() => reader.TryOpenAssociatedPortablePdb(@"b.dll", null, out pdbProvider, out pdbPath));
                Assert.Throws<ArgumentNullException>(() => reader.TryOpenAssociatedPortablePdb(null, _ => null, out pdbProvider, out pdbPath));
                Assert.False(reader.TryOpenAssociatedPortablePdb("C:\\a\\\0\\b", _ => null, out pdbProvider, out pdbPath));
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_CollocatedFile()
        {
            var peStream = new MemoryStream(PortablePdbs.DocumentsDll);
            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    return new MemoryStream(PortablePdbs.DocumentsPdb);
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;

                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                Assert.Equal(Path.Combine("pedir", "Documents.pdb"), pathQueried);

                Assert.Equal(Path.Combine("pedir", "Documents.pdb"), pdbPath);
                var pdbReader = pdbProvider.GetMetadataReader();
                Assert.Equal(13, pdbReader.Documents.Count);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_Embedded()
        {
            var peStream = new MemoryStream(PortablePdbs.DocumentsEmbeddedDll);
            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    return null;
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;

                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                Assert.Equal(Path.Combine("pedir", "Documents.Embedded.pdb"), pathQueried);

                Assert.Null(pdbPath);
                var pdbReader = pdbProvider.GetMetadataReader();
                Assert.Equal(13, pdbReader.Documents.Count);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_EmbeddedOnly()
        {
            var peStream = new MemoryStream(PortablePdbs.DocumentsEmbeddedDll);
            using (var reader = new PEReader(peStream))
            {
                MetadataReaderProvider pdbProvider;
                string pdbPath;

                Assert.True(reader.TryOpenAssociatedPortablePdb(@"x", _ => null, out pdbProvider, out pdbPath));

                Assert.Null(pdbPath);
                var pdbReader = pdbProvider.GetMetadataReader();
                Assert.Equal(13, pdbReader.Documents.Count);
            }
        }

        [Fact]
        public unsafe void TryOpenAssociatedPortablePdb_EmbeddedUnused()
        {
            var peStream = new MemoryStream(PortablePdbs.DocumentsEmbeddedDll);
            using (var reader = new PEReader(peStream))
            {
                using (MetadataReaderProvider embeddedProvider = reader.ReadEmbeddedPortablePdbDebugDirectoryData(reader.ReadDebugDirectory()[2]))
                {
                    var embeddedReader = embeddedProvider.GetMetadataReader();
                    var embeddedBytes = new BlobReader(embeddedReader.MetadataPointer, embeddedReader.MetadataLength).ReadBytes(embeddedReader.MetadataLength);

                    string pathQueried = null;

                    Func<string, Stream> streamProvider = p =>
                    {
                        Assert.Null(pathQueried);
                        pathQueried = p;
                        return new MemoryStream(embeddedBytes);
                    };

                    MetadataReaderProvider pdbProvider;
                    string pdbPath;

                    Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                    Assert.Equal(Path.Combine("pedir", "Documents.Embedded.pdb"), pathQueried);

                    Assert.Equal(Path.Combine("pedir", "Documents.Embedded.pdb"), pdbPath);
                    var pdbReader = pdbProvider.GetMetadataReader();
                    Assert.Equal(13, pdbReader.Documents.Count);
                }
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_UnixStylePath()
        {
            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/abc/def.xyz", id, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    return null;
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;
                Assert.False(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                Assert.Equal(Path.Combine("pedir", "def.xyz"), pathQueried);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_WindowsSpecificPath()
        {
            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"C:def.xyz", id, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    return null;
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;
                Assert.False(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                Assert.Equal(Path.Combine("pedir", "def.xyz"), pathQueried);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_WindowsInvalidCharacters()
        {
            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/*/c*.pdb", id, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    return null;
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;
                Assert.False(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                Assert.Equal(PathUtilities.CombinePathWithRelativePath("pedir", "c*.pdb"), pathQueried);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_DuplicateEntries_CodeView()
        {
            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0);
            ddBuilder.AddReproducibleEntry();
            ddBuilder.AddCodeViewEntry(@"/a/b/c.pdb", id, portablePdbVersion: 0x0100, age: 0x1234);
            ddBuilder.AddCodeViewEntry(@"/a/b/d.pdb", id, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    return null;
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;
                Assert.False(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                Assert.Equal(PathUtilities.CombinePathWithRelativePath("pedir", "c.pdb"), pathQueried);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_DuplicateEntries_Embedded()
        {
            var pdbBuilder1 = new BlobBuilder();
            pdbBuilder1.WriteBytes(PortablePdbs.DocumentsPdb);

            var pdbBuilder2 = new BlobBuilder();
            pdbBuilder2.WriteByte(1);

            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0x0100);
            ddBuilder.AddReproducibleEntry();
            ddBuilder.AddEmbeddedPortablePdbEntry(pdbBuilder1, portablePdbVersion: 0x0100);
            ddBuilder.AddEmbeddedPortablePdbEntry(pdbBuilder2, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    return null;
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;
                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));
                Assert.Equal(PathUtilities.CombinePathWithRelativePath("pedir", "a.pdb"), pathQueried);
                Assert.Null(pdbPath);

                Assert.Equal(13, pdbProvider.GetMetadataReader().Documents.Count);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_CodeViewVsEmbedded_NonMatchingPdbId()
        {
            var pdbBuilder = new BlobBuilder();
            pdbBuilder.WriteBytes(PortablePdbs.DocumentsPdb);

            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0x0100);
            ddBuilder.AddEmbeddedPortablePdbEntry(pdbBuilder, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;
                    
                    // Doesn't match the id
                    return new MemoryStream(PortablePdbs.DocumentsPdb);
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;
                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));

                Assert.Null(pdbPath);
                Assert.Equal(PathUtilities.CombinePathWithRelativePath("pedir", "a.pdb"), pathQueried);

                Assert.Equal(13, pdbProvider.GetMetadataReader().Documents.Count);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_BadPdbFile_FallbackToEmbedded()
        {
            var pdbBuilder = new BlobBuilder();
            pdbBuilder.WriteBytes(PortablePdbs.DocumentsPdb);

            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0x0100);
            ddBuilder.AddEmbeddedPortablePdbEntry(pdbBuilder, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                string pathQueried = null;

                Func<string, Stream> streamProvider = p =>
                {
                    Assert.Null(pathQueried);
                    pathQueried = p;

                    // Bad PDB
                    return new MemoryStream(new byte[] { 0x01 });
                };

                MetadataReaderProvider pdbProvider;
                string pdbPath;
                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), streamProvider, out pdbProvider, out pdbPath));

                Assert.Null(pdbPath);
                Assert.Equal(PathUtilities.CombinePathWithRelativePath("pedir", "a.pdb"), pathQueried);

                Assert.Equal(13, pdbProvider.GetMetadataReader().Documents.Count);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_ExpectedExceptionFromStreamProvider_FallbackOnEmbedded_Valid()
        {
            var pdbBuilder = new BlobBuilder();
            pdbBuilder.WriteBytes(PortablePdbs.DocumentsPdb);

            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0x0100);
            ddBuilder.AddEmbeddedPortablePdbEntry(pdbBuilder, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                MetadataReaderProvider pdbProvider;
                string pdbPath;

                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new IOException(); }, out pdbProvider, out pdbPath));
                Assert.Null(pdbPath);
                Assert.Equal(13, pdbProvider.GetMetadataReader().Documents.Count);

                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new BadImageFormatException(); }, out pdbProvider, out pdbPath));
                Assert.Null(pdbPath);
                Assert.Equal(13, pdbProvider.GetMetadataReader().Documents.Count);

                Assert.True(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new FileNotFoundException(); }, out pdbProvider, out pdbPath));
                Assert.Null(pdbPath);
                Assert.Equal(13, pdbProvider.GetMetadataReader().Documents.Count);
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_ExpectedExceptionFromStreamProvider_FallbackOnEmbedded_Invalid()
        {
            var pdbBuilder = new BlobBuilder();
            pdbBuilder.WriteBytes(new byte[] { 0x01 });

            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0x0100);
            ddBuilder.AddEmbeddedPortablePdbEntry(pdbBuilder, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                MetadataReaderProvider pdbProvider;
                string pdbPath;

                // reports the first error:
                Assert.Throws<IOException>(() => 
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new IOException(); }, out pdbProvider, out pdbPath));

                // reports the first error:
                AssertEx.Throws<BadImageFormatException>(() =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new BadImageFormatException("Bang!"); }, out pdbProvider, out pdbPath),
                    e => Assert.Equal("Bang!", e.Message));

                // file doesn't exist, fall back to embedded without reporting FileNotFoundExeception
                Assert.Throws<BadImageFormatException>(() =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new FileNotFoundException(); }, out pdbProvider, out pdbPath));

                Assert.Throws<BadImageFormatException>(() =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => null, out pdbProvider, out pdbPath));
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_ExpectedExceptionFromStreamProvider_NoFallback()
        {
            var pdbBuilder = new BlobBuilder();
            pdbBuilder.WriteBytes(PortablePdbs.DocumentsPdb);

            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                MetadataReaderProvider pdbProvider;
                string pdbPath;

                Assert.Throws<IOException>(() =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new IOException(); }, out pdbProvider, out pdbPath));

                AssertEx.Throws<BadImageFormatException>(() =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new BadImageFormatException("Bang!"); }, out pdbProvider, out pdbPath),
                    e => Assert.Equal("Bang!", e.Message));

                // file doesn't exist and no embedded => return false
                Assert.False(reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new FileNotFoundException(); }, out pdbProvider, out pdbPath));
            }
        }

        [Fact]
        public void TryOpenAssociatedPortablePdb_BadStreamProvider()
        {
            var pdbBuilder = new BlobBuilder();
            pdbBuilder.WriteBytes(PortablePdbs.DocumentsPdb);

            var id = new BlobContentId(Guid.Parse("18091B06-32BB-46C2-9C3B-7C9389A2F6C6"), 0x12345678);
            var ddBuilder = new DebugDirectoryBuilder();
            ddBuilder.AddCodeViewEntry(@"/a/b/a.pdb", id, portablePdbVersion: 0x0100);

            var peStream = new MemoryStream(TestBuilders.BuildPEWithDebugDirectory(ddBuilder));

            using (var reader = new PEReader(peStream))
            {
                MetadataReaderProvider pdbProvider;
                string pdbPath;

                // pass-thru:
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { throw new ArgumentException(); }, out pdbProvider, out pdbPath));

                Assert.Throws<InvalidOperationException>(() =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { return new TestStream(canRead: false, canWrite: true, canSeek: true); }, out pdbProvider, out pdbPath));

                Assert.Throws<InvalidOperationException>(() =>
                    reader.TryOpenAssociatedPortablePdb(Path.Combine("pedir", "file.exe"), _ => { return new TestStream(canRead: true, canWrite: true, canSeek: false); }, out pdbProvider, out pdbPath));
            }
        }

        [Fact]
        public void Dispose()
        {
            var peStream = new MemoryStream(PortablePdbs.DocumentsEmbeddedDll);
            var reader = new PEReader(peStream);

            MetadataReaderProvider pdbProvider;
            string pdbPath;

            Assert.True(reader.TryOpenAssociatedPortablePdb(@"x", _ => null, out pdbProvider, out pdbPath));
            Assert.NotNull(pdbProvider);
            Assert.Null(pdbPath);

            var ddEntries = reader.ReadDebugDirectory();
            var ddCodeView = ddEntries[0];
            var ddEmbedded = ddEntries[2];

            var embeddedPdbProvider = reader.ReadEmbeddedPortablePdbDebugDirectoryData(ddEmbedded);

            // dispose the PEReader:
            reader.Dispose();

            Assert.False(reader.IsEntireImageAvailable);

            Assert.Throws<ObjectDisposedException>(() => reader.PEHeaders);
            Assert.Throws<ObjectDisposedException>(() => reader.HasMetadata);
            Assert.Throws<ObjectDisposedException>(() => reader.GetMetadata());
            Assert.Throws<ObjectDisposedException>(() => reader.GetSectionData(1000));
            Assert.Throws<ObjectDisposedException>(() => reader.GetMetadataReader());
            Assert.Throws<ObjectDisposedException>(() => reader.GetMethodBody(0));
            Assert.Throws<ObjectDisposedException>(() => reader.GetEntireImage());
            Assert.Throws<ObjectDisposedException>(() => reader.ReadDebugDirectory());
            Assert.Throws<ObjectDisposedException>(() => reader.ReadCodeViewDebugDirectoryData(ddCodeView));
            Assert.Throws<ObjectDisposedException>(() => reader.ReadEmbeddedPortablePdbDebugDirectoryData(ddEmbedded));

            MetadataReaderProvider __;
            string ___;
            Assert.Throws<ObjectDisposedException>(() => reader.TryOpenAssociatedPortablePdb(@"x", _ => null, out __, out ___));

            // ok to use providers after PEReader disposed:
            var pdbReader = pdbProvider.GetMetadataReader();
            Assert.Equal(13, pdbReader.Documents.Count);

            pdbReader = embeddedPdbProvider.GetMetadataReader();
            Assert.Equal(13, pdbReader.Documents.Count);

            embeddedPdbProvider.Dispose();
        }
    }
}

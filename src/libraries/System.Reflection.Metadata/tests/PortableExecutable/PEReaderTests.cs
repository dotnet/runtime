// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using TestUtilities;
using Xunit;

namespace System.Reflection.Metadata.Tests
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
            var valid = new MemoryStream(TestResources.Misc.Members);
            var peReader = new PEReader(valid, PEStreamOptions.Default);
            Assert.True(valid.CanRead);
            peReader.Dispose();
            Assert.False(valid.CanRead);
        }

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

        [Fact]
        public void IL_LazyLoad()
        {
            var peStream = new MemoryStream(TestResources.Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                var md = reader.GetMetadataReader();
                var il = reader.GetMethodBody(md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1)).RelativeVirtualAddress);

                AssertEx.Equal(new byte[] { 0, 42 }, il.GetILBytes());
                Assert.Equal(8, il.MaxStack);
            }
        }

        [Fact]
        public void IL_EagerLoad()
        {
            var peStream = new MemoryStream(TestResources.Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage))
            {
                var md = reader.GetMetadataReader();
                var il = reader.GetMethodBody(md.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1)).RelativeVirtualAddress);

                AssertEx.Equal(new byte[] { 0, 42 }, il.GetILBytes());
                Assert.Equal(8, il.MaxStack);
            }
        }

        [Fact]
        public void Metadata_LazyLoad()
        {
            var peStream = new MemoryStream(TestResources.Misc.Members);
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
            var peStream = new MemoryStream(TestResources.Misc.Members);
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
            var peStream = new MemoryStream(TestResources.Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                Assert.Equal(4608, reader.GetEntireImage().Length);
            }
        }

        [Fact]
        public void EntireImage_EagerLoad()
        {
            var peStream = new MemoryStream(TestResources.Misc.Members);
            using (var reader = new PEReader(peStream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage))
            {
                Assert.Equal(4608, reader.GetEntireImage().Length);
            }
        }
    }
}

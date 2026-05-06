// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class MetadataRootBuilderTests
    {
        private string ReadVersion(BlobBuilder metadata)
        {
            using (var provider = MetadataReaderProvider.FromMetadataImage(metadata.ToImmutableArray()))
            {
                return provider.GetMetadataReader().MetadataVersion;
            }
        }

        [Fact]
        public void Ctor_Errors()
        {
            var mdBuilder = new MetadataBuilder();

            Assert.Throws<ArgumentNullException>(() => new MetadataRootBuilder(null));
            AssertExtensions.Throws<ArgumentException>("metadataVersion", () => new MetadataRootBuilder(mdBuilder, new string('x', 255)));
        }

        [Fact]
        public void Serialize_Errors()
        {
            var mdBuilder = new MetadataBuilder();
            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var builder = new BlobBuilder();

            Assert.Throws<ArgumentNullException>(() => rootBuilder.Serialize(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rootBuilder.Serialize(builder, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rootBuilder.Serialize(builder, 0, -1));
        }

        [Fact]
        public void Headers()
        {
            var mdBuilder = new MetadataBuilder();
            var rootBuilder = new MetadataRootBuilder(mdBuilder);

            var builder = new BlobBuilder();
            rootBuilder.Serialize(builder, 0, 0);

            AssertEx.Equal(new byte[]
            {
                // signature:
                0x42, 0x53, 0x4A, 0x42,
                // major version (1)
                0x01, 0x00,
                // minor version (1)
                0x01, 0x00,
                // reserved (0)
                0x00, 0x00, 0x00, 0x00,

                // padded version length:
                0x0C, 0x00, 0x00, 0x00,

                // padded version:
                (byte)'v', (byte)'4', (byte)'.', (byte)'0', (byte)'.', (byte)'3', (byte)'0', (byte)'3', (byte)'1', (byte)'9', 0x00, 0x00,

                // flags (0):
                0x00, 0x00,

                // stream count:
                0x05, 0x00,

                // stream headers:
                0x6C, 0x00, 0x00, 0x00,
                0x1C, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'~', 0x00, 0x00,

                0x88, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'S', (byte)'t', (byte)'r', (byte)'i', (byte)'n', (byte)'g', (byte)'s', 0x00, 0x00, 0x00, 0x00,

                0x8C, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'U', (byte)'S', 0x00,

                0x90, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'G', (byte)'U', (byte)'I', (byte)'D', 0x00, 0x00, 0x00,

                0x90, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'B', (byte)'l', (byte)'o', (byte)'b', 0x00, 0x00, 0x00,

                // --------
                // #~
                // --------

                // Reserved (0)
                0x00, 0x00, 0x00, 0x00,

                // Major Version (2)
                0x02,

                // Minor Version (0)
                0x00,

                // Heap Sizes
                0x00,

                // Reserved (1)
                0x01,

                // Present tables
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // Sorted tables
                0x00, 0xFA, 0x01, 0x33, 0x00, 0x16, 0x00, 0x00,

                // Rows (empty)
                // Tables (empty)

                // Padding and alignment
                0x00, 0x00, 0x00, 0x00,

                // --------
                // #Strings
                // --------

                0x00, 0x00, 0x00, 0x00,

                // --------
                // #US
                // --------
                0x00, 0x00, 0x00, 0x00,

                // --------
                // #GUID
                // --------

                // --------
                // #Blob
                // --------

                0x00, 0x00, 0x00, 0x00,
            }, builder.ToArray());
        }

        [Fact]
        public void EncHeaders()
        {
            var mdBuilder = new MetadataBuilder();
            mdBuilder.AddEncLogEntry(MetadataTokens.MethodDefinitionHandle(1), EditAndContinueOperation.AddMethod);
            mdBuilder.AddCustomAttribute(MetadataTokens.MethodDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1), default);
            mdBuilder.AddCustomAttribute(MetadataTokens.MethodDefinitionHandle(0), MetadataTokens.MethodDefinitionHandle(1), default);

            var rootBuilder = new MetadataRootBuilder(mdBuilder);

            var builder = new BlobBuilder();
            rootBuilder.Serialize(builder, 0, 0);

            AssertEx.Equal(new byte[]
            {
                // signature:
                0x42, 0x53, 0x4A, 0x42,
                // major version (1)
                0x01, 0x00,
                // minor version (1)
                0x01, 0x00,
                // reserved (0)
                0x00, 0x00, 0x00, 0x00,

                // padded version length:
                0x0C, 0x00, 0x00, 0x00,

                // padded version:
                (byte)'v', (byte)'4', (byte)'.', (byte)'0', (byte)'.', (byte)'3', (byte)'0', (byte)'3', (byte)'1', (byte)'9', 0x00, 0x00,

                // flags (0):
                0x00, 0x00,

                // stream count:
                0x06, 0x00,

                // stream headers:
                0x7C, 0x00, 0x00, 0x00,
                0x44, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'-', 0x00, 0x00,

                0xC0, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'S', (byte)'t', (byte)'r', (byte)'i', (byte)'n', (byte)'g', (byte)'s', 0x00, 0x00, 0x00, 0x00,

                0xC4, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'U', (byte)'S', 0x00,

                0xC8, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'G', (byte)'U', (byte)'I', (byte)'D', 0x00, 0x00, 0x00,

                0xC8, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'B', (byte)'l', (byte)'o', (byte)'b', 0x00, 0x00, 0x00,

                0xCC, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'J', (byte)'T', (byte)'D', 0x00, 0x00, 0x00, 0x00,

                // --------
                // #-
                // --------

                // Reserved (0)
                0x00, 0x00, 0x00, 0x00,

                // Major Version (2)
                0x02,

                // Minor Version (0)
                0x00,

                // Heap Sizes
                0xA7,

                // Reserved (1)
                0x01,

                // Present tables
                0x00, 0x10, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00,

                // Sorted tables
                0x00, 0xEA, 0x01, 0x33, 0x00, 0x16, 0x00, 0x00,

                // Rows
                0x02, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00,

                //
                // Custom Attribute table (token, token, token)
                //

                0x20, 0x00, 0x00, 0x00,     0x0A, 0x00, 0x00, 0x00,     0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,     0x0A, 0x00, 0x00, 0x00,     0x00, 0x00, 0x00, 0x00,

                //
                // EncLog Table (token, operation)
                //

                0x01, 0x00, 0x00, 0x06,     0x01, 0x00, 0x00, 0x00,

                // Padding and alignment
                0x00, 0x00, 0x00, 0x00,

                // --------
                // #Strings
                // --------

                0x00, 0x00, 0x00, 0x00,

                // --------
                // #US
                // --------
                0x00, 0x00, 0x00, 0x00,

                // --------
                // #GUID
                // --------

                // --------
                // #Blob
                // --------

                0x00, 0x00, 0x00, 0x00,

                // --------
                // #JTD
                // --------
            }, builder.ToArray());
        }

        [Fact]
        public void MetadataVersion_Default()
        {
            var mdBuilder = new MetadataBuilder();
            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);

            var builder = new BlobBuilder();
            rootBuilder.Serialize(builder, 0, 0);

            AssertEx.Equal(new byte[]
            {
                // padded version length:
                0x0C, 0x00, 0x00, 0x00,

                // padded version:
                (byte)'v', (byte)'4', (byte)'.', (byte)'0', (byte)'.', (byte)'3', (byte)'0', (byte)'3', (byte)'1', (byte)'9', 0x00, 0x00,
            }, builder.Slice(12, -132));

            Assert.Equal(rootBuilder.MetadataVersion, ReadVersion(builder));
        }

        [Fact]
        public void MetadataVersion_Empty()
        {
            var version = "";

            var mdBuilder = new MetadataBuilder();
            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder, version);

            var builder = new BlobBuilder();
            rootBuilder.Serialize(builder, 0, 0);

            AssertEx.Equal(new byte[]
            {
                // padded version length:
                0x04, 0x00, 0x00, 0x00,

                // padded version:
                0x00, 0x00, 0x00, 0x00,
            }, builder.Slice(12, -132));

            Assert.Equal(version, ReadVersion(builder));
        }

        [Fact]
        public void MetadataVersion_MaxLength()
        {
            var version = new string('x', 254);

            var mdBuilder = new MetadataBuilder();
            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder, version);

            var builder = new BlobBuilder();
            rootBuilder.Serialize(builder, 0, 0);

            AssertEx.Equal(new byte[]
            {
                // padded version length:
                0x00, 0x01, 0x00, 0x00,

                // padded version:
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78,
                0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x78, 0x00, 0x00
            }, builder.Slice(12, -132));

            Assert.Equal(version, ReadVersion(builder));
        }

        [Fact]
        public void MetadataVersion()
        {
            var version = "\u1234\ud800";

            var mdBuilder = new MetadataBuilder();
            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder, version);

            var builder = new BlobBuilder();
            rootBuilder.Serialize(builder, 0, 0);

            AssertEx.Equal(new byte[]
            {
                // padded version length:
                0x08, 0x00, 0x00, 0x00,

                // padded version:
                // [ E1 88 B4 ] -> U+1234
                // [ ED ] -> invalid (ED cannot be followed by A0) -> U+FFFD
                // [ A0 ] -> invalid (not ASCII, not valid leading byte) -> U+FFFD
                // [ 80 ] -> invalid (not ASCII, not valid leading byte) -> U+FFFD
                0xE1, 0x88, 0xB4, 0xED, 0xA0, 0x80, 0x00, 0x00,
            }, builder.Slice(12, -132));

            // the default decoder replaces bad byte sequences by U+FFFD
            if (PlatformDetection.IsNetCore)
            {
                Assert.Equal("\u1234\ufffd\ufffd\ufffd", ReadVersion(builder));
            }
            else
            {
                // Versions of .NET prior to Core 3.0 didn't follow Unicode recommendations for U+FFFD substitution,
                // so they sometimes emitted too few replacement chars.
                Assert.Equal("\u1234\ufffd\ufffd", ReadVersion(builder));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class PortablePdbBuilderTests
    {
        [Fact]
        public void Ctor_Errors()
        {
            var mdBuilder = new MetadataBuilder();

            Assert.Throws<ArgumentNullException>(() => new PortablePdbBuilder(null, MetadataRootBuilder.EmptyRowCounts, default(MethodDefinitionHandle)));
            Assert.Throws<ArgumentNullException>(() => new PortablePdbBuilder(mdBuilder, default(ImmutableArray<int>), default(MethodDefinitionHandle)));

            var rowCounts = new int[128];
            rowCounts[64] = 1;
            AssertExtensions.Throws<ArgumentException>("typeSystemRowCounts", () => new PortablePdbBuilder(mdBuilder, ImmutableArray.Create(rowCounts), default(MethodDefinitionHandle)));

            rowCounts = new int[64];
            rowCounts[63] = 1;
            AssertExtensions.Throws<ArgumentException>("typeSystemRowCounts", () => new PortablePdbBuilder(mdBuilder, ImmutableArray.Create(rowCounts), default(MethodDefinitionHandle)));

            rowCounts = new int[64];
            rowCounts[(int)TableIndex.EventPtr] = 1;
            AssertExtensions.Throws<ArgumentException>("typeSystemRowCounts", () => new PortablePdbBuilder(mdBuilder, ImmutableArray.Create(rowCounts), default(MethodDefinitionHandle)));

            rowCounts = new int[64];
            rowCounts[(int)TableIndex.CustomDebugInformation] = 1;
            AssertExtensions.Throws<ArgumentException>("typeSystemRowCounts", () => new PortablePdbBuilder(mdBuilder, ImmutableArray.Create(rowCounts), default(MethodDefinitionHandle)));

            rowCounts = new int[64];
            rowCounts[(int)TableIndex.MethodDef] = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => new PortablePdbBuilder(mdBuilder, ImmutableArray.Create(rowCounts), default(MethodDefinitionHandle)));

            rowCounts = new int[64];
            rowCounts[(int)TableIndex.GenericParamConstraint] = 0x01000000;
            Assert.Throws<ArgumentOutOfRangeException>(() => new PortablePdbBuilder(mdBuilder, ImmutableArray.Create(rowCounts), default(MethodDefinitionHandle)));
        }

        [Fact]
        public void Serialize_Errors()
        {
            var mdBuilder = new MetadataBuilder();
            var pdbBuilder = new PortablePdbBuilder(mdBuilder, MetadataRootBuilder.EmptyRowCounts, default(MethodDefinitionHandle));
            var builder = new BlobBuilder();

            Assert.Throws<ArgumentNullException>(() => pdbBuilder.Serialize(null));
        }

        [Fact]
        public void Headers()
        {
            var mdBuilder = new MetadataBuilder();
            var pdbBuilder = new PortablePdbBuilder(
                mdBuilder,
                MetadataRootBuilder.EmptyRowCounts,
                MetadataTokens.MethodDefinitionHandle(0x123456),
                _ => new BlobContentId(new Guid("44332211-6655-8877-AA99-010203040506"), 0xFFEEDDCC));

            var builder = new BlobBuilder();
            pdbBuilder.Serialize(builder);

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
                (byte)'P', (byte)'D', (byte)'B', (byte)' ', (byte)'v', (byte)'1', (byte)'.', (byte)'0', 0x00, 0x00, 0x00, 0x00,
                
                // flags (0):
                0x00, 0x00,

                // stream count:
                0x06, 0x00,

                // stream headers (offset, size, padded name)
                0x7C, 0x00, 0x00, 0x00,
                0x20, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'P', (byte)'d', (byte)'b', 0x00, 0x00, 0x00, 0x00,

                0x9C, 0x00, 0x00, 0x00,
                0x1C, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'~', 0x00, 0x00,

                0xB8, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'S', (byte)'t', (byte)'r', (byte)'i', (byte)'n', (byte)'g', (byte)'s', 0x00, 0x00, 0x00, 0x00,

                0xBC, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'U', (byte)'S', 0x00,

                0xC0, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'G', (byte)'U', (byte)'I', (byte)'D', 0x00, 0x00, 0x00,

                0xC0, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00,
                (byte)'#', (byte)'B', (byte)'l', (byte)'o', (byte)'b', 0x00, 0x00, 0x00,

                // --------
                // #Pdb
                // --------

                // PDB ID
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0xAA, 0x99, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
                0xCC, 0xDD, 0xEE, 0xFF,

                // EntryPoint
                0x56, 0x34, 0x12, 0x06,

                // ReferencedTypeSystemTables
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // TypeSystemTableRows (empty)

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
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

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
        public void PdbStream_TypeSystemRowCounts()
        {
            var rowCounts = new int[MetadataTokens.TableCount];
            rowCounts[(int)TableIndex.MethodDef] = 0xFFFFFF;
            rowCounts[(int)TableIndex.TypeDef] = 0x123456;

            var mdBuilder = new MetadataBuilder();
            var pdbBuilder = new PortablePdbBuilder(
                mdBuilder,
                ImmutableArray.Create(rowCounts),
                MetadataTokens.MethodDefinitionHandle(0x123456),
                _ => new BlobContentId(new Guid("44332211-6655-8877-AA99-010203040506"), 0xFFEEDDCC));

            var builder = new BlobBuilder();
            pdbBuilder.Serialize(builder);

            AssertEx.Equal(new byte[]
            {
                // PDB ID
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0xAA, 0x99, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
                0xCC, 0xDD, 0xEE, 0xFF,

                // EntryPoint
                0x56, 0x34, 0x12, 0x06,

                // ReferencedTypeSystemTables
                0x44, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // TypeSystemTableRows
                0x56, 0x34, 0x12, 0x00,
                0xFF, 0xFF, 0xFF, 0x00

            }, builder.Slice(124, -40));
        }
    }
}

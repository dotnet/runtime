// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class PortablePdbBuilderTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(65534, true)]
        [InlineData(65535, true)]
        [InlineData(65536, false)]
        [InlineData(0x1fffffff, false)]
        public void SequencePoints_InitialColumn(int startColumn, bool valid)
        {
            var points = new BlobBuilder();
            points.WriteCompressedInteger(0); // local signature
            points.WriteCompressedInteger(0); // IL offset
            points.WriteCompressedInteger(1); // delta lines
            points.WriteCompressedSignedInteger(0); // delta columns
            points.WriteCompressedInteger(1); // start line
            points.WriteCompressedInteger(startColumn);

            using var provider = CreateSequencePointsPdb(points);
            var enumerator = provider.GetMetadataReader().GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(1)).GetSequencePoints().GetEnumerator();
            if (valid)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(startColumn, enumerator.Current.StartColumn);
                Assert.Equal(startColumn, enumerator.Current.EndColumn);
                Assert.False(enumerator.MoveNext());
            }
            else
            {
                Assert.Throws<BadImageFormatException>(() => enumerator.MoveNext());
            }
        }

        [Theory]
        [InlineData(0, 1, 65533, true)]
        [InlineData(0, 1, 65534, true)]
        [InlineData(0, 1, 65535, false)]
        [InlineData(0, 65535, 0x1fffffff, false)]
        [InlineData(1, 1, 65533, true)]
        [InlineData(1, 1, 65534, true)]
        [InlineData(1, 1, 65535, false)]
        [InlineData(1, 65535, -65535, true)]
        [InlineData(1, 65535, -65536, false)]
        [InlineData(1, 65535, 0x0fffffff, false)]
        [InlineData(1, 0, -0x10000000, false)]
        public void SequencePoints_EndColumn(int deltaLines, int startColumn, int deltaColumns, bool valid)
        {
            var points = new BlobBuilder();
            points.WriteCompressedInteger(0); // local signature
            points.WriteCompressedInteger(0); // IL offset
            points.WriteCompressedInteger(deltaLines);
            if (deltaLines == 0)
            {
                points.WriteCompressedInteger(deltaColumns);
            }
            else
            {
                points.WriteCompressedSignedInteger(deltaColumns);
            }
            points.WriteCompressedInteger(1); // start line
            points.WriteCompressedInteger(startColumn);

            using var provider = CreateSequencePointsPdb(points);
            var enumerator = provider.GetMetadataReader().GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(1)).GetSequencePoints().GetEnumerator();
            if (valid)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(startColumn, enumerator.Current.StartColumn);
                Assert.Equal(startColumn + deltaColumns, enumerator.Current.EndColumn);
                Assert.False(enumerator.MoveNext());
            }
            else
            {
                Assert.Throws<BadImageFormatException>(() => enumerator.MoveNext());
            }
        }

        [Theory]
        [InlineData(1, -1, true)]
        [InlineData(1, 65533, true)]
        [InlineData(1, 65534, true)]
        [InlineData(1, 65535, false)]
        [InlineData(1, -2, false)]
        [InlineData(65535, -65535, true)]
        [InlineData(65535, -65536, false)]
        [InlineData(65535, 0x0fffffff, false)]
        [InlineData(0, -0x10000000, false)]
        public void SequencePoints_StartColumnDelta(int startColumn, int deltaColumns, bool valid)
        {
            var points = new BlobBuilder();
            points.WriteCompressedInteger(0); // local signature
            points.WriteCompressedInteger(0); // IL offset
            points.WriteCompressedInteger(1); // delta lines
            points.WriteCompressedSignedInteger(0); // delta columns
            points.WriteCompressedInteger(1); // start line
            points.WriteCompressedInteger(startColumn);
            points.WriteCompressedInteger(1); // delta IL offset
            points.WriteCompressedInteger(1); // delta lines
            points.WriteCompressedSignedInteger(0); // delta columns
            points.WriteCompressedSignedInteger(1); // delta start line
            points.WriteCompressedSignedInteger(deltaColumns);

            using var provider = CreateSequencePointsPdb(points);
            var enumerator = provider.GetMetadataReader().GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(1)).GetSequencePoints().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(startColumn, enumerator.Current.StartColumn);
            if (valid)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(startColumn + deltaColumns, enumerator.Current.StartColumn);
                Assert.Equal(startColumn + deltaColumns, enumerator.Current.EndColumn);
                Assert.False(enumerator.MoveNext());
            }
            else
            {
                Assert.Throws<BadImageFormatException>(() => enumerator.MoveNext());
            }
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(SequencePoint.HiddenLine - 2, true)]
        [InlineData(SequencePoint.HiddenLine - 1, false)]
        [InlineData(SequencePoint.HiddenLine, false)]
        [InlineData(SequencePoint.HiddenLine + 1, true)]
        [InlineData(0x1ffffffe, true)]
        public void SequencePoints_InitialLine(int startLine, bool valid)
        {
            var points = new BlobBuilder();
            points.WriteCompressedInteger(0); // local signature
            points.WriteCompressedInteger(0); // IL offset
            points.WriteCompressedInteger(1); // delta lines
            points.WriteCompressedSignedInteger(0); // delta columns
            points.WriteCompressedInteger(startLine);
            points.WriteCompressedInteger(1); // start column

            using var provider = CreateSequencePointsPdb(points);
            var enumerator = provider.GetMetadataReader().GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(1)).GetSequencePoints().GetEnumerator();
            if (valid)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(startLine, enumerator.Current.StartLine);
                Assert.Equal(startLine + 1, enumerator.Current.EndLine);
                Assert.False(enumerator.Current.IsHidden);
                Assert.False(enumerator.MoveNext());
            }
            else
            {
                Assert.Throws<BadImageFormatException>(() => enumerator.MoveNext());
            }
        }

        [Theory]
        [InlineData(0, SequencePoint.HiddenLine - 1, true)]
        [InlineData(0, SequencePoint.HiddenLine, false)]
        [InlineData(0, SequencePoint.HiddenLine + 1, true)]
        [InlineData(0x1ffffffe, 1, true)]
        [InlineData(0x1fffffff, 0, true)]
        [InlineData(0x1fffffff, 1, false)]
        [InlineData(0x1fffffff, 0x1fffffff, false)]
        public void SequencePoints_EndLine(int startLine, int deltaLines, bool valid)
        {
            var points = new BlobBuilder();
            points.WriteCompressedInteger(0); // local signature
            points.WriteCompressedInteger(0); // IL offset
            points.WriteCompressedInteger(deltaLines);
            if (deltaLines == 0)
            {
                points.WriteCompressedInteger(1); // delta columns
            }
            else
            {
                points.WriteCompressedSignedInteger(1); // delta columns
            }
            points.WriteCompressedInteger(startLine);
            points.WriteCompressedInteger(1); // start column

            using var provider = CreateSequencePointsPdb(points);
            var enumerator = provider.GetMetadataReader().GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(1)).GetSequencePoints().GetEnumerator();
            if (valid)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(startLine, enumerator.Current.StartLine);
                Assert.Equal(startLine + deltaLines, enumerator.Current.EndLine);
                Assert.False(enumerator.Current.IsHidden);
                Assert.False(enumerator.MoveNext());
            }
            else
            {
                Assert.Throws<BadImageFormatException>(() => enumerator.MoveNext());
            }
        }

        [Theory]
        [InlineData(0, SequencePoint.HiddenLine - 1, true)]
        [InlineData(0, SequencePoint.HiddenLine, false)]
        [InlineData(0, SequencePoint.HiddenLine + 1, true)]
        [InlineData(SequencePoint.HiddenLine + 1, -1, false)]
        [InlineData(0x10000000, 0x0fffffff, true)]
        [InlineData(0x1fffffff, 1, false)]
        [InlineData(0, -1, false)]
        [InlineData(0x10000000, -0x10000000, true)]
        [InlineData(0x1fffffff, -0x10000000, true)]
        public void SequencePoints_StartLineDelta(int startLine, int deltaLines, bool valid)
        {
            var points = new BlobBuilder();
            points.WriteCompressedInteger(0); // local signature
            points.WriteCompressedInteger(0); // IL offset
            points.WriteCompressedInteger(0); // delta lines
            points.WriteCompressedInteger(1); // delta columns
            points.WriteCompressedInteger(startLine);
            points.WriteCompressedInteger(1); // start column
            points.WriteCompressedInteger(1); // delta IL offset
            points.WriteCompressedInteger(0); // delta lines
            points.WriteCompressedInteger(1); // delta columns
            points.WriteCompressedSignedInteger(deltaLines);
            points.WriteCompressedSignedInteger(0); // delta start column

            using var provider = CreateSequencePointsPdb(points);
            var enumerator = provider.GetMetadataReader().GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(1)).GetSequencePoints().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(startLine, enumerator.Current.StartLine);
            if (valid)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(startLine + deltaLines, enumerator.Current.StartLine);
                Assert.Equal(startLine + deltaLines, enumerator.Current.EndLine);
                Assert.False(enumerator.Current.IsHidden);
                Assert.False(enumerator.MoveNext());
            }
            else
            {
                Assert.Throws<BadImageFormatException>(() => enumerator.MoveNext());
            }
        }

        [Fact]
        public void SequencePoints_HiddenBetweenHighLines()
        {
            var points = new BlobBuilder();
            points.WriteCompressedInteger(0); // local signature
            points.WriteCompressedInteger(0); // IL offset
            points.WriteCompressedInteger(0); // delta lines
            points.WriteCompressedInteger(1); // delta columns
            points.WriteCompressedInteger(0x1fffffff); // start line
            points.WriteCompressedInteger(1); // start column
            points.WriteCompressedInteger(1); // delta IL offset
            points.WriteCompressedInteger(0); // hidden point
            points.WriteCompressedInteger(0);
            points.WriteCompressedInteger(1); // delta IL offset
            points.WriteCompressedInteger(0); // delta lines
            points.WriteCompressedInteger(1); // delta columns
            points.WriteCompressedSignedInteger(-1); // delta start line
            points.WriteCompressedSignedInteger(0); // delta start column

            using var provider = CreateSequencePointsPdb(points);
            var enumerator = provider.GetMetadataReader().GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(1)).GetSequencePoints().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(0x1fffffff, enumerator.Current.StartLine);
            Assert.True(enumerator.MoveNext());
            Assert.True(enumerator.Current.IsHidden);
            Assert.Equal(SequencePoint.HiddenLine, enumerator.Current.StartLine);
            Assert.Equal(SequencePoint.HiddenLine, enumerator.Current.EndLine);
            Assert.Equal(0, enumerator.Current.StartColumn);
            Assert.Equal(0, enumerator.Current.EndColumn);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(0x1ffffffe, enumerator.Current.StartLine);
            Assert.Equal(0x1ffffffe, enumerator.Current.EndLine);
            Assert.False(enumerator.Current.IsHidden);
            Assert.False(enumerator.MoveNext());
        }

        private static MetadataReaderProvider CreateSequencePointsPdb(BlobBuilder points)
        {
            var metadata = new MetadataBuilder();
            DocumentHandle document = metadata.AddDocument(metadata.GetOrAddDocumentName("test.cs"), default, default, default);
            metadata.AddMethodDebugInformation(document, metadata.GetOrAddBlob(points));
            var rowCounts = new int[MetadataTokens.TableCount];
            rowCounts[(int)TableIndex.MethodDef] = 1;
            var pdb = new PortablePdbBuilder(metadata, ImmutableArray.Create(rowCounts), default);
            var image = new BlobBuilder();
            pdb.Serialize(image);

            return MetadataReaderProvider.FromPortablePdbImage(image.ToImmutableArray());
        }

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

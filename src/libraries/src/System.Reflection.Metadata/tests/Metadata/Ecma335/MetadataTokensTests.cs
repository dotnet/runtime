// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class MetadataTokensTests
    {
        private static readonly Handle assemblyRefHandle = AssemblyReferenceHandle.FromRowId(1);
        private static readonly Handle virtualAssemblyRefHandle = AssemblyReferenceHandle.FromVirtualIndex(AssemblyReferenceHandle.VirtualIndex.System_Runtime);
        private static readonly Handle virtualBlobHandle = BlobHandle.FromVirtualIndex(BlobHandle.VirtualIndex.AttributeUsage_AllowSingle, 0);
        private static readonly Handle userStringHandle = UserStringHandle.FromIndex(1);
        private static readonly Handle stringHandle = StringHandle.FromIndex(1);
        private static readonly Handle winrtPrefixedStringHandle = StringHandle.FromIndex(1).WithWinRTPrefix();
        private static readonly Handle blobHandle = BlobHandle.FromIndex(1);
        private static readonly Handle guidHandle = GuidHandle.FromIndex(16);
        private static readonly Handle exportedTypeHandle = ExportedTypeHandle.FromRowId(42);

        [Fact]
        public void GetRowNumber()
        {
            Assert.Equal(1, MetadataTokens.GetRowNumber(assemblyRefHandle));
            Assert.Equal(-1, MetadataTokens.GetRowNumber(virtualAssemblyRefHandle));

            Assert.Throws<ArgumentException>(() => MetadataTokens.GetRowNumber(virtualBlobHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetRowNumber(userStringHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetRowNumber(stringHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetRowNumber(winrtPrefixedStringHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetRowNumber(blobHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetRowNumber(guidHandle));
        }

        [Fact]
        public void GetHeapOffset()
        {
            Assert.Equal(-1, MetadataTokens.GetHeapOffset(virtualBlobHandle));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(userStringHandle));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(stringHandle));
            Assert.Equal(-1, MetadataTokens.GetHeapOffset(winrtPrefixedStringHandle));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(blobHandle));
            Assert.Equal(16, MetadataTokens.GetHeapOffset(guidHandle));

            Assert.Throws<ArgumentException>(() => MetadataTokens.GetHeapOffset(assemblyRefHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetHeapOffset(virtualAssemblyRefHandle));
        }

        [Fact]
        public void GetToken()
        {
            Assert.Equal(0x23000001, MetadataTokens.GetToken(assemblyRefHandle));
            Assert.Equal(0, MetadataTokens.GetToken(virtualAssemblyRefHandle));
            Assert.Equal(0x70000001, MetadataTokens.GetToken(userStringHandle));

            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(virtualBlobHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(stringHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(winrtPrefixedStringHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(blobHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(guidHandle));
        }

        [Fact]
        public void CreateHandle()
        {
            Assert.Equal(assemblyRefHandle, MetadataTokens.Handle(0x23000001));
            Assert.Equal(userStringHandle, MetadataTokens.Handle(0x70000001));
            Assert.Equal(exportedTypeHandle, MetadataTokens.ExportedTypeHandle((int)(TokenTypeIds.ExportedType | exportedTypeHandle.RowId)));

            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(-1));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x71000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x72000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x73000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x74000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x7a000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x7e000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x7fffffff));
        }
    }
}

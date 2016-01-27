// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class MetadataTokensTests
    {
        private static readonly EntityHandle s_assemblyRefHandle = AssemblyReferenceHandle.FromRowId(1);
        private static readonly EntityHandle s_virtualAssemblyRefHandle = AssemblyReferenceHandle.FromVirtualIndex(AssemblyReferenceHandle.VirtualIndex.System_Runtime);
        private static readonly Handle s_virtualBlobHandle = BlobHandle.FromVirtualIndex(BlobHandle.VirtualIndex.AttributeUsage_AllowSingle, 0);
        private static readonly Handle s_userStringHandle = UserStringHandle.FromOffset(1);
        private static readonly Handle s_stringHandle = StringHandle.FromOffset(1);
        private static readonly Handle s_winrtPrefixedStringHandle = StringHandle.FromOffset(1).WithWinRTPrefix();
        private static readonly Handle s_blobHandle = BlobHandle.FromOffset(1);
        private static readonly Handle s_guidHandle = GuidHandle.FromIndex(16);
        private static readonly EntityHandle s_exportedTypeHandle = ExportedTypeHandle.FromRowId(42);

        [Fact]
        public void GetRowNumber()
        {
            Assert.Equal(1, MetadataTokens.GetRowNumber(s_assemblyRefHandle));
            Assert.Equal(-1, MetadataTokens.GetRowNumber(s_virtualAssemblyRefHandle));
        }

        [Fact]
        public void GetHeapOffset()
        {
            Assert.Equal(-1, MetadataTokens.GetHeapOffset(s_virtualBlobHandle));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(s_userStringHandle));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(s_stringHandle));
            Assert.Equal(-1, MetadataTokens.GetHeapOffset(s_winrtPrefixedStringHandle));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(s_blobHandle));
            Assert.Equal(16, MetadataTokens.GetHeapOffset(s_guidHandle));

            Assert.Throws<ArgumentException>(() => MetadataTokens.GetHeapOffset(s_assemblyRefHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetHeapOffset(s_virtualAssemblyRefHandle));
        }

        [Fact]
        public void GetToken()
        {
            Assert.Equal(0x23000001, MetadataTokens.GetToken((Handle)s_assemblyRefHandle));
            Assert.Equal(0, MetadataTokens.GetToken((Handle)s_virtualAssemblyRefHandle));

            Assert.Equal(0x23000001, MetadataTokens.GetToken(s_assemblyRefHandle));
            Assert.Equal(0, MetadataTokens.GetToken(s_virtualAssemblyRefHandle));
            Assert.Equal(0x70000001, MetadataTokens.GetToken(s_userStringHandle));

            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(s_virtualBlobHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(s_stringHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(s_winrtPrefixedStringHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(s_blobHandle));
            Assert.Throws<ArgumentException>(() => MetadataTokens.GetToken(s_guidHandle));
        }

        [Fact]
        public void HandleFactories_FromToken()
        {
            Assert.Equal(s_assemblyRefHandle, MetadataTokens.Handle(0x23000001));
            Assert.Equal(s_userStringHandle, MetadataTokens.Handle(0x70000001));
            Assert.Equal(s_exportedTypeHandle, MetadataTokens.ExportedTypeHandle((int)(TokenTypeIds.ExportedType | s_exportedTypeHandle.RowId)));

            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(-1));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x71000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x72000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x73000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x74000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x7a000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x7e000001));
            Assert.Throws<ArgumentException>(() => MetadataTokens.Handle(0x7fffffff));
        }

        [Fact]
        public void HandleFactories_FromTableIndex()
        {
            Assert.Equal(s_assemblyRefHandle, MetadataTokens.Handle(TableIndex.AssemblyRef, 1));
            Assert.Equal(s_exportedTypeHandle, MetadataTokens.Handle(TableIndex.ExportedType, s_exportedTypeHandle.RowId));

            Assert.Equal(s_assemblyRefHandle, MetadataTokens.EntityHandle(TableIndex.AssemblyRef, 1));
            Assert.Equal(s_exportedTypeHandle, MetadataTokens.EntityHandle(TableIndex.ExportedType, s_exportedTypeHandle.RowId));

            Assert.Equal(s_userStringHandle, MetadataTokens.Handle((TableIndex)0x70, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataTokens.Handle((TableIndex)0x71, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataTokens.Handle((TableIndex)0x72, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataTokens.Handle((TableIndex)0x73, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataTokens.Handle((TableIndex)0x74, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataTokens.Handle((TableIndex)0x7a, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataTokens.Handle((TableIndex)0x7e, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MetadataTokens.Handle((TableIndex)0x7f, 0xffffff));
        }

        [Fact]
        public void SpecificHandleFactories()
        {
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.MethodDefinitionHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.MethodImplementationHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.MethodSpecificationHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.TypeDefinitionHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.ExportedTypeHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.TypeReferenceHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.TypeSpecificationHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.InterfaceImplementationHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.MemberReferenceHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.FieldDefinitionHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.EventDefinitionHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.PropertyDefinitionHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.StandaloneSignatureHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.ParameterHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.GenericParameterHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.GenericParameterConstraintHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.ModuleReferenceHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.AssemblyReferenceHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.CustomAttributeHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.DeclarativeSecurityAttributeHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.ConstantHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.ManifestResourceHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.AssemblyFileHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.DocumentHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.MethodDebugInformationHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.LocalScopeHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.LocalVariableHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.LocalConstantHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.ImportScopeHandle(1)));
            Assert.Equal(1, MetadataTokens.GetRowNumber(MetadataTokens.CustomDebugInformationHandle(1)));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(MetadataTokens.UserStringHandle(1)));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(MetadataTokens.StringHandle(1)));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(MetadataTokens.BlobHandle(1)));
            Assert.Equal(1, MetadataTokens.GetHeapOffset(MetadataTokens.GuidHandle(1)));
            Assert.Equal(1, MetadataTokens.GetHeapOffset((BlobHandle)MetadataTokens.DocumentNameBlobHandle(1)));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class LargeTablesAndHeapsTests
    {
        [Fact]
        public void Baseline()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Guids()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(HeapIndex.Guid, 0x10000 * 16);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.GetOrAddGuid(Guid.NewGuid());
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * LARGE + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + LARGE + 2 + LARGE, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Blobs()
        {
            var mdBuilder = new MetadataBuilder();

            for (int i = 0; i < 0x10000 / sizeof(int); i++)
            {
                mdBuilder.GetOrAddBlob(ImmutableArray.Create(BitConverter.GetBytes(i)));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + LARGE + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(LARGE, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(LARGE, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + LARGE + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + LARGE + 2 + 2 + LARGE, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + LARGE, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(LARGE + 2 + LARGE + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Strings()
        {
            var mdBuilder = new MetadataBuilder();

            for (int i = 0; i < 0x10000 / 4; i++)
            {
                mdBuilder.GetOrAddString($"<{i}>");
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + LARGE, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + LARGE + LARGE, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + LARGE + LARGE + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + LARGE + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + LARGE, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(LARGE, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + LARGE + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + LARGE + LARGE, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + LARGE + LARGE + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + LARGE + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + LARGE + LARGE + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + LARGE + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + LARGE, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void TypeRef()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.TypeRef, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddTypeReference(default(ModuleDefinitionHandle), default(StringHandle), default(StringHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + LARGE + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void TypeDef()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.TypeDef, 0x10000);
            mdBuilder.SetCapacity(TableIndex.ClassLayout, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                var t = mdBuilder.AddTypeDefinition(0, default(StringHandle), default(StringHandle), default(EntityHandle), default(FieldDefinitionHandle), default(MethodDefinitionHandle));
                mdBuilder.AddTypeLayout(t, 0, 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + LARGE + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(LARGE + LARGE, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + LARGE, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.EventTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(LARGE + LARGE, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + LARGE + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void TypeLayout()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.TypeDef, 0x10000);
            mdBuilder.SetCapacity(TableIndex.ClassLayout, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                var t = mdBuilder.AddTypeDefinition(0, default(StringHandle), default(StringHandle), default(EntityHandle), default(FieldDefinitionHandle), default(MethodDefinitionHandle));
                mdBuilder.AddTypeLayout(t, 0, 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + LARGE + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(LARGE + LARGE, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + LARGE, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.EventTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(LARGE + LARGE, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + LARGE + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Field()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Field, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddFieldDefinition(0, default(StringHandle), default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + LARGE + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + LARGE, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + LARGE + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + LARGE, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void FieldLayout()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Field, 0x10000);
            mdBuilder.SetCapacity(TableIndex.FieldLayout, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                var f = mdBuilder.AddFieldDefinition(0, default(StringHandle), default(BlobHandle));
                mdBuilder.AddFieldLayout(f, 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + LARGE + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + LARGE, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + LARGE + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + LARGE, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void FieldRva()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Field, 0x10000);
            mdBuilder.SetCapacity(TableIndex.FieldRva, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                var f = mdBuilder.AddFieldDefinition(0, default(StringHandle), default(BlobHandle));
                mdBuilder.AddFieldRelativeVirtualAddress(f, 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + LARGE + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + LARGE, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + LARGE + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + LARGE, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void MethodDef()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.MethodDef, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + LARGE, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + LARGE + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + LARGE + LARGE, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + LARGE + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + LARGE + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(LARGE + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(LARGE + LARGE, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void StateMachineMethod()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.MethodDef, 0x10000);
            mdBuilder.SetCapacity(TableIndex.StateMachineMethod, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                var m = mdBuilder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));
                mdBuilder.AddStateMachineMethod(m, default(MethodDefinitionHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + LARGE, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + LARGE + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + LARGE + LARGE, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + LARGE + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + LARGE + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(LARGE + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(LARGE + LARGE, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Param()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Param, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddParameter(0, default(StringHandle), 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + LARGE, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void InterfaceImpl()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.InterfaceImpl, 0x10000);

            for (int i = 0; i < 0x100; i++)
            {
                for (int j = 0; j < 0x100; j++)
                {
                    mdBuilder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(i + 1), MetadataTokens.TypeDefinitionHandle(j + 1));
                }
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void MemberRef()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.MemberRef, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddMemberReference(default(TypeDefinitionHandle), default(StringHandle), default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + LARGE + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + LARGE + LARGE, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void StandaloneSignatures()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.StandAloneSig, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddStandaloneSignature(default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void DeclSecurity()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.DeclSecurity, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddDeclarativeSecurityAttribute(default(TypeDefinitionHandle), 0, default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Event()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Event, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddEvent(0, default(StringHandle), default(TypeDefinitionHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Property()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Property, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddProperty(0, default(StringHandle), default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + LARGE + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + LARGE, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + LARGE, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void AssemblyRef()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.AssemblyRef, 0x10000);

            var nullVersion = new Version(0, 0, 0, 0);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddAssemblyReference(default(StringHandle), nullVersion, default(StringHandle), default(BlobHandle), 0, default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + LARGE, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + LARGE, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void File()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.File, 0x10000);

            var nullVersion = new Version(0, 0, 0, 0);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddAssemblyFile(default(StringHandle), default(BlobHandle), true);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + LARGE, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + LARGE, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void ExportedType()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.ExportedType, 0x10000);

            var nullVersion = new Version(0, 0, 0, 0);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddExportedType(0, default(StringHandle), default(StringHandle), default(AssemblyFileHandle), 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + LARGE, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + LARGE, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void ManifestResource()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.ManifestResource, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddManifestResource(0, default(StringHandle), default(EntityHandle), 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void GenericParam()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.GenericParam, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddGenericParameter(default(TypeDefinitionHandle), 0, default(StringHandle), i);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void GenericParamConstraint()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.GenericParamConstraint, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddGenericParameterConstraint(default(GenericParameterHandle), default(TypeDefinitionHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void MethodSpec()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.MethodSpec, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddMethodSpecification(default(MethodDefinitionHandle), default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void Document()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Document, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddDocument(default(BlobHandle), default(GuidHandle), default(BlobHandle), default(GuidHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void LocalScope()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.LocalScope, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddLocalScope(default(MethodDefinitionHandle), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), 0, 0);
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void LocalVariable()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.LocalVariable, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddLocalVariable(0, 0, default(StringHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + LARGE + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void LocalConstant()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.LocalConstant, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddLocalConstant(default(StringHandle), default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + LARGE + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }

        [Fact]
        public void ImportScope()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.ImportScope, 0x10000);

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddImportScope(default(ImportScopeHandle), default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder);
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                const int LARGE = 4;

                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + LARGE + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(LARGE + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(LARGE + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }
        
        [Fact]
        public void Others()
        {
            var mdBuilder = new MetadataBuilder();

            mdBuilder.SetCapacity(TableIndex.Constant, 0x10000);
            mdBuilder.SetCapacity(TableIndex.CustomAttribute, 0x10000);
            mdBuilder.SetCapacity(TableIndex.FieldMarshal, 0x10000);
            mdBuilder.SetCapacity(TableIndex.EventMap, 0x10000);
            mdBuilder.SetCapacity(TableIndex.PropertyMap, 0x10000);
            mdBuilder.SetCapacity(TableIndex.MethodSemantics, 0x10000);
            mdBuilder.SetCapacity(TableIndex.MethodImpl, 0x10000);
            mdBuilder.SetCapacity(TableIndex.NestedClass, 0x10000);
            mdBuilder.SetCapacity(TableIndex.CustomDebugInformation, 0x10000);
            object one = 1;

            for (int i = 0; i < 0x10000; i++)
            {
                mdBuilder.AddConstant(default(ParameterHandle), one);
                mdBuilder.AddCustomAttribute(default(ParameterHandle), default(MethodDefinitionHandle), default(BlobHandle));
                mdBuilder.AddMarshallingDescriptor(default(ParameterHandle), default(BlobHandle));
                mdBuilder.AddEventMap(default(TypeDefinitionHandle), default(EventDefinitionHandle));
                mdBuilder.AddPropertyMap(default(TypeDefinitionHandle), default(PropertyDefinitionHandle));
                mdBuilder.AddMethodSemantics(default(EventDefinitionHandle), 0, default(MethodDefinitionHandle));
                mdBuilder.AddMethodImplementation(default(TypeDefinitionHandle), default(MethodDefinitionHandle), default(MethodDefinitionHandle));
                mdBuilder.AddNestedType(default(TypeDefinitionHandle), default(TypeDefinitionHandle));
                mdBuilder.AddCustomDebugInformation(default(ParameterHandle), default(GuidHandle), default(BlobHandle));
            }

            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var rootBuilder = new MetadataRootBuilder(mdBuilder, suppressValidation: true); // NestedClass not sorted
            var mdBlob = new BlobBuilder();
            rootBuilder.Serialize(mdBlob, 0, 0);

            // validate sizes table rows that reference guids:
            using (var mdProvider = MetadataReaderProvider.FromMetadataImage(mdBlob.ToImmutableArray()))
            {
                var mdReader = mdProvider.GetMetadataReader();

                Assert.Equal(2 + 3 * 2 + 2, mdReader.ModuleTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.TypeRefTable.RowSize);
                Assert.Equal(4 + 2 + 2 + 2 + 2 + 2, mdReader.TypeDefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.FieldTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.MethodDefTable.RowSize);
                Assert.Equal(4 + 2, mdReader.ParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.InterfaceImplTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MemberRefTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.ConstantTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomAttributeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.FieldMarshalTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.DeclSecurityTable.RowSize);
                Assert.Equal(6 + 2, mdReader.ClassLayoutTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldLayoutTable.RowSize);
                Assert.Equal(2, mdReader.StandAloneSigTable.RowSize);
                Assert.Equal(2 + 2, mdReader.EventMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.EventTable.RowSize);
                Assert.Equal(2 + 2, mdReader.PropertyMapTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.PropertyTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodSemanticsTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.MethodImplTable.RowSize);
                Assert.Equal(2, mdReader.ModuleRefTable.RowSize);
                Assert.Equal(2, mdReader.TypeSpecTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.ImplMapTable.RowSize);
                Assert.Equal(4 + 2, mdReader.FieldRvaTable.RowSize);
                Assert.Equal(16 + 2 + 2 + 2, mdReader.AssemblyTable.RowSize);
                Assert.Equal(12 + 2 + 2 + 2 + 2, mdReader.AssemblyRefTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.FileTable.RowSize);
                Assert.Equal(8 + 2 + 2 + 2, mdReader.ExportedTypeTable.RowSize);
                Assert.Equal(8 + 2 + 2, mdReader.ManifestResourceTable.RowSize);
                Assert.Equal(2 + 2, mdReader.NestedClassTable.RowSize);
                Assert.Equal(4 + 2 + 2, mdReader.GenericParamTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodSpecTable.RowSize);
                Assert.Equal(2 + 2, mdReader.GenericParamConstraintTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2, mdReader.DocumentTable.RowSize);
                Assert.Equal(2 + 2, mdReader.MethodDebugInformationTable.RowSize);
                Assert.Equal(2 + 2 + 2 + 2 + 4 + 4, mdReader.LocalScopeTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.LocalVariableTable.RowSize);
                Assert.Equal(2 + 2, mdReader.LocalConstantTable.RowSize);
                Assert.Equal(2 + 2, mdReader.ImportScopeTable.RowSize);
                Assert.Equal(2 + 2, mdReader.StateMachineMethodTable.RowSize);
                Assert.Equal(2 + 2 + 2, mdReader.CustomDebugInformationTable.RowSize);
            }
        }
    }
}

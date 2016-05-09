// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class MetadataBuilderTests
    {
        [Fact]
        public void Add()
        {
            var builder = new MetadataBuilder();

            builder.AddModule(default(int), default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Module]);

            builder.AddAssembly(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), default(AssemblyFlags), default(AssemblyHashAlgorithm));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Assembly]);

            var assemblyReference = builder.AddAssemblyReference(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), default(AssemblyFlags), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.AssemblyRef]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(assemblyReference));

            var typeDefinition = builder.AddTypeDefinition(default(TypeAttributes), default(StringHandle), default(StringHandle), default(EntityHandle), default(FieldDefinitionHandle), default(MethodDefinitionHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.TypeDef]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(typeDefinition));

            builder.AddTypeLayout(default(TypeDefinitionHandle), default(ushort), default(uint));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.ClassLayout]);

            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.InterfaceImpl]);

            builder.AddNestedType(default(TypeDefinitionHandle), default(TypeDefinitionHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.NestedClass]);

            var typeReference = builder.AddTypeReference(EntityHandle.ModuleDefinition, default(StringHandle), default(StringHandle));
            Assert.Equal(1, MetadataTokens.GetRowNumber(typeReference));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.TypeRef]);

            builder.AddTypeSpecification(default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.TypeSpec]);

            builder.AddStandaloneSignature(default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.StandAloneSig]);

            builder.AddProperty(default(PropertyAttributes), default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Property]);

            builder.AddPropertyMap(default(TypeDefinitionHandle), default(PropertyDefinitionHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.PropertyMap]);

            builder.AddEvent(default(EventAttributes), default(StringHandle), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Event]);

            builder.AddEventMap(default(TypeDefinitionHandle), default(EventDefinitionHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.EventMap]);

            builder.AddConstant(MetadataTokens.FieldDefinitionHandle(1), default(object));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Constant]);

            builder.AddMethodSemantics(MetadataTokens.EventDefinitionHandle(1), default(ushort), default(MethodDefinitionHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.MethodSemantics]);

            builder.AddCustomAttribute(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.CustomAttribute]);

            builder.AddMethodSpecification(MetadataTokens.MethodDefinitionHandle(1), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.MethodSpec]);

            builder.AddModuleReference(default(StringHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.ModuleRef]);

            builder.AddParameter(default(ParameterAttributes), default(StringHandle), default(int));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Param]);

            var genericParameter = builder.AddGenericParameter(MetadataTokens.MethodDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), default(int));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.GenericParam]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(genericParameter));

            builder.AddGenericParameterConstraint(default(GenericParameterHandle), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.GenericParamConstraint]);

            builder.AddFieldDefinition(default(FieldAttributes), default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Field]);

            builder.AddFieldLayout(default(FieldDefinitionHandle), default(int));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.FieldLayout]);

            builder.AddMarshallingDescriptor(MetadataTokens.FieldDefinitionHandle(1), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.FieldMarshal]);

            builder.AddFieldRelativeVirtualAddress(default(FieldDefinitionHandle), default(int));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.FieldRva]);

            var methodDefinition = builder.AddMethodDefinition(default(MethodAttributes), default(MethodImplAttributes), default(StringHandle), default(BlobHandle), default(int), default(ParameterHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.MethodDef]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(methodDefinition));

            builder.AddMethodImport(MetadataTokens.MethodDefinitionHandle(1), default(MethodImportAttributes), default(StringHandle), default(ModuleReferenceHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.ImplMap]);

            builder.AddMethodImplementation(default(TypeDefinitionHandle), MetadataTokens.MethodDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.MethodImpl]);

            var memberReference = builder.AddMemberReference(MetadataTokens.TypeDefinitionHandle(1), default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.MemberRef]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(memberReference));

            builder.AddManifestResource(default(ManifestResourceAttributes), default(StringHandle), MetadataTokens.AssemblyFileHandle(1), default(uint));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.ManifestResource]);

            builder.AddAssemblyFile(default(StringHandle), default(BlobHandle), default(Boolean));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.File]);

            builder.AddExportedType(default(TypeAttributes), default(StringHandle), default(StringHandle), MetadataTokens.AssemblyFileHandle(1), default(int));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.ExportedType]);

            builder.AddDeclarativeSecurityAttribute(MetadataTokens.TypeDefinitionHandle(1), default(DeclarativeSecurityAction), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.DeclSecurity]);

            builder.AddEncLogEntry(MetadataTokens.TypeDefinitionHandle(1), default(EditAndContinueOperation));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.EncLog]);

            builder.AddEncMapEntry(MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.EncMap]);

            var document = builder.AddDocument(default(BlobHandle), default(GuidHandle), default(BlobHandle), default(GuidHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.Document]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(document));

            builder.AddMethodDebugInformation(default(DocumentHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.MethodDebugInformation]);

            var localScope = builder.AddLocalScope(default(MethodDefinitionHandle), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), default(int), default(int));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.LocalScope]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(localScope));

            var localVariable = builder.AddLocalVariable(default(LocalVariableAttributes), default(int), default(StringHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.LocalVariable]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(localVariable));

            var localConstant = builder.AddLocalConstant(default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.LocalConstant]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(localConstant));

            var importScope = builder.AddImportScope(default(ImportScopeHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.ImportScope]);
            Assert.Equal(1, MetadataTokens.GetRowNumber(importScope));

            builder.AddStateMachineMethod(default(MethodDefinitionHandle), default(MethodDefinitionHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.StateMachineMethod]);

            builder.AddCustomDebugInformation(default(EntityHandle), default(GuidHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCounts()[(int)TableIndex.CustomDebugInformation]);
        }

        /// <summary>
        /// Add methods do miminal validation to avoid overhead.
        /// </summary>
        [Fact]
        public void Add_Errors()
        {
            var builder = new MetadataBuilder();

            var badHandleKind = CustomAttributeHandle.FromRowId(1);

            Assert.Throws<ArgumentNullException>(() => builder.AddAssemblyReference(default(StringHandle), null, default(StringHandle), default(BlobHandle), 0, default(BlobHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddTypeDefinition(0, default(StringHandle), default(StringHandle), badHandleKind, default(FieldDefinitionHandle), default(MethodDefinitionHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddInterfaceImplementation(default(TypeDefinitionHandle), badHandleKind));
            Assert.Throws<ArgumentException>(() => builder.AddTypeReference(badHandleKind, default(StringHandle), default(StringHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddEvent(0, default(StringHandle), badHandleKind));
            Assert.Throws<ArgumentException>(() => builder.AddConstant(badHandleKind, 0));
            Assert.Throws<ArgumentException>(() => builder.AddMethodSemantics(badHandleKind, 0, default(MethodDefinitionHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddCustomAttribute(badHandleKind, default(MethodDefinitionHandle), default(BlobHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddCustomAttribute(default(TypeDefinitionHandle), badHandleKind, default(BlobHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddMethodSpecification(badHandleKind, default(BlobHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddGenericParameter(badHandleKind, 0, default(StringHandle), 0));
            Assert.Throws<ArgumentException>(() => builder.AddGenericParameterConstraint(default(GenericParameterHandle), badHandleKind));
            Assert.Throws<ArgumentException>(() => builder.AddMarshallingDescriptor(badHandleKind, default(BlobHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddMethodImplementation(default(TypeDefinitionHandle), badHandleKind, default(MethodDefinitionHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddMethodImplementation(default(TypeDefinitionHandle), default(MethodDefinitionHandle), badHandleKind));
            Assert.Throws<ArgumentException>(() => builder.AddMemberReference(badHandleKind, default(StringHandle), default(BlobHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddManifestResource(0, default(StringHandle), badHandleKind, 0));
            Assert.Throws<ArgumentException>(() => builder.AddExportedType(0, default(StringHandle), default(StringHandle), badHandleKind, 0));
            Assert.Throws<ArgumentException>(() => builder.AddDeclarativeSecurityAttribute(badHandleKind, 0, default(BlobHandle)));
            Assert.Throws<ArgumentException>(() => builder.AddCustomDebugInformation(badHandleKind, default(GuidHandle), default(BlobHandle)));
        }

        [Fact, ActiveIssue("https://github.com/dotnet/roslyn/issues/9852")]
        public void HeapOverflow_UserString()
        {
            string veryLargeString = new string('x', 0x00fffff0 / 2);

            var builder1 = new MetadataBuilder();
            Assert.Equal(0x70000001, MetadataTokens.GetToken(builder1.GetOrAddUserString(veryLargeString)));

            // TODO: https://github.com/dotnet/roslyn/issues/9852
            // Should throw: Assert.Throws<ImageFormatLimitationException>(() => builder1.GetOrAddUserString("123"));
            // Assert.Equal(0x70fffff6, MetadataTokens.GetToken(builder1.GetOrAddUserString("12")));
            // Assert.Equal(0x70fffff6, MetadataTokens.GetToken(builder1.GetOrAddUserString("12")));
            Assert.Equal(0x70fffff6, MetadataTokens.GetToken(builder1.GetOrAddUserString(veryLargeString + "z")));
            Assert.Throws<ImageFormatLimitationException>(() => builder1.GetOrAddUserString("12"));

            var builder2 = new MetadataBuilder();
            Assert.Equal(0x70000001, MetadataTokens.GetToken(builder2.GetOrAddUserString("123")));
            Assert.Equal(0x70000009, MetadataTokens.GetToken(builder2.GetOrAddUserString(veryLargeString)));
            Assert.Equal(0x70fffffe, MetadataTokens.GetToken(builder2.GetOrAddUserString("4"))); // TODO: should throw https://github.com/dotnet/roslyn/issues/9852

            var builder3 = new MetadataBuilder(userStringHeapStartOffset: 0x00fffffe);
            Assert.Equal(0x70ffffff, MetadataTokens.GetToken(builder3.GetOrAddUserString("1"))); // TODO: should throw https://github.com/dotnet/roslyn/issues/9852

            var builder4 = new MetadataBuilder(userStringHeapStartOffset: 0x00fffff7);
            Assert.Equal(0x70fffff8, MetadataTokens.GetToken(builder4.GetOrAddUserString("1"))); // 4B
            Assert.Equal(0x70fffffc, MetadataTokens.GetToken(builder4.GetOrAddUserString("2"))); // 4B 

            var builder5 = new MetadataBuilder(userStringHeapStartOffset: 0x00fffff8);
            Assert.Equal(0x70fffff9, MetadataTokens.GetToken(builder5.GetOrAddUserString("1"))); // 4B
            Assert.Equal(0x70fffffd, MetadataTokens.GetToken(builder5.GetOrAddUserString("2"))); // 4B // TODO: should throw https://github.com/dotnet/roslyn/issues/9852
        }

        // TODO: test overflow of other heaps, tables

        [Fact]
        public void SetCapacity()
        {
            var builder = new MetadataBuilder();

            builder.GetOrAddString("11111");
            builder.GetOrAddGuid(Guid.NewGuid());
            builder.GetOrAddBlob("2222");
            builder.GetOrAddUserString("3333");

            builder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));
            builder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));
            builder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));

            builder.SetCapacity(TableIndex.MethodDef, 1);
            builder.SetCapacity(TableIndex.MethodDef, 1000);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity(TableIndex.MethodDef, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity(TableIndex.MethodDef, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity((TableIndex)0xff, 10));

            builder.SetCapacity(HeapIndex.String, 3);
            builder.SetCapacity(HeapIndex.String, 1000);
            builder.SetCapacity(HeapIndex.Blob, 3);
            builder.SetCapacity(HeapIndex.Blob, 1000);
            builder.SetCapacity(HeapIndex.Guid, 3);
            builder.SetCapacity(HeapIndex.Guid, 1000);
            builder.SetCapacity(HeapIndex.UserString, 3);
            builder.SetCapacity(HeapIndex.UserString, 1000);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity(HeapIndex.String, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity(HeapIndex.String, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity((HeapIndex)0xff, 10));
        }
    }
}

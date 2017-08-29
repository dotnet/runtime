// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class MetadataBuilderTests
    {
        [Fact]
        public void Ctor_Errors()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetadataBuilder(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetadataBuilder(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetadataBuilder(0, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetadataBuilder(0, 0, 0, -1));
            AssertExtensions.Throws<ArgumentException>("guidHeapStartOffset", () => new MetadataBuilder(0, 0, 0, 1));

            new MetadataBuilder(userStringHeapStartOffset: 0x00fffffe);
            Assert.Throws<ImageFormatLimitationException>(() => new MetadataBuilder(userStringHeapStartOffset: 0x00ffffff));
        }

        [Fact]
        public void Add()
        {
            var builder = new MetadataBuilder();

            builder.AddModule(default(int), default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Module));

            builder.AddAssembly(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), default(AssemblyFlags), default(AssemblyHashAlgorithm));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Assembly));

            var assemblyReference = builder.AddAssemblyReference(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), default(AssemblyFlags), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.AssemblyRef));
            Assert.Equal(1, MetadataTokens.GetRowNumber(assemblyReference));

            var typeDefinition = builder.AddTypeDefinition(default(TypeAttributes), default(StringHandle), default(StringHandle), default(EntityHandle), default(FieldDefinitionHandle), default(MethodDefinitionHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.TypeDef));
            Assert.Equal(1, MetadataTokens.GetRowNumber(typeDefinition));

            builder.AddTypeLayout(default(TypeDefinitionHandle), default(ushort), default(uint));
            Assert.Equal(1, builder.GetRowCount(TableIndex.ClassLayout));

            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCount(TableIndex.InterfaceImpl));

            builder.AddNestedType(default(TypeDefinitionHandle), default(TypeDefinitionHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.NestedClass));

            var typeReference = builder.AddTypeReference(EntityHandle.ModuleDefinition, default(StringHandle), default(StringHandle));
            Assert.Equal(1, MetadataTokens.GetRowNumber(typeReference));
            Assert.Equal(1, builder.GetRowCount(TableIndex.TypeRef));

            builder.AddTypeSpecification(default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.TypeSpec));

            builder.AddStandaloneSignature(default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.StandAloneSig));

            builder.AddProperty(default(PropertyAttributes), default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Property));

            builder.AddPropertyMap(default(TypeDefinitionHandle), default(PropertyDefinitionHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.PropertyMap));

            builder.AddEvent(default(EventAttributes), default(StringHandle), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Event));

            builder.AddEventMap(default(TypeDefinitionHandle), default(EventDefinitionHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.EventMap));

            builder.AddConstant(MetadataTokens.FieldDefinitionHandle(1), default(object));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Constant));

            builder.AddMethodSemantics(MetadataTokens.EventDefinitionHandle(1), default(ushort), default(MethodDefinitionHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.MethodSemantics));

            builder.AddCustomAttribute(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.CustomAttribute));

            builder.AddMethodSpecification(MetadataTokens.MethodDefinitionHandle(1), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.MethodSpec));

            builder.AddModuleReference(default(StringHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.ModuleRef));

            builder.AddParameter(default(ParameterAttributes), default(StringHandle), default(int));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Param));

            var genericParameter = builder.AddGenericParameter(MetadataTokens.MethodDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), default(int));
            Assert.Equal(1, builder.GetRowCount(TableIndex.GenericParam));
            Assert.Equal(1, MetadataTokens.GetRowNumber(genericParameter));

            builder.AddGenericParameterConstraint(default(GenericParameterHandle), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCount(TableIndex.GenericParamConstraint));

            builder.AddFieldDefinition(default(FieldAttributes), default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Field));

            builder.AddFieldLayout(default(FieldDefinitionHandle), default(int));
            Assert.Equal(1, builder.GetRowCount(TableIndex.FieldLayout));

            builder.AddMarshallingDescriptor(MetadataTokens.FieldDefinitionHandle(1), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.FieldMarshal));

            builder.AddFieldRelativeVirtualAddress(default(FieldDefinitionHandle), default(int));
            Assert.Equal(1, builder.GetRowCount(TableIndex.FieldRva));

            var methodDefinition = builder.AddMethodDefinition(default(MethodAttributes), default(MethodImplAttributes), default(StringHandle), default(BlobHandle), default(int), default(ParameterHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.MethodDef));
            Assert.Equal(1, MetadataTokens.GetRowNumber(methodDefinition));

            builder.AddMethodImport(MetadataTokens.MethodDefinitionHandle(1), default(MethodImportAttributes), default(StringHandle), default(ModuleReferenceHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.ImplMap));

            builder.AddMethodImplementation(default(TypeDefinitionHandle), MetadataTokens.MethodDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCount(TableIndex.MethodImpl));

            var memberReference = builder.AddMemberReference(MetadataTokens.TypeDefinitionHandle(1), default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.MemberRef));
            Assert.Equal(1, MetadataTokens.GetRowNumber(memberReference));

            builder.AddManifestResource(default(ManifestResourceAttributes), default(StringHandle), MetadataTokens.AssemblyFileHandle(1), default(uint));
            Assert.Equal(1, builder.GetRowCount(TableIndex.ManifestResource));

            builder.AddAssemblyFile(default(StringHandle), default(BlobHandle), default(Boolean));
            Assert.Equal(1, builder.GetRowCount(TableIndex.File));

            builder.AddExportedType(default(TypeAttributes), default(StringHandle), default(StringHandle), MetadataTokens.AssemblyFileHandle(1), default(int));
            Assert.Equal(1, builder.GetRowCount(TableIndex.ExportedType));

            builder.AddDeclarativeSecurityAttribute(MetadataTokens.TypeDefinitionHandle(1), default(DeclarativeSecurityAction), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.DeclSecurity));

            builder.AddEncLogEntry(MetadataTokens.TypeDefinitionHandle(1), default(EditAndContinueOperation));
            Assert.Equal(1, builder.GetRowCount(TableIndex.EncLog));

            builder.AddEncMapEntry(MetadataTokens.TypeDefinitionHandle(1));
            Assert.Equal(1, builder.GetRowCount(TableIndex.EncMap));

            var document = builder.AddDocument(default(BlobHandle), default(GuidHandle), default(BlobHandle), default(GuidHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.Document));
            Assert.Equal(1, MetadataTokens.GetRowNumber(document));

            builder.AddMethodDebugInformation(default(DocumentHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.MethodDebugInformation));

            var localScope = builder.AddLocalScope(default(MethodDefinitionHandle), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), default(int), default(int));
            Assert.Equal(1, builder.GetRowCount(TableIndex.LocalScope));
            Assert.Equal(1, MetadataTokens.GetRowNumber(localScope));

            var localVariable = builder.AddLocalVariable(default(LocalVariableAttributes), default(int), default(StringHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.LocalVariable));
            Assert.Equal(1, MetadataTokens.GetRowNumber(localVariable));

            var localConstant = builder.AddLocalConstant(default(StringHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.LocalConstant));
            Assert.Equal(1, MetadataTokens.GetRowNumber(localConstant));

            var importScope = builder.AddImportScope(default(ImportScopeHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.ImportScope));
            Assert.Equal(1, MetadataTokens.GetRowNumber(importScope));

            builder.AddStateMachineMethod(default(MethodDefinitionHandle), default(MethodDefinitionHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.StateMachineMethod));

            builder.AddCustomDebugInformation(default(EntityHandle), default(GuidHandle), default(BlobHandle));
            Assert.Equal(1, builder.GetRowCount(TableIndex.CustomDebugInformation));

            Assert.Equal(0, builder.GetRowCount(TableIndex.AssemblyOS));
            Assert.Equal(0, builder.GetRowCount(TableIndex.AssemblyProcessor));
            Assert.Equal(0, builder.GetRowCount(TableIndex.AssemblyRefOS));
            Assert.Equal(0, builder.GetRowCount(TableIndex.AssemblyRefProcessor));
            Assert.Equal(0, builder.GetRowCount(TableIndex.EventPtr));
            Assert.Equal(0, builder.GetRowCount(TableIndex.FieldPtr));
            Assert.Equal(0, builder.GetRowCount(TableIndex.MethodPtr));
            Assert.Equal(0, builder.GetRowCount(TableIndex.ParamPtr));
            Assert.Equal(0, builder.GetRowCount(TableIndex.PropertyPtr));

            var rowCounts = builder.GetRowCounts();
            Assert.Equal(MetadataTokens.TableCount, rowCounts.Length);
            foreach (TableIndex tableIndex in Enum.GetValues(typeof(TableIndex)))
            {
                Assert.Equal(builder.GetRowCount(tableIndex), rowCounts[(int)tableIndex]);
            }
        }

        [Fact]
        public void GetRowCount_Errors()
        {
            var builder = new MetadataBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetRowCount((TableIndex)0x2D));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetRowCount((TableIndex)0x2E));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetRowCount((TableIndex)0x2F));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetRowCount((TableIndex)0x38));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetRowCount((TableIndex)0x39));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetRowCount((TableIndex)255));
        }

        /// <summary>
        /// Add methods do minimal validation to avoid overhead.
        /// </summary>
        [Fact]
        public void Add_ArgumentErrors()
        {
            var builder = new MetadataBuilder();

            var badHandleKind = CustomAttributeHandle.FromRowId(1);
            
            Assert.Throws<ArgumentNullException>(() => builder.AddAssembly(default(StringHandle), null, default(StringHandle), default(BlobHandle), 0, 0));
            Assert.Throws<ArgumentNullException>(() => builder.AddAssemblyReference(default(StringHandle), null, default(StringHandle), default(BlobHandle), 0, default(BlobHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddTypeDefinition(0, default(StringHandle), default(StringHandle), badHandleKind, default(FieldDefinitionHandle), default(MethodDefinitionHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddInterfaceImplementation(default(TypeDefinitionHandle), badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddTypeReference(badHandleKind, default(StringHandle), default(StringHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddEvent(0, default(StringHandle), badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddConstant(badHandleKind, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddMethodSemantics(badHandleKind, 0, default(MethodDefinitionHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddCustomAttribute(badHandleKind, default(MethodDefinitionHandle), default(BlobHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddCustomAttribute(default(TypeDefinitionHandle), badHandleKind, default(BlobHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddMethodSpecification(badHandleKind, default(BlobHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddGenericParameter(badHandleKind, 0, default(StringHandle), 0));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddGenericParameterConstraint(default(GenericParameterHandle), badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddMarshallingDescriptor(badHandleKind, default(BlobHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddMethodImplementation(default(TypeDefinitionHandle), badHandleKind, default(MethodDefinitionHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddMethodImplementation(default(TypeDefinitionHandle), default(MethodDefinitionHandle), badHandleKind));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddMemberReference(badHandleKind, default(StringHandle), default(BlobHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddManifestResource(0, default(StringHandle), badHandleKind, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddExportedType(0, default(StringHandle), default(StringHandle), badHandleKind, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddDeclarativeSecurityAttribute(badHandleKind, 0, default(BlobHandle)));
            AssertExtensions.Throws<ArgumentException>(null, () => builder.AddCustomDebugInformation(badHandleKind, default(GuidHandle), default(BlobHandle)));

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddModule(-1, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle)));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddModule(ushort.MaxValue + 1, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle)));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddParameter(0, default(StringHandle), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddGenericParameter(default(TypeDefinitionHandle), 0, default(StringHandle), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddFieldRelativeVirtualAddress(default(FieldDefinitionHandle), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), -2, default(ParameterHandle)));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddLocalVariable(0, -1, default(StringHandle)));
        }

        [Fact]
        public void MultipleModuleAssemblyEntries()
        {
            var builder = new MetadataBuilder();

            builder.AddAssembly(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), 0, 0);
            builder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            Assert.Throws<InvalidOperationException>(() => builder.AddAssembly(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), 0, 0));
            Assert.Throws<InvalidOperationException>(() => builder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle)));
        }

        [Fact]
        public void Add_BadValues()
        {
            var builder = new MetadataBuilder();

            builder.AddAssembly(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), (AssemblyFlags)(-1), (AssemblyHashAlgorithm)(-1));
            builder.AddAssemblyReference(default(StringHandle), new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), (AssemblyFlags)(-1), default(BlobHandle));
            builder.AddTypeDefinition((TypeAttributes)(-1), default(StringHandle), default(StringHandle), default(TypeDefinitionHandle), default(FieldDefinitionHandle), default(MethodDefinitionHandle));
            builder.AddProperty((PropertyAttributes)(-1), default(StringHandle), default(BlobHandle));
            builder.AddEvent((EventAttributes)(-1), default(StringHandle), default(TypeDefinitionHandle));
            builder.AddMethodSemantics(default(EventDefinitionHandle), (MethodSemanticsAttributes)(-1), default(MethodDefinitionHandle));
            builder.AddParameter((ParameterAttributes)(-1), default(StringHandle), 0);
            builder.AddGenericParameter(default(TypeDefinitionHandle), (GenericParameterAttributes)(-1), default(StringHandle), 0);
            builder.AddFieldDefinition((FieldAttributes)(-1), default(StringHandle), default(BlobHandle));
            builder.AddMethodDefinition((MethodAttributes)(-1), (MethodImplAttributes)(-1), default(StringHandle), default(BlobHandle), -1, default(ParameterHandle));
            builder.AddMethodImport(default(MethodDefinitionHandle), (MethodImportAttributes)(-1), default(StringHandle), default(ModuleReferenceHandle));
            builder.AddManifestResource((ManifestResourceAttributes)(-1), default(StringHandle), default(AssemblyFileHandle), 0);
            builder.AddExportedType((TypeAttributes)(-1), default(StringHandle), default(StringHandle), default(AssemblyFileHandle), 0);
            builder.AddDeclarativeSecurityAttribute(default(TypeDefinitionHandle), (DeclarativeSecurityAction)(-1), default(BlobHandle));
            builder.AddEncLogEntry(default(TypeDefinitionHandle), (EditAndContinueOperation)(-1));
            builder.AddLocalVariable((LocalVariableAttributes)(-1), 0, default(StringHandle));
        }

        [Fact]
        public void GetOrAddErrors()
        {
            var mdBuilder = new MetadataBuilder();
            AssertExtensions.Throws<ArgumentNullException>("value", () => mdBuilder.GetOrAddBlob((BlobBuilder)null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => mdBuilder.GetOrAddBlob((byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => mdBuilder.GetOrAddBlob(default(ImmutableArray<byte>)));
            AssertExtensions.Throws<ArgumentNullException>("value", () => mdBuilder.GetOrAddBlobUTF8(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => mdBuilder.GetOrAddBlobUTF16(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => mdBuilder.GetOrAddDocumentName(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => mdBuilder.GetOrAddString(null));
        }

        [Fact]
        public void Heaps_Empty()
        {
            var mdBuilder = new MetadataBuilder();
            var serialized = mdBuilder.GetSerializedMetadata(MetadataRootBuilder.EmptyRowCounts, 12, isStandaloneDebugMetadata: false);

            var builder = new BlobBuilder();
            mdBuilder.WriteHeapsTo(builder, serialized.StringHeap);

            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // #String
                0x00, 0x00, 0x00, 0x00, // #US
                // #Guid
                0x00, 0x00, 0x00, 0x00  // #Blob
            }, builder.ToArray());
        }

        [Fact]
        public void Heaps()
        {
            var mdBuilder = new MetadataBuilder();

            var g0 = mdBuilder.GetOrAddGuid(default(Guid));
            Assert.True(g0.IsNil);
            Assert.Equal(0, g0.Index);

            var g1 = mdBuilder.GetOrAddGuid(new Guid("D39F3559-476A-4D1E-B6D2-88E66395230B"));
            Assert.Equal(1, g1.Index);

            var s0 = mdBuilder.GetOrAddString("");
            Assert.False(s0.IsVirtual);
            Assert.Equal(0, s0.GetWriterVirtualIndex());

            var s1 = mdBuilder.GetOrAddString("foo");
            Assert.True(s1.IsVirtual);
            Assert.Equal(1, s1.GetWriterVirtualIndex());

            var us0 = mdBuilder.GetOrAddUserString("");
            Assert.Equal(1, us0.GetHeapOffset());

            var us1 = mdBuilder.GetOrAddUserString("bar");
            Assert.Equal(3, us1.GetHeapOffset());

            var b0 = mdBuilder.GetOrAddBlob(new byte[0]);
            Assert.Equal(0, b0.GetHeapOffset());

            var b1 = mdBuilder.GetOrAddBlob(new byte[] { 1, 2 });
            Assert.Equal(1, b1.GetHeapOffset());

            var serialized = mdBuilder.GetSerializedMetadata(MetadataRootBuilder.EmptyRowCounts, 12, isStandaloneDebugMetadata: false);

            Assert.Equal(0, mdBuilder.SerializeHandle(g0));
            Assert.Equal(1, mdBuilder.SerializeHandle(g1));
            Assert.Equal(0, mdBuilder.SerializeHandle(serialized.StringMap, s0));
            Assert.Equal(1, mdBuilder.SerializeHandle(serialized.StringMap, s1));
            Assert.Equal(1, mdBuilder.SerializeHandle(us0));
            Assert.Equal(3, mdBuilder.SerializeHandle(us1));
            Assert.Equal(0, mdBuilder.SerializeHandle(b0));
            Assert.Equal(1, mdBuilder.SerializeHandle(b1));

            var heaps = new BlobBuilder();
            mdBuilder.WriteHeapsTo(heaps, serialized.StringHeap);

            AssertEx.Equal(new byte[] 
            {
                // #String
                0x00, 
                0x66, 0x6F, 0x6F, 0x00,
                0x00, 0x00, 0x00,
                // #US
                0x00,
                0x01, 0x00,
                0x07, 0x62, 0x00, 0x61, 0x00, 0x72, 0x00, 0x00,
                0x00, 
                // #Guid
                0x59, 0x35, 0x9F, 0xD3, 0x6A, 0x47, 0x1E, 0x4D, 0xB6, 0xD2, 0x88, 0xE6, 0x63, 0x95, 0x23, 0x0B,
                // #Blob
                0x00, 0x02, 0x01, 0x02
            }, heaps.ToArray());
        }

        [Fact]
        public void GetOrAddDocumentName1()
        {
            var mdBuilder = new MetadataBuilder();
            mdBuilder.GetOrAddDocumentName("");
            mdBuilder.GetOrAddDocumentName("/a/b/c");
            mdBuilder.GetOrAddDocumentName(@"\a\b\cc");
            mdBuilder.GetOrAddDocumentName(@"/a/b\c");
            mdBuilder.GetOrAddDocumentName(@"/\a/\b\\//c");
            mdBuilder.GetOrAddDocumentName(@"a/");
            mdBuilder.GetOrAddDocumentName(@"/");
            mdBuilder.GetOrAddDocumentName(@"\\");
            mdBuilder.GetOrAddDocumentName("\ud800"); // unpaired surrogate
            mdBuilder.GetOrAddDocumentName("\0");

            var serialized = mdBuilder.GetSerializedMetadata(MetadataRootBuilder.EmptyRowCounts, 12, isStandaloneDebugMetadata: false);

            var heaps = new BlobBuilder();
            mdBuilder.WriteHeapsTo(heaps, serialized.StringHeap);

            AssertEx.Equal(new byte[]
            {
                // #String
                0x00, 0x00, 0x00, 0x00,
                // #US
                0x00, 0x00, 0x00, 0x00,
                
                0x00,              // 0x00

                // ""
                0x02, (byte)'/', 0x00,

                0x01, (byte)'a',   // 0x04
                0x01, (byte)'b',   // 0x06
                0x01, (byte)'c',   // 0x08

                // "/a/b/c"
                0x05, (byte)'/', 0x00, 0x04, 0x06, 0x08,

                // 0x10
                0x02, (byte)'c', (byte)'c', 

                // @"\a\b\cc"
                0x05, (byte)'\\', 0x00, 0x04, 0x06, 0x10,

                // 0x19
                0x03, (byte)'b', (byte)'\\', (byte)'c',

                // @"/a/b\c"
                0x04, (byte)'/', 0x00, 0x04, 0x19,

                // 0x22
                0x02, (byte)'\\', (byte)'a',

                // 0x25
                0x04, (byte)'\\', (byte)'b', (byte)'\\', (byte)'\\',

                // @"/\a/\b\\//c"
                0x06, (byte)'/', 0x00, 0x22, 0x25, 0x00, 0x08,
                
                // @"a/"
                0x03, (byte)'/', 0x04, 0x00,

                // @"/"
                0x03, (byte)'/', 0x00, 0x00,

                // @"\\"
                0x04, (byte)'\\', 0x00, 0x00, 0x00,

                // 0x3E
                0x03, 0xED, 0xA0, 0x80,

                // "\ud800"
                0x02, (byte)'/', 0x3E,

                // 0x45
                0x01, 0x00,

                // "\0"
                0x02, (byte)'/', 0x45,

                // heap padding
                0x00, 0x00
            }, heaps.ToArray());
        }

        [Fact]
        public void GetOrAddDocumentName2()
        {
            var mdBuilder = new MetadataBuilder();
            mdBuilder.AddModule(0, default(StringHandle), default(GuidHandle), default(GuidHandle), default(GuidHandle));

            var n1 = mdBuilder.GetOrAddDocumentName("");
            var n2 = mdBuilder.GetOrAddDocumentName("/a/b/c");
            var n3 = mdBuilder.GetOrAddDocumentName(@"\a\b\cc");
            var n4 = mdBuilder.GetOrAddDocumentName(@"/a/b\c");
            var n5 = mdBuilder.GetOrAddDocumentName(@"/\a/\b\\//c");
            var n6 = mdBuilder.GetOrAddDocumentName(@"a/");
            var n7 = mdBuilder.GetOrAddDocumentName(@"/");
            var n8 = mdBuilder.GetOrAddDocumentName(@"\\");
            var n9 = mdBuilder.GetOrAddDocumentName("\ud800"); // unpaired surrogate
            var n10 = mdBuilder.GetOrAddDocumentName("\0");

            var root = new MetadataRootBuilder(mdBuilder);
            var rootBuilder = new BlobBuilder();
            root.Serialize(rootBuilder, 0, 0);
            var mdImage = rootBuilder.ToImmutableArray();

            using (var provider = MetadataReaderProvider.FromMetadataImage(mdImage))
            {
                var mdReader = provider.GetMetadataReader();
                Assert.Equal("", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n1))));
                Assert.Equal("/a/b/c", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n2))));
                Assert.Equal(@"\a\b\cc", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n3))));
                Assert.Equal(@"/a/b\c", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n4))));
                Assert.Equal(@"/\a/\b\\//c", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n5))));
                Assert.Equal(@"a/", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n6))));
                Assert.Equal(@"/", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n7))));
                Assert.Equal(@"\\", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n8))));
                Assert.Equal("\uFFFd\uFFFd", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n9))));
                Assert.Equal("\0", mdReader.GetString(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(n10))));
            }
        }

        [Fact]
        public void Heaps_StartOffsets()
        {
            var mdBuilder = new MetadataBuilder(
                userStringHeapStartOffset: 0x10,
                stringHeapStartOffset: 0x20, 
                blobHeapStartOffset: 0x30,
                guidHeapStartOffset: 0x40);

            var g = mdBuilder.GetOrAddGuid(new Guid("D39F3559-476A-4D1E-B6D2-88E66395230B"));
            Assert.Equal(5, g.Index);

            var s0 = mdBuilder.GetOrAddString("");
            Assert.False(s0.IsVirtual);
            Assert.Equal(0, s0.GetWriterVirtualIndex());

            var s1 = mdBuilder.GetOrAddString("foo");
            Assert.True(s1.IsVirtual);
            Assert.Equal(1, s1.GetWriterVirtualIndex());

            var us0 = mdBuilder.GetOrAddUserString("");
            Assert.Equal(0x11, us0.GetHeapOffset());

            var us1 = mdBuilder.GetOrAddUserString("bar");
            Assert.Equal(0x13, us1.GetHeapOffset());

            var b0 = mdBuilder.GetOrAddBlob(new byte[0]);
            Assert.Equal(0, b0.GetHeapOffset());

            var b1 = mdBuilder.GetOrAddBlob(new byte[] { 1, 2 });
            Assert.Equal(0x31, b1.GetHeapOffset());

            var serialized = mdBuilder.GetSerializedMetadata(MetadataRootBuilder.EmptyRowCounts, 12, isStandaloneDebugMetadata: false);

            Assert.Equal(5, mdBuilder.SerializeHandle(g));
            Assert.Equal(0, mdBuilder.SerializeHandle(serialized.StringMap, s0));
            Assert.Equal(0x21, mdBuilder.SerializeHandle(serialized.StringMap, s1));
            Assert.Equal(0x11, mdBuilder.SerializeHandle(us0));
            Assert.Equal(0x13, mdBuilder.SerializeHandle(us1));
            Assert.Equal(0, mdBuilder.SerializeHandle(b0));
            Assert.Equal(0x31, mdBuilder.SerializeHandle(b1));

            var heaps = new BlobBuilder();
            mdBuilder.WriteHeapsTo(heaps, serialized.StringHeap);

            AssertEx.Equal(new byte[]
            {
                // #String
                0x00,
                0x66, 0x6F, 0x6F, 0x00,
                0x00, 0x00, 0x00,
                // #US
                0x00,
                0x01, 0x00,
                0x07, 0x62, 0x00, 0x61, 0x00, 0x72, 0x00, 0x00,
                0x00, 
                // #Guid
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x59, 0x35, 0x9F, 0xD3, 0x6A, 0x47, 0x1E, 0x4D, 0xB6, 0xD2, 0x88, 0xE6, 0x63, 0x95, 0x23, 0x0B,
                // #Blob
                0x00, 0x02, 0x01, 0x02
            }, heaps.ToArray());

            Assert.Throws<ArgumentNullException>(() => mdBuilder.GetOrAddString(null));
        }

        [Fact]
        public void Heaps_Reserve()
        {
            var mdBuilder = new MetadataBuilder();

            var guid = mdBuilder.ReserveGuid();
            var us = mdBuilder.ReserveUserString(3);

            Assert.Equal(MetadataTokens.GuidHandle(1), guid.Handle);
            Assert.Equal(MetadataTokens.UserStringHandle(1), us.Handle);

            var serialized = mdBuilder.GetSerializedMetadata(MetadataRootBuilder.EmptyRowCounts, 12, isStandaloneDebugMetadata: false);

            var builder = new BlobBuilder();
            mdBuilder.WriteHeapsTo(builder, serialized.StringHeap);

            AssertEx.Equal(new byte[]
            {
                // #String
                0x00, 0x00, 0x00, 0x00,
                // #US
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                // #Guid
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                // #Blob
                0x00, 0x00, 0x00, 0x00
            }, builder.ToArray());

            guid.CreateWriter().WriteGuid(new Guid("D39F3559-476A-4D1E-B6D2-88E66395230B"));
            us.CreateWriter().WriteUserString("bar");

            AssertEx.Equal(new byte[]
            {
                // #String
                0x00, 0x00, 0x00, 0x00,
                // #US
                0x00, 0x07, 0x62, 0x00, 0x61, 0x00, 0x72, 0x00, 0x00, 0x00, 0x00, 0x00,
                // #Guid
                0x59, 0x35, 0x9F, 0xD3, 0x6A, 0x47, 0x1E, 0x4D, 0xB6, 0xD2, 0x88, 0xE6, 0x63, 0x95, 0x23, 0x0B,
                // #Blob
                0x00, 0x00, 0x00, 0x00
            }, builder.ToArray());
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
            Assert.Throws<ImageFormatLimitationException>(() => builder4.GetOrAddUserString("3")); // hits the limit exactly 

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
            builder.GetOrAddBlobUTF8("2222");
            builder.GetOrAddUserString("3333");

            builder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));
            builder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));
            builder.AddMethodDefinition(0, 0, default(StringHandle), default(BlobHandle), 0, default(ParameterHandle));

            builder.SetCapacity(TableIndex.MethodDef, 0);
            builder.SetCapacity(TableIndex.MethodDef, 1);
            builder.SetCapacity(TableIndex.MethodDef, 1000);
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
            builder.SetCapacity(HeapIndex.String, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity(HeapIndex.String, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCapacity((HeapIndex)0xff, 10));
        }

        [Fact]
        public void ValidateClassLayoutTable()
        {
            var builder = new MetadataBuilder();
            builder.AddTypeLayout(MetadataTokens.TypeDefinitionHandle(2), 1, 1);
            builder.AddTypeLayout(MetadataTokens.TypeDefinitionHandle(1), 1, 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddTypeLayout(MetadataTokens.TypeDefinitionHandle(1), 1, 1);
            builder.AddTypeLayout(MetadataTokens.TypeDefinitionHandle(1), 1, 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }

        [Fact]
        public void ValidateFieldLayoutTable()
        {
            var builder = new MetadataBuilder();
            builder.AddFieldLayout(MetadataTokens.FieldDefinitionHandle(2), 1);
            builder.AddFieldLayout(MetadataTokens.FieldDefinitionHandle(1), 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddFieldLayout(MetadataTokens.FieldDefinitionHandle(1), 1);
            builder.AddFieldLayout(MetadataTokens.FieldDefinitionHandle(1), 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }

        [Fact]
        public void ValidateFieldRvaTable()
        {
            var builder = new MetadataBuilder();
            builder.AddFieldRelativeVirtualAddress(MetadataTokens.FieldDefinitionHandle(2), 1);
            builder.AddFieldRelativeVirtualAddress(MetadataTokens.FieldDefinitionHandle(1), 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddFieldRelativeVirtualAddress(MetadataTokens.FieldDefinitionHandle(1), 1);
            builder.AddFieldRelativeVirtualAddress(MetadataTokens.FieldDefinitionHandle(1), 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }

        [Fact]
        public void ValidateGenericParamTable()
        {
            var builder = new MetadataBuilder();
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(2), default(GenericParameterAttributes), default(StringHandle), index: 0);
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddGenericParameter(MetadataTokens.MethodDefinitionHandle(2), default(GenericParameterAttributes), default(StringHandle), index: 0);
            builder.AddGenericParameter(MetadataTokens.MethodDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddGenericParameter(MetadataTokens.MethodDefinitionHandle(2), default(GenericParameterAttributes), default(StringHandle), index: 0);
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(2), default(GenericParameterAttributes), default(StringHandle), index: 0);
            builder.AddGenericParameter(MetadataTokens.MethodDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 1);
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddGenericParameter(MetadataTokens.MethodDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            builder.AddGenericParameter(MetadataTokens.TypeDefinitionHandle(1), default(GenericParameterAttributes), default(StringHandle), index: 0);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }

        [Fact]
        public void ValidateGenericParamConstaintTable()
        {
            var builder = new MetadataBuilder();
            builder.AddGenericParameterConstraint(MetadataTokens.GenericParameterHandle(2), default(TypeDefinitionHandle));
            builder.AddGenericParameterConstraint(MetadataTokens.GenericParameterHandle(1), default(TypeDefinitionHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddGenericParameterConstraint(MetadataTokens.GenericParameterHandle(1), default(TypeDefinitionHandle));
            builder.AddGenericParameterConstraint(MetadataTokens.GenericParameterHandle(1), default(TypeDefinitionHandle));
            builder.ValidateOrder(); // ok
        }

        [Fact]
        public void ValidateImplMapTable()
        {
            var builder = new MetadataBuilder();
            builder.AddMethodImport(MetadataTokens.MethodDefinitionHandle(2), default(MethodImportAttributes), default(StringHandle), default(ModuleReferenceHandle));
            builder.AddMethodImport(MetadataTokens.MethodDefinitionHandle(1), default(MethodImportAttributes), default(StringHandle), default(ModuleReferenceHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddMethodImport(MetadataTokens.MethodDefinitionHandle(1), default(MethodImportAttributes), default(StringHandle), default(ModuleReferenceHandle));
            builder.AddMethodImport(MetadataTokens.MethodDefinitionHandle(1), default(MethodImportAttributes), default(StringHandle), default(ModuleReferenceHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }

        [Fact]
        public void ValidateInterfaceImplTable()
        {
            var builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(2), MetadataTokens.TypeDefinitionHandle(1));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(2));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeReferenceHandle(2));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeReferenceHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeSpecificationHandle(2));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeSpecificationHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeReferenceHandle(1));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeSpecificationHandle(1));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeReferenceHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeSpecificationHandle(1));
            builder.AddInterfaceImplementation(MetadataTokens.TypeDefinitionHandle(1), MetadataTokens.TypeDefinitionHandle(1));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }

        [Fact]
        public void ValidateMethodImplTable()
        {
            var builder = new MetadataBuilder();
            builder.AddMethodImplementation(MetadataTokens.TypeDefinitionHandle(2), default(MethodDefinitionHandle), default(MethodDefinitionHandle));
            builder.AddMethodImplementation(MetadataTokens.TypeDefinitionHandle(1), default(MethodDefinitionHandle), default(MethodDefinitionHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddMethodImplementation(MetadataTokens.TypeDefinitionHandle(1), default(MethodDefinitionHandle), default(MethodDefinitionHandle));
            builder.AddMethodImplementation(MetadataTokens.TypeDefinitionHandle(1), default(MethodDefinitionHandle), default(MethodDefinitionHandle));
            builder.ValidateOrder(); // ok
        }

        [Fact]
        public void ValidateNestedClassTable()
        {
            var builder = new MetadataBuilder();
            builder.AddNestedType(MetadataTokens.TypeDefinitionHandle(2), default(TypeDefinitionHandle));
            builder.AddNestedType(MetadataTokens.TypeDefinitionHandle(1), default(TypeDefinitionHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddNestedType(MetadataTokens.TypeDefinitionHandle(1), default(TypeDefinitionHandle));
            builder.AddNestedType(MetadataTokens.TypeDefinitionHandle(1), default(TypeDefinitionHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }

        [Fact]
        public void ValidateLocalScopeTable()
        {
            var builder = new MetadataBuilder();
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(2), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 0, length: 1);
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 0, length: 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 1, length: 1);
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 0, length: 1);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 0, length: 1);
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 0, length: 2);
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 0, length: 1);
            builder.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), default(ImportScopeHandle), default(LocalVariableHandle), default(LocalConstantHandle), startOffset: 0, length: 1);
            builder.ValidateOrder(); // ok
        }

        [Fact]
        public void ValidateStateMachineMethodTable()
        {
            var builder = new MetadataBuilder();
            builder.AddStateMachineMethod(MetadataTokens.MethodDefinitionHandle(2), default(MethodDefinitionHandle));
            builder.AddStateMachineMethod(MetadataTokens.MethodDefinitionHandle(1), default(MethodDefinitionHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());

            builder = new MetadataBuilder();
            builder.AddStateMachineMethod(MetadataTokens.MethodDefinitionHandle(1), default(MethodDefinitionHandle));
            builder.AddStateMachineMethod(MetadataTokens.MethodDefinitionHandle(1), default(MethodDefinitionHandle));
            Assert.Throws<InvalidOperationException>(() => builder.ValidateOrder());
        }
    }
}

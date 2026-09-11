// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class TypeDefinitionTests
    {
        [Fact]
        public void SingleTypeNoMembers()
        {
            string source = """
                .class public auto ansi sealed beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // One for the <Module> type, one for Test.
            Assert.Equal(2, reader.TypeDefinitions.Count);
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal(string.Empty, reader.GetString(typeDef.Namespace));
            Assert.Equal("Test", reader.GetString(typeDef.Name));
        }


        [Fact]
        public void TypeNotFound_CreatesForwardReference()
        {
            // Referencing a type that doesn't exist creates a forward reference placeholder,
            // matching native ilasm behavior where types can be referenced before declaration.
            string source = """
                .class public auto ansi sealed beforefieldinit Test extends NonExistentType
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void MultipleTypeNotFound_CreatesForwardReferences()
        {
            // Multiple references to non-existent types create forward reference placeholders,
            // matching native ilasm behavior
            string source = """
                .class public auto ansi beforefieldinit Test extends NonExistentBase implements NonExistentInterface
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void Diagnostic_NoBaseType()
        {
            // Using .base when the current type has no base type (interface)
            string source = """
                .assembly extern mscorlib { }
                .class interface public abstract auto ansi Test
                {
                    .class interface nested public abstract auto ansi Nested
                        implements .base
                    {
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.NoBaseType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void Diagnostic_UnsealedValueType()
        {
            // A value type that extends System.ValueType but is not sealed (warning, auto-sealed)
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public sequential ansi beforefieldinit MyStruct
                    extends [System.Runtime]System.ValueType
                {
                    .field public int32 value
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void ClassLayout_PackAndSize()
        {
            // Test .pack and .size directives for explicit struct layout
            string source = """
                .class public sequential ansi sealed beforefieldinit TestStruct
                    extends [System.Runtime]System.ValueType
                {
                    .pack 4
                    .size 16
                    .field public int32 field1
                }
                .assembly extern System.Runtime { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeHandle = reader.TypeDefinitions
                .First(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "TestStruct");

            var layout = reader.GetTypeDefinition(typeHandle).GetLayout();
            Assert.Equal(4, layout.PackingSize);
            Assert.Equal(16, (int)layout.Size);
        }


        [Fact]
        public void ExplicitLayout_SetsTypeLayoutFlags()
        {
            // Test that ExplicitLayout (0x10) is set for structs with field offsets
            string source = """
                .class public explicit ansi sealed beforefieldinit Test
                    extends [System.Runtime]System.ValueType
                {
                    .field [0] public int32 x
                    .field [4] public int32 y
                }
                .assembly extern System.Runtime { }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);

            var blobBuilder = new System.Reflection.Metadata.BlobBuilder();
            result.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.TypeDefinitions
                .Select(h => reader.GetTypeDefinition(h))
                .First(t => reader.GetString(t.Name) == "Test");

            Assert.True(typeDef.Attributes.HasFlag(System.Reflection.TypeAttributes.ExplicitLayout),
                $"Expected ExplicitLayout, got {typeDef.Attributes}");
        }


        [Fact]
        public void ExtendedLayout_SetsExtendedLayoutFlag()
        {
            // Test the 'extended' class attribute (exercises MetadataExtensions.ExtendedLayout)
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public extended ansi sealed beforefieldinit Test
                    extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.TypeDefinitions
                .Select(h => reader.GetTypeDefinition(h))
                .First(t => reader.GetString(t.Name) == "Test");

            // ExtendedLayout = 0x18
            Assert.Equal((System.Reflection.TypeAttributes)0x18, typeDef.Attributes & (System.Reflection.TypeAttributes)0x18);
        }


        [Fact]
        public void UnsignedIntTypes_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static unsigned int8 f1
                    .field public static unsigned int16 f2
                    .field public static unsigned int32 f3
                    .field public static unsigned int64 f4
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var fields = typeDef.GetFields().ToArray();
            Assert.Equal(4, fields.Length);
            // unsigned int8 → Byte (0x05)
            var sig1 = reader.GetBlobReader(reader.GetFieldDefinition(fields[0]).Signature);
            Assert.Equal(0x06, sig1.ReadByte()); // FIELD
            Assert.Equal(0x05, sig1.ReadByte()); // ELEMENT_TYPE_U1
            // unsigned int16 → UInt16 (0x07)
            var sig2 = reader.GetBlobReader(reader.GetFieldDefinition(fields[1]).Signature);
            Assert.Equal(0x06, sig2.ReadByte());
            Assert.Equal(0x07, sig2.ReadByte()); // ELEMENT_TYPE_U2
            // unsigned int32 → UInt32 (0x09)
            var sig3 = reader.GetBlobReader(reader.GetFieldDefinition(fields[2]).Signature);
            Assert.Equal(0x06, sig3.ReadByte());
            Assert.Equal(0x09, sig3.ReadByte()); // ELEMENT_TYPE_U4
            // unsigned int64 → UInt64 (0x0B)
            var sig4 = reader.GetBlobReader(reader.GetFieldDefinition(fields[3]).Signature);
            Assert.Equal(0x06, sig4.ReadByte());
            Assert.Equal(0x0B, sig4.ReadByte()); // ELEMENT_TYPE_U8
        }


        [Fact]
        public void ArrayBoundsType_ZeroBased()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestArrayBounds { }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public int32[0...] m_arr
                    .method public hidebysig instance void M() cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void ArrayBoundsType_MultiDimensional()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestArrayBounds { }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(int32[5...,3...] arr) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void RefanyType_Accepted()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestRefany { }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals (int32, refany)
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void ClassVisibility_PublicIsPreserved()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi beforefieldinit PublicType extends [mscorlib]System.Object { }
                .class private auto ansi beforefieldinit PrivateType extends [mscorlib]System.Object { }
                .class auto ansi beforefieldinit DefaultType extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var pub = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("PublicType", reader.GetString(pub.Name));
            Assert.Equal(TypeAttributes.Public, pub.Attributes & TypeAttributes.VisibilityMask);

            var priv = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(3));
            Assert.Equal("PrivateType", reader.GetString(priv.Name));
            Assert.Equal(TypeAttributes.NotPublic, priv.Attributes & TypeAttributes.VisibilityMask);

            var def = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(4));
            Assert.Equal("DefaultType", reader.GetString(def.Name));
            Assert.Equal(TypeAttributes.NotPublic, def.Attributes & TypeAttributes.VisibilityMask);
        }


        [Fact]
        public void DottedName_SQStringQuotesStripped()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly 'My-Assembly' { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var asmDef = reader.GetAssemblyDefinition();
            Assert.Equal("My-Assembly", reader.GetString(asmDef.Name));
        }

        [Fact]
        public void DottedName_SQStringSegmentQuotesStripped()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly tls2 { }
                .class public auto ansi beforefieldinit 'tls'.tls2
                    extends [mscorlib]System.Object
                {
                    .field public static uint8 b
                    .method private hidebysig specialname rtspecialname static void .cctor() cil managed
                    {
                        ldc.i4.1
                        stsfld uint8 'tls'.tls2::b
                        ret
                    }
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("tls", reader.GetString(typeDef.Namespace));
            Assert.Equal("tls2", reader.GetString(typeDef.Name));
        }


        [Fact]
        public void Interface_NoImplicitBaseType()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class interface public abstract auto ansi IMyInterface
                {
                    .method public hidebysig newslot abstract virtual instance void DoWork() cil managed { }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("IMyInterface", reader.GetString(typeDef.Name));
            Assert.True(typeDef.Attributes.HasFlag(TypeAttributes.Interface));
            Assert.True(typeDef.BaseType.IsNil);
        }


        [Fact]
        public void TypeName_NoDotPrefix()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi beforefieldinit MyNamespace.MyType extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("MyType", reader.GetString(typeDef.Name));
            Assert.Equal("MyNamespace", reader.GetString(typeDef.Namespace));
        }


        [Fact]
        public void Namespace_NoLeadingDot()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .namespace System.Tests
                {
                    .class public auto ansi beforefieldinit MyType extends [mscorlib]System.Object { }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("MyType", reader.GetString(typeDef.Name));
            Assert.Equal("System.Tests", reader.GetString(typeDef.Namespace));
        }


        [Fact]
        public void LeadingDotInTypeName_Preserved()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public sequential ansi sealed '.GlobalStructStartingWithDot'
                    extends [mscorlib]System.ValueType
                {
                    .field public int32 Value
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal(".GlobalStructStartingWithDot", reader.GetString(typeDef.Name));
        }


        [Theory]
        [InlineData("class [mscorlib]System.String", SignatureTypeCode.String)]
        [InlineData("class [mscorlib]System.Object", SignatureTypeCode.Object)]
        public void WellKnownClassType_UsesPrimitiveTypeCode(string ilType, SignatureTypeCode expectedCode)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static {{ilType}} myField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigReader = reader.GetBlobReader(field.Signature);
            sigReader.ReadByte(); // field signature header (0x06)
            byte typeCode = sigReader.ReadByte();
            Assert.Equal((byte)expectedCode, typeCode);
        }


        [Theory]
        [InlineData("valuetype [mscorlib]System.Boolean", SignatureTypeCode.Boolean)]
        [InlineData("valuetype [mscorlib]System.Int32", SignatureTypeCode.Int32)]
        [InlineData("valuetype [mscorlib]System.Int64", SignatureTypeCode.Int64)]
        [InlineData("valuetype [mscorlib]System.Single", SignatureTypeCode.Single)]
        [InlineData("valuetype [mscorlib]System.Double", SignatureTypeCode.Double)]
        [InlineData("valuetype [mscorlib]System.Char", SignatureTypeCode.Char)]
        [InlineData("valuetype [mscorlib]System.Byte", SignatureTypeCode.Byte)]
        [InlineData("valuetype [mscorlib]System.IntPtr", SignatureTypeCode.IntPtr)]
        public void WellKnownValueType_UsesPrimitiveTypeCode(string ilType, SignatureTypeCode expectedCode)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static {{ilType}} myField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigReader = reader.GetBlobReader(field.Signature);
            sigReader.ReadByte(); // field signature header
            byte typeCode = sigReader.ReadByte();
            Assert.Equal((byte)expectedCode, typeCode);
        }


        [Fact]
        public void ExplicitLayout_EmitsClassLayoutWithDefaultValues()
        {
            // Types with explicit layout should emit a ClassLayout row
            // even when .pack and .size are not specified, matching native ilasm.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public explicit sealed ansi Test extends [mscorlib]System.ValueType
                {
                    .field [0] public int32 x
                    .field [4] public int32 y
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // ClassLayout table should have an entry for the explicit layout type
            int classLayoutCount = reader.GetTableRowCount(TableIndex.ClassLayout);
            Assert.True(classLayoutCount >= 1, $"ClassLayout table should have at least 1 entry for explicit layout type, has {classLayoutCount}");

            // Verify the layout has default values (pack=0, size=0)
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var layout = typeDef.GetLayout();
            Assert.Equal(0, layout.PackingSize);
            Assert.Equal(0, layout.Size);
        }


        [Fact]
        public void SequentialLayout_NoClassLayoutWithoutPackOrSize()
        {
            // Types with sequential layout should NOT emit ClassLayout
            // unless .pack or .size is explicitly specified.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public sequential sealed ansi Test extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                    .field public int32 y
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // ClassLayout table should have NO entries for sequential layout without .pack/.size
            int classLayoutCount = reader.GetTableRowCount(TableIndex.ClassLayout);
            Assert.Equal(0, classLayoutCount);
        }


        [Fact]
        public void ExplicitLayout_WithPackAndSize_EmitsSpecifiedValues()
        {
            // When .pack and .size are explicitly set, those values should be emitted.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public explicit sealed ansi Test extends [mscorlib]System.ValueType
                {
                    .pack 4
                    .size 16
                    .field [0] public int32 x
                    .field [4] public int32 y
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var layout = typeDef.GetLayout();
            Assert.Equal(4, layout.PackingSize);
            Assert.Equal(16, layout.Size);
        }

    }
}

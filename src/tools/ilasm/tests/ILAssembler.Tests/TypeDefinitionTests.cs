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

namespace Microsoft.DotNet.ILAssembler.Tests
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


        [Theory]
        [InlineData(".assembly netstandard { }")]
        [InlineData(".mscorlib\n.assembly netstandard { }")]
        [InlineData(".assembly mscorlib { }")]
        [InlineData(".assembly System.Private.CoreLib { }")]
        [InlineData(".assembly System.Runtime { }")]
        [InlineData(".assembly NETSTANDARD { }")]
        [InlineData(".assembly extern netstandard { }\n.assembly netstandard { }", 1)]
        [InlineData(".mscorlib\n.assembly test { }")]
        public void SystemObjectInCoreLibrary_HasNoBaseType(string assembly, int assemblyReferenceCount = 0)
        {
            string source = $$"""
                {{assembly}}
                .class public System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));

            Assert.Equal("System", reader.GetString(type.Namespace));
            Assert.Equal("Object", reader.GetString(type.Name));
            Assert.Equal(TypeAttributes.Public, type.Attributes);
            Assert.True(type.BaseType.IsNil, $"Unexpected base token: 0x{MetadataTokens.GetToken(type.BaseType):X8}");
            Assert.Equal(assemblyReferenceCount, reader.AssemblyReferences.Count);
        }

        [Theory]
        [InlineData("extends System.Object", "")]
        [InlineData("extends Other", "")]
        [InlineData("", ".class public System.Object extends Other { }")]
        [InlineData("", ".class public System.Object { }")]
        public void SystemObjectInCoreLibrary_ExplicitOrRepeatedBaseIsIgnored(string baseClause, string redeclaration)
        {
            string source = $$"""
                .mscorlib
                .assembly netstandard { }
                .class public System.Object {{baseClause}} { }
                {{redeclaration}}
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));

            Assert.True(type.BaseType.IsNil);
        }

        [Theory]
        [InlineData(".assembly test { }", ".class public System.Object { }", "", false)]
        [InlineData(".assembly System.Runtime.Other { }", ".class public System.Object { }", "", false)]
        [InlineData(".assembly netstandard { }", ".class public Other.Object { }", "", true)]
        [InlineData(".assembly netstandard { }", ".class public System.Other { }", "", true)]
        [InlineData(".assembly netstandard { }", ".class public System.object { }", "", true)]
        [InlineData(".assembly netstandard { }", ".class public system.Object { }", "", true)]
        [InlineData(".mscorlib\n.assembly test { }", ".class public Outer { .class nested public System.Object { }", "}", true)]
        public void OtherClasses_KeepImplicitObjectBase(string assembly, string declaration, string closingDeclaration, bool localBase)
        {
            string source = $$"""
                {{assembly}}
                {{declaration}}
                {{closingDeclaration}}
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(
                declaration.Contains("nested") ? 4 : 2));
            if (localBase)
            {
                Assert.Equal(HandleKind.TypeDefinition, type.BaseType.Kind);
                TypeDefinition baseDefinition = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
                Assert.Equal("System", reader.GetString(baseDefinition.Namespace));
                Assert.Equal("Object", reader.GetString(baseDefinition.Name));
                Assert.True(baseDefinition.GetDeclaringType().IsNil);
                Assert.Empty(reader.AssemblyReferences);
                return;
            }
            Assert.Equal("Object", GetBaseTypeName(reader, type));
            TypeReference baseType = reader.GetTypeReference((TypeReferenceHandle)type.BaseType);
            Assert.Equal("System", reader.GetString(baseType.Namespace));
            Assert.Equal(HandleKind.AssemblyReference, baseType.ResolutionScope.Kind);
            AssemblyReference assemblyReference = reader.GetAssemblyReference((AssemblyReferenceHandle)baseType.ResolutionScope);
            Assert.Equal("mscorlib", reader.GetString(assemblyReference.Name));
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
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public sequential ansi beforefieldinit MyStruct
                    extends [System.Runtime]System.ValueType
                {
                    .field public int32 'value'
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("", true)]
        [InlineData("sealed", false)]
        [InlineData("sealed", true)]
        public void ValueType_PreservesSealedAttributeInErrorTolerantMode(string attributes, bool errorTolerant)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public {{attributes}} MyStruct extends [mscorlib]System.ValueType { }
                """;

            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { ErrorTolerant = errorTolerant });
            bool sealedType = attributes == "sealed";
            if (sealedType)
            {
                Assert.Empty(diagnostics);
            }
            else
            {
                Diagnostic diagnostic = Assert.Single(diagnostics);
                Assert.Equal(DiagnosticIds.UnsealedValueType, diagnostic.Id);
                Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            }

            if (!sealedType && !errorTolerant)
            {
                Assert.Null(result);
                return;
            }

            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal(sealedType, type.Attributes.HasFlag(TypeAttributes.Sealed));
        }

        [Theory]
        [InlineData(".mscorlib\n.assembly System.Runtime { }", "System.Enum", "System.ValueType")]
        [InlineData(".mscorlib\n.assembly System.Runtime { }", "System.Enum", "[mscorlib]System.ValueType")]
        [InlineData(".assembly mscorlib { }", "System.Enum", "System.ValueType")]
        [InlineData(".assembly System.Private.CoreLib { }", "System.Enum", "System.ValueType")]
        [InlineData(".assembly System.Runtime { }", "System.Enum", "System.ValueType")]
        [InlineData(".assembly netstandard { }", "System.Enum", "System.ValueType")]
        [InlineData(".assembly MSCORLIB { }", "System.Enum", "System.ValueType")]
        [InlineData(".assembly SYSTEM.PRIVATE.CORELIB { }", "System.Enum", "System.ValueType")]
        [InlineData(".assembly SYSTEM.RUNTIME { }", "System.Enum", "System.ValueType")]
        [InlineData(".assembly NETSTANDARD { }", "System.Enum", "System.ValueType")]
        [InlineData(".mscorlib\n.assembly test { }", "System.Enum", "System.ValueType")]
        public void SystemEnumInCoreLibrary_IsNotSealed(string assembly, string name, string baseType)
        {
            string source = $$"""
                {{assembly}}
                .class public abstract auto ansi beforefieldinit {{name}}
                    extends {{baseType}}
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));

            Assert.Equal("System", reader.GetString(type.Namespace));
            Assert.Equal("Enum", reader.GetString(type.Name));
            Assert.Equal(TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit, type.Attributes);
            if (baseType.StartsWith("[", StringComparison.Ordinal))
            {
                Assert.Equal("ValueType", GetBaseTypeName(reader, type));
            }
            else
            {
                Assert.Equal(HandleKind.TypeDefinition, type.BaseType.Kind);
                TypeDefinition baseDefinition = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
                Assert.Equal("System", reader.GetString(baseDefinition.Namespace));
                Assert.Equal("ValueType", reader.GetString(baseDefinition.Name));
                Assert.Empty(reader.AssemblyReferences);
            }
        }

        [Theory]
        [InlineData(".assembly test { }", ".class public abstract System.Enum")]
        [InlineData(".assembly extern mscorlib { }\n.assembly test { }", ".class public abstract System.Enum")]
        [InlineData(".assembly System.Runtime.Other { }", ".class public abstract System.Enum")]
        [InlineData(".assembly extern System.Runtime { }\n.assembly test { }", ".class public abstract System.Enum")]
        [InlineData(".mscorlib\n.assembly test { }", ".class public abstract Other.Enum")]
        [InlineData(".mscorlib\n.assembly test { }", ".class public abstract System.Other")]
        [InlineData(".mscorlib\n.assembly test { }", ".class public Outer { .class nested public abstract System.Enum", "}")]
        [InlineData(".mscorlib\n.assembly test { }", ".class public MyStruct")]
        [InlineData(".mscorlib\n.assembly test { }", ".class flags(0x80000081) System.Enum")]
        public void UnsealedValueTypeOtherThanCoreLibrarySystemEnum_IsReported(
            string assembly, string declaration, string closingDeclaration = "")
        {
            string source = $$"""
                {{assembly}}
                {{declaration}} extends System.ValueType { }
                {{closingDeclaration}}
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("sealed", true)]
        public void UserEnum_PreservesSealedAttribute(string attributes, bool sealedType)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public {{attributes}} UserEnum extends [mscorlib]System.Enum
                {
                    .field public specialname rtspecialname int32 value__
                    .field public static literal valuetype UserEnum Zero = int32(0)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));

            Assert.Equal(sealedType, type.Attributes.HasFlag(TypeAttributes.Sealed));
            Assert.Equal("Enum", GetBaseTypeName(reader, type));
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

            var sourceText = new Microsoft.DotNet.ILAssembler.SourceText(source, "test.il");
            var compiler = new Microsoft.DotNet.ILAssembler.DocumentCompiler();
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
            // Test the 'extended' class attribute.
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
        public void TypeName_QuotedNamespaceSegment()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi beforefieldinit 'tls'.tls1 extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("tls1", reader.GetString(typeDef.Name));
            Assert.Equal("tls", reader.GetString(typeDef.Namespace));
        }

        [Fact]
        public void TypeName_QuotedStartupCodeNamespaceSegment()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class private abstract auto ansi sealed '<StartupCode$Test>'.$Test extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("$Test", reader.GetString(typeDef.Name));
            Assert.Equal("<StartupCode$Test>", reader.GetString(typeDef.Namespace));
        }

        [Fact]
        public void TypeName_QuotedSegmentAfterDottedName()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi beforefieldinit System.Collections.'Type' extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("Type", reader.GetString(typeDef.Name));
            Assert.Equal("System.Collections", reader.GetString(typeDef.Namespace));
        }

        [Fact]
        public void TypeName_EmptyQuotedNamespaceSegmentPreservesSeparator()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi beforefieldinit ''.Foo.'Bar' extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("Bar", reader.GetString(typeDef.Name));
            Assert.Equal(".Foo", reader.GetString(typeDef.Namespace));
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
        public void Namespace_DottedTypeName_ComposesWithSeparator()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .namespace Outer
                {
                    .class public auto ansi beforefieldinit Inner.Type extends [mscorlib]System.Object { }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("Type", reader.GetString(typeDef.Name));
            Assert.Equal("Outer.Inner", reader.GetString(typeDef.Namespace));
        }

        [Theory]
        [InlineData(null, "TestType", "", "TestType")]
        [InlineData(null, ".TestType", "", ".TestType")]
        [InlineData(null, "..TestType", "", ".TestType")]
        [InlineData(null, "...TestType", ".", ".TestType")]
        [InlineData(null, "N..TestType", "N", ".TestType")]
        [InlineData(null, "N...TestType", "N.", ".TestType")]
        [InlineData(null, "ILDisassembler.TestStruct", "ILDisassembler", "TestStruct")]
        [InlineData(null, "Library.Not+Nested", "Library", "Not+Nested")]
        [InlineData(null, "<StartupCode$FSharp-Core>.$Prim-types", "<StartupCode$FSharp-Core>", "$Prim-types")]
        [InlineData(null, ".GlobalStructStartingWithDot", "", ".GlobalStructStartingWithDot")]
        [InlineData("", "TestType", "", "TestType")]
        [InlineData(".N", "TestType", ".N", "TestType")]
        [InlineData(".", "TestType", "", ".TestType")]
        [InlineData("..", "TestType", ".", ".TestType")]
        [InlineData("..N", "TestType", "..N", "TestType")]
        [InlineData("N.M", "TestType", "N.M", "TestType")]
        [InlineData("N.M.", "TestType", "N.M", ".TestType")]
        [InlineData("N.M..", "TestType", "N.M.", ".TestType")]
        [InlineData("N", ".TestType", "N", ".TestType")]
        [InlineData("N.", ".TestType", "N.", ".TestType")]
        [InlineData(".", ".TestType", ".", ".TestType")]
        [InlineData("A.B.C", "D.E.F", "A.B.C.D.E", "F")]
        public void NamespaceAndTypeName_SplitsBasedOnFullName(
            string? namespaceName,
            string className,
            string expectedNamespace,
            string expectedTypeName)
        {
            string fullName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly extern Other { }
                .assembly Test { }
                .class public Holder
                {
                    .field public class '{{fullName}}' Local
                    .field public class [Other]'{{fullName}}' External
                }
                {{(namespaceName is null ? "" : $".namespace '{namespaceName}' {{")}}
                .class public '{{className}}' { }
                {{(namespaceName is null ? "" : "}")}}
                .class extern forwarder '{{fullName}}'
                {
                    .assembly extern Other
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();

            Assert.Equal(3, reader.TypeDefinitions.Count);
            TypeDefinitionHandle typeHandle = MetadataTokens.TypeDefinitionHandle(3);
            TypeDefinition typeDefinition = reader.GetTypeDefinition(typeHandle);
            Assert.Equal(expectedNamespace, reader.GetString(typeDefinition.Namespace));
            Assert.Equal(expectedTypeName, reader.GetString(typeDefinition.Name));

            BlobReader localSignature = reader.GetBlobReader(reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1)).Signature);
            Assert.Equal(SignatureKind.Field, localSignature.ReadSignatureHeader().Kind);
            Assert.Equal(SignatureTypeCode.TypeHandle, localSignature.ReadSignatureTypeCode());
            Assert.Equal((EntityHandle)typeHandle, localSignature.ReadTypeHandle());
            Assert.Equal(0, localSignature.RemainingBytes);

            BlobReader externalSignature = reader.GetBlobReader(reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(2)).Signature);
            Assert.Equal(SignatureKind.Field, externalSignature.ReadSignatureHeader().Kind);
            Assert.Equal(SignatureTypeCode.TypeHandle, externalSignature.ReadSignatureTypeCode());
            EntityHandle externalHandle = externalSignature.ReadTypeHandle();
            Assert.Equal(HandleKind.TypeReference, externalHandle.Kind);
            Assert.Equal(0, externalSignature.RemainingBytes);
            TypeReference typeReference = reader.GetTypeReference((TypeReferenceHandle)externalHandle);
            Assert.Equal(expectedNamespace, reader.GetString(typeReference.Namespace));
            Assert.Equal(expectedTypeName, reader.GetString(typeReference.Name));

            ExportedType exportedType = reader.GetExportedType(Assert.Single(reader.ExportedTypes));
            Assert.Equal(expectedNamespace, reader.GetString(exportedType.Namespace));
            Assert.Equal(expectedTypeName, reader.GetString(exportedType.Name));
        }

        [Fact]
        public void EmptyNamespaceNames_EmitExpectedMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public A { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                .namespace '.N'
                {
                    .class public B { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                }
                .namespace '.'
                {
                    .class public C { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                }
                .namespace '..'
                {
                    .class public D { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                }
                .namespace '..N'
                {
                    .class public E { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                }
                .namespace N.M
                {
                    .class public F { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                }
                .namespace 'N.M.'
                {
                    .class public G { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                }
                .namespace 'N.M..'
                {
                    .class public H { .method public hidebysig specialname rtspecialname instance void .ctor() { ret } }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            Assert.Equal(
                new[]
                {
                    ("", "<Module>"), ("", "A"), (".N", "B"), ("", ".C"), (".", ".D"),
                    ("..N", "E"), ("N.M", "F"), ("N.M", ".G"), ("N.M.", ".H")
                },
                reader.TypeDefinitions.Select(handle =>
                {
                    TypeDefinition type = reader.GetTypeDefinition(handle);
                    return (reader.GetString(type.Namespace), reader.GetString(type.Name));
                }));
        }

        [Theory]
        [InlineData("Inner", "", "Inner")]
        [InlineData(".Inner", "", ".Inner")]
        [InlineData("..Inner", "", ".Inner")]
        [InlineData("...Inner", ".", ".Inner")]
        [InlineData("N.Inner", "N", "Inner")]
        [InlineData("N..Inner", "N", ".Inner")]
        public void NestedTypeName_SplitsNamespaceAndName_WithoutCurrentNamespace(
            string className,
            string expectedNamespace,
            string expectedTypeName)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly Test { }
                .namespace Ignored
                {
                    .class public Outer
                    {
                        .class nested public '{{className}}' { }
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            Assert.Equal(3, reader.TypeDefinitions.Count);
            TypeDefinition nestedType = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(3));
            Assert.Equal(expectedNamespace, reader.GetString(nestedType.Namespace));
            Assert.Equal(expectedTypeName, reader.GetString(nestedType.Name));
            Assert.Equal(MetadataTokens.TypeDefinitionHandle(2), nestedType.GetDeclaringType());
        }

        [Theory]
        [InlineData(".", "T", "", ".T", "", ".T")]
        [InlineData("N.", "T", "N", ".T", "N", ".T")]
        [InlineData("N", "..T", "N..", "T", "N.", ".T")]
        [InlineData("A.B.C", "D.E.F", "A.B.C.D.E", "F", "A.B.C.D.E", "F")]
        public void NormalizedTypeDefinitions_Merge(
            string firstNamespace,
            string firstName,
            string secondNamespace,
            string secondName,
            string expectedNamespace,
            string expectedTypeName)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly Test { }
                .namespace '{{firstNamespace}}'
                {
                    .class public '{{firstName}}' { .field public int32 X }
                }
                .namespace '{{secondNamespace}}'
                {
                    .class public '{{secondName}}' { .field public int32 Y }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            Assert.Equal(2, reader.TypeDefinitions.Count);
            TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal(expectedNamespace, reader.GetString(type.Namespace));
            Assert.Equal(expectedTypeName, reader.GetString(type.Name));
            Assert.Equal(new[] { "X", "Y" }, type.GetFields().Select(handle => reader.GetString(reader.GetFieldDefinition(handle).Name)));
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
        [InlineData("Boolean")]
        [InlineData("Byte")]
        [InlineData("Char")]
        [InlineData("Double")]
        [InlineData("Int16")]
        [InlineData("Int32")]
        [InlineData("Int64")]
        [InlineData("IntPtr")]
        [InlineData("SByte")]
        [InlineData("Single")]
        [InlineData("TypedReference")]
        [InlineData("UInt16")]
        [InlineData("UInt32")]
        [InlineData("UInt64")]
        [InlineData("UIntPtr")]
        [InlineData("Void")]
        public void WellKnownValueType_PreservesExplicitTypeReference(string typeName)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static valuetype [mscorlib]System.{{typeName}} myField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigReader = reader.GetBlobReader(field.Signature);
            Assert.Equal((byte)SignatureKind.Field, sigReader.ReadByte());
            Assert.Equal((byte)SignatureTypeKind.ValueType, sigReader.ReadByte());
            var typeHandle = (TypeReferenceHandle)sigReader.ReadTypeHandle();
            TypeReference type = reader.GetTypeReference(typeHandle);
            Assert.Equal("System", reader.GetString(type.Namespace));
            Assert.Equal(typeName, reader.GetString(type.Name));
            Assert.Equal("mscorlib", reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name));
            Assert.Equal(0, sigReader.RemainingBytes);
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

        [Fact]
        public void QuotedTypeName_WithConsecutiveDots_PreservesLeadingDotInName()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit 'Contoso.Tools..Nested' extends [mscorlib]System.Object
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.TypeDefinitions
                .Select(handle => reader.GetTypeDefinition(handle))
                .Single(type => reader.GetString(type.Name) == ".Nested");

            Assert.Equal(".Nested", reader.GetString(typeDef.Name));
            Assert.Equal("Contoso.Tools", reader.GetString(typeDef.Namespace));
        }

        [Fact]
        public void ClassAttributes_EmitExpectedFlagsAndImplicitBaseTypes()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi Outer extends [mscorlib]System.Object
                {
                    .class nested private auto ansi NestedPrivate extends [mscorlib]System.Object { }
                    .class nested family auto ansi NestedFamily extends [mscorlib]System.Object { }
                    .class nested assembly auto ansi NestedAssembly extends [mscorlib]System.Object { }
                    .class nested famandassem auto ansi NestedFamAndAssem extends [mscorlib]System.Object { }
                    .class nested famorassem auto ansi NestedFamOrAssem extends [mscorlib]System.Object { }
                }

                .class public auto unicode import serializable windowsruntime UnicodeImported extends [mscorlib]System.Object { }
                .class public value ValueTypeByKeyword { }
                .class public enum EnumTypeByKeyword { }
                .class flags(0x00100001) FlaggedType extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var types = reader.TypeDefinitions
                .Select(handle => (Handle: handle, Definition: reader.GetTypeDefinition(handle)))
                .ToDictionary(item => reader.GetString(item.Definition.Name));

            Assert.Equal(
                TypeAttributes.NestedPrivate,
                types["NestedPrivate"].Definition.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(
                TypeAttributes.NestedFamily,
                types["NestedFamily"].Definition.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(
                TypeAttributes.NestedAssembly,
                types["NestedAssembly"].Definition.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(
                TypeAttributes.NestedFamANDAssem,
                types["NestedFamAndAssem"].Definition.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(
                TypeAttributes.NestedFamORAssem,
                types["NestedFamOrAssem"].Definition.Attributes & TypeAttributes.VisibilityMask);

            TypeAttributes unicodeImported = types["UnicodeImported"].Definition.Attributes;
            Assert.Equal(TypeAttributes.UnicodeClass, unicodeImported & TypeAttributes.StringFormatMask);
            Assert.True(unicodeImported.HasFlag(TypeAttributes.Import));
            Assert.True(unicodeImported.HasFlag(TypeAttributes.Serializable));
            Assert.True(unicodeImported.HasFlag(TypeAttributes.WindowsRuntime));

            Assert.True(types["ValueTypeByKeyword"].Definition.Attributes.HasFlag(TypeAttributes.Sealed));
            Assert.Equal("ValueType", GetBaseTypeName(reader, types["ValueTypeByKeyword"].Definition));
            Assert.Equal("Enum", GetBaseTypeName(reader, types["EnumTypeByKeyword"].Definition));

            Assert.Equal((TypeAttributes)0x00100001, types["FlaggedType"].Definition.Attributes);
        }

        [Fact]
        public void LegacyClassFlagSentinels_EmitImplicitValueTypeAndEnumBases()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class flags(0x80100001) FlagValueType { }
                .class flags(0x40100001) FlagEnumType { }
                """;

            var compiler = new DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == DiagnosticIds.UnsealedValueType);
            Assert.NotNull(result);

            var image = new BlobBuilder();
            result!.Serialize(image);
            using var pe = new PEReader(image.ToImmutableArray());
            var reader = pe.GetMetadataReader();
            var types = reader.TypeDefinitions
                .Select(handle => reader.GetTypeDefinition(handle))
                .ToDictionary(type => reader.GetString(type.Name));

            Assert.True(types["FlagValueType"].Attributes.HasFlag(TypeAttributes.BeforeFieldInit));
            Assert.False(types["FlagValueType"].Attributes.HasFlag(TypeAttributes.Sealed));
            Assert.Equal("ValueType", GetBaseTypeName(reader, types["FlagValueType"]));
            Assert.True(types["FlagEnumType"].Attributes.HasFlag(TypeAttributes.BeforeFieldInit));
            Assert.Equal("Enum", GetBaseTypeName(reader, types["FlagEnumType"]));
        }

        [Fact]
        public void NestedType_EmitsDeclaringTypeMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Outer extends [mscorlib]System.Object
                {
                    .class nested public auto ansi beforefieldinit Inner extends [mscorlib]System.Object
                    {
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var outerHandle = reader.TypeDefinitions
                .First(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Outer");
            var innerHandle = reader.TypeDefinitions
                .First(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Inner");

            var inner = reader.GetTypeDefinition(innerHandle);
            Assert.Equal(TypeAttributes.NestedPublic, inner.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal("Outer", reader.GetString(reader.GetTypeDefinition(outerHandle).Name));
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.NestedClass));
        }

        [Fact]
        public void InterfaceImplementation_EmitsInterfaceImplRow()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class interface public abstract auto ansi IWorker
                {
                    .method public hidebysig newslot abstract virtual instance void Run() cil managed { }
                }
                .class public auto ansi beforefieldinit Worker extends [mscorlib]System.Object implements IWorker
                {
                    .method public hidebysig newslot virtual instance void Run() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var workerHandle = reader.TypeDefinitions
                .First(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Worker");
            var worker = reader.GetTypeDefinition(workerHandle);
            var interfaceImplHandle = Assert.Single(worker.GetInterfaceImplementations());
            var interfaceImpl = reader.GetInterfaceImplementation(interfaceImplHandle);

            Assert.Equal(HandleKind.TypeDefinition, interfaceImpl.Interface.Kind);
            Assert.Equal("IWorker", reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)interfaceImpl.Interface).Name));
        }

        [Fact]
        public void InterfaceImplementationDirective_EmitsCustomAttributeOnInterfaceImpl()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class interface public abstract auto ansi IWorker
                {
                }
                .class public auto ansi Worker extends [mscorlib]System.Object implements IWorker
                {
                    .interfaceimpl type IWorker
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var worker = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(definition => reader.GetString(definition.Name) == "Worker");
            var implementationHandle = Assert.Single(worker.GetInterfaceImplementations());
            var implementation = reader.GetInterfaceImplementation(implementationHandle);
            var attribute = reader.GetCustomAttribute(
                Assert.Single(reader.GetCustomAttributes(implementationHandle)));

            Assert.Equal(HandleKind.TypeDefinition, implementation.Interface.Kind);
            Assert.Equal(
                "IWorker",
                reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)implementation.Interface).Name));
            Assert.Equal(
                [0x01, 0x00, 0x00, 0x00],
                reader.GetBlobBytes(attribute.Value));
        }

        [Fact]
        public void ClassReferencedBeforeDeclaration_ExtendsObject()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public User extends [mscorlib]System.Object
                {
                    .field public class Later f
                }
                .class public Later { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var later = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "Later");

            Assert.Equal("Object", GetBaseTypeName(reader, later));
        }

        [Fact]
        public void ValueTypeReferencedBeforeDeclaration_ExtendsValueType()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public User extends [mscorlib]System.Object
                {
                    .field public valuetype Later f
                }
                .class public value sealed Later
                {
                    .field public int32 x
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var later = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "Later");

            Assert.Equal("ValueType", GetBaseTypeName(reader, later));
        }

        [Fact]
        public void UnsealedValueTypeReferencedBeforeDeclaration_IsReported()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public User extends [System.Runtime]System.Object
                {
                    .field public valuetype Later f
                }
                .class public sequential ansi beforefieldinit Later
                    extends [System.Runtime]System.ValueType
                {
                    .field public int32 x
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
        }

        [Fact]
        public void SecondDeclarationOfAGenericType_KeepsOneGenericParameter()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public Twice<T> extends [mscorlib]System.Object { }
                .class public Twice<T>
                {
                    .field public !T f
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var twice = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "Twice");

            Assert.Single(twice.GetGenericParameters());
            Assert.Equal("Object", GetBaseTypeName(reader, twice));
        }

        [Fact]
        public void ModuleTypeDeclaration_HasNoBaseType()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class '<Module>'
                {
                    .field static int32 g
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var module = reader.GetTypeDefinition(Assert.Single(reader.TypeDefinitions));

            Assert.Equal("<Module>", reader.GetString(module.Name));
            Assert.True(module.BaseType.IsNil);
        }

        [Fact]
        public void ClassOnlyImage_HasNoValueTypeReference()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public C extends [System.Runtime]System.Object { }
                .class public D { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.DoesNotContain(reader.TypeReferences, handle => reader.GetString(reader.GetTypeReference(handle).Name) == "ValueType");
        }

        [Theory]
        [InlineData(".assembly extern MSCORLIB { }", "MSCORLIB")]
        [InlineData(".assembly extern Other as mscorlib { }", "mscorlib")]
        [InlineData(".assembly extern System.Private.CoreLib { }", "System.Private.CoreLib")]
        [InlineData(".assembly extern netstandard { }", "netstandard")]
        public void UnsealedValueTypeInCoreLibFoundByLookup_IsReported(string coreLib, string scope)
        {
            string source = $$"""
                {{coreLib}}
                .assembly test { }
                .class public C { }
                .class public V extends [{{scope}}]System.ValueType { }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
        }

        [Theory]
        [InlineData(".class public C { }")]
        [InlineData(".class public value sealed W { }")]
        public void UnsealedValueTypeThroughEarlierTypedef_IsReported(string earlierType)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                {{earlierType}}
                .typedef [mscorlib]System.ValueType as OldValueType
                .assembly extern System.Runtime { }
                .class public V extends OldValueType { }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
        }

        [Theory]
        [InlineData(".class public C { }")]
        [InlineData(".class public value sealed W { }")]
        public void UnsealedValueTypeAfterLaterCoreLibReference_IsReported(string earlierType)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                {{earlierType}}
                .assembly extern System.Runtime { }
                .class public V extends [System.Runtime]System.ValueType { }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
        }

        private static string GetBaseTypeName(MetadataReader reader, TypeDefinition type)
        {
            Assert.Equal(HandleKind.TypeReference, type.BaseType.Kind);
            return reader.GetString(reader.GetTypeReference((TypeReferenceHandle)type.BaseType).Name);
        }

    }
}

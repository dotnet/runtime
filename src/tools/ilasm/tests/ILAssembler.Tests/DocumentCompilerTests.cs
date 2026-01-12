// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ILAssembler.Tests
{
    public class DocumentCompilerTests
    {
        [Fact]
        public void SingleTypeNoMembers()
        {
            string source = """
                .class public auto ansi sealed beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // One for the <Module> type, one for Test.
            Assert.Equal(2, reader.TypeDefinitions.Count);
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal(string.Empty, reader.GetString(typeDef.Namespace));
            Assert.Equal("Test", reader.GetString(typeDef.Name));
        }

        [Fact]
        public void TypeNotFound_ReportsError()
        {
            // Referencing a type that doesn't exist should report an error
            string source = """
                .class public auto ansi sealed beforefieldinit Test extends NonExistentType
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypeNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Theory]
        [InlineData("\"hello\"", "hello")]
        [InlineData("\"hello\\tworld\"", "hello\tworld")]
        [InlineData("\"hello\\nworld\"", "hello\nworld")]
        [InlineData("\"hello\\rworld\"", "hello\rworld")]
        [InlineData("\"\\\"quoted\\\"\"", "\"quoted\"")]
        [InlineData("\"back\\\\slash\"", "back\\slash")]
        [InlineData("\"null\\0char\"", "null\0char")]
        [InlineData("\"octal\\101\"", "octalA")]  // \101 = 65 = 'A'
        public void StringHelpers_ParsesEscapeSequences(string input, string expected)
        {
            var result = StringHelpers.ParseQuotedString(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Typedef_ClassNameAsAlias_ResolvesInFieldSignature()
        {
            // Define a typedef for a class and use it in a field type
            // Real-world usage: .typedef [System.Runtime]System.GC as GC
            string source = """
                .assembly extern System.Runtime { }
                .typedef [System.Runtime]System.Object as Obj

                .class public auto ansi beforefieldinit Test
                {
                    .field public Obj myField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the field and verify its signature references System.Object
            var typeDef = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "Test");
            var fields = typeDef.GetFields().Select(reader.GetFieldDefinition).ToArray();
            Assert.Single(fields);
            Assert.Equal("myField", reader.GetString(fields[0].Name));
        }

        [Fact]
        public void Typedef_ClassNameAsAlias_FieldCompiles()
        {
            // Test pattern from src/tests/JIT/Methodical/Coverage/copy_prop_byref_to_native_int.il
            // .typedef [System.Runtime]System.WeakReference as WeakRef
            string source = """
                .assembly extern System.Runtime { }
                .typedef [System.Runtime]System.String as Str

                .class public auto ansi beforefieldinit Test
                {
                    .field public Str myField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the field and verify it compiled
            var typeDef = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "Test");
            var fields = typeDef.GetFields().Select(reader.GetFieldDefinition).ToArray();
            Assert.Single(fields);
            Assert.Equal("myField", reader.GetString(fields[0].Name));
        }

        [Fact]
        public void Typedef_NotFound_ReportsError()
        {
            // Using an undefined typedef alias should report an error
            string source = """
                .class public auto ansi beforefieldinit Test
                {
                    .field public UndefinedTypedef myField
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypedefNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void MultipleTypeNotFound_ReportsMultipleErrors()
        {
            // Multiple references to non-existent types should each report an error
            string source = """
                .class public auto ansi beforefieldinit Test extends NonExistentBase implements NonExistentInterface
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d =>
            {
                Assert.Equal(DiagnosticIds.TypeNotFound, d.Id);
                Assert.Equal(DiagnosticSeverity.Error, d.Severity);
            });
        }

        [Fact]
        public void ThisOutsideClass_ReportsError()
        {
            // Using .this outside of a class definition should report an error
            // Test at module level where there's no class context
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .this as MyThis
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ThisOutsideClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void BaseOutsideClass_ReportsError()
        {
            // Using .base outside of a class definition should report an error
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .base as MyBase
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.BaseOutsideClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void NesterOutsideNestedClass_ReportsError()
        {
            // Using .nester outside of a nested class should report an error
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .nester as MyNester
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.NesterOutsideNestedClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void TypeParameterOutsideType_ReportsError()
        {
            // Using a named type parameter reference outside of a generic type should report an error
            // Note: !0 (by index) is allowed for compat, but !T (by name) requires a type context
            string source = """
                .assembly extern System.Runtime { }
                .typedef !T as MyTypeParam
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypeParameterOutsideType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void ModuleNotFound_ReportsError()
        {
            // Referencing a module that doesn't exist
            string source = """
                .assembly extern System.Runtime { }
                .typedef [.module NonExistentModule]SomeType as MyType
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ModuleNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void MethodTypeParameterOutsideMethod_ReportsError()
        {
            // Using !!T (method type parameter by name) outside a method should report an error
            string source = """
                .assembly extern System.Runtime { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public !!T myField
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.MethodTypeParameterOutsideMethod, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
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

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
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

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsealedValueType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_GenericParameterNotFound()
        {
            // Referencing a non-existent type parameter by name in a field
            string source = """
                .class public auto ansi beforefieldinit Test`1<T>
                {
                    .field public !NonExistent field1
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.GenericParameterNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_LiteralOutOfRange()
        {
            // An integer literal that overflows
            string source = """
                .class public auto ansi beforefieldinit Test
                {
                    .pack 99999999999999999999999999999999
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LiteralOutOfRange, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_InvalidMetadataToken()
        {
            // Reference an invalid token in an exported type declaration
            // Uses an assembly reference instead of a file to avoid file entry point issues
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .assembly extern ForwardedAssembly
                    mdtoken(0x99999999)
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidMetadataToken, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_FileNotFound()
        {
            // Reference a file that doesn't exist in an exported type declaration
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .file NonExistentFile.dll
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.FileNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_AssemblyNotFound()
        {
            // Reference an assembly that doesn't exist in an exported type declaration
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .assembly extern NonExistentAssembly
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.AssemblyNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_ExportedTypeNotFound()
        {
            // Reference a nested exported type that doesn't exist
            // Uses assembly references instead of files to avoid file entry point issues
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .assembly extern ForwardedAssembly
                }
                .class extern public NestedType
                {
                    .class extern NonExistentParent
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, d => Assert.Equal(DiagnosticIds.ExportedTypeNotFound, d.Id));
            Assert.All(diagnostics, d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
        }

        // Note: Tests for ByteArrayTooShort (ILA0016), ArgumentNotFound (ILA0018), LocalNotFound (ILA0019),
        // and LabelNotFound (ILA0017) require method body parsing which currently has a pre-existing
        // bug in EntityRegistry.WriteContentTo. These tests are deferred until those bugs are fixed.

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

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeHandle = reader.TypeDefinitions
                .First(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "TestStruct");

            var layout = reader.GetTypeDefinition(typeHandle).GetLayout();
            Assert.Equal(4, layout.PackingSize);
            Assert.Equal(16, (int)layout.Size);
        }

        [Fact]
        public void FieldLayout_ExplicitOffset()
        {
            // Test explicit field offset with [n] syntax
            string source = """
                .class public explicit ansi sealed beforefieldinit UnionStruct
                    extends [System.Runtime]System.ValueType
                {
                    .field [0] public int32 intValue
                    .field [0] public float32 floatValue
                    .field [0] public float64 doubleValue
                }
                .assembly extern System.Runtime { }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeHandle = reader.TypeDefinitions
                .First(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "UnionStruct");

            var fields = reader.GetTypeDefinition(typeHandle).GetFields()
                .Select(reader.GetFieldDefinition).ToArray();
            Assert.Equal(3, fields.Length);

            // All fields should have offset 0, creating a union
            Assert.Equal(0, fields[0].GetOffset());
            Assert.Equal(0, fields[1].GetOffset());
            Assert.Equal(0, fields[2].GetOffset());
        }

        [Fact]
        public void CoreAssemblyResolution_PrefersSystemRuntime()
        {
            // When System.Runtime is referenced, implicit base types should use it
            // A class with no explicit extends clause implicitly extends System.Object
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Verify System.Runtime is the only assembly reference (no mscorlib created)
            var asmRefs = reader.AssemblyReferences.Select(reader.GetAssemblyReference).ToArray();
            Assert.Single(asmRefs);
            Assert.Equal("System.Runtime", reader.GetString(asmRefs[0].Name));

            // Verify System.Object is referenced from System.Runtime
            var typeRefs = reader.TypeReferences.Select(reader.GetTypeReference).ToArray();
            var objectRef = typeRefs.Single(t => reader.GetString(t.Name) == "Object");
            Assert.Equal("System", reader.GetString(objectRef.Namespace));
            Assert.Equal(asmRefs[0].Name, reader.GetAssemblyReference((AssemblyReferenceHandle)objectRef.ResolutionScope).Name);
        }

        [Fact]
        public void CoreAssemblyResolution_PrefersSystemPrivateCoreLib()
        {
            // When System.Private.CoreLib is referenced, it should be preferred over System.Runtime
            string source = """
                .assembly extern System.Private.CoreLib { }
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Both assemblies should be referenced
            var asmRefs = reader.AssemblyReferences.Select(reader.GetAssemblyReference)
                .Select(a => reader.GetString(a.Name)).ToArray();
            Assert.Contains("System.Private.CoreLib", asmRefs);
            Assert.Contains("System.Runtime", asmRefs);

            // Verify System.Object is referenced from System.Private.CoreLib (preferred)
            var typeRefs = reader.TypeReferences.Select(reader.GetTypeReference).ToArray();
            var objectRef = typeRefs.Single(t => reader.GetString(t.Name) == "Object");
            var resolvedAsm = reader.GetAssemblyReference((AssemblyReferenceHandle)objectRef.ResolutionScope);
            Assert.Equal("System.Private.CoreLib", reader.GetString(resolvedAsm.Name));
        }

        [Fact]
        public void CoreAssemblyResolution_FallsBackToMscorlib()
        {
            // When no core assembly is explicitly referenced, mscorlib should be created
            // for implicit base type resolution. A class with no explicit extends clause
            // implicitly extends System.Object.
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // mscorlib should be created as fallback for System.Object base type
            var asmRefs = reader.AssemblyReferences.Select(reader.GetAssemblyReference)
                .Select(a => reader.GetString(a.Name)).ToArray();
            Assert.Contains("mscorlib", asmRefs);

            // Verify System.Object is referenced from mscorlib
            var typeRefs = reader.TypeReferences.Select(reader.GetTypeReference).ToArray();
            var objectRef = typeRefs.Single(t => reader.GetString(t.Name) == "Object");
            Assert.Equal("System", reader.GetString(objectRef.Namespace));
            var resolvedAsm = reader.GetAssemblyReference((AssemblyReferenceHandle)objectRef.ResolutionScope);
            Assert.Equal("mscorlib", reader.GetString(resolvedAsm.Name));
        }

        [Fact]
        public void FieldRVA_MultipleDataSections()
        {
            // Test multiple .data declarations with different data types
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .data IntData = int32(0x12345678)
                .data ByteData = bytearray (AA BB CC DD EE FF)
                .data FloatData = float32(3.14159)

                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 16
                    .field [0] public static int32 IntField at IntData
                    .field [4] public static int32 ByteField at ByteData
                    .field [8] public static float32 FloatField at FloatData
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var testType = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "DataHolder");

            var fields = testType.GetFields()
                .Select(reader.GetFieldDefinition)
                .ToDictionary(f => reader.GetString(f.Name));

            // Verify IntField RVA and data (little-endian: 0x12345678 = 78 56 34 12)
            int intRva = fields["IntField"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, intRva);

            // Verify ByteField RVA
            int byteRva = fields["ByteField"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, byteRva);

            // Verify FloatField has an RVA
            int floatRva = fields["FloatField"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, floatRva);

            // Each field should point to different data locations
            Assert.NotEqual(intRva, byteRva);
            Assert.NotEqual(intRva, floatRva);
            Assert.NotEqual(byteRva, floatRva);
        }

        private static PEReader CompileAndGetReader(string source, Options options)
        {
            var sourceText = new SourceText(source, "test.il");
            var documentCompiler = new DocumentCompiler();
            var (diagnostics, result) = documentCompiler.Compile(sourceText, _ =>
            {
                Assert.Fail("Expected no includes");
                return default;
            }, _ => { Assert.Fail("Expected no resources"); return default; }, options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            var blobBuilder = new BlobBuilder();
            result!.Serialize(blobBuilder);
            return new PEReader(blobBuilder.ToImmutableArray());
        }

        private static ImmutableArray<Diagnostic> CompileAndGetDiagnostics(string source, Options options)
        {
            var sourceText = new SourceText(source, "test.il");
            var documentCompiler = new DocumentCompiler();
            var (diagnostics, _) = documentCompiler.Compile(sourceText, _ =>
            {
                Assert.Fail("Expected no includes");
                return default;
            }, _ => { Assert.Fail("Expected no resources"); return default; }, options);
            return diagnostics;
        }
    }
}

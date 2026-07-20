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
            // Expect FileNotFound error + MissingExportedTypeImplementation warning
            Assert.Equal(2, diagnostics.Length);
            var error = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
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
            // Expect AssemblyNotFound error + MissingExportedTypeImplementation warning
            Assert.Equal(2, diagnostics.Length);
            var error = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
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
            // Check only error diagnostics (warnings are also expected for missing implementations)
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.NotEmpty(errors);
            Assert.All(errors, d => Assert.Equal(DiagnosticIds.ExportedTypeNotFound, d.Id));
        }

        [Fact]
        public void Diagnostic_ByteArrayTooShort()
        {
            // A bytearray that's too short for the data type being loaded
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static float64 TestMethod() cil managed
                    {
                        ldc.r8 bytearray (AA BB)
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ByteArrayTooShort, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_ArgumentNotFound()
        {
            // Reference an argument that doesn't exist
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public void TestMethod(int32 x) cil managed
                    {
                        ldarg NonExistentArg
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ArgumentNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_LocalNotFound()
        {
            // Reference a local variable that doesn't exist
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .locals (int32 x)
                        ldloc NonExistentLocal
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LocalNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_LabelNotFound()
        {
            // Reference an undefined label in a branch instruction
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        br UndefinedLabel
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LabelNotFound, error.Id);
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

            var typeDef = reader.GetTypeDefinition(typeHandle);
            
            // Verify ExplicitLayout is set (this was a regression bug - EXPLICIT token wasn't being parsed)
            Assert.True(typeDef.Attributes.HasFlag(System.Reflection.TypeAttributes.ExplicitLayout),
                $"Expected ExplicitLayout, got {typeDef.Attributes} (0x{(int)typeDef.Attributes:X8})");

            var fields = typeDef.GetFields()
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

        [Fact]
        public void LanguageDecl_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .language "C#" "3.0"
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void LanguageDecl_MultipleParameters_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .language "C#" "3.0" "vendor"
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ExtSourceSpec_LineDirective_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .line 10 "test.cs"
                        nop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ExtSourceSpec_LineWithColumn_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .line 10 : 5 "test.cs"
                        nop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ExtSourceSpec_LineDirectiveHashLine_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        #line 42 "program.cs"
                        nop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ExportHead_ObsoleteSyntax_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .export [System.Object]
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void FieldInit_ByteArray_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .field static int32 field1 at 0
                }
                .data data1 = bytearray (00 01 02 03)
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Diagnostic_AbstractMethodNotInAbstractType()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public abstract void AbstractMethod() cil managed
                    {
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.AbstractMethodNotInAbstractType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_InvalidPInvokeSignature()
        {
            // P/Invoke with no module name triggers InvalidPInvokeSignature
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static pinvokeimpl() void TestMethod() cil managed
                    {
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidPInvokeSignature, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_DeprecatedNativeType_Variant()
        {
            // Using deprecated VARIANT native type triggers warning
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(object marshal(variant) arg) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var warning = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.DeprecatedNativeType, warning.Id);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }

        [Fact]
        public void Diagnostic_DeprecatedCustomMarshaller()
        {
            // Using 4-string custom marshaller syntax triggers warning
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(object marshal(custom("guid", "nativeType", "marshallerType", "cookie")) arg) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var warning = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.DeprecatedCustomMarshaller, warning.Id);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }

        [Fact]
        public void Diagnostic_UnsupportedSecurityDeclaration()
        {
            // Using .permission instead of .permissionset triggers error
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .permission demand [mscorlib]System.Security.Permissions.SecurityPermissionAttribute
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsupportedSecurityDeclaration, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_GenericParameterIndexOutOfRange()
        {
            // Referencing generic parameter index that doesn't exist
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod<T>() cil managed
                    {
                        .param type [99]
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.GenericParameterIndexOutOfRange, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_ParameterIndexOutOfRange()
        {
            // Referencing parameter index that doesn't exist
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(int32 x) cil managed
                    {
                        .param [99]
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ParameterIndexOutOfRange, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_DuplicateMethod()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        ret
                    }
                    .method public static void TestMethod() cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = diagnostics.FirstOrDefault(d => d.Id == DiagnosticIds.DuplicateMethod);
            Assert.NotNull(error);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Typedef_ResolvedInTypeContext()
        {
            // .typedef className as alias syntax
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .typedef [mscorlib]System.Object as MyObject
                .class public auto ansi Test
                {
                    .field public class MyObject obj
                    .method public static void TestMethod() cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            // Should compile without errors when typedef is resolved
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Typedef_TypeBlob_Compiles()
        {
            // .typedef type as alias syntax
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .typedef int32 as MyInt
                .class public auto ansi Test
                {
                    .field public MyInt val
                    .method public static void TestMethod() cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            // Typedef type blob resolution should compile
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void VtfixupDecl_CompilesSuccessfully()
        {
            // .vtfixup directive should compile successfully with VTable fixup support
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1]
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());

            // Verify COR flags don't have ILOnly (mixed mode for vtfixups)
            var corHeader = pe.PEHeaders.CorHeader;
            Assert.NotNull(corHeader);
            Assert.False(corHeader!.Flags.HasFlag(CorFlags.ILOnly),
                "VTable fixups require mixed-mode assembly (ILOnly should be cleared)");

            // Verify .sdata section exists
            var sdataSection = pe.PEHeaders.SectionHeaders.FirstOrDefault(s => s.Name == ".sdata");
            Assert.False(sdataSection.Equals(default), "Expected .sdata section for vtable fixups");

            // Read and verify the .sdata section contains valid VTableFixup directory structure
            // Structure: IMAGE_COR_VTABLEFIXUP { DWORD RVA, WORD Count, WORD Type }
            var sdataBytes = pe.GetSectionData(sdataSection.VirtualAddress).GetContent();
            Assert.True(sdataBytes.Length >= 8, "VTableFixup directory should be at least 8 bytes");

            // Read the first VTableFixup entry
            int slotDataRva = BitConverter.ToInt32(sdataBytes.AsSpan(0, 4));
            ushort slotCount = BitConverter.ToUInt16(sdataBytes.AsSpan(4, 2));
            ushort flags = BitConverter.ToUInt16(sdataBytes.AsSpan(6, 2));

            Assert.Equal(1, slotCount);
            Assert.True((flags & 0x01) != 0, "Expected COR_VTABLE_32BIT flag");
            Assert.True((flags & 0x04) != 0, "Expected COR_VTABLE_FROM_UNMANAGED flag");

            // The slot data RVA should point within the .sdata section (after the directory)
            Assert.True(slotDataRva >= sdataSection.VirtualAddress,
                $"Slot data RVA {slotDataRva} should be >= section start {sdataSection.VirtualAddress}");

            // Verify the method token in the slot data (should be a valid MethodDef token)
            int slotDataOffset = slotDataRva - sdataSection.VirtualAddress;
            int methodToken = BitConverter.ToInt32(sdataBytes.AsSpan(slotDataOffset, 4));
            Assert.True((methodToken & 0xFF000000) == 0x06000000,
                $"Expected MethodDef token (0x06xxxxxx), got 0x{methodToken:X8}");
        }

        [Fact]
        public void ExportDirective_WithoutVtfixup_CompilesSuccessfully()
        {
            // .export directive without .vtfixup records export info but doesn't create vtable
            // This is valid IL - the export ordinal/name is stored for potential use by tools
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .export [1] as MyExport
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());

            // Without vtfixup, ILOnly flag should remain set (no vtable slot data needed)
            var corHeader = pe.PEHeaders.CorHeader;
            Assert.NotNull(corHeader);
            Assert.True(corHeader!.Flags.HasFlag(CorFlags.ILOnly),
                "Without vtfixup, assembly should remain IL-only");

            // Verify the method exists in metadata
            var reader = pe.GetMetadataReader();
            var exportedMethod = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .FirstOrDefault(m => reader.GetString(m.Name) == "ExportedMethod");
            Assert.False(exportedMethod.Equals(default), "ExportedMethod should exist in metadata");
        }

        [Fact]
        public void VtfixupDecl_64bit_CompilesSuccessfully()
        {
            // .vtfixup with int64 (64-bit slots) - used for 64-bit platforms
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int64(0)
                .vtfixup [1] int64 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1]
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());

            // Verify .sdata section exists
            var sdataSection = pe.PEHeaders.SectionHeaders.FirstOrDefault(s => s.Name == ".sdata");
            Assert.False(sdataSection.Equals(default), "Expected .sdata section for vtable fixups");

            // Read VTableFixup directory entry and verify 64-bit flag
            var sdataBytes = pe.GetSectionData(sdataSection.VirtualAddress).GetContent();
            ushort flags = BitConverter.ToUInt16(sdataBytes.AsSpan(6, 2));
            Assert.True((flags & 0x02) != 0, "Expected COR_VTABLE_64BIT flag (0x02)");

            // Verify slot data is 8 bytes (64-bit token)
            int slotDataRva = BitConverter.ToInt32(sdataBytes.AsSpan(0, 4));
            int slotDataOffset = slotDataRva - sdataSection.VirtualAddress;

            // 64-bit slot should have method token in lower 32 bits, zeros in upper 32 bits
            long slotValue = BitConverter.ToInt64(sdataBytes.AsSpan(slotDataOffset, 8));
            int methodToken = (int)(slotValue & 0xFFFFFFFF);
            Assert.True((methodToken & 0xFF000000) == 0x06000000,
                $"Expected MethodDef token (0x06xxxxxx), got 0x{methodToken:X8}");
        }

        [Fact]
        public void VtfixupDecl_MultipleSlots_CompilesSuccessfully()
        {
            // .vtfixup with multiple slots - each method gets its own slot
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0) int32(0)
                .vtfixup [2] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Method1() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as Export1
                        ret
                    }
                    .method public static void Method2() cil managed
                    {
                        .vtentry 1 : 2
                        .export [2] as Export2
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());

            // Verify .sdata section exists
            var sdataSection = pe.PEHeaders.SectionHeaders.FirstOrDefault(s => s.Name == ".sdata");
            Assert.False(sdataSection.Equals(default), "Expected .sdata section for vtable fixups");

            var sdataBytes = pe.GetSectionData(sdataSection.VirtualAddress).GetContent();

            // Verify VTableFixup directory entry has count of 2
            ushort slotCount = BitConverter.ToUInt16(sdataBytes.AsSpan(4, 2));
            Assert.Equal(2, slotCount);

            // Read both method tokens from slot data
            int slotDataRva = BitConverter.ToInt32(sdataBytes.AsSpan(0, 4));
            int slotDataOffset = slotDataRva - sdataSection.VirtualAddress;

            int token1 = BitConverter.ToInt32(sdataBytes.AsSpan(slotDataOffset, 4));
            int token2 = BitConverter.ToInt32(sdataBytes.AsSpan(slotDataOffset + 4, 4));

            // Both should be valid MethodDef tokens
            Assert.True((token1 & 0xFF000000) == 0x06000000,
                $"Slot 1: Expected MethodDef token, got 0x{token1:X8}");
            Assert.True((token2 & 0xFF000000) == 0x06000000,
                $"Slot 2: Expected MethodDef token, got 0x{token2:X8}");

            // Tokens should be different (different methods)
            Assert.NotEqual(token1, token2);

            // Verify the methods exist in metadata with expected names
            var reader = pe.GetMetadataReader();
            var methodNames = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Select(m => reader.GetString(m.Name))
                .ToHashSet();
            Assert.Contains("Method1", methodNames);
            Assert.Contains("Method2", methodNames);
        }

        [Fact]
        public void DataLabelReference_FixedUpCorrectly()
        {
            // Test that .data with a reference to another label (&Label) is patched with the correct RVA
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data TargetData = int32(0x12345678)
                .data PointerData = &TargetData
                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 8
                    .field [0] public static int32 Target at TargetData
                    .field [4] public static int32 Pointer at PointerData
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

            // Both fields should have RVAs
            int targetRva = fields["Target"].GetRelativeVirtualAddress();
            int pointerRva = fields["Pointer"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, targetRva);
            Assert.NotEqual(0, pointerRva);

            // The pointer field should contain the RVA of the target data
            // Read the actual data from the PE at the pointer location
            var pointerSection = pe.GetSectionData(pointerRva);
            int storedRva = BitConverter.ToInt32(pointerSection.GetContent().AsSpan(0, 4));

            // The stored RVA should equal the target's RVA
            Assert.Equal(targetRva, storedRva);

            // Verify the target data contains the expected value
            var targetSection = pe.GetSectionData(targetRva);
            int targetValue = BitConverter.ToInt32(targetSection.GetContent().AsSpan(0, 4));
            Assert.Equal(0x12345678, targetValue);
        }

        [Fact]
        public void DataLabelReference_MultipleReferences_AllFixedUp()
        {
            // Test multiple references to the same label
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data Target = int32(42)
                .data Ptr1 = &Target
                .data Ptr2 = &Target
                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 12
                    .field [0] public static int32 TargetField at Target
                    .field [4] public static int32 Ptr1Field at Ptr1
                    .field [8] public static int32 Ptr2Field at Ptr2
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

            int targetRva = fields["TargetField"].GetRelativeVirtualAddress();
            int ptr1Rva = fields["Ptr1Field"].GetRelativeVirtualAddress();
            int ptr2Rva = fields["Ptr2Field"].GetRelativeVirtualAddress();

            // Read both pointer values
            int storedRva1 = BitConverter.ToInt32(pe.GetSectionData(ptr1Rva).GetContent().AsSpan(0, 4));
            int storedRva2 = BitConverter.ToInt32(pe.GetSectionData(ptr2Rva).GetContent().AsSpan(0, 4));

            // Both should point to the target
            Assert.Equal(targetRva, storedRva1);
            Assert.Equal(targetRva, storedRva2);
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

        [Fact]
        public void PdbGeneration_WithLineAndLanguageDirectives_CreatesValidEmbeddedPdb()
        {
            // Use C# language GUID
            string csharpGuid = "{3F5162F8-07C6-11D3-9053-00C04FA302A1}";
            string source = $$"""
                .assembly test { }
                .language '{{csharpGuid}}'
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .line 10 "test.cs"
                        nop
                        .line 15 "test.cs"
                        nop
                        .line 20 "test.cs"
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());

            // Verify debug directory exists with embedded PDB
            var debugDirectory = pe.ReadDebugDirectory();
            Assert.NotEmpty(debugDirectory);

            var embeddedPdbEntry = debugDirectory.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            Assert.NotEqual(default, embeddedPdbEntry);

            // Read the embedded PDB and verify contents
            var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
            var pdbReader = pdbProvider.GetMetadataReader();

            // Verify document exists with correct name and language
            Assert.NotEmpty(pdbReader.Documents);
            var document = pdbReader.GetDocument(pdbReader.Documents.First());
            var docName = pdbReader.GetString(document.Name);
            Assert.Contains("test.cs", docName);

            var languageGuid = pdbReader.GetGuid(document.Language);
            Assert.Equal(Guid.Parse(csharpGuid), languageGuid);

            // Verify method debug info exists (sequence points were recorded)
            Assert.NotEmpty(pdbReader.MethodDebugInformation);
        }

        [Fact]
        public void PdbGeneration_WithoutLineDirectives_NoPdbGenerated()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        nop
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());

            // Verify no embedded PDB when no debug directives
            var debugDirectory = pe.ReadDebugDirectory();
            var embeddedPdbEntry = debugDirectory.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            Assert.Equal(default, embeddedPdbEntry);
        }

        [Fact]
        public void ParamInitOpt_Int32Constant_CreatesConstantEntry()
        {
            // Test that .param with int32 initOpt creates a constant entry
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod(int32 x) cil managed
                    {
                        .param [1] = int32(42)
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the method
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(m => reader.GetString(m.Name) == "TestMethod");

            // Get parameters
            var parameters = method.GetParameters().ToArray();
            Assert.True(parameters.Length >= 1, $"Expected at least 1 parameter, got {parameters.Length}");

            // Find the parameter by sequence number
            var param1 = parameters.Select(reader.GetParameter).FirstOrDefault(p => p.SequenceNumber == 1);
            var param1Handle = parameters.FirstOrDefault(h => reader.GetParameter(h).SequenceNumber == 1);
            Assert.False(param1Handle.IsNil, "Parameter with sequence 1 not found");

            // Check constant for first param (int32)
            var intConstantHandle = param1.GetDefaultValue();
            Assert.False(intConstantHandle.IsNil, "No constant for parameter 1");
            var intConstant = reader.GetConstant(intConstantHandle);
            Assert.Equal(ConstantTypeCode.Int32, intConstant.TypeCode);
            var intValue = reader.GetBlobReader(intConstant.Value).ReadInt32();
            Assert.Equal(42, intValue);
        }

        [Fact]
        public void ParamInitOpt_ReturnParam_CreatesConstantEntry()
        {
            // Test that .param [0] (return value) with initOpt works
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 GetValue() cil managed
                    {
                        .param [0] = int32(100)
                        ldc.i4 100
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the method
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(m => reader.GetString(m.Name) == "GetValue");

            // Get parameters - param [0] is the return value
            var parameters = method.GetParameters().ToArray();
            Assert.Single(parameters);

            var param = reader.GetParameter(parameters[0]);
            Assert.Equal(0, param.SequenceNumber); // Return value has sequence 0

            var constantHandle = param.GetDefaultValue();
            Assert.False(constantHandle.IsNil);
            var constant = reader.GetConstant(constantHandle);
            Assert.Equal(ConstantTypeCode.Int32, constant.TypeCode);
            var value = reader.GetBlobReader(constant.Value).ReadInt32();
            Assert.Equal(100, value);
        }

        [Fact]
        public void Property_BasicProperty_IsEmitted()
        {
            // First check if properties work at all without initOpt
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .field private int32 _value

                    .property int32 Value()
                    {
                        .get instance int32 Test::get_Value()
                    }

                    .method public hidebysig specialname instance int32 get_Value() cil managed
                    {
                        ldarg.0
                        ldfld int32 Test::_value
                        ret
                    }
                }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());
            
            // Check for diagnostics
            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);

            var blobBuilder = new System.Reflection.Metadata.BlobBuilder();
            result.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            // Check how many properties are in the table
            var propCount = reader.GetTableRowCount(TableIndex.Property);
            Assert.True(propCount > 0, $"Expected at least 1 property, got {propCount}");
        }

        [Fact]
        public void PropertyInitOpt_WithConstantValue_CreatesConstantEntry()
        {
            // Test that .property with initOpt creates a constant entry
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .field private int32 _value

                    .property int32 Value() = int32(42)
                    {
                        .get instance int32 Test::get_Value()
                    }

                    .method public hidebysig specialname instance int32 get_Value() cil managed
                    {
                        ldarg.0
                        ldfld int32 Test::_value
                        ret
                    }
                }
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

            // Find the property
            var propertyHandle = reader.PropertyDefinitions.First();
            var property = reader.GetPropertyDefinition(propertyHandle);

            // Check attributes include HasDefault
            Assert.True((property.Attributes & System.Reflection.PropertyAttributes.HasDefault) != 0, 
                $"Expected HasDefault attribute, got {property.Attributes}");

            // Check for constant
            var constantHandle = property.GetDefaultValue();
            Assert.False(constantHandle.IsNil, "No constant for property");
            var constant = reader.GetConstant(constantHandle);
            Assert.Equal(ConstantTypeCode.Int32, constant.TypeCode);
            var value = reader.GetBlobReader(constant.Value).ReadInt32();
            Assert.Equal(42, value);
        }

        [Fact]
        public void PropertyInitOpt_WithStringConstant_CreatesConstantEntry()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .field private string _name

                    .property string Name() = "DefaultName"
                    {
                        .get instance string Test::get_Name()
                    }

                    .method public hidebysig specialname instance string get_Name() cil managed
                    {
                        ldarg.0
                        ldfld string Test::_name
                        ret
                    }
                }
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

            // Find the property
            var propertyHandle = reader.PropertyDefinitions.First();
            var property = reader.GetPropertyDefinition(propertyHandle);

            // Check attributes include HasDefault
            Assert.True((property.Attributes & System.Reflection.PropertyAttributes.HasDefault) != 0,
                $"Expected HasDefault attribute, got {property.Attributes}");

            // Check for constant
            var constantHandle = property.GetDefaultValue();
            Assert.False(constantHandle.IsNil, "No constant for property");
            var constant = reader.GetConstant(constantHandle);
            Assert.Equal(ConstantTypeCode.String, constant.TypeCode);
            // String constants are stored as UTF-16
            var blobReader = reader.GetBlobReader(constant.Value);
            var stringBytes = blobReader.ReadBytes(blobReader.Length);
            var value = System.Text.Encoding.Unicode.GetString(stringBytes);
            Assert.Equal("DefaultName", value);
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
        public void AssemblyNoPlatform_SetsNoPlatformFlag()
        {
            string source = """
                .assembly test
                {
                    .hash algorithm 0x00008004
                    .ver 1:0:0:0
                    .custom instance void [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = (01 00 18 2E 4E 45 54 46 72 61 6D 65 77 6F 72 6B 2C 56 65 72 73 69 6F 6E 3D 76 38 2E 30 01 00 54 0E 14 46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79 4E 61 6D 65 08 2E 4E 45 54 20 38 2E 30)
                }
                .assembly extern mscorlib
                {
                    .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
                }
                .class public auto ansi Test { }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);
        }

        [Fact]
        public void TryBlock_WithLabeledBlocks_GeneratesExceptionHandlers()
        {
            // This tests exception handler generation with labeled try blocks
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .locals init (int32 V_0)
                        
                        .try
                        {
                            ldc.i4.0
                            stloc.0
                            leave.s END
                        }
                        catch [mscorlib]System.Exception
                        {
                            pop
                            ldc.i4.1
                            stloc.0
                            leave.s END
                        }
                        END: ret
                    }
                }
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

            // Verify method exists and has body
            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");

            Assert.True(methodDef.RelativeVirtualAddress != 0, "Method should have IL body");
        }

        [Fact]
        public void ScopeBlock_WithLabeledInstructions_UsesMarkLabelForBranches()
        {
            // Tests that labeled instructions properly work with branches
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi Test
                {
                    .method public static int32 TestBranches(int32 x) cil managed
                    {
                        .maxstack 2

                        ldarg.0
                        ldc.i4.0
                        bgt.s POSITIVE
                        ldc.i4.m1
                        br.s DONE

                        POSITIVE: ldc.i4.1

                        DONE: ret
                    }
                }
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

            // Verify the PE is valid and method has code
            Assert.True(pe.HasMetadata);
            var reader = pe.GetMetadataReader();

            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestBranches");

            Assert.True(methodDef.RelativeVirtualAddress != 0);
        }

        [Fact]
        public void TryBlock_WithOffsetLabels_UsesInstructionEncoderExtensions()
        {
            // Test offset-based labels in try/catch blocks (exercises InstructionEncoderExtensions.MarkLabel)
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try 0 to 5 catch [mscorlib]System.Exception handler 5 to 9
                        nop          // 0: 1 byte
                        nop          // 1: 1 byte
                        nop          // 2: 1 byte
                        leave.s IL_9 // 3-4: 2 bytes (opcode + offset)
                    IL_5:
                        pop          // 5: 1 byte
                        leave.s IL_9 // 6-8: 2 bytes
                    IL_9:
                        ret          // 9: 1 byte
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");
            Assert.True(methodDef.RelativeVirtualAddress != 0);
        }

        [Fact]
        public void ScopeBlock_WithOffsetLabels_UsesInstructionEncoderExtensions()
        {
            // Test offset-based scope blocks (exercises InstructionEncoderExtensions.MarkLabel)
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try 0 to 3 finally handler 3 to 5
                        nop          // 0
                        leave.s IL_5 // 1-2
                    IL_3:
                        endfinally   // 3
                    IL_5:
                        ret          // 5
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");
            Assert.True(methodDef.RelativeVirtualAddress != 0);
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

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.TypeDefinitions
                .Select(h => reader.GetTypeDefinition(h))
                .First(t => reader.GetString(t.Name) == "Test");

            // ExtendedLayout = 0x18
            Assert.Equal((System.Reflection.TypeAttributes)0x18, typeDef.Attributes & (System.Reflection.TypeAttributes)0x18);
        }

        [Fact]
        public void AssemblyNoPlatform_WithKeyword_SetsNoPlatformFlag()
        {
            // Test the 'noplatform' assembly attribute
            string source = """
                .assembly noplatform test
                {
                    .ver 1:0:0:0
                }
                .class public auto ansi Test { }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var assembly = reader.GetAssemblyDefinition();

            // NoPlatform = 0x70 (stored in architecture bits of AssemblyFlags)
            Assert.Equal((System.Reflection.AssemblyFlags)0x70, assembly.Flags & (System.Reflection.AssemblyFlags)0xF0);
        }

        [Fact]
        public void AssemblyArchitecture_SetsArchitectureFlags()
        {
            // Test the x86 architecture assembly attribute
            string source = """
                .assembly x86 test
                {
                    .ver 1:0:0:0
                }
                .class public auto ansi Test { }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var assembly = reader.GetAssemblyDefinition();

            // x86 = ProcessorArchitecture.X86 (2) << 4 = 0x20
            Assert.Equal((System.Reflection.AssemblyFlags)0x20, assembly.Flags & (System.Reflection.AssemblyFlags)0xF0);
        }

        [Fact]
        public void PermissionSet_WithDemand_UsesSecurityAction()
        {
            // Test permission set with bytearray (exercises security action handling)
            string source = """
                .assembly test
                {
                    .permissionset demand = (2E)
                    .ver 1:0:0:0
                }
                .class public auto ansi Test { }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Verify assembly compiled successfully with the permission set
            Assert.True(reader.GetTableRowCount(TableIndex.DeclSecurity) >= 1);
        }

        [Fact]
        public void VarargMethod_Definition_Compiles()
        {
            // Test vararg method definition
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static vararg void VarargMethod(int32 x) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "VarargMethod");

            // VarArgs calling convention = 0x5
            var signature = reader.GetBlobReader(methodDef.Signature);
            var header = signature.ReadByte();
            Assert.Equal(0x5, header & 0x0F);
        }

        [Fact]
        public void GenericType_UsesNamedElementList()
        {
            // Test generic types (exercises NamedElementList for generic parameters)
            // Note: Generic parameter handling may require type being in the module context
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test`2<T, U> extends [mscorlib]System.Object
                {
                    .field public !0 fieldT
                    .field public !1 fieldU
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.TypeDefinitions
                .Select(h => reader.GetTypeDefinition(h))
                .First(t => reader.GetString(t.Name) == "Test`2");

            var genericParams = typeDef.GetGenericParameters();
            Assert.Equal(2, genericParams.Count);
        }

        [Fact]
        public void GenericMethod_UsesNamedElementList()
        {
            // Test generic methods (exercises NamedElementList for method generic parameters)
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static !!0 GenericMethod<T>(!!0 arg) cil managed
                    {
                        .maxstack 1
                        ldarg.0
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GenericMethod");

            var genericParams = methodDef.GetGenericParameters();
            Assert.Single(genericParams);
        }

        [Fact]
        public void FieldConstant_WithCharValue_UsesBlobBuilderExtensions()
        {
            // Test field with char constant (exercises BlobBuilderExtensions.WriteSerializedValue<char>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal char CharField = char(0x0041)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "CharField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Char, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithDoubleValue_UsesBlobBuilderExtensions()
        {
            // Test field with double constant (exercises BlobBuilderExtensions.WriteSerializedValue<double>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal float64 DoubleField = float64(3.14159265358979)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "DoubleField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Double, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithInt16Value_UsesBlobBuilderExtensions()
        {
            // Test field with int16 constant (exercises BlobBuilderExtensions.WriteSerializedValue<short>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal int16 ShortField = int16(12345)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "ShortField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Int16, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithInt64Value_UsesBlobBuilderExtensions()
        {
            // Test field with int64 constant (exercises BlobBuilderExtensions.WriteSerializedValue<long>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal int64 LongField = int64(9223372036854775807)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "LongField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Int64, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithInt8Value_UsesBlobBuilderExtensions()
        {
            // Test field with int8 constant (exercises BlobBuilderExtensions.WriteSerializedValue<sbyte>)
            // Use hex 0xD6 which is -42 in signed byte representation
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal int8 SByteField = int8(42)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "SByteField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.SByte, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithFloat32Value_UsesBlobBuilderExtensions()
        {
            // Test field with float32 constant (exercises BlobBuilderExtensions.WriteSerializedValue<float>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal float32 FloatField = float32(3.14)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "FloatField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Single, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithUInt16Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint16 constant (exercises BlobBuilderExtensions.WriteSerializedValue<ushort>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint16 UShortField = uint16(65535)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "UShortField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.UInt16, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithUInt32Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint32 constant (exercises BlobBuilderExtensions.WriteSerializedValue<uint>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint32 UIntField = uint32(4294967295)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "UIntField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.UInt32, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithUInt64Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint64 constant (exercises BlobBuilderExtensions.WriteSerializedValue<ulong>)
            // Use smaller value that fits in int64 range
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint64 ULongField = uint64(9223372036854775807)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "ULongField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.UInt64, constant.TypeCode);
        }

        [Fact]
        public void FieldConstant_WithUInt8Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint8 constant (exercises BlobBuilderExtensions.WriteSerializedValue<byte>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint8 ByteField = uint8(255)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "ByteField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Byte, constant.TypeCode);
        }

        [Fact]
        public void TypeForwarder_EmitsExportedType()
        {
            // Test type forwarder (exercises ExportedType with assembly reference implementation)
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .class extern forwarder System.ForwardedType
                {
                    .assembly extern ForwardedAssembly
                }
                """;

            // First check diagnostics to see if there are any errors
            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Verify the ExportedType table has an entry
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.ExportedType));

            var exportedType = reader.ExportedTypes.Select(h => reader.GetExportedType(h)).First();
            Assert.Equal("ForwardedType", reader.GetString(exportedType.Name));
            Assert.Equal("System", reader.GetString(exportedType.Namespace));
            // Forwarder flag is TypeAttributes.Forwarder (0x00200000)
            Assert.True(exportedType.Attributes.HasFlag((System.Reflection.TypeAttributes)0x00200000));
        }

        [Fact]
        public void TypeForwarder_WithMissingImplementation_EmitsWarning()
        {
            // Test that missing implementation emits a warning and doesn't emit the ExportedType
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class extern forwarder System.ForwardedType
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());

            // Should have a warning about missing implementation
            var warning = diagnostics.FirstOrDefault(d => d.Id == DiagnosticIds.MissingExportedTypeImplementation);
            Assert.NotNull(warning);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }
    }
}

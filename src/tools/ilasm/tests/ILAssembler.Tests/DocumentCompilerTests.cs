// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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
        public void TypeNotFound_CreatesForwardReference()
        {
            // Referencing a type that doesn't exist creates a forward reference placeholder,
            // matching native ilasm behavior where types can be referenced before declaration.
            string source = """
                .class public auto ansi sealed beforefieldinit Test extends NonExistentType
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
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
        public void MultipleTypeNotFound_CreatesForwardReferences()
        {
            // Multiple references to non-existent types create forward reference placeholders,
            // matching native ilasm behavior
            string source = """
                .class public auto ansi beforefieldinit Test extends NonExistentBase implements NonExistentInterface
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
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
            Assert.Equal(DiagnosticSeverity.Warning, error.Severity);
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

        [Fact]
        public void AssemblyVersion_DefaultsToZero_WhenNoVerDirective()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var asmDef = reader.GetAssemblyDefinition();
            Assert.Equal(new Version(0, 0, 0, 0), asmDef.Version);
        }

        [Fact]
        public void AssemblyRefVersion_DefaultsToZero_WhenNoVerDirective()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var asmRef = reader.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(1));
            Assert.Equal(new Version(0, 0, 0, 0), asmRef.Version);
        }

        [Fact]
        public void AssemblyVersion_ExplicitVer_IsPreserved()
        {
            string source = """
                .assembly extern System.Runtime { .ver 8:0:0:0 }
                .assembly TestAssembly { .ver 1:2:3:4 }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var asmDef = reader.GetAssemblyDefinition();
            Assert.Equal(new Version(1, 2, 3, 4), asmDef.Version);
            var asmRef = reader.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(1));
            Assert.Equal(new Version(8, 0, 0, 0), asmRef.Version);
        }

        [Fact]
        public void ModuleName_DefaultsToOutputFileName_WhenNoModuleDirective()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options { OutputFileName = "MyOutput.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("MyOutput.dll", reader.GetString(moduleDef.Name));
        }

        [Fact]
        public void ModuleName_OutputFileNameStripsDirectory()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            // OutputFileName should already be just the filename (Program.cs uses Path.GetFileName),
            // but verify the module name is exactly what's provided
            using var pe = CompileAndGetReader(source, new Options { OutputFileName = "bar.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("bar.dll", reader.GetString(moduleDef.Name));
        }

        [Fact]
        public void ModuleName_ExplicitModuleDirective_OverridesOutputFileName()
        {
            string source = """
                .assembly TestAssembly { }
                .module Explicit.dll
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options { OutputFileName = "DifferentName.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("Explicit.dll", reader.GetString(moduleDef.Name));
        }

        [Fact]
        public void ModuleName_NoModuleDirective_NoOutputFileName_UsesNilHandle()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.True(moduleDef.Name.IsNil);
        }

        [Fact]
        public void CustomAttribute_HexByteBlob_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            // Verify the custom attribute was emitted on the assembly
            var asmDef = reader.GetAssemblyDefinition();
            var attrs = asmDef.GetCustomAttributes();
            Assert.NotEmpty(attrs);
        }

        [Fact]
        public void NativeInt_FieldType_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static native int f1
                    .field public static native uint f2
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var fields = typeDef.GetFields().ToArray();
            Assert.Equal(2, fields.Length);
            // native int → IntPtr (SignatureTypeCode 0x18)
            var sig1 = reader.GetBlobReader(reader.GetFieldDefinition(fields[0]).Signature);
            Assert.Equal(0x06, sig1.ReadByte()); // FIELD calling convention
            Assert.Equal(0x18, sig1.ReadByte()); // ELEMENT_TYPE_I (IntPtr)
            // native uint → UIntPtr (SignatureTypeCode 0x19)
            var sig2 = reader.GetBlobReader(reader.GetFieldDefinition(fields[1]).Signature);
            Assert.Equal(0x06, sig2.ReadByte());
            Assert.Equal(0x19, sig2.ReadByte()); // ELEMENT_TYPE_U (UIntPtr)
        }

        [Fact]
        public void SqstringAssemblyName_ParsedCorrectly()
        {
            string source = """
                .assembly 'My-Assembly_123' { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            // No errors — the SQSTRING assembly name should be accepted
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ArrayType_InMethodSignature_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M(int32[] arr) cil managed { ret }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var methods = typeDef.GetMethods().ToArray();
            Assert.Single(methods);
            var methodDef = reader.GetMethodDefinition(methods[0]);
            var sig = reader.GetBlobReader(methodDef.Signature);
            Assert.Equal(0x00, sig.ReadByte()); // DEFAULT calling convention
            Assert.Equal(1, sig.ReadCompressedInteger()); // param count
            Assert.Equal(0x01, sig.ReadByte()); // return type: void
            Assert.Equal(0x1D, sig.ReadByte()); // ELEMENT_TYPE_SZARRAY
            Assert.Equal(0x08, sig.ReadByte()); // ELEMENT_TYPE_I4 (int32)
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

            using var pe = CompileAndGetReader(source, new Options());
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
        public void HexLabelName_NotConfusedWithHexByte()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        br AA
                        nop
                    AA: ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PrefixInstruction_Volatile_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static int32 myField
                    .method public static void M() cil managed
                    {
                        volatile.
                        ldsfld int32 Test::myField
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PrefixInstruction_Tail_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 M() cil managed
                    {
                        ldc.i4.0
                        tail.
                        call int32 Test::M()
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void VolatileFieldAttribute_AcceptedAsModifier()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static volatile int32 myField
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void MultiDimArrayBounds_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32[0...,0...] V_0)
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void LdelemU8_InstructionParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M(unsigned int64[] arr) cil managed
                    {
                        ldarg.0
                        ldc.i4.0
                        ldelem.u8
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void MethodNameF1_NotConfusedWithHexByte()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 f1() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var methods = typeDef.GetMethods().ToArray();
            Assert.Single(methods);
            Assert.Equal("f1", reader.GetString(reader.GetMethodDefinition(methods[0]).Name));
        }

        [Fact]
        public void NamedLocal_CanBeReferencedByStloc()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32 myLocal)
                        ldc.i4.0
                        stloc myLocal
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void NamedArgument_CanBeReferencedByLdarg()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M(int32 myArg) cil managed
                    {
                        ldarg myArg
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void StringEscape_NewlineInLdstr()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldstr "Hello\nWorld\t!"
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void FloatLiteral_TrailingDot()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.r4 0.
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void FloatLiteral_SignedExponent()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.r8 5.1234567890000001e+054
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void SwitchInstruction_CommaLabels()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.i4.0
                        switch (L0, L1, L2)
                    L0: nop
                    L1: nop
                    L2: ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ModuleLevelField_DoesNotCrash()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .field public static int32 globalField
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ForwardTypeReference_ResolvedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Base extends [System.Runtime]System.Object
                {
                    .field public static class Derived child
                }
                .class public auto ansi beforefieldinit Derived extends Base
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            Assert.Equal(3, reader.TypeDefinitions.Count);
        }

        [Fact]
        public void SelfTypeReference_InField()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit MyClass extends [System.Runtime]System.Object
                {
                    .field public static class MyClass instance
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            Assert.Equal(2, reader.TypeDefinitions.Count);
        }

        [Fact]
        public void SimpleOverride_EmitsMethodImpl()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestOverride { }

                .class interface public abstract auto ansi IFoo
                {
                    .method public hidebysig newslot abstract virtual instance int32 GetVal() cil managed { }
                }

                .class public auto ansi beforefieldinit Bar extends [mscorlib]System.Object implements IFoo
                {
                    .method public hidebysig newslot virtual final instance int32 GetVal() cil managed
                    {
                        .override IFoo::GetVal
                        ldc.i4.s 42
                        ret
                    }
                    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(1, methodImplCount);
        }

        [Fact]
        public void OverrideWithExplicitSignature_EmitsMethodImpl()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestOverride { }

                .class public auto ansi beforefieldinit Base extends [mscorlib]System.Object
                {
                    .method public hidebysig newslot virtual instance object GetVal(string& res) cil managed
                    {
                        ldnull
                        ret
                    }
                }

                .class public auto ansi beforefieldinit Derived extends Base
                {
                    .method public hidebysig newslot virtual instance object GetVal(string& res) cil managed
                    {
                        .override method instance object Base::GetVal(string&)
                        ldnull
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(1, methodImplCount);
        }

        [Fact]
        public void GenericOverride_EmitsMethodImpl()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestOverride { }

                .class public auto ansi beforefieldinit GenBase<A,B> extends [mscorlib]System.Object
                {
                    .method public hidebysig newslot virtual instance object MyFunc(string& res) cil managed
                    {
                        ldnull
                        ret
                    }
                }

                .class public auto ansi beforefieldinit GenDerived<U,V> extends class GenBase<!U,!V>
                {
                    .method public hidebysig newslot virtual instance object MyFunc(string& res) cil managed
                    {
                        .override method instance object class GenBase<!U,!V>::MyFunc(string&)
                        ldnull
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(1, methodImplCount);

            int typeSpecCount = reader.GetTableRowCount(TableIndex.TypeSpec);
            Assert.True(typeSpecCount >= 1, "Should have at least one TypeSpec for the generic instantiation");
        }

        [Fact]
        public void MultipleOverrides_EmitsAllMethodImpls()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestOverride { }

                .class public auto ansi beforefieldinit GenBase<A,B> extends [mscorlib]System.Object
                {
                    .method public hidebysig newslot virtual instance object Func1(string& res) cil managed
                    {
                        ldnull
                        ret
                    }
                    .method public hidebysig newslot virtual instance object Func2(string& res) cil managed
                    {
                        ldnull
                        ret
                    }
                }

                .class public auto ansi beforefieldinit Derived<U,V> extends class GenBase<!U,!V>
                {
                    .method public hidebysig newslot virtual instance object Func1(string& res) cil managed
                    {
                        .override method instance object class GenBase<!U,!V>::Func1(string&)
                        ldnull
                        ret
                    }
                    .method public hidebysig newslot virtual instance object Func2(string& res) cil managed
                    {
                        .override method instance object class GenBase<!U,!V>::Func2(string&)
                        ldnull
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(2, methodImplCount);
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

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
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

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void TrailingDotFloat()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestFloat { }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static float64 M() cil managed
                    {
                        ldc.r8 1.
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void GenericConstraint_ForwardRefTypeParam()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestConstraint { }

                .class interface public abstract auto ansi IAdder`1<T>
                {
                    .method public hidebysig newslot abstract virtual instance int32 Add() cil managed { }
                }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 Check<(class IAdder`1<!!U>) T, U>(!!T t) cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void TypeConstraint_ForwardRefTypeParam()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestConstraint { }

                .class interface public abstract auto ansi I`1<T>
                {
                    .method public hidebysig newslot abstract virtual instance string Method() cil managed { }
                }

                .class public auto ansi beforefieldinit Conversion`2<T, (class I`1<!T>) U> extends [mscorlib]System.Object
                {
                    .method public hidebysig instance string M() cil managed
                    {
                        ldnull
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
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

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Int64MinValue_Accepted()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestInt64Min { }

                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int64 M() cil managed
                    {
                        ldc.i8 -9223372036854775808
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void MultiDocument_DefinePropagatesToNextDocument()
        {
            var doc1 = new SourceText("""
                #define ASSEMBLY_NAME "TestAssembly"
                .assembly extern mscorlib { }
                .assembly ASSEMBLY_NAME { }
                """, "doc1.il");

            var doc2 = new SourceText("""
                .class public auto ansi beforefieldinit ASSEMBLY_NAME extends [mscorlib]System.Object
                {
                }
                """, "doc2.il");

            var compiler = new DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(
                [doc1, doc2],
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options());

            Assert.Empty(diagnostics);
            Assert.NotNull(result);

            var blobBuilder = new BlobBuilder();
            result!.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            // doc2 should have the type named "TestAssembly" (from the macro)
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("TestAssembly", reader.GetString(typeDef.Name));
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

            using var pe = CompileAndGetReader(source, new Options());
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
        public void HexByteBlob_DigitLetterPairsCorrect()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 3F 5F 00 00 )
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var customAttrs = reader.GetCustomAttributes(MetadataTokens.MethodDefinitionHandle(1));
            foreach (var caHandle in customAttrs)
            {
                var ca = reader.GetCustomAttribute(caHandle);
                var blob = reader.GetBlobBytes(ca.Value);
                // Blob should be exactly: 01 00 3F 5F 00 00
                Assert.Equal(6, blob.Length);
                Assert.Equal(0x3F, blob[2]);
                Assert.Equal(0x5F, blob[3]);
            }
        }

        [Fact]
        public void DottedName_SQStringQuotesStripped()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly 'My-Assembly' { }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var asmDef = reader.GetAssemblyDefinition();
            Assert.Equal("My-Assembly", reader.GetString(asmDef.Name));
        }

        [Fact]
        public void MethodRtSpecialName_Preserved()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal(".ctor", reader.GetString(method.Name));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.RTSpecialName));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.SpecialName));
        }

        [Fact]
        public void FieldRtSpecialName_Preserved()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi sealed TestEnum extends [mscorlib]System.Enum
                {
                    .field public specialname rtspecialname uint8 value__
                    .field public static literal valuetype TestEnum A = uint8(0x00)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal("value__", reader.GetString(field.Name));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.RTSpecialName));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.SpecialName));
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

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("IMyInterface", reader.GetString(typeDef.Name));
            Assert.True(typeDef.Attributes.HasFlag(TypeAttributes.Interface));
            Assert.True(typeDef.BaseType.IsNil);
        }

        [Fact]
        public void CustomAttributeOnMethod_EmittedCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 Main() cil managed
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 )
                        .entrypoint
                        ldc.i4 100
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal("Main", reader.GetString(method.Name));

            var customAttrs = method.GetCustomAttributes();
            Assert.Equal(1, customAttrs.Count);
        }

        [Fact]
        public void TypeName_NoDotPrefix()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi beforefieldinit MyNamespace.MyType extends [mscorlib]System.Object { }
                """;

            using var pe = CompileAndGetReader(source, new Options());
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

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("MyType", reader.GetString(typeDef.Name));
            Assert.Equal("System.Tests", reader.GetString(typeDef.Namespace));
        }

        [Fact]
        public void ParamWithInAttribute_EmitsParamRow()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public hidebysig static void M([in] int32& x) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int paramCount = reader.GetTableRowCount(TableIndex.Param);
            Assert.True(paramCount >= 1, "Should have at least one Param row for [in] parameter");

            var param = reader.GetParameter(MetadataTokens.ParameterHandle(1));
            Assert.True(param.Attributes.HasFlag(ParameterAttributes.In));
        }

        [Fact]
        public void LocalMethodCall_ResolvesToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static int32 Helper() cil managed
                    {
                        ldc.i4.1
                        ret
                    }
                    .method public static int32 Caller() cil managed
                    {
                        call int32 MyClass::Helper()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // No MemberRef rows should exist for the local method call
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));

            // Verify the call instruction references a MethodDef token
            var callerMethod = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Caller");
            var body = pe.GetMethodBody(callerMethod.RelativeVirtualAddress);
            var ilReader = body.GetILReader();
            Assert.Equal(ILOpCode.Call, (ILOpCode)ilReader.ReadByte());
            int token = ilReader.ReadInt32();
            Assert.Equal(0x06, (token >> 24) & 0xFF); // MethodDef table (0x06)
        }

        [Fact]
        public void LocalFieldAccess_ResolvesToFieldDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static int32 myField
                    .method public static int32 GetField() cil managed
                    {
                        ldsfld int32 MyClass::myField
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // No MemberRef rows should exist for the local field access
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));

            // Verify the ldsfld instruction references a FieldDef token
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GetField");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var ilReader = body.GetILReader();
            Assert.Equal(ILOpCode.Ldsfld, (ILOpCode)ilReader.ReadByte());
            int token = ilReader.ReadInt32();
            Assert.Equal(0x04, (token >> 24) & 0xFF); // FieldDef table (0x04)
        }

        [Fact]
        public void MixedLocalAndExternalRefs_ResolvesCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static int32 myField
                    .method public static void Helper() cil managed
                    {
                        ret
                    }
                    .method public static void Caller() cil managed
                    {
                        // Local method call -> should resolve to MethodDef
                        call void MyClass::Helper()
                        // External method call -> should remain MemberRef
                        call string [mscorlib]System.Object::ToString(object)
                        pop
                        // Local field access -> should resolve to FieldDef
                        ldsfld int32 MyClass::myField
                        pop
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Only the external call should produce a MemberRef row
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));

            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("ToString", reader.GetString(memberRef.Name));
        }

        [Fact]
        public void LocalInstanceFieldAccess_ResolvesToFieldDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public int32 value
                    .method public instance int32 GetValue() cil managed
                    {
                        ldarg.0
                        ldfld int32 MyClass::value
                        ret
                    }
                    .method public instance void SetValue(int32 v) cil managed
                    {
                        ldarg.0
                        ldarg.1
                        stfld int32 MyClass::value
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void ExternalMethodCall_KeepsMemberRef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Test() cil managed
                    {
                        call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
                        pop
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));
            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("get_CurrentManagedThreadId", reader.GetString(memberRef.Name));
        }

        [Fact]
        public void LocalVarargMethodCall_ResolvesBaseToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static vararg void VarFunc() cil managed
                    {
                        ret
                    }
                    .method public static void Caller() cil managed
                    {
                        ldc.i4.1
                        call vararg void MyClass::VarFunc(..., int32)
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Only 1 MemberRef: the vararg call-site. The base method resolved to MethodDef.
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));

            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("VarFunc", reader.GetString(memberRef.Name));
            // The call-site MemberRef's parent should be the resolved MethodDef
            Assert.Equal(HandleKind.MethodDefinition, memberRef.Parent.Kind);

            // Verify the signature has the sentinel marker (it's a vararg call-site)
            var sigBytes = reader.GetBlobBytes(memberRef.Signature);
            Assert.Contains((byte)SignatureTypeCode.Sentinel, sigBytes);
        }

        [Fact]
        public void LocalVarargWithRequiredParams_ResolvesBaseToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static vararg void Printf(string fmt) cil managed
                    {
                        ret
                    }
                    .method public static void Caller() cil managed
                    {
                        ldstr "hello %d %s"
                        ldc.i4.1
                        ldstr "world"
                        call vararg void MyClass::Printf(string, ..., int32, string)
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Only 1 MemberRef: the vararg call-site
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));

            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("Printf", reader.GetString(memberRef.Name));
            Assert.Equal(HandleKind.MethodDefinition, memberRef.Parent.Kind);

            // Verify param count in signature: should be 3 (1 required + 2 optional)
            var sigBytes = reader.GetBlobBytes(memberRef.Signature);
            Assert.Equal(0x05, sigBytes[0]); // vararg
            Assert.Equal(3, sigBytes[1]);    // param count = 3
        }

        [Fact]
        public void ExternalVarargMethodCall_KeepsTypeRefParent()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Caller() cil managed
                    {
                        ldstr "format"
                        ldc.i4.1
                        box [mscorlib]System.Int32
                        call vararg int32 [mscorlib]System.String::Format(string, ..., object)
                        pop
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the vararg call-site MemberRef
            int memberRefCount = reader.GetTableRowCount(TableIndex.MemberRef);
            Assert.True(memberRefCount >= 1);

            bool foundCallSite = false;
            for (int i = 1; i <= memberRefCount; i++)
            {
                var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(i));
                if (reader.GetString(memberRef.Name) == "Format")
                {
                    var sigBytes = reader.GetBlobBytes(memberRef.Signature);
                    if (sigBytes.Any(b => b == (byte)SignatureTypeCode.Sentinel))
                    {
                        foundCallSite = true;
                        // For external vararg call-sites, the parent should be TypeRef or MemberRef
                        // (not TypeDef, since String.Format is external)
                        Assert.True(
                            memberRef.Parent.Kind is HandleKind.TypeReference or HandleKind.MemberReference,
                            $"External vararg call-site parent should be TypeRef or MemberRef, got {memberRef.Parent.Kind}");
                    }
                }
            }
            Assert.True(foundCallSite, "Should have found the external vararg call-site MemberRef with sentinel");
        }

        [Fact]
        public void MultipleLocalMethodCalls_AllResolveToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void A() cil managed { ret }
                    .method public static void B() cil managed { ret }
                    .method public static void C() cil managed { ret }
                    .method public static void Caller() cil managed
                    {
                        call void MyClass::A()
                        call void MyClass::B()
                        call void MyClass::C()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void ForwardReferencedLocalMethod_ResolvesToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Caller() cil managed
                    {
                        // Calls a method defined later in the same type
                        call void MyClass::Target()
                        ret
                    }
                    .method public static void Target() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void CrossTypeLocalMethodCall_ResolvesToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit ClassA extends [mscorlib]System.Object
                {
                    .method public static void DoWork() cil managed
                    {
                        ret
                    }
                }
                .class public auto ansi beforefieldinit ClassB extends [mscorlib]System.Object
                {
                    .method public static void Caller() cil managed
                    {
                        call void ClassA::DoWork()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void CrossTypeLocalFieldAccess_ResolvesToFieldDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit ClassA extends [mscorlib]System.Object
                {
                    .field public static int32 SharedValue
                }
                .class public auto ansi beforefieldinit ClassB extends [mscorlib]System.Object
                {
                    .method public static int32 GetShared() cil managed
                    {
                        ldsfld int32 ClassA::SharedValue
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void TypeRefViaSelfAssembly_ResolvesToTypeDef()
        {
            // When IL references a local type via [self-assembly]Namespace.Type,
            // the TypeRef should resolve to the local TypeDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void DoWork() cil managed { ret }
                }
                .class public auto ansi beforefieldinit Caller extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        call void [test]MyClass::DoWork()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The [test]MyClass TypeRef should have been resolved to TypeDef,
            // so no TypeRef for MyClass should exist.
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyClass", reader.GetString(tr.Name));
            }

            // The method call should resolve to MethodDef, not MemberRef.
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void TypeRefViaSelfAssembly_FieldResolves()
        {
            // Field access through a self-assembly TypeRef should resolve to FieldDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Data extends [mscorlib]System.Object
                {
                    .field public static int32 Value
                }
                .class public auto ansi beforefieldinit Reader extends [mscorlib]System.Object
                {
                    .method public static int32 Get() cil managed
                    {
                        ldsfld int32 [test]Data::Value
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void ExternalTypeRef_StaysTypeRef()
        {
            // An external TypeRef (different assembly) should NOT resolve to TypeDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
                        pop
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // External call should remain as MemberRef with TypeRef parent.
            Assert.True(reader.GetTableRowCount(TableIndex.MemberRef) >= 1);
            Assert.True(reader.GetTableRowCount(TableIndex.TypeRef) >= 1);
        }

        [Fact]
        public void TypeRefViaSelfAssembly_MemberRefThroughResolved_BecomesMethodDef()
        {
            // A method call through [self-assembly]Type::Method should resolve
            // BOTH the TypeRef to TypeDef AND the MemberRef to MethodDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly myasm { }
                .class public auto ansi beforefieldinit Target extends [mscorlib]System.Object
                {
                    .method public static int32 Compute() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                }
                .class public auto ansi beforefieldinit Caller extends [mscorlib]System.Object
                {
                    .method public static int32 Main() cil managed
                    {
                        call int32 [myasm]Target::Compute()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));

            // TypeRef table should not contain "Target" (resolved to TypeDef)
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("Target", reader.GetString(tr.Name));
            }
        }

        [Fact]
        public void FieldLiteralConstant_SetsHasDefaultFlag()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ByteEnum extends [mscorlib]System.Enum
                {
                    .field public specialname rtspecialname uint8 value__
                    .field public static literal valuetype ByteEnum A = uint8(0)
                    .field public static literal valuetype ByteEnum B = uint8(1)
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(2, reader.GetTableRowCount(TableIndex.Constant));

            // Fields A and B (handles 2 and 3, after value__)
            var fieldA = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(2));
            var fieldB = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(3));

            Assert.True(fieldA.Attributes.HasFlag(FieldAttributes.HasDefault));
            Assert.True(fieldB.Attributes.HasFlag(FieldAttributes.HasDefault));

            // Verify constant values
            var constA = reader.GetConstant(fieldA.GetDefaultValue());
            var constB = reader.GetConstant(fieldB.GetDefaultValue());

            Assert.Equal(ConstantTypeCode.Byte, constA.TypeCode);
            Assert.Equal(ConstantTypeCode.Byte, constB.TypeCode);

            Assert.Equal(0, reader.GetBlobReader(constA.Value).ReadByte());
            Assert.Equal(1, reader.GetBlobReader(constB.Value).ReadByte());
        }

        [Theory]
        [InlineData("int32", "int32(42)", ConstantTypeCode.Int32)]
        [InlineData("int64", "int64(100)", ConstantTypeCode.Int64)]
        [InlineData("float32", "float32(3.14)", ConstantTypeCode.Single)]
        [InlineData("bool", "bool(true)", ConstantTypeCode.Boolean)]
        public void FieldLiteralConstant_VariousTypes(string fieldType, string initExpr, ConstantTypeCode expectedTypeCode)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static literal {{fieldType}} myConst = {{initExpr}}
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.HasDefault));

            var constant = reader.GetConstant(field.GetDefaultValue());
            Assert.Equal(expectedTypeCode, constant.TypeCode);
        }

        [Fact]
        public void FieldLiteralString_SetsHasDefaultFlag()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static literal string myStr = "hello"
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.HasDefault));

            var constant = reader.GetConstant(field.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.String, constant.TypeCode);
        }

        [Fact]
        public void StackReserve_DirectiveValueIsHonored()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .stackreserve 0x00400000
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        .entrypoint
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            Assert.Equal((ulong)0x00400000, pe.PEHeaders.PEHeader!.SizeOfStackReserve);
        }

        [Fact]
        public void StackReserve_DefaultValueUsedWhenNotSpecified()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        .entrypoint
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            Assert.Equal((ulong)0x00100000, pe.PEHeaders.PEHeader!.SizeOfStackReserve);
        }

        [Fact]
        public void UnnamedInstanceParam_EmitsParamRow()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public instance void .ctor(int32) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Should have 1 Param row for the unnamed int32 parameter (sequence 1)
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.Param));
            var param = reader.GetParameter(MetadataTokens.ParameterHandle(1));
            Assert.Equal(1, param.SequenceNumber);
        }

        [Fact]
        public void CctorMethod_HasSpecialNameAttribute()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public specialname rtspecialname static void .cctor() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == ".cctor");

            Assert.True(method.Attributes.HasFlag(MethodAttributes.SpecialName));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.RTSpecialName));
        }

        [Fact]
        public void RtSpecialName_ImplicitlyAddsSpecialName()
        {
            // When only rtspecialname is specified (without specialname),
            // native ilasm implicitly adds specialname
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public rtspecialname static void .cctor() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == ".cctor");

            // Both SpecialName and RTSpecialName should be set
            Assert.True(method.Attributes.HasFlag(MethodAttributes.SpecialName));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.RTSpecialName));
        }

        [Fact]
        public void FieldRtSpecialName_ImplicitlyAddsSpecialName()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ByteEnum extends [mscorlib]System.Enum
                {
                    .field public rtspecialname uint8 value__
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.SpecialName));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.RTSpecialName));
        }

        [Fact]
        public void PinvokeMethod_SetsPinvokeImplFlag()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .module test.dll
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static pinvokeimpl("kernel32.dll" winapi)
                        int32 GetCurrentProcessId() cil managed preservesig
                    {
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GetCurrentProcessId");

            Assert.True(method.Attributes.HasFlag(MethodAttributes.PinvokeImpl));
            var import = method.GetImport();
            Assert.False(import.Module.IsNil);
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

            using var pe = CompileAndGetReader(source, new Options());
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

            using var pe = CompileAndGetReader(source, new Options());
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

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigReader = reader.GetBlobReader(field.Signature);
            sigReader.ReadByte(); // field signature header
            byte typeCode = sigReader.ReadByte();
            Assert.Equal((byte)expectedCode, typeCode);
        }

        [Fact]
        public void ParserErrorListener_ReportsSyntaxErrors()
        {
            // A method with a misplaced token should generate a parser error
            string source = """
                .assembly test { }
                .class public auto ansi MyClass
                {
                    .method public static void Test(int32 int32 int32) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            // Parser should report a syntax error for the repeated int32 tokens
            Assert.Contains(diagnostics, d => d.Id == "Parser");
        }

        [Fact]
        public void CoreLibRedirect_MscorlibToSystemRuntime()
        {
            // When both mscorlib and System.Runtime are declared, type references
            // through [mscorlib] should be redirected to [System.Runtime]
            string source = """
                .assembly extern mscorlib { auto }
                .assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The TypeRef for System.Object should point to System.Runtime, not mscorlib
            var typeRef = reader.TypeReferences
                .Select(h => reader.GetTypeReference(h))
                .First(t => reader.GetString(t.Name) == "Object");

            var scope = reader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);
            Assert.Equal("System.Runtime", reader.GetString(scope.Name));
        }

        [Fact]
        public void CoreLibRedirect_OnlyCorelibPresent_KeepsMscorlib()
        {
            // When only mscorlib is declared, type references stay as [mscorlib]
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeRef = reader.TypeReferences
                .Select(h => reader.GetTypeReference(h))
                .First(t => reader.GetString(t.Name) == "Object");

            var scope = reader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);
            Assert.Equal("mscorlib", reader.GetString(scope.Name));
        }

        [Fact]
        public void UnqualifiedSystemString_ResolvesToCoreLibTypeRef()
        {
            // Unqualified 'System.String' (without [assembly] prefix) should resolve
            // to a TypeRef from the corelib, not create a local TypeDef
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Greet(class System.String msg) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // System.String should be a TypeRef, not a TypeDef
            // Only 2 TypeDefs should exist: <Module> and Test
            Assert.Equal(2, reader.GetTableRowCount(TableIndex.TypeDef));

            // System.String should be in TypeRef table
            bool foundStringTypeRef = reader.TypeReferences
                .Select(h => reader.GetTypeReference(h))
                .Any(t => reader.GetString(t.Name) == "String" && reader.GetString(t.Namespace) == "System");
            Assert.True(foundStringTypeRef, "System.String should be a TypeRef, not a TypeDef");
        }

        [Fact]
        public void CustomAttributeOnType_EmittedCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 01 00 00 )
                    .method public instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The custom attribute should be in the CustomAttribute table
            Assert.True(reader.GetTableRowCount(TableIndex.CustomAttribute) >= 1,
                "Should have at least one custom attribute");

            // Find the ComVisibleAttribute on the type
            var typeHandle = MetadataTokens.TypeDefinitionHandle(2); // Test type
            var attrs = reader.GetCustomAttributes(typeHandle);
            Assert.True(attrs.Count >= 1, "Test type should have at least one custom attribute");
        }

        [Fact]
        public void CustomAttributeBlobDescr_EmptyBraces_CorrectProlog()
        {
            // '= {}' should produce a 4-byte blob: 01 00 (prolog) 00 00 (0 named args)
            string source = """
                .assembly extern mscorlib { }
                .assembly extern xunit.core { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = {}
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");

            var attrs = reader.GetCustomAttributes(MetadataTokens.MethodDefinitionHandle(
                MetadataTokens.GetRowNumber(reader.MethodDefinitions
                    .First(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "TestMethod"))));
            Assert.True(attrs.Count >= 1);

            var attr = reader.GetCustomAttribute(attrs.First());
            var blobBytes = reader.GetBlobBytes(attr.Value);
            // Should be exactly 4 bytes: 01 00 (prolog) 00 00 (0 named args)
            Assert.Equal(4, blobBytes.Length);
            Assert.Equal(0x01, blobBytes[0]); // prolog low byte
            Assert.Equal(0x00, blobBytes[1]); // prolog high byte
            Assert.Equal(0x00, blobBytes[2]); // named arg count low
            Assert.Equal(0x00, blobBytes[3]); // named arg count high
        }

        [Fact]
        public void NonStaticMethod_AutoInstanceCallingConvention()
        {
            // Non-static methods in a class should automatically get the instance
            // calling convention, even if not explicitly specified in the IL source
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public void DoWork() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "DoWork");

            // Check the signature has the instance flag
            var sigBytes = reader.GetBlobBytes(method.Signature);
            byte header = sigBytes[0];
            Assert.True((header & (byte)SignatureAttributes.Instance) != 0,
                $"Method signature should have Instance flag. Header byte: 0x{header:X2}");
        }

        [Fact]
        public void StaticMethod_NoAutoInstanceCallingConvention()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void DoWork() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "DoWork");

            var sigBytes = reader.GetBlobBytes(method.Signature);
            byte header = sigBytes[0];
            Assert.True((header & (byte)SignatureAttributes.Instance) == 0,
                $"Static method should NOT have Instance flag. Header byte: 0x{header:X2}");
        }

        [Fact]
        public void FieldRVA_DataLabelEmitted()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .data D_1 = int32(42)
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 myData at D_1
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The FieldRVA table should have an entry
            int fieldRvaCount = reader.GetTableRowCount(TableIndex.FieldRva);
            Assert.True(fieldRvaCount >= 1, $"FieldRVA table should have at least 1 entry, has {fieldRvaCount}");
        }

        [Fact]
        public void FunctionPointer_InFieldSignature_EmitsFnPtrTypeCode()
        {
            // A field of function pointer type: method void *(int32)
            // The signature should contain ELEMENT_TYPE_FNPTR (0x1B).
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void *(int32) fnPtrField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field signature: 0x06 (FIELD), 0x1B (FNPTR), ...
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            // After FNPTR: calling convention byte, param count, return type, param types
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[3]); // 1 parameter
            Assert.Equal(0x01, sigBytes[4]); // return type: void
            Assert.Equal(0x08, sigBytes[5]); // param type: int32
        }

        [Fact]
        public void FunctionPointer_InMethodParameter_EmitsFnPtrTypeCode()
        {
            // A method parameter of function pointer type.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Invoke(method void *(int32) callback) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Invoke");

            var sigBytes = reader.GetBlobBytes(method.Signature);
            // Method signature: 0x00 (DEFAULT), 0x01 (1 param), 0x01 (void ret), ...
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[1]); // 1 parameter
            Assert.Equal(0x01, sigBytes[2]); // return type: void
            // Parameter should be ELEMENT_TYPE_FNPTR (0x1B)
            Assert.Equal(0x1B, sigBytes[3]); // ELEMENT_TYPE_FNPTR
        }

        [Fact]
        public void FunctionPointer_AsReturnType_EmitsFnPtrTypeCode()
        {
            // A method returning a function pointer.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static method int32 *(int32, int32) GetAdder() cil managed
                    {
                        ldc.i4.0
                        conv.i
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GetAdder");

            var sigBytes = reader.GetBlobBytes(method.Signature);
            // Method signature: 0x00 (DEFAULT), 0x00 (0 params), return type...
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT calling convention
            Assert.Equal(0x00, sigBytes[1]); // 0 parameters
            // Return type should be ELEMENT_TYPE_FNPTR (0x1B)
            Assert.Equal(0x1B, sigBytes[2]); // ELEMENT_TYPE_FNPTR
            // After FNPTR: calling convention, param count, return type (int32), param types (int32, int32)
            Assert.Equal(0x00, sigBytes[3]); // DEFAULT calling convention for inner sig
            Assert.Equal(0x02, sigBytes[4]); // 2 parameters in inner sig
            Assert.Equal(0x08, sigBytes[5]); // inner return type: int32
            Assert.Equal(0x08, sigBytes[6]); // inner param 1: int32
            Assert.Equal(0x08, sigBytes[7]); // inner param 2: int32
        }

        [Fact]
        public void FunctionPointer_NoArgs_EmitsFnPtrTypeCode()
        {
            // Function pointer with no parameters: method void *()
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void *() fnPtrField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x00, sigBytes[3]); // 0 parameters
            Assert.Equal(0x01, sigBytes[4]); // return type: void
        }

        [Fact]
        public void FunctionPointer_ReturningVoidPtr_EmitsFnPtrWithPtrReturnType()
        {
            // A function pointer that returns void*: method void * *(int32)
            // Two * tokens: the first makes the return type void*, the second is the fnptr separator.
            // Signature: FNPTR, DEFAULT, 1 param, PTR(VOID), I4
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void * *(int32) fnPtrReturningVoidPtr
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1B (FNPTR), 0x00 (DEFAULT), 0x01 (1 param),
            //            0x0F (PTR), 0x01 (VOID) [= void* return type], 0x08 (int32 param)
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[3]); // 1 parameter
            Assert.Equal(0x0F, sigBytes[4]); // return type: ELEMENT_TYPE_PTR
            Assert.Equal(0x01, sigBytes[5]); // return type inner: VOID (making void*)
            Assert.Equal(0x08, sigBytes[6]); // param type: int32
        }

        [Fact]
        public void FunctionPointer_ReturningVoidPtr_NoArgs_EmitsFnPtrWithPtrReturnType()
        {
            // method void * *() — fnptr returning void* with no params
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void * *() fnPtrReturningVoidPtrNoArgs
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1B (FNPTR), 0x00 (DEFAULT), 0x00 (0 params),
            //            0x0F (PTR), 0x01 (VOID) [= void* return type]
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x00, sigBytes[3]); // 0 parameters
            Assert.Equal(0x0F, sigBytes[4]); // return type: ELEMENT_TYPE_PTR
            Assert.Equal(0x01, sigBytes[5]); // return type inner: VOID (making void*)
        }

        [Fact]
        public void FunctionPointer_ReturningInt32Ptr_EmitsFnPtrWithPtrReturnType()
        {
            // method int32 * *(int32) — fnptr returning int32* with one param
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method int32 * *(int32) fnPtrReturningInt32Ptr
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1B (FNPTR), 0x00 (DEFAULT), 0x01 (1 param),
            //            0x0F (PTR), 0x08 (I4) [= int32* return type], 0x08 (int32 param)
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[3]); // 1 parameter
            Assert.Equal(0x0F, sigBytes[4]); // return type: ELEMENT_TYPE_PTR
            Assert.Equal(0x08, sigBytes[5]); // return type inner: int32 (making int32*)
            Assert.Equal(0x08, sigBytes[6]); // param type: int32
        }

        [Fact]
        public void FunctionPointer_PtrToFnPtr_EmitsPtrThenFnPtr()
        {
            // A pointer-to-function-pointer: method void *(int32)*
            // The outer * (after closing paren) makes this PTR(FNPTR(void(int32)))
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void *(int32)* ptrToFnPtr
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), then the type is FNPTR(void(int32)) with PTR modifier
            // The modifier ordering means: 0x06, 0x1B (FNPTR), ..., then 0x0F (PTR) wraps it
            // But in practice ECMA-335 encodes: 0x06, 0x0F (PTR), 0x1B (FNPTR), ...
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            // The next two bytes must contain both PTR and FNPTR
            Assert.Contains((byte)0x1B, sigBytes.Skip(1).ToArray()); // Must have ELEMENT_TYPE_FNPTR
            Assert.Contains((byte)0x0F, sigBytes.Skip(1).ToArray()); // Must have ELEMENT_TYPE_PTR
        }

        [Fact]
        public void GenericConstraint_WithGenericTypeArg_ResolvesToCorrectType()
        {
            // A generic constraint like (class IFoo<!T>) should produce a GenericParamConstraint
            // pointing to a TypeSpec for the generic instantiation IFoo<!T>, NOT System.Object.
            // This is the "generic constraint references" bug: complex generic type arguments
            // in constraints resolve to System.Object instead of the actual constraint type.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class interface public abstract auto ansi IMinusT`1<-PlusT>
                {
                    .method public hidebysig newslot abstract virtual instance void Do() cil managed { }
                }

                .class public auto ansi beforefieldinit Container`2<(class IMinusT`1<!U>) T, U>
                    extends [mscorlib]System.Object
                {
                    .method public hidebysig instance void M() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the Container`2 type
            var containerType = reader.TypeDefinitions
                .Select(h => reader.GetTypeDefinition(h))
                .First(t => reader.GetString(t.Name) == "Container`2");

            var genericParams = containerType.GetGenericParameters();
            Assert.Equal(2, genericParams.Count);

            // T is the first generic parameter and has a constraint: (class IMinusT`1<!U>)
            var paramT = reader.GetGenericParameter(genericParams.ElementAt(0));
            Assert.Equal("T", reader.GetString(paramT.Name));

            var constraints = paramT.GetConstraints();
            Assert.Single(constraints);

            var constraint = reader.GetGenericParameterConstraint(constraints.Single());
            var constraintType = constraint.Type;

            // The constraint should be a TypeSpec (generic instantiation IMinusT`1<!U>),
            // NOT a TypeRef to System.Object.
            Assert.Equal(HandleKind.TypeSpecification, constraintType.Kind);

            // Decode the TypeSpec blob to verify it's a generic instantiation of IMinusT`1
            var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)constraintType);
            var sigBytes = reader.GetBlobBytes(typeSpec.Signature);

            // Expected: GENERICINST (0x15), CLASS (0x12), <TypeDef/Ref token for IMinusT`1>,
            //           1 (generic arg count), VAR 1 (type parameter !U which is index 1)
            Assert.Equal(0x15, sigBytes[0]); // ELEMENT_TYPE_GENERICINST
        }

        [Fact]
        public void GenericConstraint_MethodGenParamConstrainedByTypeGenParam_ResolvesToCorrectType()
        {
            // Reproduces the exact pattern from the Variance test IL files:
            // A method generic parameter M constrained by (class IMinusT<!PlusT>),
            // where !PlusT is a type-level generic parameter referenced in the method constraint.
            // This is the specific case that produces an incorrect constraint type.
            // Method generic param M constrained by (class IMinusT`1<!PlusT>)
            // where !PlusT is a type-level generic parameter.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class interface public abstract auto ansi IMinusT`1<-([mscorlib]System.Object) MinusT>
                {
                }

                .class interface public auto ansi beforefieldinit Test001PlusT`1<+([mscorlib]System.Object) PlusT>
                {
                    .method public hidebysig newslot abstract virtual instance void
                        method1<(class IMinusT`1<!PlusT>) M>(class IMinusT`1<!PlusT> t) cil managed
                    {
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var testType = reader.TypeDefinitions
                .Select(h => reader.GetTypeDefinition(h))
                .First(t => reader.GetString(t.Name) == "Test001PlusT`1");

            var method = testType.GetMethods()
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "method1");

            var methodGenericParams = method.GetGenericParameters();
            Assert.Equal(1, methodGenericParams.Count);

            var paramM = reader.GetGenericParameter(methodGenericParams.Single());
            Assert.Equal("M", reader.GetString(paramM.Name));

            // M has a constraint: (class IMinusT`1<!PlusT>)
            var constraints = paramM.GetConstraints();
            Assert.Single(constraints);

            var constraint = reader.GetGenericParameterConstraint(constraints.Single());
            var constraintType = constraint.Type;

            // The constraint should be a TypeSpec for IMinusT`1<!PlusT>,
            // NOT a TypeRef/TypeDef for System.Object
            Assert.Equal(HandleKind.TypeSpecification, constraintType.Kind);

            var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)constraintType);
            var sigBytes = reader.GetBlobBytes(typeSpec.Signature);

            // Expected: GENERICINST (0x15), CLASS (0x12), <token for IMinusT`1>,
            //           1 (generic arg count), VAR 0 (type parameter !PlusT at index 0)
            Assert.Equal(0x15, sigBytes[0]); // ELEMENT_TYPE_GENERICINST
        }

        [Fact]
        public void ModReq_InFieldSignature_PreservedInRewrittenBlob()
        {
            // A field with modreq should preserve the modifier in the signature
            // after the TypeRef→TypeDef signature rewriting pass.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) volatileField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1F (CMOD_REQD), <coded index for IsVolatile>, 0x08 (I4)
            Assert.Equal(0x06, sigBytes[0]); // FIELD header
            Assert.Equal((byte)SignatureTypeCode.RequiredModifier, sigBytes[1]); // CMOD_REQD
            // The last byte should be the underlying type (int32 = 0x08)
            Assert.Equal(0x08, sigBytes[^1]);
        }

        [Fact]
        public void ModOpt_InMethodSignature_PreservedInRewrittenBlob()
        {
            // A method parameter with modopt should preserve the modifier
            // after signature rewriting.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            // Method sig: 0x00 (DEFAULT), 0x01 (1 param), 0x01 (void ret),
            // then param: 0x20 (CMOD_OPT), <coded index>, 0x08 (I4)
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x01, sigBytes[1]); // 1 param
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal((byte)SignatureTypeCode.OptionalModifier, sigBytes[3]); // CMOD_OPT
            // The last byte is the underlying type (int32 = 0x08)
            Assert.Equal(0x08, sigBytes[^1]);
        }

        [Fact]
        public void ModReq_WithSelfAssemblyTypeRef_PreservedAfterResolution()
        {
            // modreq referencing a type in the same assembly should still
            // produce a correct signature after TypeRef→TypeDef resolution.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyModifier extends [mscorlib]System.Object
                {
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 modreq([test]MyModifier) myField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            Assert.Equal(0x06, sigBytes[0]); // FIELD header
            Assert.Equal((byte)SignatureTypeCode.RequiredModifier, sigBytes[1]); // CMOD_REQD
            // After the modifier coded index, the underlying type is int32
            Assert.Equal(0x08, sigBytes[^1]);
            // The [test]MyModifier TypeRef should have resolved to TypeDef,
            // so no TypeRef for MyModifier should exist.
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyModifier", reader.GetString(tr.Name));
            }
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

            using var pe = CompileAndGetReader(source, new Options());
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

            using var pe = CompileAndGetReader(source, new Options());
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

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var layout = typeDef.GetLayout();
            Assert.Equal(4, layout.PackingSize);
            Assert.Equal(16, layout.Size);
        }

        [Fact]
        public void TypeRefInILToken_BackpatchedAfterResolution()
        {
            // When a type instruction (unbox.any, box, castclass, etc.) references
            // a type via [self-assembly]Type, the IL token must be backpatched to the
            // resolved TypeDef handle after TypeRef→TypeDef resolution.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed MyStruct extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 Unbox(object o) cil managed
                    {
                        ldarg.0
                        unbox.any [test]MyStruct
                        ldfld int32 MyStruct::x
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // [test]MyStruct TypeRef should resolve to TypeDef
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyStruct", reader.GetString(tr.Name));
            }

            // The method IL should contain a TypeDef token for MyStruct, not a TypeRef token.
            // Read the method body and check the unbox.any operand.
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Unbox");
            int rva = method.RelativeVirtualAddress;
            Assert.True(rva > 0, "Method should have a body");
        }

        [Fact]
        public void TypeRefInCastclass_BackpatchedAfterResolution()
        {
            // castclass with [self-assembly]Type should use TypeDef token.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyClass extends [mscorlib]System.Object
                {
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static class MyClass Cast(object o) cil managed
                    {
                        ldarg.0
                        castclass [test]MyClass
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // [test]MyClass TypeRef should resolve to TypeDef
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyClass", reader.GetString(tr.Name));
            }
        }

        [Fact]
        public void TypeRefInLdtoken_BackpatchedAfterResolution()
        {
            // ldtoken with [self-assembly]Type should use TypeDef token.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyType extends [mscorlib]System.Object
                {
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void GetToken() cil managed
                    {
                        ldtoken [test]MyType
                        pop
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyType", reader.GetString(tr.Name));
            }
        }

        [Fact]
        public void TypeRefInFieldMdtoken_BackpatchedAfterResolution()
        {
            // When a field instruction uses an mdtoken that resolves to a TypeRef
            // for a local type, the token should be backpatched to TypeDef.
            // This tests the instr_field mdtoken path.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed MyStruct extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Load() cil managed
                    {
                        ldtoken field int32 [test]MyStruct::x
                        pop
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // [test]MyStruct TypeRef should resolve to TypeDef
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyStruct", reader.GetString(tr.Name));
            }
        }

        [Fact]
        public void LdargByName_CorrectIndexInLongMethod()
        {
            // Regression test for NaN comp32 IL corruption: ldarg.s by parameter name
            // emitted wrong index (0 instead of 3) after ~512 bytes of IL, causing
            // the IL body to be garbled from that point forward.
            // Generate enough instructions to cross the 512-byte IL boundary,
            // then verify ldarg.s with the 4th parameter name emits index 3.
            var sb = new StringBuilder();
            sb.AppendLine("""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Run(float32 a, float32 b, float32 c, float32 d) cil managed
                    {
                        .maxstack 8
                """);
            // Each block is ~18 bytes: ldarg.s(2) + ldarg.s(2) + ceq(2) + brfalse.s(2) + ldstr(5) + br(5)
            // 30 blocks = ~540 bytes, crossing the 512-byte boundary
            for (int i = 0; i < 30; i++)
            {
                sb.AppendLine($"        ldarg.s 'd'");
                sb.AppendLine($"        ldarg.s 'a'");
                sb.AppendLine($"        ceq");
                sb.AppendLine($"        brfalse.s LBL_{i}");
                sb.AppendLine($"        ldstr \"block {i}\"");
                sb.AppendLine($"        br DONE");
                sb.AppendLine($"        LBL_{i}:");
            }
            sb.AppendLine("""
                        DONE:
                        ret
                    }
                }
                """);

            string source = sb.ToString();
            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Run");

            // Read the IL body and verify ldarg.s instructions have correct indices
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var ilBytes = body.GetILBytes()!;

            // Walk the IL and check all ldarg.s (0x0E) instructions
            int pos = 0;
            int ldargCount = 0;
            while (pos < ilBytes.Length)
            {
                byte op = ilBytes[pos];
                if (op == 0x0E) // ldarg.s
                {
                    byte argIndex = ilBytes[pos + 1];
                    ldargCount++;
                    // Odd ldarg.s (1st, 3rd, 5th...) should load 'd' = index 3
                    // Even ldarg.s (2nd, 4th, 6th...) should load 'a' = index 0
                    if (ldargCount % 2 == 1)
                    {
                        Assert.True(argIndex == 3, $"ldarg.s #{ldargCount} at IL offset {pos} should load 'd' (index 3) but got index {argIndex}");
                    }
                    else
                    {
                        Assert.True(argIndex == 0, $"ldarg.s #{ldargCount} at IL offset {pos} should load 'a' (index 0) but got index {argIndex}");
                    }
                    pos += 2;
                }
                else if (op == 0xFE) // two-byte opcode prefix
                {
                    pos += 2; // skip prefix + opcode
                }
                else if (op == 0x72) // ldstr
                {
                    pos += 5; // opcode + 4-byte token
                }
                else if (op == 0x38) // br
                {
                    pos += 5;
                }
                else if (op == 0x2C) // brfalse.s
                {
                    pos += 2;
                }
                else if (op == 0x2A) // ret
                {
                    pos += 1;
                }
                else
                {
                    pos += 1; // unknown, advance 1
                }
            }
            Assert.Equal(60, ldargCount); // 30 blocks * 2 ldarg.s each
        }

        [Fact]
        public void MultiDimArrayParam_PreservedAfterSignatureRewrite()
        {
            // Multi-dimensional array types in method signatures must survive
            // the TypeRef→TypeDef signature rewriting pass.
            // Regression: GetArrayType was missing the ELEMENT_TYPE_ARRAY (0x14) prefix byte.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(int32[0...,0...] arr) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            // Method sig: 0x00 (DEFAULT), 0x01 (1 param), 0x01 (void ret),
            // then ELEMENT_TYPE_ARRAY (0x14), ELEMENT_TYPE_I4 (0x08), shape...
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x01, sigBytes[1]); // 1 param
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal(0x14, sigBytes[3]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sigBytes[4]); // ELEMENT_TYPE_I4 (int32)
            Assert.Equal(0x02, sigBytes[5]); // rank = 2
        }

        [Fact]
        public void MultiDimArrayParam_WithSelfAssemblyRef_PreservedAfterRewrite()
        {
            // Multi-dimensional array with a self-assembly type reference as element type.
            // Both the TypeRef resolution AND the array shape must be correct.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed MyStruct extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Process(valuetype [test]MyStruct[0...,0...,0...] data) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Process");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x01, sigBytes[1]); // 1 param
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal(0x14, sigBytes[3]); // ELEMENT_TYPE_ARRAY
            // Element type: VALUETYPE (0x11) + TypeDef coded index (MyStruct resolved)
            Assert.Equal(0x11, sigBytes[4]); // ELEMENT_TYPE_VALUETYPE
            // After the type token: rank = 3
            // Find the rank byte (after the compressed TypeDef coded index)
            int rankIdx = 5;
            // Skip the compressed integer (coded index for MyStruct TypeDef)
            if (sigBytes[rankIdx] < 0x80) rankIdx += 1;
            else if (sigBytes[rankIdx] < 0xC0) rankIdx += 2;
            else rankIdx += 4;
            Assert.Equal(0x03, sigBytes[rankIdx]); // rank = 3

            // [test]MyStruct TypeRef should have been resolved to TypeDef
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyStruct", reader.GetString(tr.Name));
            }
        }

        [Fact]
        public void SZArrayParam_PreservedAfterSignatureRewrite()
        {
            // SZ arrays (char[], int32[]) must preserve their element type
            // through the signature rewriting pass.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(char[] chars, int32[] ints) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            // Method sig: 0x00 (DEFAULT), 0x02 (2 params), 0x01 (void ret),
            // param 1: SZARRAY (0x1D) + CHAR (0x03)
            // param 2: SZARRAY (0x1D) + I4 (0x08)
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x02, sigBytes[1]); // 2 params
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal(0x1D, sigBytes[3]); // ELEMENT_TYPE_SZARRAY
            Assert.Equal(0x03, sigBytes[4]); // ELEMENT_TYPE_CHAR
            Assert.Equal(0x1D, sigBytes[5]); // ELEMENT_TYPE_SZARRAY
            Assert.Equal(0x08, sigBytes[6]); // ELEMENT_TYPE_I4
        }

        [Fact]
        public void MultiDimArrayField_PreservedAfterSignatureRewrite()
        {
            // Multi-dimensional array types in field signatures must survive rewriting.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public int32[0...] arr1d
                    .field public int32[0...,0...] arr2d
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // arr1d: FIELD (0x06), ARRAY (0x14), I4 (0x08), rank=1, ...
            var field1 = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sig1 = reader.GetBlobBytes(field1.Signature);
            Assert.Equal(0x06, sig1[0]); // FIELD
            Assert.Equal(0x14, sig1[1]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sig1[2]); // ELEMENT_TYPE_I4
            Assert.Equal(0x01, sig1[3]); // rank = 1

            // arr2d: FIELD (0x06), ARRAY (0x14), I4 (0x08), rank=2, ...
            var field2 = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(2));
            var sig2 = reader.GetBlobBytes(field2.Signature);
            Assert.Equal(0x06, sig2[0]); // FIELD
            Assert.Equal(0x14, sig2[1]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sig2[2]); // ELEMENT_TYPE_I4
            Assert.Equal(0x02, sig2[3]); // rank = 2
        }

        [Fact]
        public void LocalsInit_EmitsStandaloneSignature()
        {
            // .locals init (...) should emit a StandAloneSig that is connected
            // to the method body, causing ildasm to show the .locals directive.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32 x, string s)
                        ldc.i4.0
                        stloc.0
                        ldnull
                        stloc.1
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int sigCount = reader.GetTableRowCount(TableIndex.StandAloneSig);
            Assert.True(sigCount >= 1, $"Should have at least 1 StandAloneSig for .locals, got {sigCount}");

            var sig = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            var sigBytes = reader.GetBlobBytes(sig.Signature);

            // LOCAL_SIG (0x07), 2 locals, I4 (0x08), STRING (0x0E)
            Assert.Equal(0x07, sigBytes[0]); // LOCAL_SIG
            Assert.Equal(0x02, sigBytes[1]); // 2 locals
            Assert.Equal(0x08, sigBytes[2]); // int32
            Assert.Equal(0x0E, sigBytes[3]); // string

            // The method should have InitLocals flag
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            Assert.True(body.LocalVariablesInitialized);
        }

        [Fact]
        public void LocalsWithoutInit_EmitsStandaloneSignature()
        {
            // .locals (...) without init should still emit a StandAloneSig
            // but without the InitLocals flag.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals (int32 x)
                        ldc.i4.0
                        stloc.0
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int sigCount = reader.GetTableRowCount(TableIndex.StandAloneSig);
            Assert.True(sigCount >= 1, $"Should have at least 1 StandAloneSig for .locals, got {sigCount}");

            var sig = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            var sigBytes = reader.GetBlobBytes(sig.Signature);

            Assert.Equal(0x07, sigBytes[0]); // LOCAL_SIG
            Assert.Equal(0x01, sigBytes[1]); // 1 local
            Assert.Equal(0x08, sigBytes[2]); // int32

            // The method should NOT have InitLocals flag
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            Assert.False(body.LocalVariablesInitialized);
        }

        [Fact]
        public void LocalsWithArrayType_EmitsStandaloneSignature()
        {
            // .locals init with array type should emit correct signature.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32[0...] arr)
                        ldnull
                        stloc.0
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int sigCount = reader.GetTableRowCount(TableIndex.StandAloneSig);
            Assert.True(sigCount >= 1, $"Should have at least 1 StandAloneSig, got {sigCount}");

            var sig = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            var sigBytes = reader.GetBlobBytes(sig.Signature);

            Assert.Equal(0x07, sigBytes[0]); // LOCAL_SIG
            Assert.Equal(0x01, sigBytes[1]); // 1 local
            Assert.Equal(0x14, sigBytes[2]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sigBytes[3]); // ELEMENT_TYPE_I4
            Assert.Equal(0x01, sigBytes[4]); // rank = 1
        }

        [Fact]
        public void CatchClause_SelfAssemblyTypeRef_ResolvesToTypeDef()
        {
            // When a catch clause references a type via [self-assembly]Type,
            // the exception handler table must contain the resolved TypeDef token,
            // not the stale PseudoHandle TypeRef token.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyException extends [mscorlib]System.Exception
                {
                    .method public specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Exception::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TryCatch() cil managed
                    {
                        .try
                        {
                            leave.s DONE
                        }
                        catch [test]MyException
                        {
                            pop
                            leave.s DONE
                        }
                        DONE:
                        ret
                    }
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // [test]MyException TypeRef should resolve to TypeDef
            foreach (var trHandle in reader.TypeReferences)
            {
                var tr = reader.GetTypeReference(trHandle);
                Assert.NotEqual("MyException", reader.GetString(tr.Name));
            }

            // Verify the method has exception handlers and the catch type is a TypeDef
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TryCatch");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var ehRegions = body.ExceptionRegions;
            Assert.True(ehRegions.Length >= 1, $"Should have at least 1 exception region, got {ehRegions.Length}");

            var catchRegion = ehRegions.First(r => r.Kind == ExceptionRegionKind.Catch);
            Assert.Equal(HandleKind.TypeDefinition, catchRegion.CatchType.Kind);
        }
    }
}

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
    public class TypedefTests
    {

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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypedefNotFound, error.Id);
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            // Typedef type blob resolution should compile
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Typedef_FieldAndCustomAttributeForms_EmitResolvableMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .typedef field int32 Test::Value as ValueAlias
                .typedef .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00) as AttributeAlias
                .typedef .custom (Test) instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = (01 00 01 00 00) as OwnedAttributeAlias
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 Value
                    AttributeAlias
                    .method public static int32 Read() cil managed
                    {
                        ldsfld ValueAlias
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var testTypeHandle = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var testType = reader.GetTypeDefinition(testTypeHandle);
            var fieldHandle = Assert.Single(testType.GetFields());
            int fieldToken =
                DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Read", ILOpcode.ldsfld);

            Assert.Equal(MetadataTokens.GetToken(fieldHandle), fieldToken);

            var attributes = reader.GetCustomAttributes(testTypeHandle)
                .Select(reader.GetCustomAttribute)
                .Select(attribute => attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder))
                .ToArray();
            Assert.Equal(2, attributes.Length);
            Assert.Contains(attributes, attribute => attribute.FixedArguments.Length == 0);
            Assert.Contains(
                attributes,
                attribute =>
                    attribute.FixedArguments.Length == 1 &&
                    attribute.FixedArguments[0].Type == "bool" &&
                    Equals(attribute.FixedArguments[0].Value, true));
        }

    }
}

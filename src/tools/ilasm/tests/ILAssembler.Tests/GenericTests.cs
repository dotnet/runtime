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
    public class GenericTests
    {

        [Fact]
        public void TypeParameterOutsideType_ReportsError()
        {
            // Using a named type parameter reference outside of a generic type should report an error
            // Note: !0 (by index) is allowed for compat, but !T (by name) requires a type context
            string source = """
                .assembly extern System.Runtime { }
                .typedef !T as MyTypeParam
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypeParameterOutsideType, error.Id);
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.MethodTypeParameterOutsideMethod, error.Id);
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.GenericParameterNotFound, error.Id);
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.GenericParameterIndexOutOfRange, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GenericMethod");

            var genericParams = methodDef.GetGenericParameters();
            Assert.Single(genericParams);
        }

        [Fact]
        public void GenericType_MoreThanMetadataIndexRange_IsAcceptedForCompatibility()
        {
            const int GenericParameterCount = 65_537;
            StringBuilder source = new("""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test<
                """);

            for (int i = 0; i < GenericParameterCount; i++)
            {
                if (i != 0)
                {
                    source.Append(',');
                }
                if (i == GenericParameterCount - 1)
                {
                    source.Append("(class [mscorlib]System.Object) ");
                }
                source.Append('T').Append(i);
            }

            source.Append("> { }");

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source.ToString(), new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(ushort.MaxValue + 1, reader.GetTableRowCount(TableIndex.GenericParam));
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.GenericParamConstraint));
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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(1, methodImplCount);

            int typeSpecCount = reader.GetTableRowCount(TableIndex.TypeSpec);
            Assert.True(typeSpecCount >= 1, "Should have at least one TypeSpec for the generic instantiation");
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
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

    }
}

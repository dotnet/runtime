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
        public void DuplicateGenericParameterNames_PreserveFirstNameBindingInFieldSignature()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit DuplicateName`2<T, T> extends [mscorlib]System.Object
                {
                    .field public !T Value
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeDef = reader.TypeDefinitions
                .Select(h => reader.GetTypeDefinition(h))
                .First(t => reader.GetString(t.Name) == "DuplicateName`2");

            var genericParameters = typeDef.GetGenericParameters()
                .Select(reader.GetGenericParameter)
                .ToArray();
            Assert.Equal(2, genericParameters.Length);
            Assert.All(genericParameters, parameter => Assert.Equal("T", reader.GetString(parameter.Name)));

            var field = reader.GetFieldDefinition(typeDef.GetFields().Single());
            Assert.Equal("!0", field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void GenericParameterAttributesAndBounds_EmitExpectedMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class interface public abstract auto ansi IVariant`5<+T, -U, class .ctor V, valuetype byreflike W, flags(0x0004) X>
                {
                }
                .class public auto ansi Constrained`1<(class [mscorlib]System.IDisposable) T> extends [mscorlib]System.Object
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var variantType = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "IVariant`5");
            var parameters = variantType.GetGenericParameters()
                .Select(reader.GetGenericParameter)
                .ToDictionary(parameter => reader.GetString(parameter.Name));

            Assert.Equal(GenericParameterAttributes.Covariant, parameters["T"].Attributes & GenericParameterAttributes.VarianceMask);
            Assert.Equal(GenericParameterAttributes.Contravariant, parameters["U"].Attributes & GenericParameterAttributes.VarianceMask);
            Assert.True(parameters["V"].Attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
            Assert.True(parameters["V"].Attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint));
            Assert.True(parameters["W"].Attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint));
            Assert.True(((int)parameters["W"].Attributes & 0x20) != 0);
            Assert.Equal((GenericParameterAttributes)0x0004, parameters["X"].Attributes);

            var constrainedType = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "Constrained`1");
            var constrainedParameter = reader.GetGenericParameter(Assert.Single(constrainedType.GetGenericParameters()));
            var constraint = reader.GetGenericParameterConstraint(Assert.Single(constrainedParameter.GetConstraints()));

            Assert.Equal(HandleKind.TypeSpecification, constraint.Type.Kind);
            Assert.Equal(
                "[mscorlib]System.IDisposable",
                reader.GetTypeSpecification((TypeSpecificationHandle)constraint.Type)
                    .DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void ClassGenericParameterAndConstraintDirectives_AttachCustomAttributes()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi ByName`1<([mscorlib]System.ICloneable) T> extends [mscorlib]System.Object
                {
                    .param type T
                        .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = (01 00 01 00 00)
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                    .param constraint T, [mscorlib]System.ICloneable
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                        .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = (01 00 01 00 00)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var types = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Where(definition => reader.GetString(definition.Name) == "ByName`1")
                .ToDictionary(definition => reader.GetString(definition.Name));

            AssertGenericParameterAnnotation(reader, types["ByName`1"], "ICloneable");
        }

        [Fact]
        public void MethodGenericParameterAndConstraintDirectives_AttachCustomAttributes()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M<T>() cil managed
                    {
                        .param type [0]
                            .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                        .param type T
                            .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = (01 00 01 00 00)
                        .param constraint [0], [mscorlib]System.IDisposable
                            .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                        .param constraint T, [mscorlib]System.ICloneable
                            .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.GetMethodDefinition(Assert.Single(reader.MethodDefinitions));
            var parameterHandle = Assert.Single(method.GetGenericParameters());
            var parameter = reader.GetGenericParameter(parameterHandle);
            var constraints = parameter.GetConstraints().ToArray();

            Assert.Equal(2, reader.GetCustomAttributes(parameterHandle).Count);
            Assert.Equal(2, constraints.Length);
            Assert.All(constraints, constraint => Assert.Single(reader.GetCustomAttributes(constraint)));
            Assert.Equal(
                new[] { "ICloneable", "IDisposable" },
                constraints
                    .Select(reader.GetGenericParameterConstraint)
                    .Select(constraint => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)constraint.Type).Name))
                    .OrderBy(name => name));
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

            var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)constraintType);
            Assert.Equal(
                "IMinusT`1<!1>",
                typeSpec.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void RepeatedSelfReferentialConstraint_IsNotDuplicated()
        {
            string source = """
                .assembly test { }
                .class interface public abstract auto ansi I`1<(class I`1<!TSelf>) TSelf>
                {
                    .param constraint TSelf, class I`1<!TSelf>
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "I`1");
            var parameter = reader.GetGenericParameter(Assert.Single(type.GetGenericParameters()));

            Assert.Single(parameter.GetConstraints());
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
            Assert.Equal(
                "IMinusT`1<!0>",
                typeSpec.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void VariantGenericParameters_EmitVarianceFlags()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class interface public abstract auto ansi IVariant`2<+([mscorlib]System.Object) TOut, -([mscorlib]System.Object) TIn>
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var type = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(definition => reader.GetString(definition.Name) == "IVariant`2");
            var parameters = type.GetGenericParameters()
                .Select(reader.GetGenericParameter)
                .ToArray();

            Assert.Equal(2, parameters.Length);
            Assert.Equal("TOut", reader.GetString(parameters[0].Name));
            Assert.Equal("TIn", reader.GetString(parameters[1].Name));
            Assert.True((parameters[0].Attributes & GenericParameterAttributes.Covariant) != 0);
            Assert.True((parameters[1].Attributes & GenericParameterAttributes.Contravariant) != 0);
        }

        [Fact]
        public void GenericFieldAndMethod_EmitVarAndMVarSignatures()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit GenericBox`1<T> extends [mscorlib]System.Object
                {
                    .field public !0 Value

                    .method public static !!0 Identity<U>(!!0 arg) cil managed
                    {
                        ldarg.0
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var type = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(definition => reader.GetString(definition.Name) == "GenericBox`1");
            var field = reader.GetFieldDefinition(type.GetFields().Single());
            var method = type.GetMethods()
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "Identity");

            Assert.Equal("!0", field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));

            var typeParameter = reader.GetGenericParameter(type.GetGenericParameters().Single());
            var methodParameter = reader.GetGenericParameter(method.GetGenericParameters().Single());
            Assert.Equal("T", reader.GetString(typeParameter.Name));
            Assert.Equal("U", reader.GetString(methodParameter.Name));

            MethodSignature<string> methodSignature =
                method.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.True(methodSignature.Header.IsGeneric);
            Assert.Equal(1, methodSignature.GenericParameterCount);
            Assert.Equal("!!0", methodSignature.ReturnType);
            Assert.Equal(new[] { "!!0" }, methodSignature.ParameterTypes);
        }

        private static void AssertGenericParameterAnnotation(
            MetadataReader reader,
            TypeDefinition type,
            string expectedConstraint)
        {
            var parameterHandle = Assert.Single(type.GetGenericParameters());
            var parameter = reader.GetGenericParameter(parameterHandle);
            var constraintHandle = Assert.Single(parameter.GetConstraints());
            var constraint = reader.GetGenericParameterConstraint(constraintHandle);

            Assert.Equal(2, reader.GetCustomAttributes(parameterHandle).Count);
            Assert.Equal(2, reader.GetCustomAttributes(constraintHandle).Count);
            Assert.Equal(HandleKind.TypeReference, constraint.Type.Kind);
            Assert.Equal(
                expectedConstraint,
                reader.GetString(reader.GetTypeReference((TypeReferenceHandle)constraint.Type).Name));
        }

    }
}

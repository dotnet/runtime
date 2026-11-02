// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using Internal.IL;
using Xunit;

namespace ILAssembler.Tests
{
    internal static class DocumentCompilerTestHelpers
    {
        internal static MetadataStringDecoder Decoder { get; } = new();

        internal static PEReader CompileAndGetReader(string source, Options options)
        {
            var sourceText = new SourceText(source, "test.il");
            return CompileAndGetReader(sourceText, _ =>
            {
                Assert.Fail("Expected no includes");
                return default;
            }, _ =>
            {
                Assert.Fail("Expected no resources");
                return default;
            }, options);
        }

        internal static PEReader CompileAndGetReader(SourceText sourceText, Func<string, SourceText> includedDocumentLoader, Options options)
        {
            return CompileAndGetReader(sourceText, includedDocumentLoader, _ =>
            {
                Assert.Fail("Expected no resources");
                return default;
            }, options);
        }

        internal static PEReader CompileAndGetReader(SourceText sourceText, Func<string, SourceText> includedDocumentLoader, Func<string, byte[]?> resourceLocator, Options options)
        {
            var documentCompiler = new DocumentCompiler();
            var (diagnostics, result) = documentCompiler.Compile(sourceText, includedDocumentLoader, resourceLocator, options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            var blobBuilder = new BlobBuilder();
            result!.Serialize(blobBuilder);
            return new PEReader(blobBuilder.ToImmutableArray());
        }

        internal static ImmutableArray<byte> Compile(string source, Options options)
        {
            var (diagnostics, result) = CompileWithDiagnostics(source, options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            return Serialize(result);
        }

        internal static (ImmutableArray<Diagnostic> Diagnostics, CompilationResult? Result) CompileWithDiagnostics(string source, Options options)
        {
            var sourceText = new SourceText(source, "test.il");
            var documentCompiler = new DocumentCompiler();
            var (diagnostics, result) = documentCompiler.Compile(sourceText, _ =>
            {
                Assert.Fail("Expected no includes");
                return default;
            }, _ => { Assert.Fail("Expected no resources"); return default; }, options);
            return (diagnostics, result);
        }

        internal static ImmutableArray<byte> Serialize(CompilationResult result)
        {
            var blobBuilder = new BlobBuilder();
            result.Serialize(blobBuilder);
            return blobBuilder.ToImmutableArray();
        }

        internal static string MethodSource(string instructions, string methodHeader = "public static void M(int32 arg)") => $$"""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public Test
            {
                .method {{methodHeader}} cil managed
                {
                    .maxstack 8
                    .locals init (int32 local)
                    {{instructions}}
                }
            }
            """;

        internal static MethodBodyBlock GetMethodBody(PEReader pe, string methodName = "M")
        {
            MetadataReader reader = pe.GetMetadataReader();
            MethodDefinition method = reader.MethodDefinitions.Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == methodName);
            return pe.GetMethodBody(method.RelativeVirtualAddress);
        }

        internal static byte[] CompileMethodIL(string instructions, Options options, string methodHeader = "public static void M(int32 arg)")
        {
            using PEReader pe = CompileAndGetReader(MethodSource(instructions, methodHeader), options);
            return GetMethodBody(pe).GetILBytes()!;
        }

        internal static ImmutableArray<byte> CompileAndGetImageBytes(string source, Options options)
        {
            return Compile(source, options);
        }

        internal static CompilationResult CompileAndGetResult(string source, Options options)
        {
            var (diagnostics, result) = CompileWithDiagnostics(source, options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            return result!;
        }

        /// <summary>Gets the Portable PDB that the compilation returned for writing beside the image.</summary>
        internal static ImmutableArray<byte> GetPortablePdb(CompilationResult result)
        {
            Assert.True(result.PortablePdb.HasValue, "Expected the compilation to produce a Portable PDB");
            return result.PortablePdb!.Value;
        }

        internal static MetadataReaderProvider GetPortablePdbReaderProvider(CompilationResult result)
            => MetadataReaderProvider.FromPortablePdbImage(GetPortablePdb(result));

        internal static ImmutableArray<byte> CompileAndGetPortablePdb(string source, Options options)
            => GetPortablePdb(CompileAndGetResult(source, options));

        /// <summary>
        /// Compiles a program with one method, with a PDB and deterministically, and returns the image and the PDB
        /// that its CodeView entry refers to. Different names give different PDB ids: the method's sequence point
        /// is in a document named after it, and the PDB does not record method names.
        /// </summary>
        internal static (ImmutableArray<byte> Image, ImmutableArray<byte> Pdb) CompileImageAndPdb(string name)
        {
            CompilationResult result = CompileAndGetResult(OneMethodSource(name, $".line 1 '{name}.cs'"), new Options { Debug = true, Deterministic = true });
            return (Serialize(result), GetPortablePdb(result));
        }

        /// <summary>Compiles a program with one method without a PDB: the image has no debug directory.</summary>
        internal static ImmutableArray<byte> CompileImageWithoutPdb(string name)
            => Compile(OneMethodSource(name, lineDirective: string.Empty), new Options { Deterministic = true });

        private static string OneMethodSource(string methodName, string lineDirective) => $$"""
            .assembly extern System.Runtime { }
            .assembly test { }
            .class public auto ansi beforefieldinit Test
            {
                .method public static void {{methodName}}() cil managed
                {
                    {{lineDirective}}
                    ret
                }
            }
            """;

        internal static int GetFirstTokenOperand(PEReader pe, MetadataReader reader, string methodName, ILOpcode targetOpcode)
        {
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(m => reader.GetString(m.Name) == methodName);
            Assert.True(method.RelativeVirtualAddress > 0, $"Method '{methodName}' should have a body");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            byte[] il = body.GetILBytes()!;

            int offset = 0;
            while (offset < il.Length)
            {
                var opcode = (ILOpcode)il[offset];
                int operandStart = offset + 1;
                if (opcode == ILOpcode.prefix1)
                {
                    opcode = (ILOpcode)(0x100 + il[offset + 1]);
                    operandStart = offset + 2;
                }

                if (opcode == targetOpcode)
                {
                    return BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(operandStart));
                }

                if (opcode == ILOpcode.switch_)
                {
                    uint count = BinaryPrimitives.ReadUInt32LittleEndian(il.AsSpan(operandStart));
                    offset = operandStart + 4 + checked((int)count * 4);
                }
                else
                {
                    offset += opcode.GetSize();
                }
            }

            Assert.Fail($"Opcode '{targetOpcode}' not found in method '{methodName}'");
            return 0;
        }

        internal static ImmutableArray<int> GetTokenOperands(
            PEReader pe,
            MetadataReader reader,
            string methodName,
            ILOpcode targetOpcode)
        {
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == methodName);
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
            var operands = ImmutableArray.CreateBuilder<int>();

            int offset = 0;
            while (offset < il.Length)
            {
                var opcode = (ILOpcode)il[offset];
                int operandStart = offset + 1;
                if (opcode == ILOpcode.prefix1)
                {
                    opcode = (ILOpcode)(0x100 + il[offset + 1]);
                    operandStart = offset + 2;
                }

                if (opcode == targetOpcode)
                {
                    operands.Add(BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(operandStart)));
                }

                if (opcode == ILOpcode.switch_)
                {
                    uint count = BinaryPrimitives.ReadUInt32LittleEndian(il.AsSpan(operandStart));
                    offset = operandStart + 4 + checked((int)count * 4);
                }
                else
                {
                    offset += opcode.GetSize();
                }
            }

            return operands.ToImmutable();
        }

        internal static void AssertTypeDefToken(MetadataReader reader, int token, string expectedName)
        {
            var handle = MetadataTokens.EntityHandle(token);
            Assert.Equal(HandleKind.TypeDefinition, handle.Kind);
            var typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            Assert.Equal(expectedName, reader.GetString(typeDef.Name));
        }

        internal static void AssertFieldDefToken(MetadataReader reader, int token, string expectedName)
        {
            var handle = MetadataTokens.EntityHandle(token);
            Assert.Equal(HandleKind.FieldDefinition, handle.Kind);
            var fieldDef = reader.GetFieldDefinition((FieldDefinitionHandle)handle);
            Assert.Equal(expectedName, reader.GetString(fieldDef.Name));
        }

        internal static TypeReference AssertTypeRefToken(MetadataReader reader, int token, string expectedName)
        {
            var handle = MetadataTokens.EntityHandle(token);
            Assert.Equal(HandleKind.TypeReference, handle.Kind);
            var typeRef = reader.GetTypeReference((TypeReferenceHandle)handle);
            Assert.Equal(expectedName, reader.GetString(typeRef.Name));
            return typeRef;
        }

        internal static TypeReferenceHandle FindTypeRef(MetadataReader reader, string name)
        {
            foreach (var trHandle in reader.TypeReferences)
            {
                if (reader.GetString(reader.GetTypeReference(trHandle).Name) == name)
                {
                    return trHandle;
                }
            }

            Assert.Fail($"Expected a TypeRef row named '{name}'");
            return default;
        }

        internal static ImmutableArray<Diagnostic> CompileAndGetDiagnostics(string source, Options options)
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

        internal sealed class MetadataStringDecoder :
            ISignatureTypeProvider<string, object?>,
            ICustomAttributeTypeProvider<string>
        {
            public string GetArrayType(string elementType, ArrayShape shape)
            {
                var builder = new StringBuilder(elementType);
                builder.Append('[');
                for (int i = 0; i < shape.Rank; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    int lowerBound = i < shape.LowerBounds.Length ? shape.LowerBounds[i] : 0;
                    if (i < shape.LowerBounds.Length)
                    {
                        builder.Append(lowerBound);
                    }

                    builder.Append("...");
                    if (i < shape.Sizes.Length)
                    {
                        builder.Append(lowerBound + shape.Sizes[i] - 1);
                    }
                }

                builder.Append(']');
                return builder.ToString();
            }

            public string GetByReferenceType(string elementType) => elementType + "&";

            public string GetFunctionPointerType(MethodSignature<string> signature)
            {
                var builder = new StringBuilder("method ");
                builder.Append(signature.ReturnType);
                builder.Append(" *(");
                for (int i = 0; i < signature.ParameterTypes.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    if (i == signature.RequiredParameterCount)
                    {
                        builder.Append("..., ");
                    }

                    builder.Append(signature.ParameterTypes[i]);
                }

                builder.Append(')');
                return builder.ToString();
            }

            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
                genericType + "<" + string.Join(",", typeArguments) + ">";

            public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;

            public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;

            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) =>
                unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifier + ")";

            public string GetPinnedType(string elementType) => elementType + " pinned";

            public string GetPointerType(string elementType) => elementType + "*";

            public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
            {
                PrimitiveTypeCode.Boolean => "bool",
                PrimitiveTypeCode.Byte => "uint8",
                PrimitiveTypeCode.Char => "char",
                PrimitiveTypeCode.Double => "float64",
                PrimitiveTypeCode.Int16 => "int16",
                PrimitiveTypeCode.Int32 => "int32",
                PrimitiveTypeCode.Int64 => "int64",
                PrimitiveTypeCode.IntPtr => "native int",
                PrimitiveTypeCode.Object => "object",
                PrimitiveTypeCode.SByte => "int8",
                PrimitiveTypeCode.Single => "float32",
                PrimitiveTypeCode.String => "string",
                PrimitiveTypeCode.TypedReference => "typedref",
                PrimitiveTypeCode.UInt16 => "uint16",
                PrimitiveTypeCode.UInt32 => "uint32",
                PrimitiveTypeCode.UInt64 => "uint64",
                PrimitiveTypeCode.UIntPtr => "native uint",
                PrimitiveTypeCode.Void => "void",
                _ => throw new ArgumentOutOfRangeException(nameof(typeCode)),
            };

            public string GetSZArrayType(string elementType) => elementType + "[]";

            public string GetSystemType() => "System.Type";

            public string GetTypeFromDefinition(
                MetadataReader reader,
                TypeDefinitionHandle handle,
                byte rawTypeKind)
            {
                TypeDefinition definition = reader.GetTypeDefinition(handle);
                string name = definition.Namespace.IsNil
                    ? reader.GetString(definition.Name)
                    : reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);

                TypeDefinitionHandle declaringType = definition.GetDeclaringType();
                return declaringType.IsNil
                    ? name
                    : GetTypeFromDefinition(reader, declaringType, 0) + "/" + reader.GetString(definition.Name);
            }

            public string GetTypeFromReference(
                MetadataReader reader,
                TypeReferenceHandle handle,
                byte rawTypeKind)
            {
                TypeReference reference = reader.GetTypeReference(handle);
                string name = reference.Namespace.IsNil
                    ? reader.GetString(reference.Name)
                    : reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);

                if (name == "System.Type")
                {
                    return name;
                }

                return reference.ResolutionScope.Kind switch
                {
                    HandleKind.AssemblyReference =>
                        "[" + reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope).Name) + "]" + name,
                    HandleKind.ModuleReference =>
                        "[.module " + reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)reference.ResolutionScope).Name) + "]" + name,
                    HandleKind.TypeReference =>
                        GetTypeFromReference(reader, (TypeReferenceHandle)reference.ResolutionScope, 0) + "/" + name,
                    _ => name,
                };
            }

            public string GetTypeFromSerializedName(string name) => name;

            public string GetTypeFromSpecification(
                MetadataReader reader,
                object? genericContext,
                TypeSpecificationHandle handle,
                byte rawTypeKind) =>
                reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

            public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

            public bool IsSystemType(string type) =>
                type is "System.Type" or "[mscorlib]System.Type" or "[System.Runtime]System.Type";
        }
    }
}

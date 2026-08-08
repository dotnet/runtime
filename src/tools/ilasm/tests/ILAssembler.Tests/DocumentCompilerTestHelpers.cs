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
using Internal.IL;
using Xunit;

namespace ILAssembler.Tests
{
    internal static class DocumentCompilerTestHelpers
    {
        internal static PEReader CompileAndGetReader(string source, Options options)
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
    }
}

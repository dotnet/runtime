// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using ILCompiler.Reflection.ReadyToRun;
using Internal.ReadyToRunConstants;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

internal sealed record R2RMethodSignature(string DeclaringType, string Name, ImmutableArray<string> TypeArguments)
{
    // Decode the method identified by a dictionary fixup or virtual override check.
    public static R2RMethodSignature FromFixup(ReadyToRunReader reader, FixupCell fixup)
    {
        var entry = reader.ImportSections[(int)fixup.TableIndex].Entries[(int)fixup.CellOffset];
        var decoder = new R2RSignatureDecoder<string, R2RMethodSignature, DisassemblingGenericContext>(
            new SignatureProvider(), new([], []), reader.GetGlobalMetadata()?.MetadataReader,
            reader, reader.GetOffset((int)entry.SignatureRVA));

        var kind = (ReadyToRunFixupKind)decoder.ReadByte();
        if ((kind & ReadyToRunFixupKind.ModuleOverride) != 0)
        {
            // The decoder constructor has already selected the overridden metadata reader.
            decoder.ReadUInt();
            kind &= ~ReadyToRunFixupKind.ModuleOverride;
        }

        switch (kind)
        {
            case ReadyToRunFixupKind.MethodDictionary:
                return decoder.ParseMethod();

            case ReadyToRunFixupKind.Check_VirtualFunctionOverride:
            case ReadyToRunFixupKind.Verify_VirtualFunctionOverride:
                var flags = (ReadyToRunVirtualFunctionOverrideFlags)decoder.ReadUInt();
                R2RMethodSignature declaration = decoder.ParseMethod();
                decoder.ParseType(); // Implementation type
                return flags.HasFlag(ReadyToRunVirtualFunctionOverrideFlags.VirtualFunctionOverridden)
                    ? decoder.ParseMethod() : declaration;

            default:
                throw new NotSupportedException($"Cannot decode a method from fixup kind '{kind}'.");
        }
    }

    private sealed class SignatureProvider : DisassemblingTypeProvider, IR2RSignatureTypeProvider<string, R2RMethodSignature, DisassemblingGenericContext>
    {
        public string GetCanonType() => "__Canon";

        public R2RMethodSignature GetMethodFromMethodDef(MetadataReader reader, MethodDefinitionHandle handle, string owningTypeOverride)
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);
            return new(owningTypeOverride ?? MetadataNameFormatter.FormatHandle(reader, method.GetDeclaringType()),
                reader.GetString(method.Name), []);
        }

        public R2RMethodSignature GetMethodFromMemberRef(MetadataReader reader, MemberReferenceHandle handle, string owningTypeOverride)
        {
            MemberReference method = reader.GetMemberReference(handle);
            return new(owningTypeOverride ?? MetadataNameFormatter.FormatHandle(reader, method.Parent),
                reader.GetString(method.Name), []);
        }

        public R2RMethodSignature GetInstantiatedMethod(R2RMethodSignature method, ImmutableArray<string> instantiation)
            => method with { TypeArguments = instantiation };

        public R2RMethodSignature GetConstrainedMethod(R2RMethodSignature method, string constraint)
            => throw new NotSupportedException("Constrained methods are not supported by this test decoder.");

        public R2RMethodSignature GetMethodWithFlags(ReadyToRunMethodSigFlags flags, R2RMethodSignature method) => method;
    }
}

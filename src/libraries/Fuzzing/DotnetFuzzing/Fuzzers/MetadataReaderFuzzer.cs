// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes <see cref="PEReader"/> and <see cref="MetadataReader"/>, which read images through raw pointers (MemoryBlock uses
/// Unsafe.ReadUnaligned on the mapped bytes). The image is placed right before (or after) a guard page, so any read past the
/// end is an access violation. Everything reachable is enumerated and decoded: headers, sections, the debug directory
/// (CodeView, embedded portable PDB, PDB checksum), every table row, strings, blobs, GUIDs, user strings, method, field,
/// type-spec, member-reference, method-spec, local and standalone signatures, custom attribute values, method bodies with
/// their exception regions, and portable PDB documents, sequence points, scopes and locals. Only BadImageFormatException is
/// expected for malformed input.
/// </summary>
/// <remarks>
/// Input layout: [0] flags (bit0 raw metadata instead of a PE image, bit1 guard page before instead of after, bit2 a loaded
/// (RVA-mapped) image), then the image.
/// </remarks>
internal sealed class MetadataReaderFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * GetAssemblyName() throws CultureNotFoundException for culture strings CultureInfo doesn't accept.
    // * A stream count of 0x8000 or more makes the MetadataReader constructor throw OverflowException.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Reflection.Metadata"];
    public string[] TargetCoreLibPrefixes => [];

    public unsafe void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        byte flags = bytes[0];
        ReadOnlySpan<byte> image = bytes.Slice(1);
        using BoundedMemory<byte> memory = BoundedMemory.AllocateFromExistingData(image, (flags & 2) != 0 ? PoisonPagePlacement.Before : PoisonPagePlacement.After);
        memory.MakeReadonly();
        fixed (byte* p = memory.Span)
        {
            if ((flags & 1) != 0)
            {
                MetadataReader reader;
                try
                {
                    reader = new MetadataReader(p, image.Length, (flags & 8) != 0 ? MetadataReaderOptions.None : MetadataReaderOptions.Default);
                }
                catch (BadImageFormatException)
                {
                    return;
                }
                catch (OverflowException) when (!s_strict)
                {
                    return;
                }

                ReadMetadata(reader, null);
            }
            else
            {
                using var peReader = new PEReader(p, image.Length, isLoadedImage: (flags & 4) != 0);
                ReadPE(peReader);
            }
        }
    }

    private static void ReadPE(PEReader peReader)
    {
        PEHeaders headers;
        try
        {
            headers = peReader.PEHeaders;
        }
        catch (BadImageFormatException)
        {
            return;
        }

        _ = headers.CoffHeader.Characteristics;
        _ = headers.PEHeader?.AddressOfEntryPoint;
        _ = headers.CorHeader?.Flags;
        _ = headers.IsDll;
        _ = headers.MetadataStartOffset;
        foreach (SectionHeader section in headers.SectionHeaders)
        {
            Try(() =>
            {
                PEMemoryBlock block = peReader.GetSectionData(section.VirtualAddress);
                _ = block.GetContent().Length;
                _ = headers.GetContainingSectionIndex(section.VirtualAddress);
            });
        }

        if (headers.PEHeader is { } peHeader)
        {
            foreach (DirectoryEntry entry in new[] { peHeader.ResourceTableDirectory, peHeader.ImportTableDirectory, peHeader.ExportTableDirectory, peHeader.CertificateTableDirectory })
            {
                Try(() =>
                {
                    if (headers.TryGetDirectoryOffset(entry, out int offset))
                    {
                        _ = offset;
                    }

                    _ = peReader.GetSectionData(entry.RelativeVirtualAddress).Length;
                });
            }
        }

        Try(() =>
        {
            foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
            {
                Try(() =>
                {
                    switch (entry.Type)
                    {
                        case DebugDirectoryEntryType.CodeView:
                            CodeViewDebugDirectoryData codeView = peReader.ReadCodeViewDebugDirectoryData(entry);
                            _ = codeView.Path;
                            _ = codeView.Guid;
                            break;
                        case DebugDirectoryEntryType.EmbeddedPortablePdb:
                            using (MetadataReaderProvider provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry))
                            {
                                ReadMetadata(provider.GetMetadataReader(), null);
                            }

                            break;
                        case DebugDirectoryEntryType.PdbChecksum:
                            PdbChecksumDebugDirectoryData checksum = peReader.ReadPdbChecksumDebugDirectoryData(entry);
                            _ = checksum.AlgorithmName;
                            _ = checksum.Checksum.Length;
                            break;
                    }
                });
            }
        });

        bool hasMetadata;
        try
        {
            hasMetadata = peReader.HasMetadata;
        }
        catch (BadImageFormatException)
        {
            return;
        }

        if (hasMetadata)
        {
            MetadataReader reader;
            try
            {
                reader = peReader.GetMetadataReader();
            }
            catch (BadImageFormatException)
            {
                return;
            }
            catch (OverflowException) when (!s_strict)
            {
                return;
            }

            ReadMetadata(reader, peReader);
        }
    }

    private static void ReadMetadata(MetadataReader reader, PEReader? peReader)
    {
        var provider = new StringTypeProvider();
        _ = reader.MetadataVersion;
        _ = reader.MetadataKind;
        _ = reader.IsAssembly;
        if (reader.DebugMetadataHeader is { } debugHeader)
        {
            _ = debugHeader.Id.Length;
            _ = debugHeader.EntryPoint;
        }
        else
        {
            Try(() => _ = reader.GetString(reader.GetModuleDefinition().Name));
            Try(() => _ = reader.GetGuid(reader.GetModuleDefinition().Mvid));
        }
        if (reader.IsAssembly)
        {
            Try(() =>
            {
                AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                _ = reader.GetString(assembly.Name);
                _ = reader.GetBlobBytes(assembly.PublicKey);
                _ = assembly.GetAssemblyName();
                ReadAttributes(reader, assembly.GetCustomAttributes(), provider);
            });
        }

        foreach (AssemblyReferenceHandle handle in reader.AssemblyReferences)
        {
            Try(() =>
            {
                AssemblyReference reference = reader.GetAssemblyReference(handle);
                _ = reference.GetAssemblyName();
                _ = reader.GetBlobBytes(reference.PublicKeyOrToken);
            });
        }

        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            Try(() =>
            {
                TypeReference type = reader.GetTypeReference(handle);
                _ = reader.GetString(type.Name) + reader.GetString(type.Namespace);
                _ = type.ResolutionScope.Kind;
            });
        }

        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            Try(() => ReadType(reader, handle, provider, peReader));
        }

        foreach (MemberReferenceHandle handle in reader.MemberReferences)
        {
            Try(() =>
            {
                MemberReference member = reader.GetMemberReference(handle);
                _ = reader.GetString(member.Name);
                if (member.GetKind() == MemberReferenceKind.Method)
                {
                    _ = member.DecodeMethodSignature(provider, null);
                }
                else
                {
                    _ = member.DecodeFieldSignature(provider, null);
                }
            });
        }

        for (int row = 1, count = reader.GetTableRowCount(TableIndex.TypeSpec); row <= count; row++)
        {
            TypeSpecificationHandle handle = MetadataTokens.TypeSpecificationHandle(row);
            Try(() => _ = reader.GetTypeSpecification(handle).DecodeSignature(provider, null));
        }

        for (int row = 1, count = reader.GetTableRowCount(TableIndex.MethodSpec); row <= count; row++)
        {
            MethodSpecificationHandle handle = MetadataTokens.MethodSpecificationHandle(row);
            Try(() => _ = reader.GetMethodSpecification(handle).DecodeSignature(provider, null));
        }

        for (int row = 1, count = reader.GetTableRowCount(TableIndex.StandAloneSig); row <= count; row++)
        {
            StandaloneSignatureHandle handle = MetadataTokens.StandaloneSignatureHandle(row);
            Try(() =>
            {
                StandaloneSignature signature = reader.GetStandaloneSignature(handle);
                if (signature.GetKind() == StandaloneSignatureKind.LocalVariables)
                {
                    _ = signature.DecodeLocalSignature(provider, null);
                }
                else
                {
                    _ = signature.DecodeMethodSignature(provider, null);
                }
            });
        }

        ReadAttributes(reader, reader.CustomAttributes, provider);

        foreach (ManifestResourceHandle handle in reader.ManifestResources)
        {
            Try(() => _ = reader.GetString(reader.GetManifestResource(handle).Name));
        }

        foreach (ExportedTypeHandle handle in reader.ExportedTypes)
        {
            Try(() => _ = reader.GetString(reader.GetExportedType(handle).Name));
        }

        foreach (DeclarativeSecurityAttributeHandle handle in reader.DeclarativeSecurityAttributes)
        {
            Try(() => _ = reader.GetBlobBytes(reader.GetDeclarativeSecurityAttribute(handle).PermissionSet));
        }

        foreach (AssemblyFileHandle handle in reader.AssemblyFiles)
        {
            Try(() => _ = reader.GetString(reader.GetAssemblyFile(handle).Name));
        }

        // User strings heap.
        Try(() =>
        {
            UserStringHandle handle = MetadataTokens.UserStringHandle(1);
            for (int i = 0; i < 10_000 && !handle.IsNil; i++)
            {
                _ = reader.GetUserString(handle);
                handle = reader.GetNextHandle(handle);
            }
        });

        // Blob and string heaps walked sequentially.
        Try(() =>
        {
            BlobHandle handle = MetadataTokens.BlobHandle(1);
            for (int i = 0; i < 10_000 && !handle.IsNil; i++)
            {
                _ = reader.GetBlobBytes(handle);
                handle = reader.GetNextHandle(handle);
            }
        });

        Try(() =>
        {
            StringHandle handle = MetadataTokens.StringHandle(1);
            for (int i = 0; i < 10_000 && !handle.IsNil; i++)
            {
                _ = reader.GetString(handle);
                handle = reader.GetNextHandle(handle);
            }
        });

        // Portable PDB tables.
        foreach (DocumentHandle handle in reader.Documents)
        {
            Try(() =>
            {
                Document document = reader.GetDocument(handle);
                _ = reader.GetString(document.Name);
                _ = reader.GetGuid(document.Language);
                _ = reader.GetBlobBytes(document.Hash);
            });
        }

        foreach (MethodDebugInformationHandle handle in reader.MethodDebugInformation)
        {
            Try(() =>
            {
                MethodDebugInformation info = reader.GetMethodDebugInformation(handle);
                int count = 0;
                foreach (SequencePoint point in info.GetSequencePoints())
                {
                    _ = point.StartLine + point.EndColumn;
                    if (++count > 10_000)
                    {
                        break;
                    }
                }

                _ = info.GetStateMachineKickoffMethod();
                if (!info.LocalSignature.IsNil)
                {
                    _ = reader.GetStandaloneSignature(info.LocalSignature).DecodeLocalSignature(provider, null);
                }
            });
        }

        foreach (LocalScopeHandle handle in reader.LocalScopes)
        {
            Try(() =>
            {
                LocalScope scope = reader.GetLocalScope(handle);
                foreach (LocalVariableHandle variable in scope.GetLocalVariables())
                {
                    _ = reader.GetString(reader.GetLocalVariable(variable).Name);
                }

                foreach (LocalConstantHandle constant in scope.GetLocalConstants())
                {
                    _ = reader.GetBlobBytes(reader.GetLocalConstant(constant).Signature);
                }

                LocalScopeHandleCollection.ChildrenEnumerator children = scope.GetChildren();
                for (int i = 0; i < 1000 && children.MoveNext(); i++)
                {
                    _ = children.Current;
                }
            });
        }

        foreach (ImportScopeHandle handle in reader.ImportScopes)
        {
            Try(() =>
            {
                foreach (ImportDefinition import in reader.GetImportScope(handle).GetImports())
                {
                    _ = import.Kind;
                }
            });
        }

        foreach (CustomDebugInformationHandle handle in reader.CustomDebugInformation)
        {
            Try(() =>
            {
                CustomDebugInformation info = reader.GetCustomDebugInformation(handle);
                _ = reader.GetGuid(info.Kind);
                _ = reader.GetBlobBytes(info.Value);
            });
        }
    }

    private static void ReadType(MetadataReader reader, TypeDefinitionHandle handle, StringTypeProvider provider, PEReader? peReader)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        _ = reader.GetString(type.Name) + reader.GetString(type.Namespace);
        _ = type.GetLayout();
        _ = type.GetDeclaringType();
        _ = type.BaseType.Kind;
        foreach (TypeDefinitionHandle nested in type.GetNestedTypes())
        {
            _ = nested;
        }

        foreach (GenericParameterHandle parameter in type.GetGenericParameters())
        {
            Try(() =>
            {
                GenericParameter gp = reader.GetGenericParameter(parameter);
                _ = reader.GetString(gp.Name);
                foreach (GenericParameterConstraintHandle constraint in gp.GetConstraints())
                {
                    _ = reader.GetGenericParameterConstraint(constraint).Type;
                }
            });
        }

        foreach (InterfaceImplementationHandle implementation in type.GetInterfaceImplementations())
        {
            Try(() => _ = reader.GetInterfaceImplementation(implementation).Interface);
        }

        foreach (MethodImplementationHandle implementation in type.GetMethodImplementations())
        {
            Try(() => _ = reader.GetMethodImplementation(implementation).MethodBody);
        }

        foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
        {
            Try(() =>
            {
                FieldDefinition field = reader.GetFieldDefinition(fieldHandle);
                _ = reader.GetString(field.Name);
                _ = field.DecodeSignature(provider, null);
                _ = field.GetOffset();
                _ = field.GetRelativeVirtualAddress();
                ConstantHandle constant = field.GetDefaultValue();
                if (!constant.IsNil)
                {
                    _ = reader.GetBlobBytes(reader.GetConstant(constant).Value);
                }

                if (!field.GetMarshallingDescriptor().IsNil)
                {
                    _ = reader.GetBlobBytes(field.GetMarshallingDescriptor());
                }
            });
        }

        foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
        {
            Try(() =>
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                _ = reader.GetString(method.Name);
                _ = method.DecodeSignature(provider, null);
                foreach (ParameterHandle parameter in method.GetParameters())
                {
                    _ = reader.GetString(reader.GetParameter(parameter).Name);
                }

                _ = method.GetImport();
                if (peReader is not null && method.RelativeVirtualAddress != 0)
                {
                    MethodBodyBlock body = peReader.GetMethodBody(method.RelativeVirtualAddress);
                    _ = body.GetILBytes();
                    _ = body.MaxStack;
                    foreach (ExceptionRegion region in body.ExceptionRegions)
                    {
                        _ = region.HandlerOffset + region.TryLength;
                    }

                    if (!body.LocalSignature.IsNil)
                    {
                        _ = reader.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(provider, null);
                    }

                    ReadIL(body.GetILReader());
                }
            });
        }

        foreach (PropertyDefinitionHandle propertyHandle in type.GetProperties())
        {
            Try(() =>
            {
                PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                _ = reader.GetString(property.Name);
                _ = property.DecodeSignature(provider, null);
                _ = property.GetAccessors().Getter;
            });
        }

        foreach (EventDefinitionHandle eventHandle in type.GetEvents())
        {
            Try(() => _ = reader.GetString(reader.GetEventDefinition(eventHandle).Name));
        }
    }

    // Walk the IL stream the way disassemblers do.
    private static void ReadIL(BlobReader il)
    {
        int count = 0;
        while (il.RemainingBytes > 0 && count++ < 100_000)
        {
            ILOpCode opCode = (ILOpCode)il.ReadByte();
            if ((byte)opCode == 0xFE && il.RemainingBytes > 0)
            {
                opCode = (ILOpCode)(0xFE00 | il.ReadByte());
            }

            if (opCode == ILOpCode.Switch)
            {
                uint targets = il.ReadUInt32();
                for (uint i = 0; i < targets && il.RemainingBytes >= 4; i++)
                {
                    _ = il.ReadInt32();
                }

                continue;
            }

            int size = opCode.IsBranch() ? opCode.GetBranchOperandSize() : OperandSize(opCode);
            il.Offset += Math.Min(size, il.RemainingBytes);
        }
    }

    private static int OperandSize(ILOpCode opCode) => opCode switch
    {
        ILOpCode.Ldc_i8 or ILOpCode.Ldc_r8 => 8,
        ILOpCode.Ldc_i4_s or ILOpCode.Ldarg_s or ILOpCode.Ldarga_s or ILOpCode.Starg_s or ILOpCode.Ldloc_s or ILOpCode.Ldloca_s or ILOpCode.Stloc_s or ILOpCode.Unaligned => 1,
        ILOpCode.Ldarg or ILOpCode.Ldarga or ILOpCode.Starg or ILOpCode.Ldloc or ILOpCode.Ldloca or ILOpCode.Stloc => 2,
        ILOpCode.Ldc_i4 or ILOpCode.Ldc_r4 or ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Ldfld or ILOpCode.Stfld or ILOpCode.Ldsfld
            or ILOpCode.Stsfld or ILOpCode.Ldflda or ILOpCode.Ldsflda or ILOpCode.Ldstr or ILOpCode.Ldtoken or ILOpCode.Box or ILOpCode.Unbox or ILOpCode.Unbox_any
            or ILOpCode.Castclass or ILOpCode.Isinst or ILOpCode.Newarr or ILOpCode.Ldelema or ILOpCode.Ldelem or ILOpCode.Stelem or ILOpCode.Ldobj
            or ILOpCode.Stobj or ILOpCode.Cpobj or ILOpCode.Initobj or ILOpCode.Sizeof or ILOpCode.Mkrefany or ILOpCode.Refanyval or ILOpCode.Ldftn
            or ILOpCode.Ldvirtftn or ILOpCode.Jmp or ILOpCode.Calli or ILOpCode.Constrained => 4,
        _ => 0,
    };

    private static void ReadAttributes(MetadataReader reader, CustomAttributeHandleCollection attributes, StringTypeProvider provider)
    {
        foreach (CustomAttributeHandle handle in attributes)
        {
            Try(() =>
            {
                CustomAttribute attribute = reader.GetCustomAttribute(handle);
                _ = attribute.Constructor.Kind;
                CustomAttributeValue<string> value = attribute.DecodeValue(provider);
                _ = value.FixedArguments.Length + value.NamedArguments.Length;
            });
        }
    }

    private static void Try(Action action)
    {
        try
        {
            action();
        }
        catch (BadImageFormatException)
        {
        }
        catch (System.Globalization.CultureNotFoundException) when (!s_strict)
        {
        }
    }

    private sealed class StringTypeProvider : ISignatureTypeProvider<string, object?>, ICustomAttributeTypeProvider<string>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => reader.GetString(reader.GetTypeDefinition(handle).Name);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => reader.GetString(reader.GetTypeReference(handle).Name);
        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => "spec";
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + shape.Rank + "]";
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetPointerType(string elementType) => elementType + "*";
        public string GetPinnedType(string elementType) => elementType + " pinned";
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType + "<" + typeArguments.Length + ">";
        public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;
        public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetFunctionPointerType(MethodSignature<string> signature) => "method";
        public string GetSystemType() => "System.Type";
        public bool IsSystemType(string type) => type == "System.Type" || type == "Type";
        public string GetTypeFromSerializedName(string name) => name;
        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;
    }
}

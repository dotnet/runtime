// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class SyntheticVectorMetadata
{
    private readonly MetadataReaderProvider _provider;
    private readonly Dictionary<string, uint> _typeDefTokens;

    private SyntheticVectorMetadata(MetadataReaderProvider provider, Dictionary<string, uint> typeDefTokens)
    {
        _provider = provider;
        _typeDefTokens = typeDefTokens;
    }

    public MetadataReader Reader => _provider.GetMetadataReader();

    public uint GetTypeDefToken(string typeName) => _typeDefTokens[typeName];

    public static SyntheticVectorMetadata Create()
    {
        MetadataBuilder builder = new();
        builder.AddAssembly(
            builder.GetOrAddString("SyntheticAsm"),
            new Version(1, 0, 0, 0),
            default,
            default,
            0,
            AssemblyHashAlgorithm.None);
        builder.AddModule(
            0,
            builder.GetOrAddString("SyntheticModule"),
            builder.GetOrAddGuid(Guid.NewGuid()),
            default,
            default);

        builder.AddTypeDefinition(
            TypeAttributes.NotPublic,
            default,
            builder.GetOrAddString("<Module>"),
            default,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));

        Dictionary<string, uint> typeDefTokens = new(StringComparer.Ordinal);
        foreach ((string ns, string name) in new[]
        {
            ("System.Runtime.Intrinsics", "Vector64`1"),
            ("System.Runtime.Intrinsics", "Vector128`1"),
            ("System.Numerics", "Vector`1"),
        })
        {
            TypeDefinitionHandle handle = builder.AddTypeDefinition(
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout,
                builder.GetOrAddString(ns),
                builder.GetOrAddString(name),
                default,
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));
            typeDefTokens.Add(name, (uint)MetadataTokens.GetToken(handle));
        }

        BlobBuilder output = new();
        new MetadataRootBuilder(builder).Serialize(output, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);

        MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(output.ToArray()));
        MetadataReader reader = provider.GetMetadataReader();

        Debug.Assert(typeDefTokens.Count == 3);
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);
            string name = reader.GetString(typeDef.Name);
            if (typeDefTokens.TryGetValue(name, out uint expectedToken))
            {
                Debug.Assert(expectedToken == (uint)MetadataTokens.GetToken(handle));
            }
        }

        return new SyntheticVectorMetadata(provider, typeDefTokens);
    }

    public static SyntheticVectorMetadata CreateWithExtraType(string extraNamespace, string extraName)
    {
        MetadataBuilder builder = new();
        builder.AddAssembly(
            builder.GetOrAddString("SyntheticAsm"),
            new Version(1, 0, 0, 0),
            default, default, 0, AssemblyHashAlgorithm.None);
        builder.AddModule(
            0,
            builder.GetOrAddString("SyntheticModule"),
            builder.GetOrAddGuid(Guid.NewGuid()),
            default, default);
        builder.AddTypeDefinition(
            TypeAttributes.NotPublic, default,
            builder.GetOrAddString("<Module>"),
            default,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));

        Dictionary<string, uint> typeDefTokens = new(StringComparer.Ordinal);
        foreach ((string ns, string name) in new[]
        {
            ("System.Runtime.Intrinsics", "Vector64`1"),
            ("System.Runtime.Intrinsics", "Vector128`1"),
            ("System.Numerics", "Vector`1"),
            (extraNamespace, extraName),
        })
        {
            TypeDefinitionHandle handle = builder.AddTypeDefinition(
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout,
                builder.GetOrAddString(ns),
                builder.GetOrAddString(name),
                default,
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));
            typeDefTokens.Add(name, (uint)MetadataTokens.GetToken(handle));
        }

        BlobBuilder output = new();
        new MetadataRootBuilder(builder).Serialize(output, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);
        MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(output.ToArray()));
        return new SyntheticVectorMetadata(provider, typeDefTokens);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

// Note: This test does NOT define any TypeMap attributes.
// Both the default entry assembly and an explicit TypeMapLib5 override use these tests.

public class TypeMapEntryAssemblyTest
{
    [Fact]
    public static void Validate_DefiningAssembly()
    {
        IReadOnlyDictionary<string, Type> externalMap = TypeMapping.GetOrCreateExternalTypeMapping<DefinedInLibrary>();
        Assert.Equal(typeof(string), externalMap["local"]);
        Assert.Equal(typeof(object), externalMap["second"]);
        Assert.Equal(typeof(object), externalMap["third"]);

        IReadOnlyDictionary<Type, Type> proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<DefinedInLibrary>();
        Assert.Equal(typeof(string), proxyMap[new object().GetType()]);
        Assert.Equal(typeof(object), proxyMap[new C1().GetType()]);
        Assert.Equal(typeof(object), proxyMap[new S1().GetType()]);

        Assert.Equal(typeof(string), TypeMapping.GetOrCreateExternalTypeMapping<DefinedInLibrary>()["local"]);
        Assert.Equal(typeof(string), TypeMapping.GetOrCreateProxyTypeMapping<DefinedInLibrary>()[typeof(object)]);
    }

    [Fact]
    public static void Validate_ConstructedDefiningAssembly()
    {
        Assert.Equal(typeof(object), TypeMapping.GetOrCreateExternalTypeMapping<DefinedGenericGroup<int>>()["generic"]);
        Assert.Equal(typeof(object), TypeMapping.GetOrCreateExternalTypeMapping<DefinedInLibrary[]>()["array"]);
        Assert.Equal(typeof(string), TypeMapping.GetOrCreateProxyTypeMapping<DefinedGenericGroup<int>>()[new object().GetType()]);
        Assert.Equal(typeof(string), TypeMapping.GetOrCreateProxyTypeMapping<DefinedInLibrary[]>()[new object().GetType()]);
    }

    [Fact]
    public static void Validate_ProxyFirst()
    {
        Assert.Equal(typeof(string), TypeMapping.GetOrCreateProxyTypeMapping<DefinedProxyFirst>()[new object().GetType()]);
        Assert.Equal(typeof(string), TypeMapping.GetOrCreateExternalTypeMapping<DefinedProxyFirst>()["local"]);
    }

    [Fact]
    public static void Validate_EmptyDefiningAssemblyGroup()
    {
        Assert.False(TypeMapping.GetOrCreateExternalTypeMapping<DefinedEmptyGroup>().TryGetValue("absent", out _));
        Assert.False(TypeMapping.GetOrCreateProxyTypeMapping<DefinedEmptyGroup>().TryGetValue(typeof(object), out _));
    }

    [Fact]
    public static void Validate_MissingDefiningAssemblyTarget()
    {
        Assert.Throws<FileNotFoundException>(() => TypeMapping.GetOrCreateExternalTypeMapping<DefinedMissingTargetGroup>());
        Assert.Throws<FileNotFoundException>(() => TypeMapping.GetOrCreateProxyTypeMapping<DefinedMissingTargetGroup>());
    }

    [Fact]
    public static void Validate_EntryAssemblyContributions()
    {
        IReadOnlyDictionary<string, Type> externalMap = TypeMapping.GetOrCreateExternalTypeMapping<DefinedInLibrary>();
        IReadOnlyDictionary<Type, Type> proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<DefinedInLibrary>();
#if TYPEMAP_ENTRY_ASSEMBLY_OVERRIDE
        Assert.Equal(typeof(Lib5Type1), externalMap["override"]);
        Assert.Equal(typeof(Lib5Proxy1), proxyMap[new Lib5Type1().GetType()]);
#else
        Assert.False(externalMap.TryGetValue("override", out _));
        Assert.False(proxyMap.TryGetValue(typeof(Lib5Type1), out _));
#endif
    }

    [Fact]
    public static void Validate_TypeMappingEntryAssembly_ExternalMap()
    {
        Console.WriteLine(nameof(Validate_TypeMappingEntryAssembly_ExternalMap));

        // These types should be resolved from TypeMapLib5's type maps
        IReadOnlyDictionary<string, Type> externalMap = TypeMapping.GetOrCreateExternalTypeMapping<AlternateEntryPoint>();

        Assert.Equal(typeof(Lib5Type1), externalMap["lib5_type1"]);
        Assert.Equal(typeof(Lib5Type2), externalMap["lib5_type2"]);

        Assert.True(externalMap.TryGetValue("lib5_type1", out Type? type1));
        Assert.Equal(typeof(Lib5Type1), type1);

        Assert.True(externalMap.TryGetValue("lib5_type2", out Type? type2));
        Assert.Equal(typeof(Lib5Type2), type2);

        Assert.False(externalMap.TryGetValue("nonexistent", out Type? _));
    }

    [Fact]
    public static void Validate_TypeMappingEntryAssembly_ProxyMap()
    {
        Console.WriteLine(nameof(Validate_TypeMappingEntryAssembly_ProxyMap));

        // These proxy mappings should be resolved from TypeMapLib5's type maps
        IReadOnlyDictionary<Type, Type> proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<AlternateEntryPoint>();

        Assert.Equal(typeof(Lib5Proxy1), proxyMap[new Lib5Type1().GetType()]);
        Assert.Equal(typeof(Lib5Proxy2), proxyMap[new Lib5Type2().GetType()]);

        Assert.False(proxyMap.TryGetValue(typeof(string), out Type? _));
    }
}

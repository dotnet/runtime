// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

// Note: This test does NOT define any TypeMap attributes.
// The TypeMappingEntryAssembly is set to TypeMapLib5 via RuntimeHostConfigurationOption,
// so the type maps should be resolved from TypeMapLib5 instead of this assembly.

public class TypeMapEntryAssemblyTest
{
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

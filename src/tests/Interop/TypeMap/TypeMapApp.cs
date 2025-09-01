// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias Original;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using DupType_MapObject = Original.Lib.AliasedName;
using DupType_MapString = Lib.AliasedName;

[assembly: TypeMapAssemblyTarget<MultipleTypeMapAssemblies>("TypeMapLib3")]
[assembly: TypeMapAssemblyTarget<MultipleTypeMapAssemblies>("TypeMapLib4")]

[assembly: TypeMapAssemblyTarget<UnknownAssemblyReference>("DoesNotExist")]

[assembly: TypeMap<TypicalUseCase>("1", typeof(C1))]
[assembly: TypeMap<TypicalUseCase>("2", typeof(S1))]
[assembly: TypeMap<TypicalUseCase>("3", typeof(Guid))]
[assembly: TypeMap<TypicalUseCase>("4", typeof(object))]

[assembly: TypeMap<object>("1", typeof(string))]
[assembly: TypeMap<string>("1", typeof(string))]
[assembly: TypeMap<int[]>("1", typeof(string))]
[assembly: TypeMap<C1.I1>("1", typeof(string))]
[assembly: TypeMap<C1.I2<int>>("1", typeof(string))]
[assembly: TypeMap<C1.I2<string>>("1", typeof(string))]
[assembly: TypeMap<C2<int>.I1>("1", typeof(string))]
[assembly: TypeMap<C2<int>.I2<int>>("1", typeof(string))]
[assembly: TypeMap<C2<string>.I1>("1", typeof(string))]
[assembly: TypeMap<C2<string>.I2<string>>("1", typeof(string))]

[assembly: TypeMapAssociation<TypicalUseCase>(typeof(C1), typeof(C1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(S1), typeof(S1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(Guid), typeof(C1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(string), typeof(S1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(List<int>), typeof(C1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(List<>), typeof(S1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(C1.I1), typeof(C1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(C1.I2<int>), typeof(S1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(C2<int>), typeof(C1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(C2<>), typeof(S1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(int[]), typeof(C1))]
[assembly: TypeMapAssociation<TypicalUseCase>(typeof(int*), typeof(S1))]

[assembly: TypeMapAssociation<object>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<string>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<int[]>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<C1.I1>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<C1.I2<int>>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<C1.I2<string>>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<C2<int>.I1>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<C2<int>.I2<int>>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<C2<string>.I1>(typeof(object), typeof(string))]
[assembly: TypeMapAssociation<C2<string>.I2<string>>(typeof(object), typeof(string))]

[assembly: TypeMap<InvalidTypeNameKey>(null!, typeof(object))]
[assembly: TypeMapAssociation<InvalidTypeNameKey>(null!, typeof(object))]

[assembly: TypeMap<DuplicateTypeNameKey>("1", typeof(object))]
[assembly: TypeMap<DuplicateTypeNameKey>("1", typeof(object))]

[assembly: TypeMapAssociation<DuplicateTypeNameKey>(typeof(DupType_MapObject), typeof(object))]
[assembly: TypeMapAssociation<DuplicateTypeNameKey>(typeof(DupType_MapString), typeof(string))]

// Redefine the same type as in the TypeMapLib2 assembly
// This is testing the duplicate type name key for the
// TypeMapAssociation scenario.
namespace Lib
{
    public class AliasedName { }
}

public class TypeMap
{
    [Fact]
    public static void Validate_ExternalTypeMapping()
    {
        Console.WriteLine(nameof(Validate_ExternalTypeMapping));

        IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<TypicalUseCase>();

        Assert.Equal(typeof(C1), map["1"]);
        Assert.Equal(typeof(S1), map["2"]);
        Assert.Equal(typeof(Guid), map["3"]);
        Assert.Equal(typeof(object), map["4"]);

        Assert.True(map.TryGetValue("1", out Type? _));
        Assert.True(map.TryGetValue("2", out Type? _));
        Assert.True(map.TryGetValue("3", out Type? _));
        Assert.True(map.TryGetValue("4", out Type? _));
        Assert.False(map.TryGetValue("5", out Type? _));
    }

    [Fact]
    public static void Validate_GroupType_Types()
    {
        Console.WriteLine(nameof(Validate_GroupType_Types));

        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<object>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<string>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<int[]>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<C1.I1>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<C1.I2<int>>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<C1.I2<string>>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<C2<int>.I1>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<C2<int>.I2<int>>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<C2<string>.I1>());
        ValidateExternalTypeMap(TypeMapping.GetOrCreateExternalTypeMapping<C2<string>.I2<string>>());

        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<object>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<string>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<int[]>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<C1.I1>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<C1.I2<int>>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<C1.I2<string>>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<C2<int>.I1>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<C2<int>.I2<int>>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<C2<string>.I1>());
        ValidateProxyTypeMap(TypeMapping.GetOrCreateProxyTypeMapping<C2<string>.I2<string>>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ValidateExternalTypeMap(IReadOnlyDictionary<string, Type> map)
        {
            Assert.Equal(typeof(string), map["1"]);
            Assert.False(map.TryGetValue("2", out Type? _));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ValidateProxyTypeMap(IReadOnlyDictionary<Type, Type> map)
        {
            Assert.Equal(typeof(string), map[typeof(object)]);
            Assert.False(map.TryGetValue(typeof(string), out Type? _));
        }
    }

    [Fact]
    public static void Validate_ProxyTypeMapping()
    {
        Console.WriteLine(nameof(Validate_ProxyTypeMapping));

        IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<TypicalUseCase>();

        Assert.Equal(typeof(C1), map[typeof(C1)]);
        Assert.Equal(typeof(S1), map[typeof(S1)]);
        Assert.Equal(typeof(C1), map[typeof(Guid)]);
        Assert.Equal(typeof(S1), map[typeof(string)]);
        Assert.Equal(typeof(C1), map[typeof(List<int>)]);
        Assert.Equal(typeof(S1), map[typeof(List<>)]);
        Assert.Equal(typeof(C1), map[typeof(C1.I1)]);
        Assert.Equal(typeof(S1), map[typeof(C1.I2<int>)]);
        Assert.Equal(typeof(C1), map[typeof(C2<int>)]);
        Assert.Equal(typeof(S1), map[typeof(C2<>)]);
        Assert.Equal(typeof(C1), map[typeof(int[])]);
        Assert.Equal(typeof(S1), map[typeof(int*)]);

        Assert.True(map.TryGetValue(typeof(C1), out Type? _));
        Assert.True(map.TryGetValue(typeof(S1), out Type? _));
        Assert.True(map.TryGetValue(typeof(Guid), out Type? _));
        Assert.True(map.TryGetValue(typeof(string), out Type? _));
        Assert.True(map.TryGetValue(typeof(List<int>), out Type? _));
        Assert.True(map.TryGetValue(typeof(List<>), out Type? _));
        Assert.True(map.TryGetValue(typeof(C1.I1), out Type? _));
        Assert.True(map.TryGetValue(typeof(C1.I2<int>), out Type? _));
        Assert.True(map.TryGetValue(typeof(C2<int>), out Type? _));
        Assert.True(map.TryGetValue(typeof(C2<>), out Type? _));
        Assert.True(map.TryGetValue(typeof(int[]), out Type? _));
        Assert.True(map.TryGetValue(typeof(int*), out Type? _));

        // Validate strict type mapping, no implicit conversions.
        Assert.False(map.TryGetValue(typeof(object), out Type? _));
        Assert.False(map.TryGetValue(typeof(void*), out Type? _));
        Assert.False(map.TryGetValue(typeof(IntPtr), out Type? _));
    }

    [Fact]
    public static void Validate_ExternalTypeMapping_DuplicateTypeKey()
    {
        Console.WriteLine(nameof(Validate_ExternalTypeMapping_DuplicateTypeKey));

        AssertExtensions.ThrowsAny<ArgumentException, BadImageFormatException>(() => TypeMapping.GetOrCreateExternalTypeMapping<DuplicateTypeNameKey>());
    }

    [Fact]
    public static void Validate_ProxyTypeMapping_DuplicateTypeKey()
    {
        Console.WriteLine(nameof(Validate_ProxyTypeMapping_DuplicateTypeKey));

        IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<DuplicateTypeNameKey>();

        Assert.Equal(typeof(object), map[typeof(DupType_MapObject)]);
        Assert.Equal(typeof(string), map[typeof(DupType_MapString)]);
    }

    [Fact]
    public static void Validate_ExternalTypeMapping_NotSupportedMethods()
    {
        Console.WriteLine(nameof(Validate_ExternalTypeMapping_NotSupportedMethods));

        IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<TypicalUseCase>();

        Assert.Throws<NotSupportedException>(() => map.Keys);
        Assert.Throws<NotSupportedException>(() => map.Values);
        Assert.Throws<NotSupportedException>(() => map.Count);
        Assert.Throws<NotSupportedException>(() => map.ContainsKey("1"));
        Assert.Throws<NotSupportedException>(() => map.GetEnumerator());
        Assert.Throws<NotSupportedException>(() => ((System.Collections.IEnumerable)map).GetEnumerator());
    }

    [Fact]
    public static void Validate_ProxyTypeMapping_NotSupportedMethods()
    {
        Console.WriteLine(nameof(Validate_ProxyTypeMapping_NotSupportedMethods));

        IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<TypicalUseCase>();

        Assert.Throws<NotSupportedException>(() => map.Keys);
        Assert.Throws<NotSupportedException>(() => map.Values);
        Assert.Throws<NotSupportedException>(() => map.Count);
        Assert.Throws<NotSupportedException>(() => map.ContainsKey(typeof(C1)));
        Assert.Throws<NotSupportedException>(() => map.GetEnumerator());
        Assert.Throws<NotSupportedException>(() => ((System.Collections.IEnumerable)map).GetEnumerator());
    }

    [Fact]
    public static void Validate_CrossAssemblyResolution()
    {
        Console.WriteLine(nameof(Validate_CrossAssemblyResolution));

        {
            IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<MultipleTypeMapAssemblies>();
            Assert.Equal(typeof(object), map["1"]);
            Assert.Equal(typeof(string), map["2"]);
            Assert.Equal(typeof(object), map["3"]);
            Assert.Equal(typeof(string), map["4"]);
        }

        {
            IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<MultipleTypeMapAssemblies>();
            Assert.Equal(typeof(string), map[typeof(object)]);
            Assert.Equal(typeof(object), map[typeof(string)]);
            Assert.Equal(typeof(string), map[typeof(C1)]);
            Assert.Equal(typeof(object), map[typeof(S1)]);
        }
    }

    [Fact]
    public static void Validate_MissingAssemblyTarget()
    {
        Console.WriteLine(nameof(Validate_MissingAssemblyTarget));

        Assert.Throws<FileNotFoundException>(() => TypeMapping.GetOrCreateExternalTypeMapping<UnknownAssemblyReference>());
        Assert.Throws<FileNotFoundException>(() => TypeMapping.GetOrCreateProxyTypeMapping<UnknownAssemblyReference>());
    }

    [Fact]
    public static void Validate_EmptyOrInvalidMappings()
    {
        Console.WriteLine(nameof(Validate_EmptyOrInvalidMappings));

        AssertExtensions.ThrowsAny<COMException, BadImageFormatException>(() => TypeMapping.GetOrCreateExternalTypeMapping<InvalidTypeNameKey>());
        AssertExtensions.ThrowsAny<COMException, BadImageFormatException>(() => TypeMapping.GetOrCreateProxyTypeMapping<InvalidTypeNameKey>());
    }
}

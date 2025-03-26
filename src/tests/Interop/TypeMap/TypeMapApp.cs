// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

[assembly: TypeMap<Guid>("1", typeof(C1))]
[assembly: TypeMap<Guid>("2", typeof(S1))]
[assembly: TypeMap<Guid>("3", typeof(Guid))]
[assembly: TypeMap<Guid>("4", typeof(object))]

[assembly: TypeMap<string>("1", typeof(string))]
[assembly: TypeMap<object>("1", typeof(object))]

[assembly: TypeMapAssociation<Guid>(typeof(C1), typeof(C1))]
[assembly: TypeMapAssociation<Guid>(typeof(S1), typeof(S1))]
[assembly: TypeMapAssociation<Guid>(typeof(Guid), typeof(C1))]
[assembly: TypeMapAssociation<Guid>(typeof(string), typeof(S1))]

[assembly: TypeMapAssociation<string>(typeof(C1), typeof(C1))]
[assembly: TypeMapAssociation<object>(typeof(S1), typeof(S1))]

class C1 {}
struct S1 {}

public class TypeMap
{
    [Fact]
    public static void Validate_ExternalTypeMapping()
    {
        Console.WriteLine("Validate_ExternalTypeMapping...");

        IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<Guid>();

        Assert.Equal(typeof(C1), map["1"]);
        Assert.Equal(typeof(S1), map["2"]);
        Assert.Equal(typeof(Guid), map["3"]);
        Assert.Equal(typeof(object), map["4"]);
    }

    [Fact]
    public static void Validate_ExternalTypeMapping_SpecialCorElements()
    {
        Console.WriteLine("Validate_ExternalTypeMapping_SpecialCorElements...");

        {
            IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<string>();
            Assert.Equal(typeof(string), map["1"]);
            Assert.False(map.TryGetValue("2", out Type? _));
        }
        {
            IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<object>();
            Assert.Equal(typeof(object), map["1"]);
            Assert.False(map.TryGetValue("2", out Type? _));
        }
    }

    [Fact]
    public static void Validate_ProxyTypeMapping()
    {
        Console.WriteLine("Validate_ProxyTypeMapping...");

        IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<Guid>();

        Assert.Equal(typeof(C1), map[typeof(C1)]);
        Assert.Equal(typeof(S1), map[typeof(S1)]);
        Assert.Equal(typeof(C1), map[typeof(Guid)]);
        Assert.Equal(typeof(S1), map[typeof(string)]);
    }

    [Fact]
    public static void Validate_ProxyTypeMapping_SpecialCorElements()
    {
        Console.WriteLine("Validate_ProxyTypeMapping_SpecialCorElements...");

        {
            IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<string>();
            Assert.Equal(typeof(C1), map[typeof(C1)]);
            Assert.False(map.TryGetValue(typeof(S1), out Type? _));
        }
        {
            IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<object>();
            Assert.Equal(typeof(S1), map[typeof(S1)]);
            Assert.False(map.TryGetValue(typeof(C1), out Type? _));
        }
    }

    [Fact]
    public static void Validate_ExternalTypeMapping_NotSupportedMethods()
    {
        Console.WriteLine("Validate_ExternalTypeMapping_NotSupportedMethods...");

        IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<Guid>();

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
        Console.WriteLine("Validate_ProxyTypeMapping_NotSupportedMethods...");

        IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<Guid>();

        Assert.Throws<NotSupportedException>(() => map.Keys);
        Assert.Throws<NotSupportedException>(() => map.Values);
        Assert.Throws<NotSupportedException>(() => map.Count);
        Assert.Throws<NotSupportedException>(() => map.ContainsKey(typeof(C1)));
        Assert.Throws<NotSupportedException>(() => map.GetEnumerator());
        Assert.Throws<NotSupportedException>(() => ((System.Collections.IEnumerable)map).GetEnumerator());
    }
}
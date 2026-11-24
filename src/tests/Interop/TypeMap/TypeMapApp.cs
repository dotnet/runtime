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

[assembly: TypeMapAssemblyTarget<DuplicateTypeMapEntriesAcrossAssemblies>("TypeMapLib3")]

[assembly: TypeMapAssemblyTarget<UnknownAssemblyReference>("DoesNotExist")]
[assembly: TypeMapAssemblyTarget<BlobOnlyAttributeTypeNames>("TypeMapBlobOnlyLib")]

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

[assembly: TypeMap<TypedValueUseCase>("1", typeof(TypedValueTarget))]
[assembly: TypeMap<TypedValueUseCase>("2", typeof(TypedValueStructTarget))]
[assembly: TypeMap<TypedValueUseCase>("bad", typeof(object))]
[assembly: TypeMap<TypedValueUseCase>("generic", typeof(TypedValueGenericTarget<string>))]
[assembly: TypeMapAssociation<TypedValueUseCase>(typeof(TypedValueSource), typeof(TypedValueTarget))]
[assembly: TypeMapAssociation<TypedValueUseCase>(typeof(TypedValueGenericSource<string>), typeof(TypedValueGenericTarget<string>))]
[assembly: TypeMapAssociation<TypedValueUseCase>(typeof(string), typeof(object))]

[assembly: TypeMap<HolderUseCase>("assembly", typeof(HolderTarget3))]
[assembly: TypeMapTypeTarget<HolderUseCase>(typeof(HolderLib))]
[assembly: TypeMapTypeTarget<HolderUseCase>(typeof(AppHolder))]

[assembly: TypeMap<HolderDuplicateUseCase>("1", typeof(object))]
[assembly: TypeMapTypeTarget<HolderDuplicateUseCase>(typeof(DuplicateHolder))]

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

[assembly: TypeMap<DuplicateTypeMapEntriesAcrossAssemblies>("1", typeof(object))]


[assembly: TypeMapAssociation<DuplicateTypeNameKey>(typeof(DupType_MapObject), typeof(object))]
[assembly: TypeMapAssociation<DuplicateTypeNameKey>(typeof(DupType_MapString), typeof(string))]

// Redefine the same type as in the TypeMapLib2 assembly
// This is testing the duplicate type name key for the
// TypeMapAssociation scenario.
namespace Lib
{
    public class AliasedName { }
}

// Linked from the assembly, and links a type of the other assembly (in the other direction too).
[TypeMap<HolderUseCase>("app", typeof(HolderTarget4))]
[TypeMapAssociation<HolderUseCase>(typeof(AppHolder), typeof(HolderTarget4))]
public class AppHolder { }

// Not linked from the assembly: only found by starting at it.
[TypeMap<HolderUseCase>("unlinked", typeof(HolderTarget4))]
[TypeMapAssociation<HolderUseCase>(typeof(AppUnlinkedHolder), typeof(HolderTarget4))]
public class AppUnlinkedHolder { }

public class AppEmptyHolder { }

[TypeMap<HolderDuplicateUseCase>("1", typeof(object))]
public class DuplicateHolder { }

public class TypeMap
{
    [Fact]
    public static void Validate_TypeMapTypeTarget_Holders()
    {
        Console.WriteLine(nameof(Validate_TypeMapTypeTarget_Holders));

        // The type maps of the group have the entries of the assembly and of the holder types that it links, transitively.
        IReadOnlyDictionary<string, Type> externalMap = TypeMapping.GetOrCreateExternalTypeMapping<HolderUseCase>();
        Assert.Equal(typeof(HolderTarget3), externalMap["assembly"]);
        Assert.Equal(typeof(HolderTarget4), externalMap["app"]);
        Assert.Equal(typeof(HolderTarget1), externalMap["lib"]);
        Assert.Equal(typeof(HolderTarget2), externalMap["chained"]);
        Assert.False(externalMap.TryGetValue("unlinked", out Type? _));

        IReadOnlyDictionary<Type, Type> proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<HolderUseCase>();
        Assert.Equal(typeof(HolderTarget4), proxyMap[typeof(AppHolder)]);
        Assert.Equal(typeof(HolderTarget1), proxyMap[typeof(HolderSource)]);
        Assert.False(proxyMap.TryGetValue(typeof(AppUnlinkedHolder), out Type? _));

        Assert.True(TypeMapping.TryGetValue<HolderUseCase, TypedValue>("lib", out TypedValue value));
        Assert.Equal(1, value.Number);
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, TypedValue>("chained", out value));
        Assert.Equal(2, value.Number);
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, TypedValue>("assembly", out value));
        Assert.Equal(3, value.Number);
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, TypedValue>(typeof(AppHolder), out value));
        Assert.Equal(4, value.Number);
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, TypedValue>("unlinked", out _));

        // The map that starts at an owner type only has the entries of the type and of the types that it links.
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, HolderLib, TypedValue>("lib", out value));
        Assert.Equal(1, value.Number);
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, HolderLib, TypedValue>("chained", out value));
        Assert.Equal(2, value.Number);
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, HolderLib, TypedValue>(typeof(HolderSource), out value));
        Assert.Equal(1, value.Number);
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, HolderLib, TypedValue>("assembly", out _));
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, HolderLib, TypedValue>("app", out _));
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, HolderLib, TypedValue>(typeof(AppHolder), out _));

        Assert.True(TypeMapping.TryGetValue<HolderUseCase, HolderLibChained, TypedValue>("chained", out value));
        Assert.Equal(2, value.Number);
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, HolderLibChained, TypedValue>("lib", out _));

        Assert.True(TypeMapping.TryGetValue<HolderUseCase, AppHolder, TypedValue>("app", out value));
        Assert.Equal(4, value.Number);
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, AppHolder, TypedValue>(typeof(AppHolder), out value));
        Assert.Equal(4, value.Number);
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, AppHolder, TypedValue>("lib", out _));
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, AppHolder, TypedValue>("assembly", out _));

        Assert.True(TypeMapping.TryGetValue<HolderUseCase, AppUnlinkedHolder, TypedValue>("unlinked", out value));
        Assert.Equal(4, value.Number);
        Assert.True(TypeMapping.TryGetValue<HolderUseCase, AppUnlinkedHolder, TypedValue>(typeof(AppUnlinkedHolder), out value));
        Assert.Equal(4, value.Number);
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, AppUnlinkedHolder, TypedValue>("app", out _));

        // A type without entries, an entry of another group, and the group of the entries of a type that is not the one of the owner.
        Assert.False(TypeMapping.TryGetValue<HolderUseCase, AppEmptyHolder, TypedValue>("app", out _));
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, AppHolder, TypedValue>("app", out _));
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, HolderLib, TypedValue>("lib", out _));

        Assert.Throws<ArgumentNullException>(() => TypeMapping.TryGetValue<HolderUseCase, AppHolder, TypedValue>((string)null!, out _));
        Assert.Throws<ArgumentNullException>(() => TypeMapping.TryGetValue<HolderUseCase, AppHolder, TypedValue>((Type)null!, out _));
    }

    [Fact]
    public static void Validate_TypeMapTypeTarget_DuplicateKey()
    {
        Console.WriteLine(nameof(Validate_TypeMapTypeTarget_DuplicateKey));

        // The same key on the assembly and on a holder type.
        AssertExtensions.ThrowsAny<ArgumentException, BadImageFormatException>(() => TypeMapping.GetOrCreateExternalTypeMapping<HolderDuplicateUseCase>());

        // The entries of the holder alone have no duplicate (the mapped type does not provide a value).
        Assert.False(TypeMapping.TryGetValue<HolderDuplicateUseCase, DuplicateHolder, TypedValue>("1", out _));
    }

    [Fact]
    public static void Validate_TryGetValue()
    {
        Console.WriteLine(nameof(Validate_TryGetValue));

        // Proxy map entries only exist for key types that are used by the program.
        GC.KeepAlive(new TypedValueSource());
        GC.KeepAlive(new TypedValueGenericSource<string>());

        // External type map: a type can provide several values, and the values can be value or reference types.
        Assert.True(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>("1", out TypedValue value));
        Assert.Equal(42, value.Number);
        Assert.Equal(nameof(TypedValue), value.Text);
        Assert.True(TypeMapping.TryGetValue<TypedValueUseCase, string>("1", out string? text));
        Assert.Equal(nameof(String), text);
        Assert.True(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>("2", out value));
        Assert.Equal(7, value.Number);
        Assert.Equal(nameof(TypedValueStructTarget), value.Text);

        // Proxy type map
        Assert.True(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>(typeof(TypedValueSource), out value));
        Assert.Equal(42, value.Number);
        Assert.True(TypeMapping.TryGetValue<TypedValueUseCase, string>(typeof(TypedValueSource), out text));
        Assert.Equal(nameof(String), text);

        // Generic mapped type
        Assert.True(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>("generic", out value));
        Assert.Equal(55, value.Number);
        Assert.Equal(nameof(TypedValueGenericTarget<string>), value.Text);
        Assert.True(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>(typeof(TypedValueGenericSource<string>), out value));
        Assert.Equal(55, value.Number);
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>(typeof(TypedValueGenericSource<int>), out _));

        // Keys that are not in the map
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>("missing", out value));
        Assert.Equal(default, value);
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, string>("missing", out text));
        Assert.Null(text);
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>(typeof(int), out value));

        // Mapped types that don't implement ITypeMapValue for the requested type
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, string>("2", out text));
        Assert.Null(text);
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>("bad", out value));
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>(typeof(string), out value));
        Assert.False(TypeMapping.TryGetValue<TypedValueUseCase, long>("1", out _));

        Assert.Throws<ArgumentNullException>(() => TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>((string)null!, out _));
        Assert.Throws<ArgumentNullException>(() => TypeMapping.TryGetValue<TypedValueUseCase, TypedValue>((Type)null!, out _));
    }

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

        // Need to use `new`, not just `typeof` due to trimming rules: https://github.com/dotnet/runtime/blob/550c960396d3bba1a83198d0a3ac24109124e82d/docs/design/features/typemap.md#type-map-entry-trimming-rules
        Assert.Equal(typeof(C1), map[new C1().GetType()]);
        Assert.Equal(typeof(S1), map[((object)default(S1)).GetType()]);
        Assert.Equal(typeof(C1), map[((object)default(Guid)).GetType()]);
        Assert.Equal(typeof(S1), map["".GetType()]);
        Assert.Equal(typeof(C1), map[new List<int>().GetType()]);
        Assert.Equal(typeof(S1), map[typeof(List<>)]);
        Assert.Equal(typeof(C1), map[new C1.I1().GetType()]);
        Assert.Equal(typeof(S1), map[new C1.I2<int>().GetType()]);
        Assert.Equal(typeof(C1), map[new C2<int>().GetType()]);
        Assert.Equal(typeof(S1), map[typeof(C2<>)]);
        Assert.Equal(typeof(C1), map[new int[1].GetType()]);
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

        AssertExtensions.ThrowsAny<ArgumentException, BadImageFormatException>(() => TypeMapping.GetOrCreateExternalTypeMapping<DuplicateTypeMapEntriesAcrossAssemblies>());

        AssertExtensions.ThrowsAny<ArgumentException, BadImageFormatException>(() => TypeMapping.TryGetValue<DuplicateTypeNameKey, TypedValue>("1", out _));
        AssertExtensions.ThrowsAny<ArgumentException, BadImageFormatException>(() => TypeMapping.TryGetValue<DuplicateTypeMapEntriesAcrossAssemblies, TypedValue>("1", out _));
    }

    [Fact]
    public static void Validate_ProxyTypeMapping_DuplicateTypeKey()
    {
        Console.WriteLine(nameof(Validate_ProxyTypeMapping_DuplicateTypeKey));

        IReadOnlyDictionary<Type, Type> map = TypeMapping.GetOrCreateProxyTypeMapping<DuplicateTypeNameKey>();

        Assert.Equal(typeof(object), map[new DupType_MapObject().GetType()]);
        Assert.Equal(typeof(string), map[new DupType_MapString().GetType()]);
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

        Assert.Throws<FileNotFoundException>(() => TypeMapping.TryGetValue<UnknownAssemblyReference, TypedValue>("1", out _));
        Assert.Throws<FileNotFoundException>(() => TypeMapping.TryGetValue<UnknownAssemblyReference, TypedValue>(typeof(object), out _));
    }

    [Fact]
    public static void Validate_MissingAssemblyTarget_DoesNotAffectGroupsWithoutTargets()
    {
        // Validates that groups without TypeMapAssemblyTarget attributes (like TypicalUseCase)
        // still work correctly alongside groups with failing targets (like UnknownAssemblyReference).
        // In R2R, groups without assembly targets must have their precached entry emitted so the
        // runtime doesn't unnecessarily fall back to attribute scanning.
        Console.WriteLine(nameof(Validate_MissingAssemblyTarget_DoesNotAffectGroupsWithoutTargets));

        Assert.Throws<FileNotFoundException>(() => TypeMapping.GetOrCreateExternalTypeMapping<UnknownAssemblyReference>());

        IReadOnlyDictionary<string, Type> externalMap = TypeMapping.GetOrCreateExternalTypeMapping<TypicalUseCase>();
        Assert.Equal(typeof(C1), externalMap["1"]);
        Assert.Equal(typeof(S1), externalMap["2"]);

        IReadOnlyDictionary<Type, Type> proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<TypicalUseCase>();
        Assert.Equal(typeof(C1), proxyMap[new C1().GetType()]);
        Assert.Equal(typeof(S1), proxyMap[((object)default(S1)).GetType()]);

        // When running in R2R mode, verify the R2R image contains TypeMap sections.
        // This confirms CrossGen2 correctly emitted entries for groups without
        // TypeMapAssemblyTarget attributes alongside groups with failed targets.
        string assemblyLocation = typeof(TypeMap).Assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            string r2rDumpFile = assemblyLocation + ".r2rdump";
            if (File.Exists(r2rDumpFile))
            {
                string[] lines = File.ReadAllLines(r2rDumpFile);
                bool hasExternalTypeMaps = false;
                bool hasProxyTypeMaps = false;
                bool hasTypeMapAssemblyTargets = false;
                foreach (string line in lines)
                {
                    if (line.Contains("ExternalTypeMaps", StringComparison.Ordinal))
                        hasExternalTypeMaps = true;
                    if (line.Contains("ProxyTypeMaps", StringComparison.Ordinal))
                        hasProxyTypeMaps = true;
                    if (line.Contains("TypeMapAssemblyTargets", StringComparison.Ordinal))
                        hasTypeMapAssemblyTargets = true;
                }
                Assert.True(hasExternalTypeMaps, "R2R image should contain ExternalTypeMaps section");
                Assert.True(hasProxyTypeMaps, "R2R image should contain ProxyTypeMaps section");
                Assert.True(hasTypeMapAssemblyTargets, "R2R image should contain TypeMapAssemblyTargets section");
            }
        }
    }

    [Fact]
    public static void Validate_BlobOnlyAttributeTypeNames()
    {
        Console.WriteLine(nameof(Validate_BlobOnlyAttributeTypeNames));

        IReadOnlyDictionary<string, Type> externalMap = TypeMapping.GetOrCreateExternalTypeMapping<BlobOnlyAttributeTypeNames>();
        Assert.Equal(typeof(C1), externalMap["blob_only_c1"]);
        Assert.Equal(typeof(S1), externalMap["blob_only_s1"]);
        Assert.Equal(typeof(Lib5Type1), externalMap["lib5_type1"]);
        Assert.True(externalMap.TryGetValue("lib5_type1", out Type? cachedExternalType));
        Assert.Equal(typeof(Lib5Type1), cachedExternalType);

        IReadOnlyDictionary<Type, Type> proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<BlobOnlyAttributeTypeNames>();
        Assert.Equal(typeof(S1), proxyMap[typeof(C1)]);
        Assert.Equal(typeof(C1), proxyMap[typeof(S1)]);
        Assert.Equal(typeof(Lib5Proxy1), proxyMap[new Lib5Type1().GetType()]);
        Assert.Equal(typeof(C1), proxyMap[new DupType_MapObject().GetType()]);
        Assert.Equal(typeof(S1), proxyMap[new DupType_MapString().GetType()]);
        Assert.True(proxyMap.TryGetValue(typeof(Lib5Type1), out Type? cachedProxyType));
        Assert.Equal(typeof(Lib5Proxy1), cachedProxyType);
    }

    [Fact]
    public static void Validate_EmptyOrInvalidMappings()
    {
        Console.WriteLine(nameof(Validate_EmptyOrInvalidMappings));

        AssertExtensions.ThrowsAny<COMException, BadImageFormatException>(() => TypeMapping.GetOrCreateExternalTypeMapping<InvalidTypeNameKey>());
        AssertExtensions.ThrowsAny<COMException, BadImageFormatException>(() => TypeMapping.GetOrCreateProxyTypeMapping<InvalidTypeNameKey>());
    }

    [ConditionalFact(typeof(ValidateNoTypeMapEntries), nameof(ValidateNoTypeMapEntries.HasR2RDumpFile))]
    public static void NoTypeMapEntriesInR2RImageWhenNoTypeMapsDefined()
    {
        Console.WriteLine(nameof(NoTypeMapEntriesInR2RImageWhenNoTypeMapsDefined));

        ValidateNoTypeMapEntries.VerifyNoTypeMapSections();
    }
}

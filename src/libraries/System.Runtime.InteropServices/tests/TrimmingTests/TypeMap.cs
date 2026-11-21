// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using TypeMapTestLibrary;

[assembly: TypeMap<UsedTypeMap>("TrimTargetIsTarget", typeof(TargetAndTrimTarget), typeof(TargetAndTrimTarget))]
[assembly: TypeMap<UsedTypeMap>("TrimTargetIsUnrelated", typeof(TargetType), typeof(TrimTarget))]
[assembly: TypeMap<UsedTypeMap>("TrimTargetIsUnreferenced", typeof(UnreferencedTargetType), typeof(UnreferencedTrimTarget))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(SourceClass), typeof(ProxyType))]

[assembly: TypeMap<UnusedTypeMap>("UnusedName", typeof(UnusedTargetType), typeof(TrimTarget))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(UnusedSourceClass), typeof(UnusedProxyType))]

[assembly: TypeMap<NestedContainer.Universe>("example/Target", typeof(TargetType))]
[assembly: TypeMap<NestedContainer.Universe>("example/\u0130Class", typeof(TargetType))]
[assembly: TypeMap<NestedContainer.Universe>("[Lexample/Target;", typeof(TargetType[]))]
[assembly: TypeMapAssociation<NestedContainer.Universe>(typeof(SourceClass), typeof(GenericProxy<int>))]
[assembly: TypeMap<EmptyTypeMap>("unused", typeof(UnreferencedTargetType), typeof(UnreferencedTrimTarget))]
[assembly: TypeMapAssemblyTarget<DependencyUniverse>("TypeMapTestLibrary")]
[assembly: TypeMap<OtherUniverse>("local/Target", typeof(TargetType))]

if (args.Length > 1 && args[0] == "instantiate")
{
    Console.WriteLine("This code path should never actually be called. It exists exclusively for the trimmer to see that types are used in a way that it can't fully analyze.");
    // Execute some code here to ensure that our "trim target" types are seen as "possibly used".
    object t = Activator.CreateInstance(Type.GetType(args[1]));
    if (t is TargetAndTrimTarget)
    {
        Console.WriteLine("Type deriving from TargetAndTrimTarget instantiated.");
    }
    else if (t is TrimTarget)
    {
        Console.WriteLine("Type deriving from TrimTarget instantiated.");
    }

    Console.WriteLine("Hash code of SourceClass instance: " + new SourceClass().GetHashCode());
    return -1;
}

IReadOnlyDictionary<string, Type> usedTypeMap = TypeMapping.GetOrCreateExternalTypeMapping<UsedTypeMap>();

if (!usedTypeMap.TryGetValue("TrimTargetIsTarget", out Type targetAndTrimTargetType))
{
    Console.WriteLine("TrimTargetIsTarget not found in used type map.");
    return 1;
}

if (targetAndTrimTargetType != GetTypeWithoutTrimAnalysis(nameof(TargetAndTrimTarget)))
{
    Console.WriteLine("TrimTargetIsTarget type does not match expected type.");
    return 2;
}

if (!usedTypeMap.TryGetValue("TrimTargetIsUnrelated", out Type targetType))
{
    Console.WriteLine("TrimTargetIsUnrelated not found in used type map.");
    return 3;
}

if (targetType != GetTypeWithoutTrimAnalysis(nameof(TargetType)))
{
    Console.WriteLine("TrimTargetIsUnrelated type does not match expected type.");
    return 4;
}

if (!RuntimeFeature.IsDynamicCodeSupported && GetTypeWithoutTrimAnalysis(nameof(TrimTarget)) is not null)
{
    Console.WriteLine("TrimTarget should not be preserved if the only place that would preserve it is a check that is optimized away.");
    return 5;
}

if (usedTypeMap.TryGetValue("TrimTargetIsUnreferenced", out _))
{
    Console.WriteLine("TrimTargetIsUnreferenced should not be found in used type map.");
    return 6;
}

IReadOnlyDictionary<Type, Type> usedProxyTypeMap = TypeMapping.GetOrCreateProxyTypeMapping<UsedTypeMap>();
if (!usedProxyTypeMap.TryGetValue(typeof(SourceClass), out Type proxyType))
{
    Console.WriteLine("SourceClass not found in used proxy type map.");
    return 7;
}

if (proxyType != GetTypeWithoutTrimAnalysis(nameof(ProxyType)))
{
    Console.WriteLine("SourceClass proxy type does not match expected type.");
    return 8;
}

if (GetTypeWithoutTrimAnalysis(nameof(UnusedTargetType)) is not null)
{
    Console.WriteLine("UnusedTargetType should not be preserved if the external type map is not used and it is not referenced otherwise even if the entry's trim target is kept.");
    return 9;
}

if (GetTypeWithoutTrimAnalysis(nameof(UnusedProxyType)) is not null)
{
    Console.WriteLine("UnusedProxyType should not be preserved if the proxy type map is not used and it is not referenced otherwise even if the entry's source type is kept.");
    return 10;
}

IReadOnlyDictionary<string, Type> nestedTypeMap = TypeMapping.GetOrCreateExternalTypeMapping<NestedContainer.Universe>();
if (!nestedTypeMap.TryGetValue("example/Target", out Type nestedTarget) ||
    nestedTarget != targetType ||
    !nestedTypeMap.TryGetValue("example/\u0130Class", out Type unicodeTarget) ||
    unicodeTarget != targetType ||
    !nestedTypeMap.TryGetValue("[Lexample/Target;", out Type arrayTarget) ||
    arrayTarget != targetType.MakeArrayType())
{
    Console.WriteLine("Nested universe external mappings do not match expected types.");
    return 11;
}

IReadOnlyDictionary<Type, Type> nestedProxyMap = TypeMapping.GetOrCreateProxyTypeMapping<NestedContainer.Universe>();
if (!nestedProxyMap.TryGetValue(typeof(SourceClass), out Type genericProxy) ||
    genericProxy != GetTypeWithoutTrimAnalysis("GenericProxy`1[System.Int32]"))
{
    Console.WriteLine("Nested universe proxy mapping does not match expected type.");
    return 12;
}

IReadOnlyDictionary<string, Type> emptyTypeMap = TypeMapping.GetOrCreateExternalTypeMapping<EmptyTypeMap>();
if (emptyTypeMap.TryGetValue("unused", out _))
{
    Console.WriteLine("The empty map contains a trimmed entry.");
    return 13;
}

Console.WriteLine(new DependencySource().GetHashCode());
IReadOnlyDictionary<string, Type> dependencyMap = TypeMapping.GetOrCreateExternalTypeMapping<DependencyUniverse>();
if (!dependencyMap.TryGetValue("dependency/Target", out Type dependencyTarget) ||
    dependencyTarget.FullName != "TypeMapTestLibrary.DependencyTarget" ||
    dependencyMap.TryGetValue("dependency/Trimmed", out _))
{
    Console.WriteLine("Cross-assembly external mappings do not match expected types.");
    return 15;
}

IReadOnlyDictionary<Type, Type> dependencyProxyMap = TypeMapping.GetOrCreateProxyTypeMapping<DependencyUniverse>();
if (!dependencyProxyMap.TryGetValue(typeof(DependencySource), out Type dependencyProxy) ||
    dependencyProxy.FullName != "TypeMapTestLibrary.DependencyProxy")
{
    Console.WriteLine("Cross-assembly proxy mapping does not match expected type.");
    return 16;
}

IReadOnlyDictionary<string, Type> otherMap = TypeMapping.GetOrCreateExternalTypeMapping<OtherUniverse>();
if (!otherMap.TryGetValue("local/Target", out Type otherTarget) || otherTarget != targetType ||
    otherMap.TryGetValue("wrong/universe", out _))
{
    Console.WriteLine("An assembly target for one universe included entries from another universe.");
    return 17;
}

if (!CheckArtifact())
    return 14;

return 100;

static bool CheckArtifact()
{
    Dictionary<string, (Dictionary<string, string> External, Dictionary<string, string> Proxy)> groups =
        ReadArtifact(Path.Combine(AppContext.BaseDirectory, "typemaps.xml"));
    if (groups.Count != 5)
    {
        Console.WriteLine("Unexpected type map universe count.");
        return false;
    }

    string assemblyName = typeof(UsedTypeMap).Assembly.GetName().Name;
    var used = groups[NormalizeTypeName($"{nameof(UsedTypeMap)},{assemblyName}")];
    var nested = groups[NormalizeTypeName($"NestedContainer+Universe,{assemblyName}")];
    var empty = groups[NormalizeTypeName($"{nameof(EmptyTypeMap)},{assemblyName}")];
    var dependency = groups[NormalizeTypeName("TypeMapTestLibrary.DependencyUniverse,TypeMapTestLibrary")];
    var other = groups[NormalizeTypeName("TypeMapTestLibrary.OtherUniverse,TypeMapTestLibrary")];

    return CheckMap(used.External, proxy: false,
            ("TrimTargetIsTarget", nameof(TargetAndTrimTarget)),
            ("TrimTargetIsUnrelated", nameof(TargetType))) &&
        CheckMap(used.Proxy, proxy: true, (nameof(SourceClass), nameof(ProxyType))) &&
        CheckMap(nested.External, proxy: false,
            ("example/Target", nameof(TargetType)),
            ("example/\u0130Class", nameof(TargetType)),
            ("[Lexample/Target;", nameof(TargetType) + "[]")) &&
        CheckMap(nested.Proxy, proxy: true,
            (nameof(SourceClass), "GenericProxy`1[[System.Int32,System.Private.CoreLib]]")) &&
        CheckMap(empty.External, proxy: false) &&
        empty.Proxy is null &&
        CheckMap(dependency.External, proxy: false,
            ("dependency/Target", "TypeMapTestLibrary.DependencyTarget,TypeMapTestLibrary")) &&
        CheckMap(dependency.Proxy, proxy: true,
            ("TypeMapTestLibrary.DependencySource,TypeMapTestLibrary", "TypeMapTestLibrary.DependencyProxy,TypeMapTestLibrary")) &&
        CheckMap(other.External, proxy: false, ("local/Target", nameof(TargetType))) &&
        other.Proxy is null;

    bool CheckMap(Dictionary<string, string> map, bool proxy, params (string Key, string Type)[] expected)
    {
        if (map is null || map.Count != expected.Length)
        {
            Console.WriteLine($"Unexpected {(proxy ? "proxy" : "external")} map entry count.");
            return false;
        }

        foreach ((string key, string type) in expected)
        {
            string expectedKey = proxy ? QualifyTypeName(key) : key;
            string expectedType = QualifyTypeName(type);
            if (!map.TryGetValue(expectedKey, out string actualType) || actualType != expectedType)
            {
                Console.WriteLine($"Missing mapping '{key}' to '{type}'.");
                return false;
            }
        }

        return true;
    }

    string QualifyTypeName(string type)
    {
        TypeName name = TypeName.Parse(type);

        return name.AssemblyName is null
            ? NormalizeTypeName($"{type},{assemblyName}")
            : name.AssemblyQualifiedName;
    }
}

static Dictionary<string, (Dictionary<string, string> External, Dictionary<string, string> Proxy)> ReadArtifact(string path)
{
    using XmlReader reader = XmlReader.Create(path);
    reader.MoveToContent();
    if (reader.Name != "typemaps" || reader.HasAttributes)
        throw new InvalidDataException("Unexpected type map XML root.");

    var groups = new Dictionary<string, (Dictionary<string, string> External, Dictionary<string, string> Proxy)>(StringComparer.Ordinal);
    bool emptyRoot = reader.IsEmptyElement;
    reader.ReadStartElement("typemaps");
    while (!emptyRoot && reader.MoveToContent() == XmlNodeType.Element)
    {
        if (reader.Name != "group" || reader.AttributeCount != 1)
            throw new InvalidDataException("Unexpected type map group.");

        string type = reader.GetAttribute("type") ?? throw new InvalidDataException("Missing type map group type.");
        bool emptyGroup = reader.IsEmptyElement;
        reader.ReadStartElement("group");
        (Dictionary<string, string> External, Dictionary<string, string> Proxy) maps = (null, null);
        while (!emptyGroup && reader.MoveToContent() == XmlNodeType.Element)
        {
            switch (reader.Name)
            {
                case "external" when maps.External is null:
                    maps.External = ReadMap(reader, proxy: false);
                    break;
                case "proxy" when maps.Proxy is null:
                    maps.Proxy = ReadMap(reader, proxy: true);
                    break;
                default:
                    throw new InvalidDataException($"Unexpected or duplicate map section '{reader.Name}'.");
            }
        }
        if (!emptyGroup)
            reader.ReadEndElement();
        groups.Add(NormalizeTypeName(type), maps);
    }
    if (!emptyRoot)
        reader.ReadEndElement();
    reader.MoveToContent();

    return groups;

    static Dictionary<string, string> ReadMap(XmlReader reader, bool proxy)
    {
        if (reader.HasAttributes)
            throw new InvalidDataException("Unexpected map section attributes.");

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        bool emptyMap = reader.IsEmptyElement;
        reader.ReadStartElement();
        while (!emptyMap && reader.MoveToContent() == XmlNodeType.Element)
        {
            if (reader.Name != "entry" || reader.AttributeCount != 2)
                throw new InvalidDataException("Unexpected type map entry.");

            string key = reader.GetAttribute("key") ?? throw new InvalidDataException("Missing type map entry key.");
            string value = reader.GetAttribute("value") ?? throw new InvalidDataException("Missing type map entry value.");
            map.Add(proxy ? NormalizeTypeName(key) : key, NormalizeTypeName(value));
            bool emptyEntry = reader.IsEmptyElement;
            reader.ReadStartElement("entry");
            if (!emptyEntry)
                reader.ReadEndElement();
        }
        if (!emptyMap)
            reader.ReadEndElement();

        return map;
    }
}

static string NormalizeTypeName(string name) => TypeName.Parse(name).AssemblyQualifiedName;

[MethodImpl(MethodImplOptions.NoInlining)]
static Type GetTypeWithoutTrimAnalysis(string typeName)
{
    return Type.GetType(typeName, throwOnError: false);
}

class UsedTypeMap;
class TargetAndTrimTarget;
class TargetType;
class TrimTarget;
class UnreferencedTargetType;
class UnreferencedTrimTarget;
class SourceClass;
class ProxyType;

class UnusedTypeMap;
class UnusedTargetType;
class UnusedSourceClass;
class UnusedProxyType;

class NestedContainer
{
    public class Universe;
}

class GenericProxy<T>;
class EmptyTypeMap;

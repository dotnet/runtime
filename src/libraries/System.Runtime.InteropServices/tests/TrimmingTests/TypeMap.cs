// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: TypeMap<UsedTypeMap>("TrimTargetIsTarget", typeof(TargetAndTrimTarget), typeof(TargetAndTrimTarget))]
[assembly: TypeMap<UsedTypeMap>("TrimTargetIsUnrelated", typeof(TargetType), typeof(TrimTarget))]
[assembly: TypeMap<UsedTypeMap>("TrimTargetIsUnreferenced", typeof(UnreferencedTargetType), typeof(UnreferencedTrimTarget))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(SourceClass), typeof(ProxyType))]

[assembly: TypeMap<UnusedTypeMap>("UnusedName", typeof(UnusedTargetType), typeof(TrimTarget))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(UnusedSourceClass), typeof(UnusedProxyType))]

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

if (GetTypeWithoutTrimAnalysis(nameof(TrimTarget)) is not null)
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

return 100;

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

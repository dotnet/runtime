// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: TypeMap<ValueTypeMap>("Value", typeof(ValueTarget))]
[assembly: TypeMapAssociation<ValueTypeMap>(typeof(ValueSource), typeof(ValueTarget))]

// The mapped types are not instantiated: the trimmer has to keep what TryGetValue needs to find the values they provide.
_ = new ValueSource();

if (!TypeMapping.TryGetValue<ValueTypeMap, int>("Value", out int number) || number != 42)
{
    Console.WriteLine("The value type value of the external type map was not found.");
    return 1;
}

if (!TypeMapping.TryGetValue<ValueTypeMap, string>("Value", out string text) || text != "value")
{
    Console.WriteLine("The reference type value of the external type map was not found.");
    return 2;
}

if (!TypeMapping.TryGetValue<ValueTypeMap, int>(typeof(ValueSource), out number) || number != 42)
{
    Console.WriteLine("The value of the proxy type map was not found.");
    return 3;
}

if (TypeMapping.TryGetValue<ValueTypeMap, long>("Value", out _) || TypeMapping.TryGetValue<ValueTypeMap, int>("Missing", out _))
{
    Console.WriteLine("A value that is not provided was found.");
    return 4;
}

// Ahead of time, a group that is only used with TryGetValue does not make the mapped types visible to reflection.
if (!RuntimeFeature.IsDynamicCodeSupported && GetTypeWithoutTrimAnalysis(nameof(ValueTarget)) is not null)
{
    Console.WriteLine("ValueTarget should not be preserved if the type map is only used with TryGetValue.");
    return 5;
}

return 100;

[MethodImpl(MethodImplOptions.NoInlining)]
static Type GetTypeWithoutTrimAnalysis(string typeName)
{
    return Type.GetType(typeName, throwOnError: false);
}

class ValueTypeMap;
class ValueSource;

class ValueTarget : ITypeMapValue<int>, ITypeMapValue<string>
{
    static int ITypeMapValue<int>.Value => 42;
    static string ITypeMapValue<string>.Value => "value";
}

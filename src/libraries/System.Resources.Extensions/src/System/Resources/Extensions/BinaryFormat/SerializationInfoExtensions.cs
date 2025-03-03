// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace System.Resources.Extensions.BinaryFormat;

internal static class SerializationInfoExtensions
{
    private static readonly Action<SerializationInfo, string, object, Type> s_updateValue =
        typeof(SerializationInfo)
        .GetMethod("UpdateValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
#if NETCOREAPP
        .CreateDelegate<Action<SerializationInfo, string, object, Type>>();
#else
        .CreateDelegate(typeof(Action<SerializationInfo, string, object, Type>)) as Action<SerializationInfo, string, object, Type>;
#endif


    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(SerializationInfo))]
    internal static void UpdateValue(this SerializationInfo si, string name, object value, Type type) =>
        s_updateValue(si, name, value, type);
}

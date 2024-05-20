// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat;

internal static class FormatterServices
{
    private static AssemblyNameInfo? s_coreLibAssemblyName;

    internal static AssemblyNameInfo CoreLibAssemblyName => s_coreLibAssemblyName ??= AssemblyNameInfo.Parse(GetAssemblyNameIncludingTypeForwards(typeof(object)).AsSpan());

    internal static string GetAssemblyNameIncludingTypeForwards(Type type)
    {
        // Special case types like arrays
        Type attributedType = type;
        while (attributedType.HasElementType)
        {
            attributedType = attributedType.GetElementType()!;
        }

        foreach (Attribute first in attributedType.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false))
        {
            return ((TypeForwardedFromAttribute)first).AssemblyFullName;
        }

        return type.Assembly.FullName!;
    }

    internal static string GetTypeFullNameIncludingTypeForwards(Type type)
    {
        return type.IsArray ?
            GetClrTypeFullNameForArray(type) :
            GetClrTypeFullNameForNonArrayTypes(type);
    }

    private static string GetClrTypeFullNameForArray(Type type)
    {
        int rank = type.GetArrayRank();

        string typeName = GetTypeFullNameIncludingTypeForwards(type.GetElementType()!);
        return rank == 1 ?
            typeName + "[]" :
            typeName + "[" + new string(',', rank - 1) + "]";
    }

    private static string GetClrTypeFullNameForNonArrayTypes(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName!;
        }

        var builder = new StringBuilder(type.GetGenericTypeDefinition().FullName).Append('[');

        foreach (Type genericArgument in type.GetGenericArguments())
        {
            builder.Append('[').Append(GetTypeFullNameIncludingTypeForwards(genericArgument)).Append(", ");
            builder.Append(GetAssemblyNameIncludingTypeForwards(genericArgument)).Append("],");
        }

        // remove the last comma and close typename for generic with a close bracket
        return builder.Remove(builder.Length - 1, 1).Append(']').ToString();
    }
}

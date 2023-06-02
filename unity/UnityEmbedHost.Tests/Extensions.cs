// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace UnityEmbedHost.Tests;

public static class Extensions
{
    public static MethodInfo[] GetAllInstanceMethods(this Type type)
        => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    public static MethodInfo FindInstanceMethodByName(this Type type, string name, Type[]? parameterTypes)
        => type
            .GetAllInstanceMethods()
            .Where(m => m.Name == name)
            .Single(m => parameterTypes == null || ParametersMatch(m, parameterTypes));

    public static MethodInfo FindInstanceMethodByNameOrExplicitInterfaceName(this Type type, Type baseType, string name, Type[]? parameterTypes)
    {
        var tmp = type.GetAllInstanceMethods()
            .Where(m => m.Name == name || (baseType.IsInterface && m.Name == $"{baseType.FullName}.{name}"))
            .Where(m => parameterTypes == null || ParametersMatch(m, parameterTypes))
            .ToArray();

        if (tmp.Length == 1)
            return tmp[0];

        // If there are multiple matches, we need to find the one that is for the least derived type we're looking at.
        // this can happen with explicit interface implementations.

        var currentType = type;
        while (currentType != typeof(object))
        {
            foreach (var candidate in tmp)
            {
                if (candidate.DeclaringType == currentType)
                    return candidate;
            }

            currentType = currentType!.BaseType;
        }

        throw new ArgumentException($"No method {name} found on {type}");
    }

    static bool ParametersMatch(MethodInfo method, Type[] parameterTypes)
    {
        var methodParameters = method.GetParameters();
        if (methodParameters.Length != parameterTypes.Length)
            return false;

        for(int i = 0; i < methodParameters.Length; i++)
        {
            if (methodParameters[i].ParameterType != parameterTypes[i])
                return false;
        }

        return true;
    }
}

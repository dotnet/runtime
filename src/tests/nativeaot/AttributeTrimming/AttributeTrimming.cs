// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

[My]
class Program
{
    static int Main()
    {
#if !TRIMMED
        typeof(Program).GetCustomAttributes(inherit: false);
#endif

        Type t = GetTypeSecretly(nameof(Canary));

#if TRIMMED
        return t == null ? 100 : 101;
#else
        return t != null ? 100 : 101;
#endif
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    static Type GetTypeSecretly(string name) => Type.GetType(name);
}

class MyAttribute : Attribute
{
    public MyAttribute()
    {
        Type.GetType(nameof(Canary)).ToString();
    }
}

class Canary { }

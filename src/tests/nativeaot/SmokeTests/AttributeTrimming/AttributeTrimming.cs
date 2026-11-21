// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

[Type]
class Program
{
    [Method]
    static int Main()
    {
        // Sanity check: we don't currently expect attributes on types to be optimized away
        if (GetTypeSecretly(nameof(TypeAttribute)) == null)
            throw new Exception("Type");

        // Main should be reflection visible
        if (MethodBase.GetCurrentMethod().Name != nameof(Main))
            throw new Exception("Name");

#if !DEBUG
        // But we should have optimized out the attributes on it
        if (GetTypeSecretly(nameof(MethodAttribute)) != null)
            throw new Exception("Method");
#endif

#if !MULTIMODULE_BUILD
        // Nothing reflects on Invoke methods of delegate types that are not statically known,
        // so the Invoke method of a delegate that is only invoked should not be reflectable.
        NeverReflectedOnDelegate del = GetDelegate();
        if (del(41) != 42)
            throw new Exception("Delegate");

        foreach (MethodInfo method in GetMethodsSecretly(del.GetType()))
        {
            if (method.Name == "Invoke")
                throw new Exception("Invoke");
        }
#endif

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static NeverReflectedOnDelegate GetDelegate() => value => value + 1;

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "That's the point")]
    static MethodInfo[] GetMethodsSecretly(Type type) => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "That's the point")]
    static Type GetTypeSecretly(string name) => Type.GetType(name);
}

class MethodAttribute : Attribute;
class TypeAttribute : Attribute;

delegate int NeverReflectedOnDelegate(int value);

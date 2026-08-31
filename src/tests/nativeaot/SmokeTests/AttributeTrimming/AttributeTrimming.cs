// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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

        return 100;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "That's the point")]
    static Type GetTypeSecretly(string name) => Type.GetType(name);
}

class MethodAttribute : Attribute;
class TypeAttribute : Attribute;

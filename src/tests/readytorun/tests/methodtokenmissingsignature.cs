// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

public class MethodTokenMissingSignatureTests
{
    private sealed class MissingDependencyContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName name)
        {
            if (name.Name == "signaturedependency")
            {
                throw new FileNotFoundException("The signature dependency must remain unavailable.", name.FullName);
            }

            return null;
        }
    }

    // In noncomposite R2R runs, the ldtoken helpers are compiled with the dependency available,
    // then run without it. Resolving a handle must not load the uncalled method's signature.
    [Theory]
    [InlineData(nameof(MethodTokenMissingSignature.GetParameterMethodHandle))]
    [InlineData(nameof(MethodTokenMissingSignature.GetReturnMethodHandle))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ResolveMethodTokenWithoutLoadingSignature(string methodName)
    {
        MissingDependencyContext context = new();
        Assembly assembly = context.LoadFromAssemblyPath(
            Path.Combine(AppContext.BaseDirectory, "methodtokensignaturehelper.dll"));
        MethodInfo method = assembly.GetType(nameof(MethodTokenMissingSignature)).GetMethod(methodName);
        RuntimeMethodHandle handle = (RuntimeMethodHandle)method.Invoke(null, null);

        Assert.NotEqual(IntPtr.Zero, handle.Value);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;

public partial class LazyLoadingTest
{
    [JSExport]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Library.Person", "Json")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "LazyLibrary.Foo", "LazyLibrary")]
#if TEST_NO_IL_LAZY_ASSEMBLY
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "NoILLazyLibrary.NoILType", "NoILLazyLibrary")]
#endif
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The reflected types are preserved by DynamicDependency.")]
    public static void Run()
    {
        Type person = Assembly.Load("Json").GetType("Library.Person", throwOnError: true)!;
        TestOutput.WriteLine("FirstName=" + person.Name);
#if TEST_NO_IL_LAZY_ASSEMBLY
        Type noILType = Assembly.Load("NoILLazyLibrary").GetType("NoILLazyLibrary.NoILType", throwOnError: true)!;
        TestOutput.WriteLine("NoILType=" + noILType.Name);
#endif
    }
}

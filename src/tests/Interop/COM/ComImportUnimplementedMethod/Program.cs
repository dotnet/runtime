// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

using ComImportUnimplemented;

using Xunit;

public class ComImportUnimplementedMethodTests
{
    // A ComImport interface method is dispatched through a CLR->COM call, which is only
    // meaningful for a runtime callable wrapper. A plain managed type that claims to implement
    // such an interface therefore must not pick the interface method up as an implementation -
    // it has to fail exactly like any other type with an unimplemented interface method.
    [Theory]
    [InlineData(true, nameof(ClassMissingComImportMethod))]
    [InlineData(false, nameof(ClassMissingManagedMethod))]
    public static void UnimplementedInterfaceMethodFailsToLoadType(bool comImport, string expectedTypeName)
    {
        TypeLoadException ex = Assert.Throws<TypeLoadException>(
            () =>
            {
                if (comImport)
                {
                    CallComImportMethod();
                }
                else
                {
                    CallManagedMethod();
                }
            });

        Assert.Contains(expectedTypeName, ex.Message);
        Assert.Contains(nameof(IComImportInterface.Method), ex.Message);
    }

    // The type load failure surfaces when these methods are prepared, so each one has to stay
    // out of the test method itself.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallComImportMethod()
    {
        IComImportInterface obj = new ClassMissingComImportMethod();
        obj.Method();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallManagedMethod()
    {
        IManagedInterface obj = new ClassMissingManagedMethod();
        obj.Method();
    }
}

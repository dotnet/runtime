// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class InternalMethodImplTest
{
    [Fact, SkipOnMono("the error message is specific to coreclr")]
    public static int TypeLoadExceptionMessageContainsMethodNameWhenInternalCallOnlyMethodIsCalled()
    {
        if (TestLibrary.Utilities.IsNativeAot)
        {
            return 100; // unsupported on NativeAOT
        }

        try
        {
            new F1();
            return -1;
        }
        catch (TypeLoadException ex)
        {
            return ex.Message.Contains("Internal call method 'F2.Foo' with non-zero RVA.") ? 100 : -1;
        }
        catch (Exception ex)
        {
            return -1;
        }
    }
}

class F1
{
    [MethodImpl(MethodImplOptions.NoInlining)] // The exception is thrown when restoring the caller with R2R
    public F1()
    {
        var f2 = new F2();
    }
}

class F2
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public void Foo()
    {

    }
}

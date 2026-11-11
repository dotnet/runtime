// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}

// This regression test only has strong meaning when run under GCStress, and covers the R2R
// delegate-constructor path only when the test assemblies are also compiled with crossgen2.
// Without GCStress, it passes without exercising the fix.
[OuterLoop]
public class Test136814
{
    [Fact]
    public static int TestEntryPoint()
    {
        Test136814 test = new Test136814();
        if (DelegateCaller.Invoke(test.Target) != 100)
        {
            return 101;
        }

        return CreateDelegateInJittedCode(test);
    }

    [Fact]
    public static int ReferenceContainingStruct()
    {
        return DelegateCaller.Invoke(new Test136814().ReferenceTarget);
    }

    // Use a separate signature type so the first case cannot have loaded it already.
    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CreateDelegateInJittedCode(Test136814 test)
    {
        return DelegateCaller.Invoke(test.JittedConstructionTarget);
    }

    [BypassReadyToRun]
    private int Target(CallbackValue? value)
    {
        return 100;
    }

    [BypassReadyToRun]
    private int JittedConstructionTarget(KindCallbackValue? value)
    {
        return 100;
    }

    [BypassReadyToRun]
    private object ReferenceTarget(ReferenceCallbackValue value)
    {
        return value.Value;
    }
}

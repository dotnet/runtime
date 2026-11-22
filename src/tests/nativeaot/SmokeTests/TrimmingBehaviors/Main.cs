// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: TypeMapAssemblyTarget<PrivateReflectionUniverse>("DisablePrivateReflectionLibrary")]

bool success = RunTest(Dataflow.Run);
success &= RunTest(DeadCodeElimination.Run);
success &= RunTest(FeatureSwitches.Run);
success &= RunTest(ILLinkDescriptor.Run);
success &= RunTest(DependencyInjectionPattern.Run);
success &= RunTest(DisablePrivateReflectionTests.Run);
success &= RunTest(DisablePrivateReflectionConsumer.Run);
success &= RunTest(DisablePrivateReflectionTests.RunTypeMap);
if (!OperatingSystem.IsWasi())
{
    success &= RunTest(UnmanagedCallersOnlyAssociatedSourceType.Run);
}

return success ? 100 : 1;

static bool RunTest(Func<int> t, [CallerArgumentExpression(nameof(t))] string name = null)
{
    Console.WriteLine($"===== Running test {name} =====");
    bool success = true;
    try
    {
        success = t() == 100;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
        success = false;
    }
    Console.WriteLine($"===== Test {name} {(success ? "succeeded" : "failed")} =====");
    return success;
}

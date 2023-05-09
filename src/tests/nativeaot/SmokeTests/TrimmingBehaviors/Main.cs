// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

bool success = RunTest(Dataflow.Run);
success &= RunTest(DeadCodeElimination.Run);
success &= RunTest(FeatureSwitches.Run);
success &= RunTest(ILLinkDescriptor.Run);
success &= RunTest(DependencyInjectionPattern.Run);

return success ? 100 : 1;

static bool RunTest(Func<int> t, [CallerArgumentExpression("t")] string name = null)
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

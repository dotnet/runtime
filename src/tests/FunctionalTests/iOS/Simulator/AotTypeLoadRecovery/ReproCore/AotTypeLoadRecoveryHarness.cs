// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ReproCore;

public static class AotTypeLoadRecoveryHarness
{
    public static void Run()
    {
        StorePathHarness.Run();
        InitObjTypeLoadHarness.Run();
        RunAndExpectFailure(nameof(LoadSideInlineHarness), LoadSideInlineHarness.Run);
    }

    private static void RunAndExpectFailure(string scenarioName, Action scenario)
    {
        try
        {
            scenario();
        }
        catch (Exception ex) when (IsExpectedRecoveryFailure(ex))
        {
            return;
        }

        throw new InvalidOperationException($"{scenarioName} completed without the expected typeload recovery failure.");
    }

    private static bool IsExpectedRecoveryFailure(Exception ex)
    {
        return ex is TypeLoadException
            or MissingFieldException
            or BadImageFormatException;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ReproCore;

public static class AotTypeLoadRecoveryHarness
{
    public static void Run()
    {
        StorePathHarness.Run();
        InitObjTypeLoadHarness.Run();
        LoadSideInlineHarness.Run();
    }
}

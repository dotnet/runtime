using System;

internal class StartupHook
{
    public static void Initialize()
    {
        AppContext.SetData("Test.StartupHookForFunctionalTest.DidRun", "Yes");
    }
}

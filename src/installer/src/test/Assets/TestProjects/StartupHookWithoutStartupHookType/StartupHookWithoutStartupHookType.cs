using System;

internal class StartupHookWrongType
{
    public static void Initialize()
    {
        // This hook should not be called because it doesn't have the
        // correct type name (StartupHook). Instead, the startup hook
        // provider code should throw an exception.
        Console.WriteLine("Hello from startup hook!");
    }
}

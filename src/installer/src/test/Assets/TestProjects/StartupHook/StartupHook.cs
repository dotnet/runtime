using System;

internal class StartupHook
{
    public static void Initialize()
    {
        // Normal success case with a simple startup hook.
        Console.WriteLine("Hello from startup hook!");
    }
}

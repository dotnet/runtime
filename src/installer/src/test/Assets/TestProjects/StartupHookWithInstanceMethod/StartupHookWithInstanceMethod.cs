using System;

internal class StartupHook
{
    public void Initialize()
    {
        // This hook should not be called because it's an instance
        // method. Instead, the startup hook provider code should
        // throw an exception.
        Console.WriteLine("Hello from startup hook with instance method!");
    }
}

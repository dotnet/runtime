// External types for async transitive cross-module tests.
// Similar to ExternalLib but with async-friendly types.
using System;

public static class AsyncExternalLib
{
    public static int ExternalValue => 77;

    public class AsyncExternalType
    {
        public string Label { get; set; } = "external";
    }
}

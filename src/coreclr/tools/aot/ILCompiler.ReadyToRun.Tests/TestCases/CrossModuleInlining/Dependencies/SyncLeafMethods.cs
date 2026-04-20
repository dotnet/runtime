// Leaf data types and values used by other cross-module fixtures to
// exercise transitive references.
using System;

public static class SyncLeafMethods
{
    public static int ExternalValue => 99;

    public class ExternalType
    {
        public int Value { get; set; }
        public string Label { get; set; } = "external";
    }

    public class Outer
    {
        public class Inner
        {
            public static int NestedValue => 77;
        }
    }
}

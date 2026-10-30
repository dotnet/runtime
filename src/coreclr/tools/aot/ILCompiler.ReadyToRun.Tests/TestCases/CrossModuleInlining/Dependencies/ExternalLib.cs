using System;

public static class ExternalLib
{
    public static int ExternalValue => 99;

    public class ExternalType
    {
        public int Value { get; set; }
    }

    public class Outer
    {
        public class Inner
        {
            public static int NestedValue => 77;
        }
    }
}

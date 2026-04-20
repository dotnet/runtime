// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

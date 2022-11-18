// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public partial class LazyMath
{
    public static int IntAdd(int a, int b)
    {
        int c = a + b;
        return c;
    }
}

namespace DebuggerTests
{
    public class ClassToCheckFieldValue
    {
        public int valueToCheck;
        public ClassToCheckFieldValue()
        {
            valueToCheck = 20;
        }
    }
}

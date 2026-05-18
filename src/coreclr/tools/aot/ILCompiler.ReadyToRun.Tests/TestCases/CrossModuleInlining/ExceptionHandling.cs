// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public static class ExceptionHandling
{
    public static int CallDependency() => InlineableLib.GetValue();

    public static int MethodWithExceptionInfo(int value)
    {
        try
        {
            return 100 / value;
        }
        catch (DivideByZeroException)
        {
            return -1;
        }
    }
}

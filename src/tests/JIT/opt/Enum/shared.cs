// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Some simple tests for the Enum.HasFlag optimization.
// Verify the optimization avoids firing for shared types.

using System;
using System.Runtime.CompilerServices;
using Xunit;

class MyG<T,U> 
{
    public enum A 
    {
        X = 1
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void foo() 
    {
        var a = MyG<object,U>.A.X;
        a.HasFlag(MyG<T,string>.A.X);
    }
}

public class My 
{
    [Fact]
    public static int TestEntryPoint() 
    {
        int result = 0;
        try 
        {
            MyG<My,My>.foo();
        }
        catch(ArgumentException)
        {
            result = 100;
        }
        return result;
    }
}

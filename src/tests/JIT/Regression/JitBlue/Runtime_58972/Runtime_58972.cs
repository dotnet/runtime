// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Runtime_58972
{
    [Fact]
    public static int TestEntryPoint()
    {
        GetItem(new MyStruct[1], 0);
        return 100;
    }

	// This code results in a struct returned in register where we replace the local
	// of type MyStruct by its only field, and where that field cannot be enregistered.
	// We would potentially miss normalization if the struct was returned as an integer
	// type and hit a defensive assertion because of it.
    static MyStruct GetItem(MyStruct[] a, int i)
    {
        try
        {
            return a[i];
        }
        catch (IndexOutOfRangeException)
        {
            ThrowHelper();
            return default;
        }
    }

    static void ThrowHelper() => throw new Exception();

    struct MyStruct
    {
        byte b;
    }
}

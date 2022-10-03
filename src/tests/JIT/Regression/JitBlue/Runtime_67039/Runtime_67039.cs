// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public class Runtime_67039
{
    public static int Main()
    {
        Vector128<float> left = Vector128.Create(1.0f, 2, 3, 4);
        Vector128<float> right = Vector128.Create(4.0f, 3, 2, 1);

        var result = Vector128.ConditionalSelect(Vector128.GreaterThan(left, right), left, right);

        Vector128<float> expectedResult = Vector128.Create(4.0f, 3, 3, 4);
        if (result == expectedResult) 
        {
            return 100;
        }
        else 
        {
            return 0;
        }
    }
}

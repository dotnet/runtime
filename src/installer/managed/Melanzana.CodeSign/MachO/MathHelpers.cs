// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana;

public static class MathHelpers
{
    public static uint Log2(uint value)
    {
        uint result = 0;
        while (value > 1)
        {
            value >>= 1;
            result++;
        }
        return result;
    }
}

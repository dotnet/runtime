// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Summary description for Class1
/// </summary>
public class MathFTestLib
{
    private static decimal epsilon = new decimal(0.000001D);

    public static decimal Epsilon
    {
        get
        {
            return epsilon;
        }

        set
        {
            epsilon = Convert.ToDecimal(value);
        }
    }

    public static bool SingleIsWithinEpsilon(float x, float y)
    {
        decimal dx = new decimal(x);
        decimal dy = new decimal(y);

        decimal diff = Math.Abs(decimal.Subtract(dx, dy));
        return diff.CompareTo(Epsilon) <= 0;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Summary description for Class1
/// </summary>
public class MathTestLib
{
    private static Decimal epsilon = new Decimal(0.000001D);

    public static Decimal Epsilon
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

    public static bool DoubleIsWithinEpsilon(double x, double y)
    {
        Decimal dx = new Decimal(x);
        Decimal dy = new Decimal(y);
        Decimal diff = Math.Abs(Decimal.Subtract(dx, dy));
        return diff.CompareTo(Epsilon) <= 0;
    }

    public static bool FloatIsWithinEpsilon(float x, float y)
    {
        Decimal dx = new Decimal(x);
        Decimal dy = new Decimal(y);
        Decimal diff = Math.Abs(Decimal.Subtract(dx, dy));
        return diff.CompareTo(Epsilon) <= 0;
    }
}

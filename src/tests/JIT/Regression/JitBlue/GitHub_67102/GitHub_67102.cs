// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In linux-arm, for double type variable, we assign pair of registers
//       to an interval. During unassigning, if the current interval doesn't
//       have any more reference, we would restore the register pair to the
//       previously assigned interval. However, after that, we were again
//       resetting the assignedInterval of single register out of the pair
//       to `nullptr` thinking that we just need to unassign. This was not
//       needed and because of that we would see mismatch in assigned
//       interval for those two registers.
using System;

public class Program_67102
{
    public static int Main()
    {
        new Func<double, double, Size>(new OrientationBasedMeasures().MinorMajorSize)(1, 2);
        return 100;
    }
}

internal enum ScrollOrientation
{
    Vertical,
    Horizontal,
}

internal class OrientationBasedMeasures
{
    public ScrollOrientation ScrollOrientation { get; set; } = ScrollOrientation.Vertical;
    public Size MinorMajorSize(double minor, double major)
    {
        return ScrollOrientation == ScrollOrientation.Vertical ?
            new Size(minor, major) :
            new Size(major, minor);
    }
}

public record struct Size(double Width, double Height);
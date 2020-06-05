// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Diagnostics;

class Runtime_37506
{
    public static int Main()
    {
        var a = new Vector<Vector4>(new Vector4(1));
        try
        {
            a = a + a;
            Debug.Assert(false, "unreachable");
        }
        catch (System.NotSupportedException)
        {
        }
        return 100;

    }
}

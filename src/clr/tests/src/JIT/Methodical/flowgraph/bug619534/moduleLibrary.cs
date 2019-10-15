// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Test case library for Dev10 bug #640711
//  Please see moduleHandleCache.cs

[assembly: System.Runtime.CompilerServices.CompilationRelaxations(0)]

public static class Throws
{
    public static void M(bool b)
    {
        if (b)
            throw new System.Exception("a");
    }
}

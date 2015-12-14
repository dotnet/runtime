// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

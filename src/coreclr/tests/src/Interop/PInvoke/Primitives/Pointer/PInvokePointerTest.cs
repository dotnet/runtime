// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe class Program
{
    [StructLayout(LayoutKind.Sequential)]
    struct NonBlittable
    {
        bool _nonBlittable;
    }

    // the "string unused" parameter is just so that we don't hit https://github.com/dotnet/coreclr/issues/27408 that
    // makes this p/invoke actually work.
    [DllImport("Unused")]
    private static extern void PointerToNonBlittableType(NonBlittable* pNonBlittable, string unused);

    static int Main()
    {
        Assert.Throws<MarshalDirectiveException>(() => PointerToNonBlittableType(null, null));

        return 100;
    }
}

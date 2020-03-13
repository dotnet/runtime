// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
class GitHub_27279
{
    unsafe static int Main()
    {
        bool res = Unsafe.IsAddressLessThan(ref Unsafe.AsRef<byte>((void*)(-1)), ref Unsafe.AsRef<byte>((void*)(1)));
        Console.WriteLine(res.ToString());
        if (res)
        {
            return 101;
        }        
        return 100;
    }
}

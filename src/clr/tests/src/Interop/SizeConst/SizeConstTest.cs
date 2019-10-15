// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct S_CHARArray_ByValTStr
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
    public string arr;
    public S_CHARArray_ByValTStr(string parr) { arr = parr; }
}

class Test
{
    internal const int ARRAY_SIZE = 100;

    //UnmanagedType.ByValTStr
    [DllImport("SizeConstNative",CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByValTStr(S_CHARArray_ByValTStr s, int size);

    static bool SizeConstByValTStr()
    {
        // always marshal managedArray.Length
        S_CHARArray_ByValTStr s = new S_CHARArray_ByValTStr();
        s.arr = "abcd";
        TakeByValTStr(s, s.arr.Length);

        // off by one byte since  sizeconst == 4 and 
        // number of bytes == 4 . We used to write 
        // one past the buffer before but now we truncate at 3rd byte.
        // In order to test this the locale of the machine need to 
        // a multibyte char set.
        s.arr = "个个";
        TakeByValTStr(s, s.arr.Length);
        return true;
    }

    static int Main(string[] args)
    {
        SizeConstByValTStr();
        return 100;
    }
}

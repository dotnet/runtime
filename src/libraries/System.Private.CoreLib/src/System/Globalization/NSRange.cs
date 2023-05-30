// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.InteropServices;
namespace System.Globalization
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NSRange
    {
        [MarshalAs(UnmanagedType.I4)]
        public int Location;
        [MarshalAs(UnmanagedType.I4)]
        public int Length;
    }
}

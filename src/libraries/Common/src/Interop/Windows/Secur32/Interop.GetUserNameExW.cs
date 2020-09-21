// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Secur32
    {
        [DllImport(Libraries.Secur32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern BOOLEAN GetUserNameExW(int NameFormat, ref char lpNameBuffer, ref uint lpnSize);

        internal const int NameSamCompatible = 2;
    }
}

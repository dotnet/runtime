// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        public const int LOCALE_NAME_MAX_LENGTH = 85;
        [DllImport(Libraries.Localization)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        internal static extern int ResolveLocaleName(string lpNameToResolve, StringBuilder lpLocaleName, int cchLocaleName);
    }
}

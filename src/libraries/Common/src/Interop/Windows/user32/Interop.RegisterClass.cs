// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class User32
    {
        [DllImport(Libraries.User32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        public static extern short RegisterClass(WNDCLASS wc);
    }
}

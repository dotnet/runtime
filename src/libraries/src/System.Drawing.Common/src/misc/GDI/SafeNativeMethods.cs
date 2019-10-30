// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    /// <summary>
    /// This is an extract of the System.Drawing IntNativeMethods in the CommonUI tree.
    /// This is done to be able to compile the GDI code in both assemblies System.Drawing and System.Windows.Forms.
    /// </summary>
    internal static partial class IntSafeNativeMethods
    {
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);
    }
}

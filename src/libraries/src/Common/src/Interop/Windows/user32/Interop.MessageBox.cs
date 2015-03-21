// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class User32
    {
        internal const int MB_ICONHAND = 0x00000010;
        internal const int MB_OKCANCEL = 0x00000001;
        internal const int MB_RIGHT = 0x00080000;
        internal const int MB_RTLREADING = 0x00100000;
        internal const int MB_TOPMOST = 0x00040000;

        internal const int IDCANCEL = 2;
        internal const int IDOK = 1;

        [DllImport(Libraries.User32, CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW", ExactSpelling = true, SetLastError = true)]
        private static extern int MessageBoxSystem(IntPtr hWnd, string text, string caption, int type);

        internal static int MessageBox(IntPtr hWnd, string text, string caption, int type)
        {
            try
            {
                return MessageBoxSystem(hWnd, text, caption, type);
            }
            catch (DllNotFoundException)
            {
                return 0;
            }
            catch (TypeLoadException)
            {
                return 0;
            }
        }
    }
}

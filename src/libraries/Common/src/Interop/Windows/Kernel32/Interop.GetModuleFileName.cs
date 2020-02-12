// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        private static extern int GetModuleFileName(HandleRef hModule, ref char buffer, int length);

        public static string GetModuleFileName(HandleRef hModule)
        {
            const int MODULE_FILENAME_LENGTH_LIMIT = short.MaxValue;

            ValueStringBuilder buffer = new ValueStringBuilder(stackalloc char[MAX_PATH]);
            int length = 0;
            while ((length = GetModuleFileName(hModule, ref buffer.GetPinnableReference(), buffer.Capacity)) == buffer.Capacity
                && Marshal.GetLastWin32Error() == Errors.ERROR_INSUFFICIENT_BUFFER)
            {
                if (buffer.Capacity > MODULE_FILENAME_LENGTH_LIMIT)
                {
                    return null;
                }

                buffer.EnsureCapacity(buffer.Capacity * 2);
            }

            // NOTE: if an error occurred, length will be 0, and we'll return an empty string
            buffer.Length = length;
            return buffer.ToString();
        }
    }
}

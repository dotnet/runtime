// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System
{
    internal static partial class Environment
    {
        internal static string SystemDirectory
        {
            get
            {
                // The path will likely be under 32 characters, e.g. C:\Windows\system32
                Span<char> buffer = stackalloc char[32];
                int requiredSize = Interop.Kernel32.GetSystemDirectoryW(buffer);

                if (requiredSize > buffer.Length)
                {
                    buffer = new char[requiredSize];
                    requiredSize = Interop.Kernel32.GetSystemDirectoryW(buffer);
                }

                if (requiredSize == 0)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                return new string(buffer.Slice(0, requiredSize));
            }
        }
    }
}

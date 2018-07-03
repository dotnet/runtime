// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        public static IntPtr GetMainWindowHandle(int processId) => IntPtr.Zero;
    }

    internal static partial class NtProcessManager
    {
        private static ProcessModuleCollection GetModules(int processId, bool firstModuleOnly)
        {
            // We don't have a good way of getting all of the modules of the particular process,
            // but we can at least get the path to the executable file for the process, and
            // other than for debugging tools, that's the main reason consumers of Modules care about it,
            // and why MainModule exists.
            char[] chars = null;
            int capacity = 64, length = 0;
            try
            {
                // Get the path to the executable
                using (Process process = Process.GetProcessById(processId))
                {
                    do
                    {
                        if(chars != null)
                        {
                            ArrayPool<char>.Shared.Return(chars);
                        }
                        capacity = Math.Min(capacity * 2, short.MaxValue);
                        chars = ArrayPool<char>.Shared.Rent(capacity);
                        length = Interop.Kernel32.GetModuleFileNameEx(process.SafeHandle, IntPtr.Zero, chars, chars.Length);
                        // GetModuleFileNameEx truncates the name if capacity isn't sufficient. If provided buffer is full and smaller
                        // than the maximum size of a Windows string (see UNICODE_STRING), retry with a bigger buffer.
                    } while (length == chars.Length - 1 && capacity <= short.MaxValue);

                    string exePath = new string(chars, 0, length);

                    if (!string.IsNullOrEmpty(exePath))
                    {
                        return new ProcessModuleCollection(1)
                        {
                            new ProcessModule()
                            {
                                FileName = exePath,
                                ModuleName = Path.GetFileName(exePath)
                            }
                        };
                    }
                }
            }
            catch { } // eat all errors
            finally
            {
                if (chars != null)
                {
                    ArrayPool<char>.Shared.Return(chars);
                }
            }

            return new ProcessModuleCollection(0);
        }
    }

    internal static class NtProcessInfoHelper
    {
        internal static ProcessInfo[] GetProcessInfos(Predicate<int> processIdFilter = null)
        {
            throw new PlatformNotSupportedException(SR.GetProcessInfoNotSupported); // NtDll.NtQuerySystemInformation is not available in Uap
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        internal static bool SupportsAtomicNonInheritablePipeCreation => true;

        private static bool IsExecutable(string fullPath)
        {
            return File.Exists(fullPath);
        }

        internal static int GetShowWindowFromWindowStyle(ProcessWindowStyle windowStyle) => windowStyle switch
        {
            ProcessWindowStyle.Hidden => Interop.Shell32.SW_HIDE,
            ProcessWindowStyle.Minimized => Interop.Shell32.SW_SHOWMINIMIZED,
            ProcessWindowStyle.Maximized => Interop.Shell32.SW_SHOWMAXIMIZED,
            _ => Interop.Shell32.SW_SHOWNORMAL,
        };

        internal static void BuildCommandLine(ProcessStartInfo startInfo, ref ValueStringBuilder commandLine)
        {
            // Construct a StringBuilder with the appropriate command line
            // to pass to CreateProcess.  If the filename isn't already
            // in quotes, we quote it here.  This prevents some security
            // problems (it specifies exactly which part of the string
            // is the file to execute).
            ReadOnlySpan<char> fileName = startInfo.FileName.AsSpan().Trim();
            bool fileNameIsQuoted = fileName.StartsWith('"') && fileName.EndsWith('"');
            if (!fileNameIsQuoted)
            {
                commandLine.Append('"');
            }

            commandLine.Append(fileName);

            if (!fileNameIsQuoted)
            {
                commandLine.Append('"');
            }

            startInfo.AppendArgumentsTo(ref commandLine);
        }

        /// <summary>Duplicates a handle as inheritable if it's valid and not inheritable.</summary>
        internal static void DuplicateAsInheritableIfNeeded(SafeFileHandle sourceHandle, ref SafeFileHandle? duplicatedHandle)
        {
            // The user can't specify invalid handle via ProcessStartInfo.Standard*Handle APIs.
            // However, Console.OpenStandard*Handle() can return INVALID_HANDLE_VALUE for a process
            // that was started with INVALID_HANDLE_VALUE as given standard handle.
            if (sourceHandle.IsInvalid)
            {
                return;
            }

            // When we know for sure that the handle is inheritable, we don't need to duplicate.
            // When GetHandleInformation fails, we still attempt to call DuplicateHandle,
            // just to keep throwing the same exception (backward compatibility).
            if (Interop.Kernel32.GetHandleInformation(sourceHandle, out Interop.Kernel32.HandleFlags flags)
                && (flags & Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT) != 0)
            {
                return;
            }

            IntPtr currentProcHandle = Interop.Kernel32.GetCurrentProcess();
            if (!Interop.Kernel32.DuplicateHandle(currentProcHandle,
                sourceHandle,
                currentProcHandle,
                out duplicatedHandle,
                0,
                bInheritHandle: true,
                Interop.Kernel32.HandleOptions.DUPLICATE_SAME_ACCESS))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        internal static string GetEnvironmentVariablesBlock(DictionaryWrapper sd)
        {
            // https://learn.microsoft.com/windows/win32/procthread/changing-environment-variables
            // "All strings in the environment block must be sorted alphabetically by name. The sort is
            //  case-insensitive, Unicode order, without regard to locale. Because the equal sign is a
            //  separator, it must not be used in the name of an environment variable."

            var keys = new string[sd.Count];
            sd.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.OrdinalIgnoreCase);

            // Join the null-terminated "key=val\0" strings
            var result = new StringBuilder(8 * keys.Length);
            foreach (string key in keys)
            {
                string? value = sd[key];

                // Ignore null values for consistency with Environment.SetEnvironmentVariable
                if (value != null)
                {
                    result.Append(key).Append('=').Append(value).Append('\0');
                }
            }

            return result.ToString();
        }
    }
}

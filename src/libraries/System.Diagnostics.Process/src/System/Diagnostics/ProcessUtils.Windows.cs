// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        internal static bool SupportsAtomicNonInheritablePipeCreation => true;

        internal static int GetShowWindowFromWindowStyle(ProcessWindowStyle windowStyle) => windowStyle switch
        {
            ProcessWindowStyle.Hidden => Interop.Shell32.SW_HIDE,
            ProcessWindowStyle.Minimized => Interop.Shell32.SW_SHOWMINIMIZED,
            ProcessWindowStyle.Maximized => Interop.Shell32.SW_SHOWMAXIMIZED,
            _ => Interop.Shell32.SW_SHOWNORMAL,
        };

        internal static int GetShellError(IntPtr error)
        {
            switch ((long)error)
            {
                case Interop.Shell32.SE_ERR_FNF:
                    return Interop.Errors.ERROR_FILE_NOT_FOUND;
                case Interop.Shell32.SE_ERR_PNF:
                    return Interop.Errors.ERROR_PATH_NOT_FOUND;
                case Interop.Shell32.SE_ERR_ACCESSDENIED:
                    return Interop.Errors.ERROR_ACCESS_DENIED;
                case Interop.Shell32.SE_ERR_OOM:
                    return Interop.Errors.ERROR_NOT_ENOUGH_MEMORY;
                case Interop.Shell32.SE_ERR_DDEFAIL:
                case Interop.Shell32.SE_ERR_DDEBUSY:
                case Interop.Shell32.SE_ERR_DDETIMEOUT:
                    return Interop.Errors.ERROR_DDE_FAIL;
                case Interop.Shell32.SE_ERR_SHARE:
                    return Interop.Errors.ERROR_SHARING_VIOLATION;
                case Interop.Shell32.SE_ERR_NOASSOC:
                    return Interop.Errors.ERROR_NO_ASSOCIATION;
                case Interop.Shell32.SE_ERR_DLLNOTFOUND:
                    return Interop.Errors.ERROR_DLL_NOT_FOUND;
                default:
                    return (int)(long)error;
            }
        }

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

        internal static string GetErrorMessage(int error) => Interop.Kernel32.GetMessage(error);
    }

    internal sealed unsafe class ShellExecuteHelper
    {
        private readonly Interop.Shell32.SHELLEXECUTEINFO* _executeInfo;
        private bool _succeeded;
        private bool _notpresent;

        public ShellExecuteHelper(Interop.Shell32.SHELLEXECUTEINFO* executeInfo)
        {
            _executeInfo = executeInfo;
        }

        private void ShellExecuteFunction()
        {
            try
            {
                if (!(_succeeded = Interop.Shell32.ShellExecuteExW(_executeInfo)))
                    ErrorCode = Marshal.GetLastWin32Error();
            }
            catch (EntryPointNotFoundException)
            {
                _notpresent = true;
            }
        }

        public bool ShellExecuteOnSTAThread()
        {
            // ShellExecute() requires STA in order to work correctly.

            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                ThreadStart threadStart = new ThreadStart(ShellExecuteFunction);
                Thread executionThread = new Thread(threadStart)
                {
                    IsBackground = true,
                    Name = ".NET Process STA"
                };
                executionThread.SetApartmentState(ApartmentState.STA);
                executionThread.Start();
                executionThread.Join();
            }
            else
            {
                ShellExecuteFunction();
            }

            if (_notpresent)
                throw new PlatformNotSupportedException(SR.UseShellExecuteNotSupported);

            return _succeeded;
        }

        public int ErrorCode { get; private set; }
    }
}

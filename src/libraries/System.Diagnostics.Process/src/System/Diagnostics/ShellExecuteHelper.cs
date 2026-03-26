// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
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

        public int ErrorCode { get; private set; }
    }
}

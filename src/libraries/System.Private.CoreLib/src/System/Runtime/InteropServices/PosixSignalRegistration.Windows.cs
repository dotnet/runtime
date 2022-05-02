// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Runtime.InteropServices
{
    public sealed partial class PosixSignalRegistration
    {
        private static readonly HashSet<Token> s_registrations = new();

        private static unsafe PosixSignalRegistration Register(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            switch (signal)
            {
                case PosixSignal.SIGINT:
                case PosixSignal.SIGQUIT:
                case PosixSignal.SIGTERM:
                case PosixSignal.SIGHUP:
                    break;

                default:
                    throw new PlatformNotSupportedException();
            }

            var token = new Token(signal, handler);
            var registration = new PosixSignalRegistration(token);

            lock (s_registrations)
            {
                if (s_registrations.Count == 0 &&
                    !Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: true))
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                s_registrations.Add(token);
            }

            return registration;
        }

        private unsafe void Unregister()
        {
            lock (s_registrations)
            {
                if (_token is Token token)
                {
                    _token = null;

                    s_registrations.Remove(token);
                    if (s_registrations.Count == 0 &&
                        !Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: false))
                    {
                        // Ignore errors due to the handler no longer being registered; this can happen, for example, with
                        // direct use of Alloc/Attach/FreeConsole which result in the table of control handlers being reset.
                        // Throw for everything else.
                        int error = Marshal.GetLastWin32Error();
                        if (error != Interop.Errors.ERROR_INVALID_PARAMETER)
                        {
                            throw Win32Marshal.GetExceptionForWin32Error(error);
                        }
                    }
                }
            }
        }

        [UnmanagedCallersOnly]
        private static Interop.BOOL HandlerRoutine(int dwCtrlType)
        {
            PosixSignal signal;
            switch (dwCtrlType)
            {
                case Interop.Kernel32.CTRL_C_EVENT:
                    signal = PosixSignal.SIGINT;
                    break;

                case Interop.Kernel32.CTRL_BREAK_EVENT:
                    signal = PosixSignal.SIGQUIT;
                    break;

                case Interop.Kernel32.CTRL_SHUTDOWN_EVENT:
                    signal = PosixSignal.SIGTERM;
                    break;

                case Interop.Kernel32.CTRL_CLOSE_EVENT:
                    signal = PosixSignal.SIGHUP;
                    break;

                default:
                    return Interop.BOOL.FALSE;
            }

            List<Token>? tokens = null;
            lock (s_registrations)
            {
                foreach (Token token in s_registrations)
                {
                    if (token.Signal == signal)
                    {
                        (tokens ??= new List<Token>()).Add(token);
                    }
                }
            }

            if (tokens is null)
            {
                return Interop.BOOL.FALSE;
            }

            var context = new PosixSignalContext(signal);
            foreach (Token handler in tokens)
            {
                handler.Handler(context);
            }

            return context.Cancel ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
    }
}

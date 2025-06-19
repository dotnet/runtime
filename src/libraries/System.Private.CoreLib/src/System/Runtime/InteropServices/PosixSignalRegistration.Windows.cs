// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Runtime.InteropServices
{
    public sealed partial class PosixSignalRegistration
    {
        private static readonly Dictionary<int, List<Token>> s_registrations = new();

        private static unsafe PosixSignalRegistration Register(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            int signo = signal switch
            {
                PosixSignal.SIGINT => Interop.Kernel32.CTRL_C_EVENT,
                PosixSignal.SIGQUIT => Interop.Kernel32.CTRL_BREAK_EVENT,
                PosixSignal.SIGTERM => Interop.Kernel32.CTRL_SHUTDOWN_EVENT,
                PosixSignal.SIGHUP => Interop.Kernel32.CTRL_CLOSE_EVENT,
                _ => throw new PlatformNotSupportedException()
            };

            var token = new Token(signal, signo, handler);
            var registration = new PosixSignalRegistration(token);

            lock (s_registrations)
            {
                if (s_registrations.Count == 0 &&
                    !Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: true))
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                if (!s_registrations.TryGetValue(signo, out List<Token>? tokens))
                {
                    s_registrations[signo] = tokens = new List<Token>();
                }

                tokens.Add(token);
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

                    if (s_registrations.TryGetValue(token.SigNo, out List<Token>? tokens))
                    {
                        tokens.Remove(token);
                        if (tokens.Count == 0)
                        {
                            s_registrations.Remove(token.SigNo);
                        }

                        if (s_registrations.Count == 0 &&
                            !Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: false))
                        {
                            // Ignore errors due to the handler no longer being registered; this can happen, for example, with
                            // direct use of Alloc/Attach/FreeConsole which result in the table of control handlers being reset.
                            // Throw for everything else.
                            int error = Marshal.GetLastPInvokeError();
                            if (error != Interop.Errors.ERROR_INVALID_PARAMETER)
                            {
                                throw Win32Marshal.GetExceptionForWin32Error(error);
                            }
                        }
                    }
                }
            }
        }

        [UnmanagedCallersOnly]
        private static Interop.BOOL HandlerRoutine(int dwCtrlType)
        {
            Token[]? tokens = null;

            lock (s_registrations)
            {
                if (s_registrations.TryGetValue(dwCtrlType, out List<Token>? registrations))
                {
                    tokens = new Token[registrations.Count];
                    registrations.CopyTo(tokens);
                }
            }

            if (tokens is null)
            {
                return Interop.BOOL.FALSE;
            }

            var context = new PosixSignalContext(0);

            // Iterate through the tokens in reverse order to match the order of registration.
            for (int i = tokens.Length - 1; i >= 0; i--)
            {
                Token token = tokens[i];
                context.Signal = token.Signal;
                token.Handler(context);
            }

            return context.Cancel ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }
    }
}

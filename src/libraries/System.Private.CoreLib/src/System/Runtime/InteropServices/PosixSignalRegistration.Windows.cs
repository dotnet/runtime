// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Runtime.InteropServices
{
    public sealed partial class PosixSignalRegistration
    {
        private static readonly Dictionary<int, List<Token>> s_registrations = new();
        private static bool s_isCtrlHandlerRegistering;

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

            bool registerCtrlHandler = false;
            lock (s_registrations)
            {
                if (s_registrations.Count == 0 && !s_isCtrlHandlerRegistering)
                {
                    registerCtrlHandler = true;
                    s_isCtrlHandlerRegistering = true;
                }
            }

            if (registerCtrlHandler)
            {
                // User may reset registrations externally by direct calls of Free/Attach/AllocConsole.
                // We do not know if it is currently registered or not. To prevent duplicate
                // registration, we try unregister existing one first.
                if (!Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: false))
                {
                    // Returns ERROR_INVALID_PARAMETER if it was not registered. Throw for everything else.
                    int error = Marshal.GetLastPInvokeError();
                    if (error != Interop.Errors.ERROR_INVALID_PARAMETER)
                    {
                        lock (s_registrations)
                        {
                            s_isCtrlHandlerRegistering = false;
                        }
                        throw Win32Marshal.GetExceptionForWin32Error(error);
                    }
                }

                if (!Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: true))
                {
                    lock (s_registrations)
                    {
                        s_isCtrlHandlerRegistering = false;
                    }
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            lock (s_registrations)
            {
                s_isCtrlHandlerRegistering = false;

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

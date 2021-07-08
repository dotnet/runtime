// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Runtime.InteropServices
{
    public sealed unsafe partial class PosixSignalRegistration
    {
        private static readonly HashSet<Token> s_handlers = new();

        private Token? _token;

        private PosixSignalRegistration(Token token) => _token = token;

        private static object SyncObj => s_handlers;

        public static partial PosixSignalRegistration Create(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (SyncObj)
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

                if (s_handlers.Count == 0 &&
                    !Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: true))
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                var token = new Token(signal, handler);
                s_handlers.Add(token);
                return new PosixSignalRegistration(token);
            }
        }

        public partial void Dispose()
        {
            lock (SyncObj)
            {
                if (_token is Token token)
                {
                    _token = null;

                    s_handlers.Remove(token);
                    if (s_handlers.Count == 0 &&
                        !Interop.Kernel32.SetConsoleCtrlHandler(&HandlerRoutine, Add: false))
                    {
                        throw Win32Marshal.GetExceptionForLastWin32Error();
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
            lock (SyncObj)
            {
                foreach (Token token in s_handlers)
                {
                    if (token.Signal == signal)
                    {
                        (tokens ??= new()).Add(token);
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

        private sealed class Token
        {
            public Token(PosixSignal signal, Action<PosixSignalContext> handler)
            {
                Signal = signal;
                Handler = handler;
            }

            public PosixSignal Signal { get; }
            public Action<PosixSignalContext> Handler { get; }
        }
    }
}

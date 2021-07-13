// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.Runtime.InteropServices
{
    public sealed partial class PosixSignalRegistration
    {
        private static readonly Dictionary<int, HashSet<Token>> s_registrations = Initialize();

        private static unsafe Dictionary<int, HashSet<Token>> Initialize()
        {
            if (!Interop.Sys.InitializeTerminalAndSignalHandling())
            {
                Interop.CheckIo(-1);
            }

            Interop.Sys.SetPosixSignalHandler(&OnPosixSignal);

            return new Dictionary<int, HashSet<Token>>();
        }

        private static PosixSignalRegistration Register(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            int signo = Interop.Sys.GetPlatformSignalNumber(signal);
            if (signo == 0)
            {
                throw new PlatformNotSupportedException();
            }

            var token = new Token(signal, signo, handler);
            var registration = new PosixSignalRegistration(token);

            lock (s_registrations)
            {
                if (!s_registrations.TryGetValue(signo, out HashSet<Token>? tokens))
                {
                    s_registrations[signo] = tokens = new HashSet<Token>();
                }

                if (tokens.Count == 0 &&
                    !Interop.Sys.EnablePosixSignalHandling(signo))
                {
                    // We can't use Win32Exception because that causes a cycle with
                    // Microsoft.Win32.Primitives.
                    Interop.CheckIo(-1);
                }

                tokens.Add(token);
            }

            return registration;
        }

        private void Unregister()
        {
            lock (s_registrations)
            {
                if (_token is Token token)
                {
                    _token = null;

                    if (s_registrations.TryGetValue(token.SigNo, out HashSet<Token>? tokens))
                    {
                        tokens.Remove(token);
                        if (tokens.Count == 0)
                        {
                            s_registrations.Remove(token.SigNo);
                            Interop.Sys.DisablePosixSignalHandling(token.SigNo);
                        }
                    }
                }
            }
        }

        [UnmanagedCallersOnly]
        private static int OnPosixSignal(int signo, PosixSignal signal)
        {
            if (GetTokens(signo) is Token[] tokens)
            {
                // This is called on the native signal handling thread. We need to move to another thread so
                // signal handling is not blocked. Otherwise we may get deadlocked when the handler depends
                // on work triggered from the signal handling thread.
                switch (signal)
                {
                    case PosixSignal.SIGINT:
                    case PosixSignal.SIGQUIT:
                    case PosixSignal.SIGTERM:
                        // For terminate/interrupt signals we use a dedicated Thread in case the ThreadPool is saturated.
                        new Thread(HandleSignal)
                        {
                            IsBackground = true,
                            Name = ".NET Signal Handler"
                        }.UnsafeStart((signo, tokens));
                        break;

                    default:
                        ThreadPool.UnsafeQueueUserWorkItem(HandleSignal, (signo, tokens));
                        break;
                }

                return 1;
            }

            return 0;

            static void HandleSignal(object? state)
            {
                (int signo, Token[]? tokens) = ((int, Token[]?))state!;
                do
                {
                    bool handlersCalled = false;
                    if (tokens != null)
                    {
                        PosixSignalContext ctx = new(0);
                        foreach (Token token in tokens)
                        {
                            // Different values for PosixSignal map to the same signo.
                            // Match the PosixSignal value used when registering.
                            ctx.Signal = token.Signal;
                            token.Handler(ctx);
                            handlersCalled = true;
                        }

                        if (ctx.Cancel)
                        {
                            return;
                        }
                    }

                    if (Interop.Sys.HandleNonCanceledPosixSignal(signo, handlersCalled ? 0 : 1))
                    {
                        return;
                    }

                    // HandleNonCanceledPosixSignal returns false when handlers got registered.
                    tokens = GetTokens(signo);
                }
                while (true);
            }

            static Token[]? GetTokens(int signo)
            {
                Token[]? results = null;

                lock (s_registrations)
                {
                    if (s_registrations.TryGetValue(signo, out HashSet<Token>? tokens))
                    {
                        results = new Token[tokens.Count];
                        tokens.CopyTo(results);
                    }
                }

                return results;
            }
        }
    }
}

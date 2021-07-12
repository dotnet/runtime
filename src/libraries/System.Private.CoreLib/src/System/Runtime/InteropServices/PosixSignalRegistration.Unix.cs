// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.Runtime.InteropServices
{
    public sealed partial class PosixSignalRegistration
    {
        private static volatile bool s_initialized;
        private static readonly Dictionary<int, List<WeakReference<PosixSignalRegistration>?>> s_registrations = new();

        private readonly Action<PosixSignalContext> _handler;
        private readonly PosixSignal _signal;
        private readonly int _signo;
        private bool _registered;
        private readonly object _gate = new object();

        private PosixSignalRegistration(PosixSignal signal, int signo, Action<PosixSignalContext> handler)
        {
            _signal = signal;
            _signo = signo;
            _handler = handler;
        }

        public static partial PosixSignalRegistration Create(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            int signo = Interop.Sys.GetPlatformSignalNumber(signal);
            if (signo == 0)
            {
                throw new PlatformNotSupportedException();
            }

            PosixSignalRegistration registration = new PosixSignalRegistration(signal, signo, handler);
            registration.Register();
            return registration;
        }

        private unsafe void Register()
        {
            if (!s_initialized)
            {
                if (!Interop.Sys.InitializeTerminalAndSignalHandling())
                {
                    // We can't use Win32Exception because that causes a cycle with
                    // Microsoft.Win32.Primitives.
                    Interop.CheckIo(-1);
                }

                Interop.Sys.SetPosixSignalHandler(&OnPosixSignal);
                s_initialized = true;
            }

            lock (s_registrations)
            {
                if (!s_registrations.TryGetValue(_signo, out List<WeakReference<PosixSignalRegistration>?>? signalRegistrations))
                {
                    signalRegistrations = new List<WeakReference<PosixSignalRegistration>?>();
                    s_registrations.Add(_signo, signalRegistrations);
                }

                if (signalRegistrations.Count == 0)
                {
                    if (!Interop.Sys.EnablePosixSignalHandling(_signo))
                    {
                        // We can't use Win32Exception because that causes a cycle with
                        // Microsoft.Win32.Primitives.
                        Interop.CheckIo(-1);
                    }
                }

                signalRegistrations.Add(new WeakReference<PosixSignalRegistration>(this));
            }

            _registered = true;
        }

        private bool CallHandler(PosixSignalContext context)
        {
            lock (_gate)
            {
                if (_registered)
                {
                    _handler(context);
                    return true;
                }

                return false;
            }
        }

        [UnmanagedCallersOnly]
        private static int OnPosixSignal(int signo, PosixSignal signal)
        {
            PosixSignalRegistration?[]? registrations = GetRegistrations(signo);
            if (registrations != null)
            {
                // This is called on the native signal handling thread. We need to move to another thread so
                // signal handling is not blocked. Otherwise we may get deadlocked when the handler depends
                // on work triggered from the signal handling thread.

                // For terminate/interrupt signals we use a dedicated Thread
                // in case the ThreadPool is saturated.
                bool useDedicatedThread = signal == PosixSignal.SIGINT ||
                                          signal == PosixSignal.SIGQUIT ||
                                          signal == PosixSignal.SIGTERM;

                if (useDedicatedThread)
                {
                    Thread handlerThread = new Thread(HandleSignal)
                    {
                        IsBackground = true,
                        Name = ".NET Signal Handler"
                    };
                    handlerThread.UnsafeStart((signo, registrations));
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItem(HandleSignal, (signo, registrations));
                }

                return 1;
            }

            return 0;
        }

        private static PosixSignalRegistration?[]? GetRegistrations(int signo)
        {
            lock (s_registrations)
            {
                if (s_registrations.TryGetValue(signo, out List<WeakReference<PosixSignalRegistration>?>? signalRegistrations))
                {
                    if (signalRegistrations.Count != 0)
                    {
                        var registrations = new PosixSignalRegistration?[signalRegistrations.Count];
                        bool hasRegistrations = false;
                        bool pruneWeakReferences = false;

                        for (int i = 0; i < signalRegistrations.Count; i++)
                        {
                            if (signalRegistrations[i]!.TryGetTarget(out PosixSignalRegistration? registration))
                            {
                                registrations[i] = registration;
                                hasRegistrations = true;
                            }
                            else
                            {
                                // WeakReference no longer holds an object. PosixSignalRegistration got finalized.
                                signalRegistrations[i] = null;
                                pruneWeakReferences = true;
                            }
                        }

                        if (pruneWeakReferences)
                        {
                            signalRegistrations.RemoveAll(item => item is null);
                        }

                        if (hasRegistrations)
                        {
                            return registrations;
                        }
                        else
                        {
                            Interop.Sys.DisablePosixSignalHandling(signo);
                        }
                    }
                }
                return null;
            }
        }

        private static void HandleSignal(object? state)
        {
            HandleSignal(((int, PosixSignalRegistration?[]))state!);
        }

        private static void HandleSignal((int signo, PosixSignalRegistration?[]? registrations) state)
        {
            do
            {
                bool handlersCalled = false;
                if (state.registrations != null)
                {
                    PosixSignalContext ctx = new(0);
                    foreach (PosixSignalRegistration? registration in state.registrations)
                    {
                        if (registration != null)
                        {
                            // Different values for PosixSignal map to the same signo.
                            // Match the PosixSignal value used when registering.
                            ctx.Signal = registration._signal;
                            if (registration.CallHandler(ctx))
                            {
                                handlersCalled = true;
                            }
                        }
                    }

                    if (ctx.Cancel)
                    {
                        return;
                    }
                }

                if (Interop.Sys.HandleNonCanceledPosixSignal(state.signo, handlersCalled ? 0 : 1))
                {
                    return;
                }

                // HandleNonCanceledPosixSignal returns false when handlers got registered.
                state.registrations = GetRegistrations(state.signo);
            } while (true);
        }

        public partial void Dispose()
        {
            if (_registered)
            {
                lock (s_registrations)
                {
                    List<WeakReference<PosixSignalRegistration>?> signalRegistrations = s_registrations[_signo];
                    bool pruneWeakReferences = false;
                    for (int i = 0; i < signalRegistrations.Count; i++)
                    {
                        if (signalRegistrations[i]!.TryGetTarget(out PosixSignalRegistration? registration))
                        {
                            if (ReferenceEquals(this, registration))
                            {
                                signalRegistrations.RemoveAt(i);
                                break;
                            }
                        }
                        else
                        {
                            // WeakReference no longer holds an object. PosixSignalRegistration got finalized.
                            signalRegistrations[i] = null;
                            pruneWeakReferences = true;
                        }
                    }

                    if (pruneWeakReferences)
                    {
                        signalRegistrations.RemoveAll(item => item is null);
                    }

                    if (signalRegistrations.Count == 0)
                    {
                        Interop.Sys.DisablePosixSignalHandling(_signo);
                    }
                }

                // Synchronize with _handler invocations.
                lock (_gate)
                {
                    _registered = false;
                }
            }
        }
    }
}

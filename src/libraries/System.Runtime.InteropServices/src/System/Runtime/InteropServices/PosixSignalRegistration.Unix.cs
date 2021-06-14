// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace System.Runtime.InteropServices
{
    public sealed class PosixSignalRegistration : IDisposable
    {
        private readonly Action<PosixSignalContext> _handler;
        private readonly PosixSignal _signal;
        private bool _registered;
        private readonly object _gate = new object();

        private PosixSignalRegistration(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            _signal = signal;
            _handler = handler;
        }

        public static PosixSignalRegistration Create(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (signal > PosixSignal.SIGHUP || signal < PosixSignal.SIGCHLD)
            {
                throw new IndexOutOfRangeException();
            }
            PosixSignalRegistration registration = new PosixSignalRegistration(signal, handler);
            registration.RegisterFor(signal);
            return registration;
        }

        private unsafe void RegisterFor(PosixSignal signal)
        {
            if (!s_initialized)
            {
                if (!Interop.Sys.InitializeTerminalAndSignalHandling())
                {
                    throw new Win32Exception();
                }
                s_initialized = true;
            }
            lock (s_registrations)
            {
                if (!s_registrations.TryGetValue(signal, out List<WeakReference<PosixSignalRegistration>>? signalRegistrations))
                {
                    if (!Interop.Sys.RegisterForPosixSignal(signal, &OnPosixSignal))
                    {
                        throw new Win32Exception();
                    }
                    signalRegistrations = new List<WeakReference<PosixSignalRegistration>>();
                    s_registrations.Add(signal, signalRegistrations);
                }
                signalRegistrations.Add(new WeakReference<PosixSignalRegistration>(this));
            }
            _registered = true;
        }

        private void Handle(PosixSignalContext context)
        {
            lock (_gate)
            {
                if (_registered)
                {
                    _handler(context);
                }
            }
        }

        [UnmanagedCallersOnly]
        private static int OnPosixSignal(PosixSignal signal)
        {
            lock (s_registrations)
            {
                if (s_registrations.TryGetValue(signal, out List<WeakReference<PosixSignalRegistration>>? signalRegistrations))
                {
                    if (signalRegistrations.Count != 0)
                    {
                        var registrations = new PosixSignalRegistration?[signalRegistrations.Count];
                        bool hasRegistrations = false;
                        for (int i = 0; i < signalRegistrations.Count; i++)
                        {
                            if (signalRegistrations[i].TryGetTarget(out PosixSignalRegistration? registration))
                            {
                                registrations[i] = registration;
                                hasRegistrations = true;
                            }
                        }
                        if (hasRegistrations)
                        {
                            // This is called on the native signal handling thread. We need to move to another thread so
                            // signal handling is not blocked. Otherwise we may get deadlocked when the handler depends
                            // on work triggered from the signal handling thread.
                            // We use a new thread rather than queueing to the ThreadPool in order to prioritize handling
                            // in case the ThreadPool is saturated.
                            Thread handlerThread = new Thread(HandleSignal)
                            {
                                IsBackground = true,
                                Name = ".NET Signal Handler"
                            };
                            handlerThread.Start((signal, registrations));
                            return 1;
                        }
                    }
                }
            }
            return 0;
        }

        private static void HandleSignal(object? state)
        {
            var (signal, registrations) = ((PosixSignal, PosixSignalRegistration?[]))state!;

            PosixSignalContext ctx = new(signal);
            foreach (var registration in registrations)
            {
                registration?.Handle(ctx);
            }

            if (!ctx.Cancel)
            {
                Interop.Sys.HandlePosixSignal(signal);
            }
        }

        ~PosixSignalRegistration()
            => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_registered)
            {
                lock (s_registrations)
                {
                    List<WeakReference<PosixSignalRegistration>> signalRegistrations = s_registrations[_signal];
                    for (int i = 0; i < signalRegistrations.Count; i++)
                    {
                        if (signalRegistrations[i].TryGetTarget(out PosixSignalRegistration? registration) &&
                            object.ReferenceEquals(this, registration))
                        {
                            signalRegistrations.RemoveAt(i);
                            break;
                        }
                    }
                    if (signalRegistrations.Count == 0)
                    {
                        Interop.Sys.UnregisterForPosixSignal(_signal);
                    }
                }

                lock (_gate)
                {
                    _registered = false;
                }
            }
        }

        private static volatile bool s_initialized;
        private static Dictionary<PosixSignal, List<WeakReference<PosixSignalRegistration>>> s_registrations = new();
    }
}

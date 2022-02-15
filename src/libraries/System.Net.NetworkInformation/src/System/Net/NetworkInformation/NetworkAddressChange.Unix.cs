// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.NetworkInformation
{
    // Linux implementation of NetworkChange
    public partial class NetworkChange
    {
        private static volatile SocketWrapper? s_socket;
        // Lock controlling access to delegate subscriptions, socket initialization, availability-changed state and timer.
        private static readonly object s_gate = new object();

        // The "leniency" window for NetworkAvailabilityChanged socket events.
        // All socket events received within this duration will be coalesced into a
        // single event. Generally, many route changed events are fired in succession,
        // and we are not interested in all of them, just the fact that network availability
        // has potentially changed as a result.
        private const int AvailabilityTimerWindowMilliseconds = 150;
        private static readonly TimerCallback s_availabilityTimerFiredCallback = OnAvailabilityTimerFired;
        private static Timer? s_availabilityTimer;
        private static bool s_availabilityHasChanged;

        public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
        {
            add
            {
                if (value != null)
                {
                    lock (s_gate)
                    {
                        if (s_socket == null)
                        {
                            CreateSocket();
                        }

                        s_addressChangedSubscribers.TryAdd(value, ExecutionContext.Capture());
                    }
                }
            }
            remove
            {
                if (value != null)
                {
                    lock (s_gate)
                    {
                        if (s_addressChangedSubscribers.Count == 0 && s_availabilityChangedSubscribers.Count == 0)
                        {
                            Debug.Assert(s_socket == null,
                                "s_socket is not null, but there are no subscribers to NetworkAddressChanged or NetworkAvailabilityChanged.");
                            return;
                        }

                        s_addressChangedSubscribers.Remove(value);
                        if (s_addressChangedSubscribers.Count == 0 && s_availabilityChangedSubscribers.Count == 0)
                        {
                            CloseSocket();
                        }
                    }
                }
            }
        }

        public static event NetworkAvailabilityChangedEventHandler? NetworkAvailabilityChanged
        {
            add
            {
                if (value != null)
                {
                    lock (s_gate)
                    {
                        if (s_socket == null)
                        {
                            CreateSocket();
                        }

                        if (s_availabilityTimer == null)
                        {
                            // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
                            bool restoreFlow = false;
                            try
                            {
                                if (!ExecutionContext.IsFlowSuppressed())
                                {
                                    ExecutionContext.SuppressFlow();
                                    restoreFlow = true;
                                }

                                s_availabilityTimer = new Timer(s_availabilityTimerFiredCallback, null, Timeout.Infinite, Timeout.Infinite);
                            }
                            finally
                            {
                                // Restore the current ExecutionContext
                                if (restoreFlow)
                                    ExecutionContext.RestoreFlow();
                            }
                        }

                        s_availabilityChangedSubscribers.TryAdd(value, ExecutionContext.Capture());
                    }
                }
            }
            remove
            {
                if (value != null)
                {
                    lock (s_gate)
                    {
                        if (s_addressChangedSubscribers.Count == 0 && s_availabilityChangedSubscribers.Count == 0)
                        {
                            Debug.Assert(s_socket == null,
                                "s_socket is not null, but there are no subscribers to NetworkAddressChanged or NetworkAvailabilityChanged.");
                            return;
                        }

                        s_availabilityChangedSubscribers.Remove(value);
                        if (s_availabilityChangedSubscribers.Count == 0)
                        {
                            if (s_availabilityTimer != null)
                            {
                                s_availabilityTimer.Dispose();
                                s_availabilityTimer = null;
                                s_availabilityHasChanged = false;
                            }

                            if (s_addressChangedSubscribers.Count == 0)
                            {
                                CloseSocket();
                            }
                        }
                    }
                }
            }
        }

        private static unsafe void CreateSocket()
        {
            Debug.Assert(s_socket == null, "s_socket is not null, must close existing socket before opening another.");
            int newSocket;
            Interop.Error result = Interop.Sys.CreateNetworkChangeListenerSocket(&newSocket);
            if (result != Interop.Error.SUCCESS)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            s_socket = new SocketWrapper(newSocket);

            new Thread(args => LoopReadSocket((SocketWrapper)args!))
            {
                IsBackground = true,
                Name = ".NET Network Address Change"
            }.UnsafeStart(s_socket);
        }

        private static void CloseSocket()
        {
            Debug.Assert(s_socket != null, "s_socket was null when CloseSocket was called.");
            Interop.Error result = Interop.Sys.CloseNetworkChangeListenerSocket(s_socket != null ? s_socket.Socket : -1);
            if (result != Interop.Error.SUCCESS)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            s_socket = null;
        }

        private static unsafe void LoopReadSocket(SocketWrapper initiallyCreatedSocket)
        {
            while (s_socket == initiallyCreatedSocket) //if both references are equal then s_socket was not changed
            {
                //we can continue processing events
                Interop.Sys.ReadEvents(initiallyCreatedSocket.Socket, &ProcessEvent);
            }
        }

        [UnmanagedCallersOnly]
        private static void ProcessEvent(int socket, Interop.Sys.NetworkChangeKind kind)
        {
            if (kind != Interop.Sys.NetworkChangeKind.None)
            {
                lock (s_gate)
                {
                    if (s_socket != null && socket == s_socket.Socket)
                    {
                        OnSocketEvent(kind);
                    }
                }
            }
        }

        private static void OnSocketEvent(Interop.Sys.NetworkChangeKind kind)
        {
            switch (kind)
            {
                case Interop.Sys.NetworkChangeKind.AddressAdded:
                case Interop.Sys.NetworkChangeKind.AddressRemoved:
                    OnAddressChanged();
                    break;
                case Interop.Sys.NetworkChangeKind.AvailabilityChanged:
                    lock (s_gate)
                    {
                        if (s_availabilityTimer != null)
                        {
                            if (!s_availabilityHasChanged)
                            {
                                s_availabilityTimer.Change(AvailabilityTimerWindowMilliseconds, -1);
                            }
                            s_availabilityHasChanged = true;
                        }
                    }
                    break;
            }
        }

        private static void OnAddressChanged()
        {
            Dictionary<NetworkAddressChangedEventHandler, ExecutionContext?>? addressChangedSubscribers = null;

            lock (s_gate)
            {
                if (s_addressChangedSubscribers.Count > 0)
                {
                    addressChangedSubscribers = new Dictionary<NetworkAddressChangedEventHandler, ExecutionContext?>(s_addressChangedSubscribers);
                }
            }

            if (addressChangedSubscribers != null)
            {
                foreach (KeyValuePair<NetworkAddressChangedEventHandler, ExecutionContext?>
                    subscriber in addressChangedSubscribers)
                {
                    NetworkAddressChangedEventHandler handler = subscriber.Key;
                    ExecutionContext? ec = subscriber.Value;

                    if (ec == null) // Flow supressed
                    {
                        handler(null, EventArgs.Empty);
                    }
                    else
                    {
                        ExecutionContext.Run(ec, s_runAddressChangedHandler, handler);
                    }
                }
            }
        }

        private static void OnAvailabilityTimerFired(object? state)
        {
            Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext?>? availabilityChangedSubscribers = null;

            lock (s_gate)
            {
                if (s_availabilityHasChanged)
                {
                    s_availabilityHasChanged = false;
                    if (s_availabilityChangedSubscribers.Count > 0)
                    {
                        availabilityChangedSubscribers =
                            new Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext?>(
                                s_availabilityChangedSubscribers);
                    }
                }
            }

            if (availabilityChangedSubscribers != null)
            {
                bool isAvailable = NetworkInterface.GetIsNetworkAvailable();
                NetworkAvailabilityEventArgs args = isAvailable ? s_availableEventArgs : s_notAvailableEventArgs;
                ContextCallback callbackContext = isAvailable ? s_runHandlerAvailable : s_runHandlerNotAvailable;

                foreach (KeyValuePair<NetworkAvailabilityChangedEventHandler, ExecutionContext?>
                    subscriber in availabilityChangedSubscribers)
                {
                    NetworkAvailabilityChangedEventHandler handler = subscriber.Key;
                    ExecutionContext? ec = subscriber.Value;

                    if (ec == null) // Flow supressed
                    {
                        handler(null, args);
                    }
                    else
                    {
                        ExecutionContext.Run(ec, callbackContext, handler);
                    }
                }
            }
        }

        // SocketWrapper class was introduced to prevent event thread leaks on Linux
        // as described on GitHub: https://github.com/dotnet/runtime/issues/63788
        // For each CreateSocket() new s_socket instance is created and we pass it to ".NET Network Address Change" thread.
        // We then continue loop in LoopReadSocket method as long as both references are equal.
        private class SocketWrapper
        {
            public SocketWrapper(int socket)
            {
                Socket = socket;
            }

            public int Socket { get; }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    // Linux implementation of NetworkChange
    public partial class NetworkChange
    {
        private static Socket? s_socket;

        private static Socket? Socket
        {
            get
            {
                Debug.Assert(Monitor.IsEntered(s_gate));
                return s_socket;
            }
            set
            {
                Debug.Assert(Monitor.IsEntered(s_gate));
                s_socket = value;
            }
        }

        // Lock controlling access to delegate subscriptions, socket, availability-changed state and timer.
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

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
        {
            add
            {
                if (value != null)
                {
                    lock (s_gate)
                    {
                        if (Socket == null)
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
                            Debug.Assert(Socket == null,
                                "Socket is not null, but there are no subscribers to NetworkAddressChanged or NetworkAvailabilityChanged.");
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

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAvailabilityChangedEventHandler? NetworkAvailabilityChanged
        {
            add
            {
                if (value != null)
                {
                    lock (s_gate)
                    {
                        if (Socket == null)
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
                            Debug.Assert(Socket == null,
                                "Socket is not null, but there are no subscribers to NetworkAddressChanged or NetworkAvailabilityChanged.");
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
            Debug.Assert(Monitor.IsEntered(s_gate));
            Debug.Assert(Socket == null, "Socket is not null, must close existing socket before opening another.");
            IntPtr newSocket;
            Interop.Error result = Interop.Sys.CreateNetworkChangeListenerSocket(&newSocket);
            if (result != Interop.Error.SUCCESS)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            Socket = new Socket(new SafeSocketHandle(newSocket, ownsHandle: true));

            // Don't capture ExecutionContext.
            ThreadPool.UnsafeQueueUserWorkItem(
                static socket => ReadEventsAsync(socket),
                Socket, preferLocal: false);
        }

        private static void CloseSocket()
        {
            Debug.Assert(Monitor.IsEntered(s_gate));
            Debug.Assert(Socket != null, "Socket was null when CloseSocket was called.");
            Socket.Dispose();
            Socket = null;
        }

        private static async void ReadEventsAsync(Socket socket)
        {
            try
            {
                while (true)
                {
                    // Wait for data to become available.
                    await socket.ReceiveAsync(Array.Empty<byte>(), SocketFlags.None).ConfigureAwait(false);

                    Interop.Error result = ReadEvents(socket);

                    if (result != Interop.Error.SUCCESS &&
                        result != Interop.Error.EAGAIN)
                    {
                        throw new Win32Exception(result.Info().RawErrno);
                    }
                }
            }
            catch (ObjectDisposedException)
            { } // Socket disposed.
            catch (SocketException se) when (se.SocketErrorCode == SocketError.OperationAborted)
            { } // ReceiveAsync aborted by disposing Socket.
            catch (Exception ex)
            {
                // Unexpected error.
                Debug.Fail($"Unexpected error: {ex}");
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, ex);
            }

            static unsafe Interop.Error ReadEvents(Socket socket)
                => Interop.Sys.ReadEvents(socket.SafeHandle, &ProcessEvent);
        }

        [UnmanagedCallersOnly]
        private static void ProcessEvent(IntPtr socket, Interop.Sys.NetworkChangeKind kind)
        {
            if (kind != Interop.Sys.NetworkChangeKind.None)
            {
                lock (s_gate)
                {
                    // It's safe to compare raw handle values because ProcessEvents gets
                    // called from ReadEvents which holds a reference on the SafeHandle.
                    if (Socket != null && socket == Socket.Handle)
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
    }
}

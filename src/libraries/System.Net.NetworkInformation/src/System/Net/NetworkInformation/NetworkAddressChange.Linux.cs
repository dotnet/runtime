// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.NetworkInformation
{
    // Linux implementation of NetworkChange
    public class NetworkChange
    {
        private static NetworkAddressChangedEventHandler s_addressChangedSubscribers;
        private static NetworkAvailabilityChangedEventHandler s_availabilityChangedSubscribers;
        private static volatile int s_socket = 0;
        // Lock controlling access to delegate subscriptions, socket initialization, availability-changed state and timer.
        private static readonly object s_gate = new object();
        private static readonly Interop.Sys.NetworkChangeEvent s_networkChangeCallback = ProcessEvent;

        // The "leniency" window for NetworkAvailabilityChanged socket events.
        // All socket events received within this duration will be coalesced into a
        // single event. Generally, many route changed events are fired in succession,
        // and we are not interested in all of them, just the fact that network availability
        // has potentially changed as a result.
        private const int AvailabilityTimerWindowMilliseconds = 150;
        private static readonly TimerCallback s_availabilityTimerFiredCallback = OnAvailabilityTimerFired;
        private static Timer s_availabilityTimer;
        private static bool s_availabilityHasChanged;

        // Introduced for supporting design-time loading of System.Windows.dll
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterNetworkChange(NetworkChange nc) { }

        public static event NetworkAddressChangedEventHandler NetworkAddressChanged
        {
            add
            {
                lock (s_gate)
                {
                    if (s_socket == 0)
                    {
                        CreateSocket();
                    }

                    s_addressChangedSubscribers += value;
                }
            }
            remove
            {
                lock (s_gate)
                {
                    if (s_addressChangedSubscribers == null && s_availabilityChangedSubscribers == null)
                    {
                        Debug.Assert(s_socket == 0, "s_socket != 0, but there are no subscribers to NetworkAddressChanged or NetworkAvailabilityChanged.");
                        return;
                    }

                    s_addressChangedSubscribers -= value;
                    if (s_addressChangedSubscribers == null && s_availabilityChangedSubscribers == null)
                    {
                        CloseSocket();
                    }
                }
            }
        }

        public static event NetworkAvailabilityChangedEventHandler NetworkAvailabilityChanged
        {
            add
            {
                lock (s_gate)
                {
                    if (s_socket == 0)
                    {
                        CreateSocket();
                    }
                    if (s_availabilityTimer == null)
                    {
                        s_availabilityTimer = new Timer(s_availabilityTimerFiredCallback, null, -1, -1);
                    }

                    s_availabilityChangedSubscribers += value;
                }
            }
            remove
            {
                lock (s_gate)
                {
                    if (s_addressChangedSubscribers == null && s_availabilityChangedSubscribers == null)
                    {
                        Debug.Assert(s_socket == 0, "s_socket != 0, but there are no subscribers to NetworkAddressChanged or NetworkAvailabilityChanged.");
                        return;
                    }

                    s_availabilityChangedSubscribers -= value;
                    if (s_availabilityChangedSubscribers == null)
                    {
                        if (s_availabilityTimer != null)
                        {
                            s_availabilityTimer.Dispose();
                            s_availabilityTimer = null;
                            s_availabilityHasChanged = false;
                        }

                        if (s_addressChangedSubscribers == null)
                        {
                            CloseSocket();
                        }
                    }
                }
            }
        }


        private static void CreateSocket()
        {
            Debug.Assert(s_socket == 0, "s_socket != 0, must close existing socket before opening another.");
            int newSocket;
            Interop.Error result = Interop.Sys.CreateNetworkChangeListenerSocket(out newSocket);
            if (result != Interop.Error.SUCCESS)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            s_socket = newSocket;
            Task.Factory.StartNew(s => LoopReadSocket((int)s), s_socket,
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static void CloseSocket()
        {
            Debug.Assert(s_socket != 0, "s_socket was 0 when CloseSocket was called.");
            Interop.Error result = Interop.Sys.CloseNetworkChangeListenerSocket(s_socket);
            if (result != Interop.Error.SUCCESS)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            s_socket = 0;
        }

        private static void LoopReadSocket(int socket)
        {
            while (socket == s_socket)
            {
                Interop.Sys.ReadEvents(socket, s_networkChangeCallback);
            }
        }
        
        private static void ProcessEvent(int socket, Interop.Sys.NetworkChangeKind kind)
        {
            if (kind != Interop.Sys.NetworkChangeKind.None)
            {
                lock (s_gate)
                {
                    if (socket == s_socket)
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
                    s_addressChangedSubscribers?.Invoke(null, EventArgs.Empty);
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

        private static void OnAvailabilityTimerFired(object state)
        {
            bool changed;
            lock (s_gate)
            {
                changed = s_availabilityHasChanged;
                s_availabilityHasChanged = false;
            }

            if (changed)
            {
                s_availabilityChangedSubscribers?.Invoke(null, new NetworkAvailabilityEventArgs(NetworkInterface.GetIsNetworkAvailable()));
            }
        }
    }
}

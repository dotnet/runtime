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
        private static volatile int s_socket = 0;
        private static readonly object s_lockObj = new object();
        private static readonly Interop.Sys.NetworkChangeEvent s_networkChangeCallback = ProcessEvent;

        public static event NetworkAddressChangedEventHandler NetworkAddressChanged
        {
            add
            {
                lock (s_lockObj)
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
                lock (s_lockObj)
                {
                    if (s_addressChangedSubscribers == null)
                    {
                        Debug.Assert(s_socket == 0, "s_socket != 0, but there are no subscribers to NetworkAddressChanged.");
                        return;
                    }

                    s_addressChangedSubscribers -= value;
                    if (s_addressChangedSubscribers == null)
                    {
                        CloseSocket();
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
                lock (s_lockObj)
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
                    NetworkAddressChangedEventHandler handler = s_addressChangedSubscribers;
                    if (handler != null)
                    {
                        handler(null, EventArgs.Empty);
                    }
                    break;
            }
        }
    }
}

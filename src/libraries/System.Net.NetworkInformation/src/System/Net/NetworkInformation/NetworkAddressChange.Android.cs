// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public partial class NetworkChange
    {
        private static readonly TimeSpan s_timerInterval = TimeSpan.FromSeconds(2);
        private static readonly object s_lockObj = new();

        private static Task? s_loopTask;
        private static CancellationTokenSource? s_cancellationTokenSource;
        private static IPAddress[]? s_lastIpAddresses;

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static event NetworkAddressChangedEventHandler? NetworkAddressChanged
        {
            add
            {
                if (value != null)
                {
                    lock (s_lockObj)
                    {
                        if (s_addressChangedSubscribers.Count == 0 &&
                            s_availabilityChangedSubscribers.Count == 0)
                        {
                            CreateAndStartLoop();
                        }
                        else
                        {
                            Debug.Assert(s_loopTask is not null);
                        }

                        s_addressChangedSubscribers.TryAdd(value, ExecutionContext.Capture());
                    }
                }
            }
            remove
            {
                if (value != null)
                {
                    lock (s_lockObj)
                    {
                        bool hadAddressChangedSubscribers = s_addressChangedSubscribers.Count != 0;
                        s_addressChangedSubscribers.Remove(value);

                        if (hadAddressChangedSubscribers && s_addressChangedSubscribers.Count == 0 &&
                            s_availabilityChangedSubscribers.Count == 0)
                        {
                            StopLoop();
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
                    lock (s_lockObj)
                    {
                        if (s_addressChangedSubscribers.Count == 0 &&
                            s_availabilityChangedSubscribers.Count == 0)
                        {
                            CreateAndStartLoop();
                        }
                        else
                        {
                            Debug.Assert(s_loopTask is not null);
                        }

                        s_availabilityChangedSubscribers.TryAdd(value, ExecutionContext.Capture());
                    }
                }
            }
            remove
            {
                if (value != null)
                {
                    lock (s_lockObj)
                    {
                        bool hadSubscribers = s_addressChangedSubscribers.Count != 0 ||
                                              s_availabilityChangedSubscribers.Count != 0;
                        s_availabilityChangedSubscribers.Remove(value);

                        if (hadSubscribers && s_addressChangedSubscribers.Count == 0 &&
                            s_availabilityChangedSubscribers.Count == 0)
                        {
                            StopLoop();
                        }
                    }
                }
            }
        }

        private static void CreateAndStartLoop()
        {
            Debug.Assert(s_cancellationTokenSource is null);
            Debug.Assert(s_loopTask is null);

            s_cancellationTokenSource = new CancellationTokenSource();
            s_loopTask = PeriodicallyCheckIfNetworkChanged(s_cancellationTokenSource.Token);
        }

        private static void StopLoop()
        {
            Debug.Assert(s_cancellationTokenSource is not null);
            Debug.Assert(s_loopTask is not null);

            s_cancellationTokenSource.Cancel();

            s_loopTask = null;
            s_cancellationTokenSource = null;
            s_lastIpAddresses = null;
        }

        private static async Task PeriodicallyCheckIfNetworkChanged(CancellationToken cancellationToken)
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            var timer = new PeriodicTimer(s_timerInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false) &&
                       !cancellationToken.IsCancellationRequested)
                {
                    CheckIfNetworkChanged();
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                timer.Dispose();
            }
        }

        private static void CheckIfNetworkChanged()
        {
            var newAddresses = GetIPAddresses();
            if (s_lastIpAddresses is IPAddress[] oldAddresses && NetworkChanged(oldAddresses, newAddresses))
            {
                OnNetworkChanged();
            }

            s_lastIpAddresses = newAddresses;
        }

        private static IPAddress[] GetIPAddresses()
        {
            var addresses = new List<IPAddress>();

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                var properties = networkInterface.GetIPProperties();
                foreach (var addressInformation in properties.UnicastAddresses)
                {
                    addresses.Add(addressInformation.Address);
                }
            }

            return addresses.ToArray();
        }

        private static bool NetworkChanged(IPAddress[] oldAddresses, IPAddress[] newAddresses)
        {
            if (oldAddresses.Length != newAddresses.Length)
            {
                return true;
            }

            for (int i = 0; i < newAddresses.Length; i++)
            {
                if (Array.IndexOf(oldAddresses, newAddresses[i]) == -1)
                {
                    return true;
                }
            }

            return false;
        }

        private static void OnNetworkChanged()
        {
            Dictionary<NetworkAddressChangedEventHandler, ExecutionContext?>? addressChangedSubscribers = null;
            Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext?>? availabilityChangedSubscribers = null;

            lock (s_lockObj)
            {
                if (s_addressChangedSubscribers.Count > 0)
                {
                    addressChangedSubscribers = new Dictionary<NetworkAddressChangedEventHandler, ExecutionContext?>(s_addressChangedSubscribers);
                }
                if (s_availabilityChangedSubscribers.Count > 0)
                {
                    availabilityChangedSubscribers = new Dictionary<NetworkAvailabilityChangedEventHandler, ExecutionContext?>(s_availabilityChangedSubscribers);
                }
            }

            if (addressChangedSubscribers != null)
            {
                foreach (KeyValuePair<NetworkAddressChangedEventHandler, ExecutionContext?>
                    subscriber in addressChangedSubscribers)
                {
                    NetworkAddressChangedEventHandler handler = subscriber.Key;
                    ExecutionContext? ec = subscriber.Value;

                    if (ec == null) // Flow suppressed
                    {
                        handler(null, EventArgs.Empty);
                    }
                    else
                    {
                        ExecutionContext.Run(ec, s_runAddressChangedHandler, handler);
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

                    if (ec == null) // Flow suppressed
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

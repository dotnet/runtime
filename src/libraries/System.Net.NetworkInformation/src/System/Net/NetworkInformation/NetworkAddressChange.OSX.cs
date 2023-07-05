// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Threading;

using CFStringRef = System.IntPtr;
using CFRunLoopRef = System.IntPtr;

namespace System.Net.NetworkInformation
{
    // OSX implementation of NetworkChange
    // See <SystemConfiguration/SystemConfiguration.h> and its documentation, as well as
    // the documentation for CFRunLoop for more information on the components involved.
    public partial class NetworkChange
    {
        private static object s_lockObj = new object();

        // The dynamic store. We listen to changes in the IPv4 and IPv6 address keys.
        // When those keys change, our callback below is called (OnAddressChanged).
        private static SafeCreateHandle? s_dynamicStoreRef;

        // The RunLoop source, created over the above SCDynamicStore.
        private static SafeCreateHandle? s_runLoopSource;

        // Listener thread that adds the RunLoopSource to its RunLoop.
        private static Thread? s_runLoopThread;

        // The listener thread's CFRunLoop.
        private static CFRunLoopRef s_runLoop;

        // Use an event to try to prevent StartRaisingEvents from returning before the
        // RunLoop actually begins. This will mitigate a race condition where the watcher
        // thread hasn't completed initialization and stop is called before the RunLoop has even started.
        private static readonly AutoResetEvent s_runLoopStartedEvent = new AutoResetEvent(false);
        private static readonly AutoResetEvent s_runLoopEndedEvent = new AutoResetEvent(false);

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
                            CreateAndStartRunLoop();
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
                            StopRunLoop();
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
                            CreateAndStartRunLoop();
                        }
                        else
                        {
                            Debug.Assert(s_runLoop != IntPtr.Zero);
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
                            StopRunLoop();
                        }
                    }
                }
            }
        }

        private static unsafe void CreateAndStartRunLoop()
        {
            Debug.Assert(s_dynamicStoreRef == null);

            Interop.SystemConfiguration.SCDynamicStoreContext storeContext = default;
            using (SafeCreateHandle storeName = Interop.CoreFoundation.CFStringCreateWithCString("NetworkAddressChange.OSX"))
            {
                s_dynamicStoreRef = Interop.SystemConfiguration.SCDynamicStoreCreate(
                    storeName.DangerousGetHandle(),
                    &OnAddressChanged,
                    &storeContext);
            }

            // Notification key string parts. We want to match notification keys
            // for any kind of IP address change, addition, or removal.
            using (SafeCreateHandle dynamicStoreDomainStateString = Interop.CoreFoundation.CFStringCreateWithCString("State:"))
            using (SafeCreateHandle compAnyRegexString = Interop.CoreFoundation.CFStringCreateWithCString("[^/]+"))
            using (SafeCreateHandle entNetIpv4String = Interop.CoreFoundation.CFStringCreateWithCString("IPv4"))
            using (SafeCreateHandle entNetIpv6String = Interop.CoreFoundation.CFStringCreateWithCString("IPv6"))
            {
                if (dynamicStoreDomainStateString.IsInvalid || compAnyRegexString.IsInvalid
                    || entNetIpv4String.IsInvalid || entNetIpv6String.IsInvalid)
                {
                    s_dynamicStoreRef.Dispose();
                    s_dynamicStoreRef = null;
                    throw new NetworkInformationException(SR.net_PInvokeError);
                }

                using (SafeCreateHandle ipv4Pattern = Interop.SystemConfiguration.SCDynamicStoreKeyCreateNetworkServiceEntity(
                        dynamicStoreDomainStateString.DangerousGetHandle(),
                        compAnyRegexString.DangerousGetHandle(),
                        entNetIpv4String.DangerousGetHandle()))
                using (SafeCreateHandle ipv6Pattern = Interop.SystemConfiguration.SCDynamicStoreKeyCreateNetworkServiceEntity(
                        dynamicStoreDomainStateString.DangerousGetHandle(),
                        compAnyRegexString.DangerousGetHandle(),
                        entNetIpv6String.DangerousGetHandle()))
                using (SafeCreateHandle patterns = Interop.CoreFoundation.CFArrayCreate(
                        new CFStringRef[2]
                        {
                            ipv4Pattern.DangerousGetHandle(),
                            ipv6Pattern.DangerousGetHandle()
                        }, (UIntPtr)2))
                {
                    // Try to register our pattern strings with the dynamic store instance.
                    if (patterns.IsInvalid || !Interop.SystemConfiguration.SCDynamicStoreSetNotificationKeys(
                                                s_dynamicStoreRef.DangerousGetHandle(),
                                                IntPtr.Zero,
                                                patterns.DangerousGetHandle()))
                    {
                        s_dynamicStoreRef.Dispose();
                        s_dynamicStoreRef = null;
                        throw new NetworkInformationException(SR.net_PInvokeError);
                    }

                    // Create a "RunLoopSource" that can be added to our listener thread's RunLoop.
                    s_runLoopSource = Interop.SystemConfiguration.SCDynamicStoreCreateRunLoopSource(
                        s_dynamicStoreRef.DangerousGetHandle(),
                        IntPtr.Zero);
                }
            }
            s_runLoopThread = new Thread(RunLoopThreadStart)
            {
                 IsBackground = true,
                 Name = ".NET Network Address Change"
            };
            s_runLoopThread.Start();
            s_runLoopStartedEvent.WaitOne(); // Wait for the new thread to finish initialization.
        }

        private static void RunLoopThreadStart()
        {
            Debug.Assert(s_runLoop == IntPtr.Zero);

            s_runLoop = Interop.RunLoop.CFRunLoopGetCurrent();
            Interop.RunLoop.CFRunLoopAddSource(
                s_runLoop,
                s_runLoopSource!.DangerousGetHandle(),
                Interop.RunLoop.kCFRunLoopDefaultMode.DangerousGetHandle());

            s_runLoopStartedEvent.Set();
            Interop.RunLoop.CFRunLoopRun();

            Interop.RunLoop.CFRunLoopRemoveSource(
                s_runLoop,
                s_runLoopSource.DangerousGetHandle(),
                Interop.RunLoop.kCFRunLoopDefaultMode.DangerousGetHandle());

            s_runLoop = IntPtr.Zero;

            s_runLoopSource.Dispose();
            s_runLoopSource = null;

            s_dynamicStoreRef!.Dispose();
            s_dynamicStoreRef = null;

            s_runLoopEndedEvent.Set();
        }

        private static void StopRunLoop()
        {
            Debug.Assert(s_runLoop != IntPtr.Zero);
            Debug.Assert(s_runLoopSource != null);
            Debug.Assert(s_dynamicStoreRef != null);

            // Allow RunLoop to finish current processing.
            SpinWait.SpinUntil(() => Interop.RunLoop.CFRunLoopIsWaiting(s_runLoop));

            Interop.RunLoop.CFRunLoopStop(s_runLoop);
            s_runLoopEndedEvent.WaitOne();
        }

        [UnmanagedCallersOnly]
        private static void OnAddressChanged(IntPtr store, IntPtr changedKeys, IntPtr info)
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

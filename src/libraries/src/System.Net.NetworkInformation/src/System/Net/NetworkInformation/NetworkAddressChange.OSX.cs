// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class NetworkChange
    {
        private static object s_lockObj = new object();

        // The list of current address-changed subscribers.
        private static NetworkAddressChangedEventHandler s_addressChangedSubscribers;

        // The list of current availability-changed subscribers.
        private static NetworkAvailabilityChangedEventHandler s_availabilityChangedSubscribers;

        // The dynamic store. We listen to changes in the IPv4 and IPv6 address keys.
        // When those keys change, our callback below is called (OnAddressChanged).
        private static SafeCreateHandle s_dynamicStoreRef;

        // The callback used when registered keys in the dynamic store change.
        private static readonly Interop.SystemConfiguration.SCDynamicStoreCallBack s_storeCallback = OnAddressChanged;

        // The RunLoop source, created over the above SCDynamicStore.
        private static SafeCreateHandle s_runLoopSource;

        // Listener thread that adds the RunLoopSource to its RunLoop.
        private static Thread s_runLoopThread;

        // The listener thread's CFRunLoop.
        private static CFRunLoopRef s_runLoop;

        // Use an event to try to prevent StartRaisingEvents from returning before the
        // RunLoop actually begins. This will mitigate a race condition where the watcher
        // thread hasn't completed initialization and stop is called before the RunLoop has even started.
        private static readonly AutoResetEvent s_runLoopStartedEvent = new AutoResetEvent(false);
        private static readonly AutoResetEvent s_runLoopEndedEvent = new AutoResetEvent(false);

        //introduced for supporting design-time loading of System.Windows.dll
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterNetworkChange(NetworkChange nc) { }

        public static event NetworkAddressChangedEventHandler NetworkAddressChanged
        {
            add
            {
                lock (s_lockObj)
                {
                    if (s_addressChangedSubscribers == null && s_availabilityChangedSubscribers == null)
                    {
                        CreateAndStartRunLoop();
                    }

                    s_addressChangedSubscribers += value;
                }
            }
            remove
            {
                lock (s_lockObj)
                {
                    bool hadAddressChangedSubscribers = s_addressChangedSubscribers != null;
                    s_addressChangedSubscribers -= value;

                    if (hadAddressChangedSubscribers && s_addressChangedSubscribers == null && s_availabilityChangedSubscribers == null)
                    {
                        StopRunLoop();
                    }
                }
            }
        }

        public static event NetworkAvailabilityChangedEventHandler NetworkAvailabilityChanged
        {
            add
            {
                lock (s_lockObj)
                {
                    if (s_addressChangedSubscribers == null && s_availabilityChangedSubscribers == null)
                    {
                        CreateAndStartRunLoop();
                    }
                    else
                    {
                        Debug.Assert(s_runLoop != IntPtr.Zero);
                    }

                    s_availabilityChangedSubscribers += value;
                }
            }
            remove
            {
                lock (s_lockObj)
                {
                    bool hadSubscribers = s_addressChangedSubscribers != null || s_availabilityChangedSubscribers != null;
                    s_availabilityChangedSubscribers -= value;

                    if (hadSubscribers && s_addressChangedSubscribers == null && s_availabilityChangedSubscribers == null)
                    {
                        StopRunLoop();
                    }
                }
            }
        }

        private static unsafe void CreateAndStartRunLoop()
        {
            Debug.Assert(s_dynamicStoreRef == null);

            var storeContext = new Interop.SystemConfiguration.SCDynamicStoreContext();
            using (SafeCreateHandle storeName = Interop.CoreFoundation.CFStringCreateWithCString("NetworkAddressChange.OSX"))
            {
                s_dynamicStoreRef = Interop.SystemConfiguration.SCDynamicStoreCreate(
                    storeName.DangerousGetHandle(),
                    s_storeCallback,
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
            s_runLoopThread = new Thread(RunLoopThreadStart);
            s_runLoopThread.Start();
            s_runLoopStartedEvent.WaitOne(); // Wait for the new thread to finish initialization.
        }

        private static void RunLoopThreadStart()
        {
            Debug.Assert(s_runLoop == IntPtr.Zero);

            s_runLoop = Interop.RunLoop.CFRunLoopGetCurrent();
            Interop.RunLoop.CFRunLoopAddSource(
                s_runLoop,
                s_runLoopSource.DangerousGetHandle(),
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

            s_dynamicStoreRef.Dispose();
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

        private static void OnAddressChanged(IntPtr store, IntPtr changedKeys, IntPtr info)
        {
            s_addressChangedSubscribers?.Invoke(null, EventArgs.Empty);
            s_availabilityChangedSubscribers?.Invoke(null, new NetworkAvailabilityEventArgs(NetworkInterface.GetIsNetworkAvailable()));
        }
    }
}

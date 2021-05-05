// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

using CFStringRef = System.IntPtr;
using CFArrayRef = System.IntPtr;
using CFIndex = System.IntPtr;
using SCDynamicStoreRef = System.IntPtr;

internal static partial class Interop
{
    internal static partial class SystemConfiguration
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SCDynamicStoreContext
        {
            public CFIndex version;
            public IntPtr info;
            public IntPtr retainFunc;
            public IntPtr releaseFunc;
            public IntPtr copyDescription;
        }

        /// <summary>
        /// Creates a new session used to interact with the dynamic store maintained by the System Configuration server.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="name">The name of the calling process or plug-in of the caller.</param>
        /// <param name="callout">The function to be called when a watched value in the dynamic store is changed.
        /// Pass null if no callouts are desired.</param>
        /// <param name="context">The context associated with the callout.</param>
        /// <returns>A reference to the new dynamic store session.</returns>
        [DllImport(Libraries.SystemConfigurationLibrary)]
        private static extern unsafe SafeCreateHandle SCDynamicStoreCreate(
            IntPtr allocator,
            CFStringRef name,
            delegate* unmanaged<SCDynamicStoreRef, CFArrayRef, IntPtr, void> callout,
            SCDynamicStoreContext* context);

        /// <summary>
        /// Creates a new session used to interact with the dynamic store maintained by the System Configuration server.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="name">The name of the calling process or plug-in of the caller.</param>
        /// <param name="callout">The function to be called when a watched value in the dynamic store is changed.
        /// Pass null if no callouts are desired.</param>
        /// <param name="context">The context associated with the callout.</param>
        /// <returns>A reference to the new dynamic store session.</returns>
        internal static unsafe SafeCreateHandle SCDynamicStoreCreate(
            CFStringRef name,
            delegate* unmanaged<SCDynamicStoreRef, CFArrayRef, IntPtr, void> callout,
            SCDynamicStoreContext* context)
        {
            return SCDynamicStoreCreate(IntPtr.Zero, name, callout, context);
        }

        /// <summary>
        /// Creates a dynamic store key that can be used to access the per-service network configuration information.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="domain">The desired domain, such as the requested configuration or the current state.</param>
        /// <param name="serviceID">The service ID or a regular expression pattern.</param>
        /// <param name="entity">The specific global entity, such as IPv4 or DNS.</param>
        /// <returns>A string containing the formatted key.</returns>
        [DllImport(Libraries.SystemConfigurationLibrary)]
        private static extern SafeCreateHandle SCDynamicStoreKeyCreateNetworkServiceEntity(
            IntPtr allocator,
            CFStringRef domain,
            CFStringRef serviceID,
            CFStringRef entity);

        /// <summary>
        /// Creates a dynamic store key that can be used to access the per-service network configuration information.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="domain">The desired domain, such as the requested configuration or the current state.</param>
        /// <param name="serviceID">The service ID or a regular expression pattern.</param>
        /// <param name="entity">The specific global entity, such as IPv4 or DNS.</param>
        /// <returns>A string containing the formatted key.</returns>
        internal static SafeCreateHandle SCDynamicStoreKeyCreateNetworkServiceEntity(
            CFStringRef domain,
            CFStringRef serviceID,
            CFStringRef entity)
        {
            return SCDynamicStoreKeyCreateNetworkServiceEntity(IntPtr.Zero, domain, serviceID, entity);
        }

        /// <summary>
        /// Creates a run loop source object that can be added to the application's run loop.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="store">The dynamic store session.</param>
        /// <param name="order">The order in which the sources that are ready to be processed are handled,
        /// on platforms that support it and for source versions that support it.</param>
        /// <returns>The new run loop source object.</returns>
        [DllImport(Libraries.SystemConfigurationLibrary)]
        private static extern SafeCreateHandle SCDynamicStoreCreateRunLoopSource(
            IntPtr allocator,
            SCDynamicStoreRef store,
            CFIndex order);

        /// <summary>
        /// Creates a run loop source object that can be added to the application's run loop.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="store">The dynamic store session.</param>
        /// <param name="order">The order in which the sources that are ready to be processed are handled,
        /// on platforms that support it and for source versions that support it.</param>
        /// <returns>The new run loop source object.</returns>
        internal static SafeCreateHandle SCDynamicStoreCreateRunLoopSource(SCDynamicStoreRef store, CFIndex order)
        {
            return SCDynamicStoreCreateRunLoopSource(IntPtr.Zero, store, order);
        }

        /// <summary>
        /// Specifies a set of keys and key patterns that should be monitored for changes.
        /// </summary>
        /// <param name="store">The dynamic store session being watched.</param>
        /// <param name="keys">An array of keys to be monitored or IntPtr.Zero if no specific keys are to be monitored.</param>
        /// <param name="patterns">An array of POSIX regex pattern strings used to match keys to be monitored,
        /// or IntPtr.Zero if no key patterns are to be monitored.</param>
        /// <returns>True if the set of notification keys and patterns was successfully updated; false otherwise.</returns>
        [DllImport(Libraries.SystemConfigurationLibrary)]
        internal static extern bool SCDynamicStoreSetNotificationKeys(SCDynamicStoreRef store, CFArrayRef keys, CFArrayRef patterns);
    }
}

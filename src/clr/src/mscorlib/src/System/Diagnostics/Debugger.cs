// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The Debugger class is a part of the System.Diagnostics package
// and is used for communicating with a debugger.

namespace System.Diagnostics
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.Versioning;

    // No data, does not need to be marked with the serializable attribute
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Debugger
    {
        // This should have been a static class, but wasn't as of v3.5.  Clearly, this is
        // broken.  We'll keep this in V4 for binary compat, but marked obsolete as error
        // so migrated source code gets fixed.
        [Obsolete("Do not create instances of the Debugger class.  Call the static methods directly on this type instead", true)]
        public Debugger()
        {
            // Should not have been instantiable - here for binary compatibility in V4.
        }

        // Break causes a breakpoint to be signalled to an attached debugger.  If no debugger
        // is attached, the user is asked if he wants to attach a debugger. If yes, then the 
        // debugger is launched.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void Break()
        {
            if (!Debugger.IsAttached)
            {
                // Try and demand UnmanagedCodePermission.  This is done in a try block because if this
                // fails we want to be able to silently eat the exception and just return so
                // that the call to Break does not possibly cause an unhandled exception.
                // The idea here is that partially trusted code shouldn't be able to launch a debugger 
                // without the user going through Watson.
                try
                {
#pragma warning disable 618
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618
                }

                // If we enter this block, we do not have permission to break into the debugger
                // and so we just return.
                catch (SecurityException)
                {
                    return;
                }
            }

            // Causing a break is now allowed.
            BreakInternal();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        static void BreakCanThrow()
        {
            if (!Debugger.IsAttached)
            {
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618
            }

            // Causing a break is now allowed.
            BreakInternal();
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void BreakInternal();

        // Launch launches & attaches a debugger to the process. If a debugger is already attached,
        // nothing happens.  
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static bool Launch()
        {
            if (Debugger.IsAttached)
                return (true);

            // Try and demand UnmanagedCodePermission.  This is done in a try block because if this
            // fails we want to be able to silently eat the exception and just return so
            // that the call to Break does not possibly cause an unhandled exception.
            // The idea here is that partially trusted code shouldn't be able to launch a debugger 
            // without the user going through Watson.
            try
            {
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618
            }

            // If we enter this block, we do not have permission to break into the debugger
            // and so we just return.
            catch (SecurityException)
            {
                return (false);
            }

            // Causing the debugger to launch is now allowed.
            return (LaunchInternal());
        }

        // This class implements code:ICustomDebuggerNotification and provides a type to be used to notify
        // the debugger that execution is about to enter a path that involves a cross-thread dependency. 
        // See code:NotifyOfCrossThreadDependency for more details. 
        private class CrossThreadDependencyNotification : ICustomDebuggerNotification
        {
            // constructor
            public CrossThreadDependencyNotification()
            {
            }
        }

        // Do not inline the slow path 
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static void NotifyOfCrossThreadDependencySlow()
        {
            CrossThreadDependencyNotification notification = new CrossThreadDependencyNotification();
            CustomNotification(notification);
        }

        // Sends a notification to the debugger to indicate that execution is about to enter a path 
        // involving a cross thread dependency. A debugger that has opted into this type of notification 
        // can take appropriate action on receipt. For example, performing a funceval normally requires 
        // freezing all threads but the one performing the funceval. If the funceval requires execution on 
        // more than one thread, as might occur in remoting scenarios, the funceval will block. This 
        // notification will apprise the debugger that it will need  to slip a thread or abort the funceval 
        // in such a situation. The notification is subject to collection after this function returns. 
        // 
        [method:System.Runtime.InteropServices.ComVisible(false)]
        public static void NotifyOfCrossThreadDependency()
        {
            if (Debugger.IsAttached)
            {
                NotifyOfCrossThreadDependencySlow();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool LaunchInternal();

        // Returns whether or not a debugger is attached to the process.
        //
        public static extern bool IsAttached
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        // Constants representing the importance level of messages to be logged.
        //
        // An attached debugger can enable or disable which messages will
        // actually be reported to the user through the COM+ debugger
        // services API.  This info is communicated to the runtime so only
        // desired events are actually reported to the debugger.  
        //
        // Constant representing the default category
        public static readonly String DefaultCategory = null;

        // Posts a message for the attached debugger.  If there is no
        // debugger attached, has no effect.  The debugger may or may not
        // report the message depending on its settings. 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void Log(int level, String category, String message);

        // Checks to see if an attached debugger has logging enabled
        //  
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool IsLogging();

        // Posts a custom notification for the attached debugger.  If there is no
        // debugger attached, has no effect.  The debugger may or may not
        // report the notification depending on its settings. 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void CustomNotification(ICustomDebuggerNotification data);

    }

}

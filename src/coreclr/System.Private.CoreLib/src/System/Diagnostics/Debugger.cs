// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The Debugger class is a part of the System.Diagnostics package
// and is used for communicating with a debugger.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    public static partial class Debugger
    {
        // Break causes a breakpoint to be signalled to an attached debugger.  If no debugger
        // is attached, the user is asked if they want to attach a debugger. If yes, then the
        // debugger is launched.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Break() => BreakInternal();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void BreakInternal();

        // Launch launches & attaches a debugger to the process. If a debugger is already attached,
        // nothing happens.
        //
        public static bool Launch() => IsAttached || LaunchInternal();

        // This class implements code:ICustomDebuggerNotification and provides a type to be used to notify
        // the debugger that execution is about to enter a path that involves a cross-thread dependency.
        // See code:NotifyOfCrossThreadDependency for more details.
        private sealed class CrossThreadDependencyNotification : ICustomDebuggerNotification { }

        // Do not inline the slow path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void NotifyOfCrossThreadDependencySlow() =>
            CustomNotification(new CrossThreadDependencyNotification());

        // Sends a notification to the debugger to indicate that execution is about to enter a path
        // involving a cross thread dependency. A debugger that has opted into this type of notification
        // can take appropriate action on receipt. For example, performing a funceval normally requires
        // freezing all threads but the one performing the funceval. If the funceval requires execution on
        // more than one thread, as might occur in remoting scenarios, the funceval will block. This
        // notification will apprise the debugger that it will need  to slip a thread or abort the funceval
        // in such a situation. The notification is subject to collection after this function returns.
        //
        public static void NotifyOfCrossThreadDependency()
        {
            if (IsAttached)
            {
                NotifyOfCrossThreadDependencySlow();
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_Launch")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LaunchInternal();

        // Returns whether or not a debugger is attached to the process.
        //
        public static extern bool IsAttached
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
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
        public static readonly string? DefaultCategory;

        // Posts a message for the attached debugger.  If there is no
        // debugger attached, has no effect.  The debugger may or may not
        // report the message depending on its settings.
        public static void Log(int level, string? category, string? message) => LogInternal(level, category, message);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_Log", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void LogInternal(int level, string? category, string? message);

        // Checks to see if an attached debugger has logging enabled
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool IsLogging();

        // Posts a custom notification for the attached debugger.  If there is no
        // debugger attached, has no effect.  The debugger may or may not
        // report the notification depending on its settings.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void CustomNotification(ICustomDebuggerNotification data);
    }
}

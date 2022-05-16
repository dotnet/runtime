// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    public static class Debugger
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DebuggerHidden] // this helps VS appear to stop on the source line calling Debugger.Break() instead of inside it
        public static void Break()
        {
#if TARGET_WINDOWS
            // IsAttached is always true when IsDebuggerPresent is true, so no need to check for it
            if (Interop.Kernel32.IsDebuggerPresent())
                Debug.DebugBreak();
#else
            // UNIXTODO: Implement Debugger.Break
#endif
        }

        public static bool IsAttached
        {
            get
            {
                return _isDebuggerAttached;
            }
        }

        public static bool Launch()
        {
            throw new PlatformNotSupportedException();
        }

        public static void NotifyOfCrossThreadDependency()
        {
            // nothing to do...yet
        }

#pragma warning disable 649  // Suppress compiler warning about _isDebuggerAttached never being assigned to.
        // _isDebuggerAttached: Do not remove: This field is known to the debugger and modified directly by the debugger.
        private static bool _isDebuggerAttached;
#pragma warning restore 649

        /// <summary>
        /// Constants representing the importance level of messages to be logged.
        ///
        /// An attached debugger can enable or disable which messages will
        /// actually be reported to the user through the COM+ debugger
        /// services API.  This info is communicated to the runtime so only
        /// desired events are actually reported to the debugger.
        /// Constant representing the default category
        /// </summary>
        public static readonly string DefaultCategory;

        /// <summary>
        /// Posts a message for the attached debugger.  If there is no
        /// debugger attached, has no effect.  The debugger may or may not
        /// report the message depending on its settings.
        /// </summary>
        public static void Log(int level, string category, string message)
        {
            if (IsLogging())
            {
                throw new NotImplementedException(); // TODO: Implement Debugger.Log, IsLogging
            }
        }

        /// <summary>
        /// Checks to see if an attached debugger has logging enabled
        /// </summary>
        public static bool IsLogging()
        {
            if (string.Empty.Length != 0)
            {
                throw new NotImplementedException(); // TODO: Implement Debugger.Log, IsLogging
            }
            return false;
        }
    }
}

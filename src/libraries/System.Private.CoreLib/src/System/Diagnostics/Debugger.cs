// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    public static partial class Debugger
    {
        /// <summary>
        /// Represents the default category of message with a constant.
        /// </summary>
        /// <remarks>
        /// The value of this default constant is `null`. <see cref="Debugger.DefaultCategory"/>
        /// is used by <see cref="Debugger.Log"/>.
        /// </remarks>
        public static readonly string? DefaultCategory;

        /// <summary>
        /// Signals a breakpoint to an attached debugger with the <paramref name="exception"/> details
        /// if a .NET debugger is attached with break on user-unhandled exception enabled and a method
        /// attributed with DebuggerDisableUserUnhandledExceptionsAttribute calls this method.
        /// </summary>
        /// <param name="exception">The user-unhandled exception.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void BreakForUserUnhandledException(Exception exception)
        {
        }

        [FeatureSwitchDefinition("System.Diagnostics.Debugger.IsSupported")]
        internal static bool IsSupported { get; } = InitializeIsSupported();

        private static bool InitializeIsSupported()
        {
            return AppContext.TryGetSwitch("System.Diagnostics.Debugger.IsSupported", out bool isSupported)
                   ? isSupported
                   : true;
        }
    }
}

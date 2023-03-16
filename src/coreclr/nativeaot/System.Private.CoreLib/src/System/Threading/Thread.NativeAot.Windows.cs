// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    using OSThreadPriority = Interop.Kernel32.ThreadPriority;

    public sealed partial class Thread
    {
        [ThreadStatic]
        private static int t_reentrantWaitSuppressionCount;

        [ThreadStatic]
        private static ApartmentType t_apartmentType;

        [ThreadStatic]
        private static ComState t_comState;

        private SafeWaitHandle _osHandle;

        private ApartmentState _initialApartmentState = ApartmentState.Unknown;

        private static volatile bool s_comInitializedOnFinalizerThread;

        public ApartmentState GetApartmentState() => GetApartmentStateCore();

        internal static void InitializeComForFinalizerThread() => InitializeComForFinalizerThreadCore();

        // TODO: https://github.com/dotnet/runtime/issues/22161
        public void DisableComObjectEagerCleanup() => DisableComObjectEagerCleanupCore();

        // Use ThreadPoolCallbackWrapper instead of calling this function directly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Thread EnsureThreadPoolThreadInitialized() => EnsureThreadPoolThreadInitializedCore();

        public void Interrupt() => InterruptCore();

        //
        // Suppresses reentrant waits on the current thread, until a matching call to RestoreReentrantWaits.
        // This should be used by code that's expected to be called inside the STA message pump, so that it won't
        // reenter itself.  In an ASTA, this should only be the CCW implementations of IUnknown and IInspectable.
        //
        internal static void SuppressReentrantWaits() => SuppressReentrantWaitsCore();

        internal static void RestoreReentrantWaits() => RestoreReentrantWaitsCore();

        internal static bool ReentrantWaitsEnabled => ReentrantWaitsEnabledCore();

        internal static ApartmentType GetCurrentApartmentType() => GetCurrentApartmentTypeCore();

        internal enum ApartmentType : byte
        {
            Unknown = 0,
            None,
            STA,
            MTA
        }

        [Flags]
        internal enum ComState : byte
        {
            InitializedByUs = 1,
            Locked = 2,
        }
    }
}

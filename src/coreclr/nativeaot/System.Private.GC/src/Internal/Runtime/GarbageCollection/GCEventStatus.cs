// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from src/coreclr/gc/gceventstatus.h and src/coreclr/gc/gceventstatus.cpp.
//
// In order for a local GC to determine what events are enabled in an efficient manner, the GC
// maintains some local state about keywords and levels that are enabled for each eventing
// provider.
//
// The GC fires events from two providers: the "main" provider and the "private" provider. This
// file tracks keyword and level information for each provider separately.
//
// It is the responsibility of the EE to inform the GC of changes to eventing state. This is
// accomplished by invoking the IGCHeap::ControlEvents and IGCHeap::ControlPrivateEvents callbacks
// on the EE's heap instance, which ultimately will enable and disable keywords and levels here.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Internal.Runtime.GarbageCollection
{
    /// <summary>
    /// Maintains all eventing state for the GC. It consists of a keyword bitmask and level for
    /// each provider that the GC can use to fire events.
    ///
    /// A level and event pair are considered to be "enabled" on a given provider if the given
    /// level is less than or equal to the current enabled level and if the keyword is present in
    /// the enabled keyword bitmask for that provider.
    /// </summary>
    internal static class GCEventStatus
    {
        /// <summary>
        /// A value per provider. Backed by an inline array rather than a managed array because
        /// the GC must not depend on the managed heap it is collecting.
        /// </summary>
        [InlineArray((int)GCEventProvider.Count)]
        private struct PerProvider
        {
            private int _element0;
        }

        /// <summary>The enabled level for each provider.</summary>
        private static PerProvider s_enabledLevels;

        /// <summary>The bitmap of enabled keywords for each provider.</summary>
        private static PerProvider s_enabledKeywords;

        /// <summary>
        /// Queries whether or not the given level and keyword are enabled on the given provider,
        /// returning true if they are.
        /// </summary>
        /// <remarks>
        /// The native implementation uses <c>LoadWithoutBarrier</c> here. There is no managed
        /// equivalent of a barrier-free volatile load, so this uses an acquire load, which is
        /// strictly stronger and therefore still correct.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(GCEventProvider provider, GCEventKeyword keyword, GCEventLevel level)
        {
            Debug.Assert(level >= GCEventLevel.None && level < GCEventLevel.Max);

            int index = (int)provider;
            return Volatile.Read(ref s_enabledLevels[index]) >= (int)level
                && (Volatile.Read(ref s_enabledKeywords[index]) & (int)keyword) != 0;
        }

        /// <summary>
        /// Sets the eventing state (level and keyword bitmap) for a given provider to the
        /// provided values.
        /// </summary>
        public static void Set(GCEventProvider provider, GCEventKeyword keywords, GCEventLevel level)
        {
            Debug.Assert((level >= GCEventLevel.None && level < GCEventLevel.Max) || level == GCEventLevel.LogAlways);

            int index = (int)provider;

            // As in the native implementation, the level and the keywords are published with two
            // separate stores. A concurrent IsEnabled call can therefore briefly observe the new
            // level with the old keywords (or the reverse); that transient mismatch only affects
            // whether an individual event is emitted, so it is tolerated rather than serialized.
            Volatile.Write(ref s_enabledLevels[index], (int)level);
            Volatile.Write(ref s_enabledKeywords[index], (int)keywords);
        }

        /// <summary>Returns the currently enabled level for a provider.</summary>
        public static GCEventLevel GetEnabledLevel(GCEventProvider provider)
            => (GCEventLevel)Volatile.Read(ref s_enabledLevels[(int)provider]);

        /// <summary>Returns the currently enabled keywords for a provider.</summary>
        public static GCEventKeyword GetEnabledKeywords(GCEventProvider provider)
            => (GCEventKeyword)Volatile.Read(ref s_enabledKeywords[(int)provider]);
    }
}

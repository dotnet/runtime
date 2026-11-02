// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed partial class Thread
    {
#pragma warning disable CA1822, IDE0060
        private ThreadPriority GetPriorityLive()
        {
            return ThreadPriority.Normal;
        }

        private bool SetPriorityLive(ThreadPriority priority)
        {
            return true;
        }
#pragma warning restore CA1822, IDE0060

        public ApartmentState GetApartmentState()
        {
            return ApartmentState.Unknown;
        }

        private static bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
            if (state != ApartmentState.Unknown)
            {
                if (throwOnError)
                {
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
                }

                return false;
            }

            return true;
        }

        partial void InitializeComOnNewThread();

        public void DisableComObjectEagerCleanup() { }

        internal const bool ReentrantWaitsEnabled = false;

        internal static void SuppressReentrantWaits()
        {
            throw new PlatformNotSupportedException();
        }

        internal static void RestoreReentrantWaits()
        {
            throw new PlatformNotSupportedException();
        }
    }
}

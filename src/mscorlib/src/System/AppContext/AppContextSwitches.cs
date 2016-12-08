// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    using System;
    using System.Runtime.CompilerServices;

    internal static class AppContextSwitches
    {
        private static int _noAsyncCurrentCulture;
        public static bool NoAsyncCurrentCulture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue(AppContextDefaultValues.SwitchNoAsyncCurrentCulture, ref _noAsyncCurrentCulture);
            }
        }

        private static int _throwExceptionIfDisposedCancellationTokenSource;
        public static bool ThrowExceptionIfDisposedCancellationTokenSource
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue(AppContextDefaultValues.SwitchThrowExceptionIfDisposedCancellationTokenSource, ref _throwExceptionIfDisposedCancellationTokenSource);
            }
        }

        private static int _preserveEventListnerObjectIdentity;
        public static bool PreserveEventListnerObjectIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue(AppContextDefaultValues.SwitchPreserveEventListnerObjectIdentity, ref _preserveEventListnerObjectIdentity);
            }
        }

        //
        // Implementation details
        //

        private static bool DisableCaching { get; set; }

        static AppContextSwitches()
        {
            bool isEnabled;
            if (AppContext.TryGetSwitch(@"TestSwitch.LocalAppContext.DisableCaching", out isEnabled))
            {
                DisableCaching = isEnabled;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetCachedSwitchValue(string switchName, ref int switchValue)
        {
            if (switchValue < 0) return false;
            if (switchValue > 0) return true;

            return GetCachedSwitchValueInternal(switchName, ref switchValue);
        }

        private static bool GetCachedSwitchValueInternal(string switchName, ref int switchValue)
        {
            bool isSwitchEnabled;
            AppContext.TryGetSwitch(switchName, out isSwitchEnabled);

            if (DisableCaching)
            {
                return isSwitchEnabled;
            }

            switchValue = isSwitchEnabled ? 1 /*true*/ : -1 /*false*/;
            return isSwitchEnabled;
        }
    }
}

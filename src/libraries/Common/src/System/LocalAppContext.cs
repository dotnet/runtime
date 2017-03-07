// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    internal partial class LocalAppContext
    {
        private static bool s_isDisableCachingInitialized;
        private static bool s_disableCaching;
        private static object s_syncObject;

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

        private static bool DisableCaching =>
            LazyInitializer.EnsureInitialized(ref s_disableCaching, ref s_isDisableCachingInitialized, ref s_syncObject, () =>
            {
                bool isEnabled;
                AppContext.TryGetSwitch(@"TestSwitch.LocalAppContext.DisableCaching", out isEnabled);
                return isEnabled;
            });
    }
}

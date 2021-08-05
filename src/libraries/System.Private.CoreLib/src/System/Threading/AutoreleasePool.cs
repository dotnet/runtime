// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static class AutoreleasePool
    {
        private static bool CheckEnableAutoreleasePool()
        {
            const string feature = "System.Threading.Thread.EnableAutoreleasePool";
#if !CORECLR
            return AppContextConfigHelper.GetBooleanConfig(feature, false);
#else
            bool isEnabled = CLRConfig.GetBoolValue(feature, out bool isSet);
            if (!isSet)
                return false;

            return isEnabled;
#endif
        }

        public static bool EnableAutoreleasePool { get; } = CheckEnableAutoreleasePool();

        [ThreadStatic]
        private static IntPtr s_AutoreleasePoolInstance;

        internal static void CreateAutoreleasePool()
        {
            if (EnableAutoreleasePool)
            {
                Debug.Assert(s_AutoreleasePoolInstance == IntPtr.Zero);
                s_AutoreleasePoolInstance = Interop.Sys.CreateAutoreleasePool();
            }
        }

        internal static void DrainAutoreleasePool()
        {
            if (EnableAutoreleasePool
                && s_AutoreleasePoolInstance != IntPtr.Zero)
            {
                Interop.Sys.DrainAutoreleasePool(s_AutoreleasePoolInstance);
                s_AutoreleasePoolInstance = IntPtr.Zero;
            }
        }
    }
}

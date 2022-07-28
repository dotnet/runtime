// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal static class AutoreleasePool
    {
        private static bool CheckEnableAutoreleasePool()
        {
            const string feature = "System.Threading.Thread.EnableAutoreleasePool";
#if CORECLR
            // In coreclr_initialize, we call ICLRRuntimeHost4->Start() which, among other things,
            // starts a finalizer thread for Objective-C's NSAutoreleasePool interop on macOS.
            // Although AppContext.Setup() is done during CreateAppDomainWithManager() call which
            // is made in coreclr_initialize right after the host has started, there is a chance of
            // race between the call to CreateAppDomainWithManager in coreclr_initialize and the
            // finalizer thread starting, that will call into the changed managed code.
            //
            // Therefore, we are using CLR configuration via QCall here, instead of AppContext.

            return CLRConfig.GetBoolValue(feature, out bool isSet) && isSet;
#else
            return AppContextConfigHelper.GetBooleanConfig(feature, false);
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

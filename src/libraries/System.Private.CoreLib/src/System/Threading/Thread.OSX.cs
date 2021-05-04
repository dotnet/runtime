// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static class ThreadOSX
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
    }

    public sealed partial class Thread
    {
        [UnmanagedCallersOnly]
        private static void CallDrain(IntPtr p)
            => Interop.Sys.DrainAutoreleasePool(p);

        internal static unsafe IntPtr CreateAutoreleasePool(out IntPtr drainFunc)
        {
            drainFunc = IntPtr.Zero;
            if (!ThreadOSX.EnableAutoreleasePool)
                return IntPtr.Zero;

            drainFunc = (IntPtr)(delegate* unmanaged<IntPtr, void>)&CallDrain;
            return Interop.Sys.CreateAutoreleasePool();
        }
    }
}

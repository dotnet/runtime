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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        internal static bool UseIcu { get; private set; } = GetUseIcuMode();

        private static bool GetGlobalizationInvariantMode()
        {
            return GetInvariantSwitchValue();
        }

        private static bool GetUseIcuMode()
        {
            bool useNlsValue = GetUseNlsSwitchValue();
            if (!Invariant)
            {
                if (useNlsValue || Interop.Globalization.LoadICU() == 0)
                {
                    return false;
                }

                return true;
            }

            return !useNlsValue;
        }
    }
}

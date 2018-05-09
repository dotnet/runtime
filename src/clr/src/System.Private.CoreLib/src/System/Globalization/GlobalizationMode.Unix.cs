// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal sealed partial class GlobalizationMode
    {
        private static bool GetGlobalizationInvariantMode()
        {
            bool invariantEnabled = GetInvariantSwitchValue();
            if (!invariantEnabled)
            {
                if (Interop.Globalization.LoadICU() == 0)
                {
                    string message = "Couldn't find a valid ICU package installed on the system. " +
                                    "Set the configuration flag System.Globalization.Invariant to true if you want to run with no globalization support.";
                    Environment.FailFast(message);
                }
            }
            return invariantEnabled;
        }
    }
}

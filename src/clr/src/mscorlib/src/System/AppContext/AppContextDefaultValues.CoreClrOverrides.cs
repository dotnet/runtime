// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static partial class AppContextDefaultValues
    {
        static partial void TryGetSwitchOverridePartial(string switchName, ref bool overrideFound, ref bool overrideValue)
        {
            overrideFound = false;
            overrideValue = false;

            string value = AppContext.GetData(switchName) as string;
            if (value != null)
            {
                overrideFound = bool.TryParse(value, out overrideValue);
            }
        }
    }
}

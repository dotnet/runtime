// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// This file is used to provide an implementation for defining a default value
// This should be compiled only in mscorlib where the AppContext class is available
//

namespace System
{
    internal static partial class AppContextDefaultValues
    {
        /// <summary>
        /// This method allows reading the override for a switch. 
        /// The implementation is platform specific
        /// </summary>
        public static bool TryGetSwitchOverride(string switchName, out bool overrideValue)
        {
            // The default value for a switch is 'false'
            overrideValue = false;

            // Read the override value
            bool overrideFound = false;

            // This partial method will be removed if there are no implementations of it.
            TryGetSwitchOverridePartial(switchName, ref overrideFound, ref overrideValue);

            return overrideFound;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Runtime.Versioning
{
    internal static class CompatibilitySwitch
    {
        /* This class contains 3 sets of api:
                * 1. internal apis : These apis are supposed to be used by mscorlib.dll and other assemblies which use the <runtime> section in config 
                *                          These apis query for the value of quirk not only in windows quirk DB but also in runtime section of config files,  
                *                          registry and environment vars.
                * 2. public apis : These apis are supposed to be used by FX assemblies which do not read the runtime section of config files and have
                *                       have their own section in config files or do not use configs at all.
                *
                * 3. specialized apis: These apis are defined in order to retrieve a specific value defined in CLR Config. That value can have specific look-up rules
                *                        for the order and location of the config sources used.
                *                        
                *     These apis are for internal use only for FX assemblies. It has not been decided if they can be used by OOB components due to EULA restrictions
                */
        internal static string? GetValueInternal(string compatibilitySwitchName)
        {
            return GetValueInternalCall(compatibilitySwitchName, false);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern string? GetValueInternalCall(string compatibilitySwitchName, bool onlyDB);
    }
}

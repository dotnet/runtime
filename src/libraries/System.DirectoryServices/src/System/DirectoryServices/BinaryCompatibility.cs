// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Reflection;
using System.Security.Permissions;
using System.Runtime.Versioning;

namespace System.DirectoryServices
{
    // This class uses reflection on System.Runtime.Versioning.BinaryCompatibility type.
    internal static class BinaryCompatibility
    {
        internal static bool TargetsAtLeast_Desktop_V4_5_3 { get { return s_targetsAtLeast_Desktop_V4_5_3; } }

        private static bool s_targetsAtLeast_Desktop_V4_5_3 = RunningOnCheck("TargetsAtLeast_Desktop_V4_5_3");

        [SecuritySafeCritical]
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        private static bool RunningOnCheck(string propertyName)
        {
            Type binaryCompatabilityType;

            try
            {
                binaryCompatabilityType = typeof(Object).GetTypeInfo().Assembly.GetType("System.Runtime.Versioning.BinaryCompatibility", false);
            }
            catch (TypeLoadException)
            {
                return false;
            }

            if (binaryCompatabilityType == null)
                return false;

            PropertyInfo runningOnProperty = binaryCompatabilityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (runningOnProperty == null)
                return false;

            return (bool)runningOnProperty.GetValue(null);
        }
    }
}

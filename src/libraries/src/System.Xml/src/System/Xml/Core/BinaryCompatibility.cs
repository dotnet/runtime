// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Reflection;

namespace System.Xml
{
    internal static class BinaryCompatibility
    {
        internal static bool TargetsAtLeast_Desktop_V4_5_2 { get { return s_targetsAtLeast_Desktop_V4_5_2; } }

        private static bool s_targetsAtLeast_Desktop_V4_5_2 = RunningOnCheck("TargetsAtLeast_Desktop_V4_5_2");

        [SecuritySafeCritical]
        private static bool RunningOnCheck(string propertyName)
        {
            Type binaryCompatabilityType;

            try
            {
                binaryCompatabilityType = typeof(Object).GetTypeInfo().Assembly.GetType("System.Runtime.Versioning.BinaryCompatibility");
            }
            catch (TypeLoadException)
            {
                return false;
            }

            if (binaryCompatabilityType == null)
                return false;

            PropertyInfo property = binaryCompatabilityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property == null)
                return false;

            return (bool)property.GetValue(null);
        }
    }
}

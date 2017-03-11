// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
#if uap
        private const string FrameworkName = ".NET Native";
#elif netfx || win8
        private const string FrameworkName = ".NET Framework";
#else // uap || wpa81 || other
        private const string FrameworkName = ".NET Core";
#endif

        private static string s_frameworkDescription;

        public static string FrameworkDescription
        {
            get
            {
                if (s_frameworkDescription == null)
                {
                    AssemblyFileVersionAttribute attr = (AssemblyFileVersionAttribute)(typeof(object).GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)));
                    Debug.Assert(attr != null);
                    s_frameworkDescription = $"{FrameworkName} {attr.Version}";
                }

                return s_frameworkDescription;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
#if uapaot
        private const string FrameworkName = ".NET Native";
#else // uap || netcoreapp
        private const string FrameworkName = ".NET Core";
#endif

        private static string s_frameworkDescription;

        public static string FrameworkDescription
        {
            get
            {
                if (s_frameworkDescription == null)
                {
                    string versionString = (string)AppContext.GetData("FX_PRODUCT_VERSION");

                    if (versionString == null)
                    {
                        // Use AssemblyInformationalVersionAttribute as fallback if the exact product version is not specified by the host
                        versionString = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                        // Strip the git hash if there is one
                        int plusIndex = versionString.IndexOf('+');
                        if (plusIndex != -1)
                            versionString = versionString.Substring(0, plusIndex);
                    }

                    s_frameworkDescription = $"{FrameworkName} {versionString}";
                }

                return s_frameworkDescription;
            }
        }
    }
}

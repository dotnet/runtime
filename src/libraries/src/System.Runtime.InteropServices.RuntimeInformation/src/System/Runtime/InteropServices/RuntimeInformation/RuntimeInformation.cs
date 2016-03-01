// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
#if netcore50aot
        private const string FrameworkName = ".NET Native";
#elif net45
        private const string FrameworkName = ".NET Framework";
#else
        private const string FrameworkName = ".NET Core";
#endif

        private static string s_frameworkDescription;

        public static string FrameworkDescription
        {
            get
            {
                return s_frameworkDescription ?? (s_frameworkDescription = $"{FrameworkName} {typeof(object).GetTypeInfo().Assembly.GetName().Version}");
            }
        }
    }
}

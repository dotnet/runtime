// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

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

        public static string FrameworkDescription
        {
            get
            {
                return FrameworkName;
            }
        }
    }
}

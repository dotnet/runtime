// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace System
{
    public static class RuntimeDetection
    {
        private static readonly string s_frameworkDescription = RuntimeInformation.FrameworkDescription;

        // public static bool IsMono { get; } = Type.GetType("Mono.Runtime") != null;
        public static bool IsNetFramework { get; } = s_frameworkDescription.StartsWith(".NET Framework");
        // public static bool IsCoreclr { get; } = s_frameworkDescription.StartsWith(".NET Core");
        public static bool IsNetNative { get; } = s_frameworkDescription.StartsWith(".NET Native");
    }
}

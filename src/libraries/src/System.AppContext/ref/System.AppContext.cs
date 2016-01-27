// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System
{
    public static partial class AppContext
    {
        public static string BaseDirectory { get { return default(string); } }
        public static void SetSwitch(string switchName, bool isEnabled) { }
        public static bool TryGetSwitch(string switchName, out bool isEnabled) { isEnabled = default(bool); return default(bool); }
        public static string TargetFrameworkName { get { return default(string); } }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System
{
    public static partial class Environment
    {
        // Emscripten VFS mounts at / and is the only drive
        public static string[] GetLogicalDrives() => DriveInfoInternal.GetLogicalDrives();

        // In the mono runtime, this maps to gethostname, which returns 'emscripten'.
        // Returning the value here allows us to exclude more of the runtime.
        public static string MachineName => "localhost";

        // Matching what we returned for an earlier release.  There isn't an established equivalent
        // on wasm.
        public static long WorkingSet => 0;

        public static string UserName => "Browser";

        private static OperatingSystem GetOSVersion()
        {
            return new OperatingSystem(PlatformID.Other, new Version(1, 0, 0, 0));
        }

        private static bool IsPrivilegedProcessCore() => false;

        private static int GetProcessId() => 42;

        /// <summary>
        /// Returns the path of the executable that started the currently executing process. Returns null when the path is not available.
        /// </summary>
        /// <returns>Path of the executable that started the currently executing process</returns>
        private static string? GetProcessPath() => null;
    }
}

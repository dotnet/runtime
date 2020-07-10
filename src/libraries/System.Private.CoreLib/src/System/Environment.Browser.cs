// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class Environment
    {
        // Emscripten VFS mounts at / and is the only drive
        public static string[] GetLogicalDrives() => new string[] { "/" };

        // In the mono runtime, this maps to gethostname, which returns 'emscripten'.
        // Returning the value here allows us to exclude more of the runtime.
        public static string MachineName => "emscripten";

        // 3.2 provides no context, so return the same value.
        public static long WorkingSet => 0;

        // 3.2 provides no context, so return the same value.
        public static string UserName => "web_user";
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static partial class ConsoleUtils
    {
        /// <summary>Whether to output ANSI color strings.</summary>
        private static volatile int s_emitAnsiColorCodes = -1;

        /// <summary>Get whether to emit ANSI color codes.</summary>
        public static bool EmitAnsiColorCodes
        {
            get
            {
                // The flag starts at -1. If it's no longer -1, it's 0 or 1 to represent false or true.
                int emitAnsiColorCodes = s_emitAnsiColorCodes;
                if (emitAnsiColorCodes != -1)
                {
                    return Convert.ToBoolean(emitAnsiColorCodes);
                }

                // We've not yet computed whether to emit codes or not. We may race with
                // other threads, and that's ok; this is idempotent unless someone is currently changing
                // the value of the relevant environment variables, in which case behavior here is undefined.

                // Per https://force-color.org/, FORCE_COLOR forces ANSI color output when set to a non-empty value.
                // DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION is treated as a legacy alias for the same behavior.
                // These take highest priority and override all other checks.
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FORCE_COLOR")) ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION")))
                {
                    s_emitAnsiColorCodes = 1;
                    return true;
                }

                // By default, we emit ANSI color codes if output isn't redirected, and suppress them if output is redirected.
                bool enabled = !Console.IsOutputRedirected;

                if (enabled)
                {
                    // Per https://no-color.org/, NO_COLOR disables ANSI color output when set.
                    enabled = Environment.GetEnvironmentVariable("NO_COLOR") is null;
                }

                // Store and return the computed answer.
                s_emitAnsiColorCodes = Convert.ToInt32(enabled);
                return enabled;
            }
        }
    }
}

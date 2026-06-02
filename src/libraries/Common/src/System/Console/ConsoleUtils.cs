// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static partial class ConsoleUtils
    {
        /// <summary>Whether to output ANSI color strings.</summary>
        private static NullableBool s_emitAnsiColorCodes;

        /// <summary>Get whether to emit ANSI color codes.</summary>
        public static bool EmitAnsiColorCodes
        {
            get
            {
                // The flag starts at Undefined. If it's no longer Undefined, it's False or True.
                NullableBool emitAnsiColorCodes = s_emitAnsiColorCodes;
                if (emitAnsiColorCodes is not NullableBool.Undefined)
                {
                    return emitAnsiColorCodes is NullableBool.True;
                }

                // We've not yet computed whether to emit codes or not. We may race with
                // other threads, and that's ok; this is idempotent unless someone is currently changing
                // the value of the relevant environment variables, in which case behavior here is undefined.
                // By default, we emit ANSI color codes if output isn't redirected and it's not overridden
                // by environment variables.
                bool enabled = GetColorOverrideFromEnvironment() ?? !Console.IsOutputRedirected;

                // Store and return the computed answer.
                s_emitAnsiColorCodes = enabled ? NullableBool.True : NullableBool.False;
                return enabled;
            }
        }

        /// <summary>
        /// Checks FORCE_COLOR, DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION, and NO_COLOR environment variables.
        /// Returns <see langword="true"/> to force color on, <see langword="false"/> to force color off,
        /// or <see langword="null"/> if no override is set.
        /// </summary>
        internal static bool? GetColorOverrideFromEnvironment()
        {
            // Per https://force-color.org/, FORCE_COLOR forces ANSI color output when set to a non-empty value.
            // DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION is treated as a legacy alias for the same behavior.
            // These take highest priority and override all other checks.
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FORCE_COLOR")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION")))
            {
                return true;
            }

            // Per https://no-color.org/, NO_COLOR overrides and disables ANSI color output when set to a non-empty value.
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
            {
                return false;
            }

            return null;
        }
    }
}

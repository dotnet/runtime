// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System
{
    internal static partial class ConsoleUtils
    {
        /// <summary>Whether to output ansi color strings.</summary>
        private static volatile int s_emitAnsiColorCodes = -1;

        private static volatile int s_useNet6KeyParser = -1;

        /// <summary>Get whether to emit ANSI color codes.</summary>
        public static bool EmitAnsiColorCodes
        {
            get
            {
                // The flag starts at -1.  If it's no longer -1, it's 0 or 1 to represent false or true.
                int emitAnsiColorCodes = s_emitAnsiColorCodes;
                if (emitAnsiColorCodes != -1)
                {
                    return Convert.ToBoolean(emitAnsiColorCodes);
                }

                // We've not yet computed whether to emit codes or not.  Do so now.  We may race with
                // other threads, and that's ok; this is idempotent unless someone is currently changing
                // the value of the relevant environment variables, in which case behavior here is undefined.

                // By default, we emit ANSI color codes if output isn't redirected, and suppress them if output is redirected.
                bool enabled = !Console.IsOutputRedirected;

                if (enabled)
                {
                    // We subscribe to the informal standard from https://no-color.org/.  If we'd otherwise emit
                    // ANSI color codes but the NO_COLOR environment variable is set, disable emitting them.
                    enabled = Environment.GetEnvironmentVariable("NO_COLOR") is null;
                }
                else
                {
                    // We also support overriding in the other direction.  If we'd otherwise avoid emitting color
                    // codes but the DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION environment variable is
                    // set to 1 or true, enable color.
                    string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION");
                    enabled = envVar is not null && (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
                }

                // Store and return the computed answer.
                s_emitAnsiColorCodes = Convert.ToInt32(enabled);
                return enabled;
            }
        }

        internal static bool UseNet6KeyParser
        {
            get
            {
                int useNet6KeyParser = s_useNet6KeyParser;

                if (useNet6KeyParser == -1)
                {
                    useNet6KeyParser = s_useNet6KeyParser = GetNet6CompatReadKeySetting() ? 1 : 0;
                }

                return useNet6KeyParser == 1;

                static bool GetNet6CompatReadKeySetting()
                {
                    if (AppContext.TryGetSwitch("System.Console.UseNet6CompatReadKey", out bool fileConfig))
                    {
                        return fileConfig;
                    }

                    string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_USENET6COMPATREADKEY");
                    return envVar is not null && (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}

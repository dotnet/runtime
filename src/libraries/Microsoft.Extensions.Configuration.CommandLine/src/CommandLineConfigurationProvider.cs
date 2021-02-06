// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if !NET461
using System.Runtime.InteropServices;
#endif

namespace Microsoft.Extensions.Configuration.CommandLine
{
    /// <summary>
    /// A command line based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class CommandLineConfigurationProvider : ConfigurationProvider
    {
        private readonly Dictionary<string, string> _switchMappings;
        private static bool s_isWindows =
#if NET461
            true;
#else
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="args">The command line args.</param>
        /// <param name="switchMappings">The switch mappings.</param>
        public CommandLineConfigurationProvider(IEnumerable<string> args, IDictionary<string, string> switchMappings = null)
        {
            Args = args ?? throw new ArgumentNullException(nameof(args));

            if (switchMappings != null)
            {
                _switchMappings = GetValidatedSwitchMappingsCopy(switchMappings);
            }
        }

        /// <summary>
        /// The command line arguments.
        /// </summary>
        protected IEnumerable<string> Args { get; }

        /// <summary>
        /// Loads the configuration data from the command line args.
        /// </summary>
        public override void Load()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (IEnumerator<string> enumerator = Args.GetEnumerator())
            {
                // Store 1st argument here and start while loop with the 2nd,
                // or 'Current', argument. This is so we can look at the
                // 'Current' argument in relation to the 1st while evaluating
                // the 1st.
                string previousArg = enumerator.MoveNext() ? enumerator.Current : null;

                // If no first arg, return empty dictionary
                if (previousArg == null)
                {
                    Data = data;
                    return;
                }

                while (enumerator.MoveNext())
                {
                    // 'Current' is now the 2nd argument in Args
                    string currentArg = enumerator.Current;
                    // TryProcessArgs will return false if previousArg is invalid
                    if (TryProcessArgs(previousArg, currentArg, out (string key, string value) loopPair))
                    {
                        // Override value when key is duplicated,
                        // so we always have the last argument win.
                        data[loopPair.key] = loopPair.value;
                    }
                    previousArg = currentArg;
                }

                // Process the last previousArg after exiting loop
                if (TryProcessArgs(previousArg, null, out (string key, string value) lastPair))
                {
                    data[lastPair.key] = lastPair.value;
                }
            }

            Data = data;
        }

        /// <summary>
        /// Reconcile two consecutive arguments into a key-value pair for the first.
        /// </summary>
        /// <remarks>
        /// Helper function to reduce repeated code in <see cref="Load"/> method.
        /// </remarks>
        /// <param name="previousArg">The first string argument</param>
        /// <param name="currentArg">The second string argument</param>
        /// <param name="pair">
        /// A properly resolved configuration key-value pair, or null if previous argument is invalid.
        /// </param>
        /// <returns>
        /// True if the args can be resolved to a proper configuration key-value pair.
        /// </returns>
        private bool TryProcessArgs(string previousArg, string currentArg, out (string key, string value) pair)
        {
            string key, value;
            int keyStartIndex = 0;

            if (previousArg.StartsWith("--"))
            {
                keyStartIndex = 2;
            }
            else if (previousArg.StartsWith("-"))
            {
                keyStartIndex = 1;
            }
            else if (previousArg.StartsWith("/")
                     && s_isWindows
                     && previousArg.IndexOf("/", StringComparison.Ordinal)
                     == previousArg.LastIndexOf("/", StringComparison.Ordinal)) // i.e. only one instance of '/'
            {
                // On Windows, "/SomeSwitch" is equivalent to "--SomeSwitch" when interpreting switch mappings
                // So we do a conversion to simplify later processing
                previousArg = $"--{previousArg.Substring(1)}";
                keyStartIndex = 2;
            }

            int separatorIndex = previousArg.IndexOf('=');

            if (separatorIndex < 0)
            {
                // If there is neither equal sign nor prefix in previous argument, it is an invalid format
                if (keyStartIndex == 0)
                {
                    // Ignore invalid formats
                    pair = default;
                    return false;
                }

                // If the switch is a key in given switch mappings, interpret it
                if (_switchMappings != null
                    && _switchMappings.TryGetValue(
                        previousArg,
                        out string mappedKey))
                {
                    key = mappedKey;
                }
                // If the switch starts with a single "-" and it isn't in given mappings,
                // or in any other case, use the switch name directly as a key
                else
                {
                    key = previousArg.Substring(keyStartIndex);
                }

                // If the argument is last in list, the next argument begins
                // with an arg delimiter, or the next argument contains '=',
                // then treat argument as switch and record value of "true"
                if (currentArg == null
                    || currentArg.StartsWith("--")
                    || currentArg.StartsWith("-")
                    || currentArg.StartsWith("/")
                        && s_isWindows
                        && currentArg.IndexOf("/", StringComparison.Ordinal)
                            == currentArg.LastIndexOf("/", StringComparison.Ordinal)
                    || currentArg.Contains("="))
                {
                    value = "true";
                }
                else
                {
                    value = currentArg;
                }
            }
            else
            {
                string keySegment = previousArg.Substring(0, separatorIndex);

                // If the switch is a key in given switch mappings, interpret it
                if (_switchMappings != null
                    && _switchMappings.TryGetValue(
                        keySegment,
                        out string mappedKeySegment))
                {
                    key = mappedKeySegment;
                }
                // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage
                else if (keyStartIndex == 1)
                {
                    throw new FormatException(
                        SR.Format(
                            SR.Error_ShortSwitchNotDefined,
                            previousArg));
                }
                // Otherwise, use the switch name directly as a key
                else
                {
                    key = previousArg.Substring(
                        keyStartIndex,
                        separatorIndex - keyStartIndex);
                }

                value = previousArg.Substring(separatorIndex + 1);
            }

            pair = (key, value);
            return true;
        }

        private Dictionary<string, string> GetValidatedSwitchMappingsCopy(IDictionary<string, string> switchMappings)
        {
            // The dictionary passed in might be constructed with a case-sensitive comparer
            // However, the keys in configuration providers are all case-insensitive
            // So we check whether the given switch mappings contain duplicated keys with case-insensitive comparer
            var switchMappingsCopy = new Dictionary<string, string>(switchMappings.Count, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> mapping in switchMappings)
            {
                // Only keys start with "--" or "-" are acceptable
                if (!mapping.Key.StartsWith("-") && !mapping.Key.StartsWith("--"))
                {
                    throw new ArgumentException(
                        SR.Format(SR.Error_InvalidSwitchMapping, mapping.Key),
                        nameof(switchMappings));
                }

                if (switchMappingsCopy.ContainsKey(mapping.Key))
                {
                    throw new ArgumentException(
                        SR.Format(SR.Error_DuplicatedKeyInSwitchMappings, mapping.Key),
                        nameof(switchMappings));
                }

                switchMappingsCopy.Add(mapping.Key, mapping.Value);
            }

            return switchMappingsCopy;
        }
    }
}

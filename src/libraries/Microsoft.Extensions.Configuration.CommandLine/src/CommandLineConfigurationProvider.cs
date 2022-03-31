// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.CommandLine
{
    /// <summary>
    /// A command line based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class CommandLineConfigurationProvider : ConfigurationProvider
    {
        private readonly Dictionary<string, string>? _switchMappings;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="args">The command line args.</param>
        /// <param name="switchMappings">The switch mappings.</param>
        public CommandLineConfigurationProvider(IEnumerable<string> args!!, IDictionary<string, string>? switchMappings = null)
        {
            Args = args;

            if (switchMappings != null)
            {
                _switchMappings = GetValidatedSwitchMappingsCopy(switchMappings);
            }
        }

        /// <summary>
        /// The command line arguments.
        /// </summary>
        protected IEnumerable<string> Args { get; private set; }

        /// <summary>
        /// Loads the configuration data from the command line args.
        /// </summary>
        public override void Load()
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string key, value;

            using (IEnumerator<string> enumerator = Args.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    string currentArg = enumerator.Current;
                    int keyStartIndex = 0;

                    if (currentArg.StartsWith("--"))
                    {
                        keyStartIndex = 2;
                    }
                    else if (currentArg.StartsWith("-"))
                    {
                        keyStartIndex = 1;
                    }
                    else if (currentArg.StartsWith("/"))
                    {
                        // "/SomeSwitch" is equivalent to "--SomeSwitch" when interpreting switch mappings
                        // So we do a conversion to simplify later processing
                        currentArg = $"--{currentArg.Substring(1)}";
                        keyStartIndex = 2;
                    }

                    int separator = currentArg.IndexOf('=');

                    if (separator < 0)
                    {
                        // If there is neither equal sign nor prefix in current argument, it is an invalid format
                        if (keyStartIndex == 0)
                        {
                            // Ignore invalid formats
                            continue;
                        }

                        // If the switch is a key in given switch mappings, interpret it
                        if (_switchMappings != null && _switchMappings.TryGetValue(currentArg, out string? mappedKey))
                        {
                            key = mappedKey;
                        }
                        // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage so ignore it
                        else if (keyStartIndex == 1)
                        {
                            continue;
                        }
                        // Otherwise, use the switch name directly as a key
                        else
                        {
                            key = currentArg.Substring(keyStartIndex);
                        }

                        string previousKey = enumerator.Current;
                        if (!enumerator.MoveNext())
                        {
                            // ignore missing values
                            continue;
                        }

                        value = enumerator.Current;
                    }
                    else
                    {
                        string keySegment = currentArg.Substring(0, separator);

                        // If the switch is a key in given switch mappings, interpret it
                        if (_switchMappings != null && _switchMappings.TryGetValue(keySegment, out string? mappedKeySegment))
                        {
                            key = mappedKeySegment;
                        }
                        // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage
                        else if (keyStartIndex == 1)
                        {
                            throw new FormatException(SR.Format(SR.Error_ShortSwitchNotDefined, currentArg));
                        }
                        // Otherwise, use the switch name directly as a key
                        else
                        {
                            key = currentArg.Substring(keyStartIndex, separator - keyStartIndex);
                        }

                        value = currentArg.Substring(separator + 1);
                    }

                    // Override value when key is duplicated. So we always have the last argument win.
                    data[key] = value;
                }
            }

            Data = data;
        }

        private static Dictionary<string, string> GetValidatedSwitchMappingsCopy(IDictionary<string, string> switchMappings)
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

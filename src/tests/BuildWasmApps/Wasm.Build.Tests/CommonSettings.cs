using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#nullable enable

namespace Wasm.Build.Tests
{
    internal static class CommonSettings
    {
        private static readonly Dictionary<string, string?> _table = new ();

        static CommonSettings()
        {
            string prefix = "WBT_";
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                string key = (string)de.Key;
                if ((key.Length < prefix.Length + 1) || !key.StartsWith(prefix, ignoreCase: true, CultureInfo.InvariantCulture))
                    continue;

                string name = key.Substring(prefix.Length);
                _table[name] = (string?)de.Value;
            }
        }

        public static bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _table.TryGetValue(key, out value);

        public static bool IsOneOrTrue(string key)
            => _table.TryGetValue(key, out string? value) &&
                                    (value == "1" || value?.ToLower() == "true");

        public static bool TryGetSplitValuesFor(string key, [NotNullWhen(true)] out string[]? values, bool allowEmpty=true)
        {
            values = null;
            if (!_table.TryGetValue(key, out string? envVarValue))
                return false;

            if (!allowEmpty && string.IsNullOrEmpty(envVarValue))
                return false;

            values = envVarValue?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // in case of allow empty
            values ??= new string[0];

            return true;
        }
    }
}

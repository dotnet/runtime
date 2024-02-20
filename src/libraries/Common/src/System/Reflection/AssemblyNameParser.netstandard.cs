// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace System.Reflection
{
    internal ref partial struct AssemblyNameParser
    {
        private static HashSet<string>? _predefinedCultureNames;
        private static readonly object _predefinedCultureNamesLock = new object();

        private static bool IsPredefinedCulture(string cultureName)
        {
            if (_predefinedCultureNames is null)
            {
                lock (_predefinedCultureNamesLock)
                {
                    _predefinedCultureNames ??= GetPredefinedCultureNames();
                }
            }

            return _predefinedCultureNames.Contains(AnsiToLower(cultureName));

            static HashSet<string> GetPredefinedCultureNames()
            {
                HashSet<string> result = new(StringComparer.Ordinal);
                foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                {
                    result.Add(AnsiToLower(culture.Name));
                }
                return result;
            }

            // Like CultureInfo, only maps [A-Z] -> [a-z].
            // All non-ASCII characters are left alone.
            static string AnsiToLower(string input)
            {
                if (input is null)
                {
                    return null;
                }

                int idx;
                for (idx = 0; idx < input.Length; idx++)
                {
                    if (input[idx] is >= 'A' and <= 'Z')
                    {
                        break;
                    }
                }

                if (idx == input.Length)
                {
                    return input; // no characters to change.
                }

                char[] chars = input.ToCharArray();
                for (; idx < chars.Length; idx++)
                {
                    char c = chars[idx];
                    if (input[idx] is >= 'A' and <= 'Z')
                    {
                        chars[idx] = (char)(c | 0x20);
                    }
                }
                return new string(chars);
            }
        }
    }
}

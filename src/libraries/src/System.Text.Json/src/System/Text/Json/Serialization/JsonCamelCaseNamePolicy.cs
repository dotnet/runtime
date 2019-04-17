// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    internal sealed class JsonCamelCaseNamePolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            {
                return name;
            }

            char[] chars = name.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                bool hasNext = (i + 1 < chars.Length);

                // Stop when next char is already lowercase.
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    // If the next char is a space, lowercase current char before exiting.
                    if (chars[i + 1] == ' ')
                    {
                        chars[i] = char.ToLowerInvariant(chars[i]);
                    }

                    break;
                }

                chars[i] = char.ToLowerInvariant(chars[i]);
            }

            return new string(chars);
        }
    }
}

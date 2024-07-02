// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal sealed class JsonPascalCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsLower(name[0]))
            {
                return name;
            }

#if NETCOREAPP
            return string.Create(name.Length, name, (chars, name) =>
            {
                name.CopyTo(chars);
                FixCasing(chars);
            });
#else
            char[] chars = name.ToCharArray();
            FixCasing(chars);
            return new string(chars);
#endif
        }

        private static void FixCasing(Span<char> chars)
        {
            chars[0] = char.ToUpperInvariant(chars[0]);
        }
    }
}

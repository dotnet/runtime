// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal sealed class JsonSnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (name.Length == 1)
            {
                return name.ToLowerInvariant();
            }

            var result = new ValueStringBuilder(2 * name.Length);

            bool wroteUnderscorePreviously = false;
            for (int x = 0; x < name.Length; x++)
            {
                var current = name[x];

                if (x > 0 && x < name.Length - 1 && char.IsLetter(current))
                {
                    // text somewhere in the middle of the string
                    var previous = name[x - 1];
                    var next = name[x + 1];

                    if (char.IsLetter(previous) && char.IsLetter(next))
                    {
                        // in the middle of a bit of text
                        var previousUpper = char.IsUpper(previous);
                        var currentUpper = char.IsUpper(current);
                        var nextUpper = char.IsUpper(next);

                        switch ((previousUpper, currentUpper, nextUpper))
                        {
                            case (false, false, false): // aaa
                            case ( true,  true,  true): // AAA
                            case ( true, false, false): // Aaa
                            {
                                // same word
                                result.Append(char.ToLowerInvariant(current));
                                wroteUnderscorePreviously = false;
                                break;
                            }

                            case (false, false,  true): // aaA
                            case ( true, false,  true): // AaA
                            {
                                // end of word
                                result.Append(char.ToLowerInvariant(current));
                                result.Append('_');
                                wroteUnderscorePreviously = true;
                                break;
                            }

                            case (false,  true,  true): // aAA
                            case ( true,  true, false): // AAa
                            case (false,  true, false): // aAa
                            {
                                // beginning of word
                                if (!wroteUnderscorePreviously)
                                {
                                    result.Append('_');
                                }
                                result.Append(char.ToLowerInvariant(current));
                                wroteUnderscorePreviously = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // beginning or end of text
                        result.Append(char.ToLowerInvariant(current));
                        wroteUnderscorePreviously = false;
                    }
                }
                else if (char.IsLetter(current))
                {
                    // text at the beginning or the end of the string
                    result.Append(char.ToLowerInvariant(current));
                    wroteUnderscorePreviously = false;
                }
                else if (char.IsNumber(current))
                {
                    // a number at any point in the string
                    if (x > 0 && !wroteUnderscorePreviously)
                    {
                        result.Append('_');
                    }

                    result.Append(current);
                    wroteUnderscorePreviously = false;

                    if (x < name.Length - 1)
                    {
                        result.Append('_');
                        wroteUnderscorePreviously = true;
                    }
                }
                else if (!wroteUnderscorePreviously)
                {
                    // any punctuation at any point in the string
                    result.Append('_');
                    wroteUnderscorePreviously = true;
                }
            }

            return result.ToString();
        }
    }
}

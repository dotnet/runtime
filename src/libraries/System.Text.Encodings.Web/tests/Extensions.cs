// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace System.Text.Encodings.Web.Tests
{
    public static class Extensions
    {
        public unsafe static int FindFirstCharacterToEncodeUtf16(this TextEncoder encoder, ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                char dummy = default;
                return encoder.FindFirstCharacterToEncode(&dummy, 0);
            }
            else
            {
                fixed (char* pText = text)
                {
                    return encoder.FindFirstCharacterToEncode(pText, text.Length);
                }
            }
        }

        public static string[] ReadAllLines(this TextReader reader)
        {
            return ReadAllLinesImpl(reader).ToArray();
        }

        private static IEnumerable<string> ReadAllLinesImpl(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}

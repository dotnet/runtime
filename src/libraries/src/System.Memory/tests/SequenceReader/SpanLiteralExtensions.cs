// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Memory.Tests.SequenceReader
{
    public class SpanLiteralExtensions
    {
        internal static void AppendAsLiteral(ReadOnlySpan<byte> span, StringBuilder sb)
        {
            for (int i = 0; i < span.Length; i++)
            {
                AppendCharLiteral((char)span[i], sb);
            }
        }

        internal static void AppendCharLiteral(char c, StringBuilder sb)
        {
            switch (c)
            {
                case '\'':
                    sb.Append(@"\'");
                    break;
                case '\"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append(@"\\");
                    break;
                case '\0':
                    sb.Append(@"\0");
                    break;
                case '\a':
                    sb.Append(@"\a");
                    break;
                case '\b':
                    sb.Append(@"\b");
                    break;
                case '\f':
                    sb.Append(@"\f");
                    break;
                case '\n':
                    sb.Append(@"\n");
                    break;
                case '\r':
                    sb.Append(@"\r");
                    break;
                case '\t':
                    sb.Append(@"\t");
                    break;
                case '\v':
                    sb.Append(@"\v");
                    break;
                default:
                    // ASCII printable character
                    if (!char.IsControl(c))
                    {
                        sb.Append(c);
                        // As UTF16 escaped character
                    }
                    else
                    {
                        sb.Append(@"\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    break;
            }
        }
    }
}

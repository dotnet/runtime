// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Text;

namespace System.Linq.Expressions.Tests
{
    public interface IFormatProvider
    {
        string Int32ToHex(int int32);
        string Int16ToHex(int int16);
        string Int8ToHex(int int8);
        string Argument(int ordinal);
        string EscapedString(string str);
        string Label(int offset);
        string MultipleLabels(int[] offsets);
        string SigByteArrayToString(byte[] sig);
    }

    public class DefaultFormatProvider : IFormatProvider
    {
        private DefaultFormatProvider() { }

        public static readonly DefaultFormatProvider Instance = new DefaultFormatProvider();

        public virtual string Int32ToHex(int int32) => int32.ToString("X8");
        public virtual string Int16ToHex(int int16) => int16.ToString("X4");
        public virtual string Int8ToHex(int int8) => int8.ToString("X2");
        public virtual string Argument(int ordinal) => string.Format("V_{0}", ordinal);
        public virtual string Label(int offset) => string.Format("IL_{0:x4}", offset);

        public virtual string MultipleLabels(int[] offsets)
        {
            var sb = new StringBuilder();
            int length = offsets.Length;
            for (int i = 0; i < length; i++)
            {
                if (i == 0) sb.Append('(');
                else sb.Append(", ");
                sb.Append(Label(offsets[i]));
            }
            sb.Append(')');
            return sb.ToString();
        }

        public virtual string EscapedString(string str)
        {
            int length = str.Length;
            var sb = new StringBuilder(length * 2);
            for (int i = 0; i < length; i++)
            {
                char ch = str[i];
                if (ch == '\t') sb.Append("\\t");
                else if (ch == '\n') sb.Append("\\n");
                else if (ch == '\r') sb.Append("\\r");
                else if (ch == '\"') sb.Append("\\\"");
                else if (ch == '\\') sb.Append("\\");
                else if (ch < 0x20 || ch >= 0x7f) sb.Append($"\\u{(int)ch:x4}");
                else sb.Append(ch);
            }
            return "\"" + sb.ToString() + "\"";
        }

        public virtual string SigByteArrayToString(byte[] sig)
        {
            var sb = new StringBuilder();
            int length = sig.Length;
            for (int i = 0; i < length; i++)
            {
                if (i == 0) sb.Append("SIG [");
                else sb.Append(' ');
                sb.Append(Int8ToHex(sig[i]));
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}

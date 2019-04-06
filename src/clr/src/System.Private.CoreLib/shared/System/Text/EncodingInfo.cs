// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Text
{
    public sealed class EncodingInfo
    {
        internal EncodingInfo(int codePage, string name, string displayName)
        {
            CodePage = codePage;
            Name = name;
            DisplayName = displayName;
        }

        public int CodePage { get; }
        public string Name { get; }
        public string DisplayName { get; }

        public Encoding GetEncoding()
        {
            return Encoding.GetEncoding(CodePage);
        }

        public override bool Equals(object? value)
        {
            if (value is EncodingInfo that)
            {
                return this.CodePage == that.CodePage;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return CodePage;
        }
    }
}

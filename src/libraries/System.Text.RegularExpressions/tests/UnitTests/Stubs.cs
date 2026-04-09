// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexReplacement
    {
        public RegexReplacement(string rep, RegexNode concat, Hashtable caps) { }

        public const int LeftPortion = -1;
        public const int RightPortion = -2;
        public const int LastGroup = -3;
        public const int WholeString = -4;
    }
}

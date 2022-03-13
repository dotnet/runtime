// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    internal static class StringExtensions
    {
        internal static bool HasCharAt(this string value, int index, char character)
        {
            return index < value.Length && value[index] == character;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static int OffsetToStringData
        {
            get
            {
                // Number of bytes from the address pointed to by a reference to
                // a String to the first 16-bit character in the String.
                // This property allows C#'s fixed statement to work on Strings.
                return string.FIRST_CHAR_OFFSET;
            }
        }
    }
}

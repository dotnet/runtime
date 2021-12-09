// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    // Use this in P/Direct function prototypes to specify
    // which character set to use when marshalling Strings.
    // Using Ansi will marshal the strings as 1 byte char*'s.
    // Using Unicode will marshal the strings as 2 byte wchar*'s.
    // Generally you probably want to use Auto, which does the
    // right thing 99% of the time.

    public enum CharSet
    {
        None = 1,       // User didn't specify how to marshal strings.
        Ansi = 2,       // Strings should be marshalled as ANSI 1 byte chars.
        Unicode = 3,    // Strings should be marshalled as Unicode 2 byte chars.
        Auto = 4,       // Marshal Strings in the right way for the target system.
    }
}

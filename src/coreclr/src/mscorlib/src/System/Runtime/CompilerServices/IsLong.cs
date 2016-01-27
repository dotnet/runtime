// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    // The C++ standard indicates that a long is always 4-bytes, whereas the
    // size of an integer is system dependent (not exceedign sizeof(long)).
    // The CLR does not offer a mechanism for encoding this distinction,
    // but it is critically important for maintaining language level type
    // safety.
    //
    // Indicates that the modified integer is a standard C++ long.
    // Could also be called IsAlternateIntegerType or something else.
    public static class IsLong
    {
    }
}

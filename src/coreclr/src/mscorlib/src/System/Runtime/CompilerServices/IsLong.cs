// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

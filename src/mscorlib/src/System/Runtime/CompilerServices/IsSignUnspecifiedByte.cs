// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.CompilerServices
{
    // C++ recognizes three char types: signed char, unsigned char, and char.
    // When a char is neither signed nor unsigned, it is a char.
    // This modopt indicates that the modified instance is a char.
    //
    // Any compiler could use this to indicate that the user has not specified
    // Sign behavior for the given byte.
    public static class IsSignUnspecifiedByte 
    {
    }
}

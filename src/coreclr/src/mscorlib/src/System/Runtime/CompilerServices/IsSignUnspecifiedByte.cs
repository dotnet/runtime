// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

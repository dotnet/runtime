// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection
{
    public static class MemberInfoExtensions
    {
        public static bool HasMetadataToken(this MemberInfo member)
        {
            Requires.NotNull(member, "member");
            return false; // never available on .NET Native
        }
        
        public static int GetMetadataToken(this MemberInfo member)
        {
            Requires.NotNull(member, "member");
            throw new InvalidOperationException(SR.MetadataTokenNotSupported);
        }
    }
}

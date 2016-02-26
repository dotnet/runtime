// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    public static class MemberInfoExtensions
    {
        public static bool HasMetadataToken(this MemberInfo member)
        {
            Requires.NotNull(member, nameof(member));
            return false; // never available on .NET Native
        }
        
        public static int GetMetadataToken(this MemberInfo member)
        {
            Requires.NotNull(member, nameof(member));
            throw new InvalidOperationException(SR.MetadataTokenNotSupported);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public static partial class CastingHelper
    {
        private static bool IsCanonicalCastTarget(TypeDesc thisType, TypeDesc otherType)
            => false;

        private static bool IsCanonicalTypeArgMatch(TypeDesc type, TypeDesc otherType)
            => false;

        private static bool IsCanonEquivalent(TypeDesc thisType, TypeDesc otherType)
            => false;
    }
}

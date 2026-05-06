// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public abstract partial class MemberInfo
    {
        internal bool HasSameMetadataDefinitionAsCore<TOther>(MemberInfo other) where TOther : MemberInfo
        {
            ArgumentNullException.ThrowIfNull(other);

            // Ensure that "other" is a runtime-implemented MemberInfo. Do this check before calling any methods on it!
            if (!(other is TOther))
                return false;

            if (MetadataToken != other.MetadataToken)
                return false;

            if (!(Module.Equals(other.Module)))
                return false;

            return true;
        }
    }
}

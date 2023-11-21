// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Property, Inherited=false, AllowMultiple=true)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        sealed class FeatureGuardAttribute : Attribute
    {
        public Type RequiresAttributeType { get; }

        public FeatureGuardAttribute(Type requiresAttributeType)
        {
            RequiresAttributeType = requiresAttributeType;
        }
    }
}

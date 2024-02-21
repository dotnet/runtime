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
        sealed class FeatureCheckAttribute : Attribute
    {
        public Type FeatureType { get; }

        public FeatureCheckAttribute(Type featureType)
        {
            FeatureType = featureType;
        }
    }
}

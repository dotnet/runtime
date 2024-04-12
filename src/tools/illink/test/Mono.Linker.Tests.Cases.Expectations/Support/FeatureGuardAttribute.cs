// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    // Allow AttributeTargets.Method for testing invalid usages of a custom FeatureGuardAttribute
    [AttributeUsage (AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class FeatureGuardAttribute : Attribute
    {
        public Type FeatureType { get; }

        public FeatureGuardAttribute (Type featureType)
        {
            FeatureType = featureType;
        }
    }
}

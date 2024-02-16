// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    // Allow AttributeTargets.Method for testing invalid usages of a custom FeatureCheckAttribute
    [AttributeUsage (AttributeTargets.Property | AttributeTargets.Method, Inherited=false)]
    public sealed class FeatureCheckAttribute : Attribute
    {
        public Type FeatureType { get; }

        public FeatureCheckAttribute (Type featureType)
        {
            FeatureType = featureType;
        }
    }
}

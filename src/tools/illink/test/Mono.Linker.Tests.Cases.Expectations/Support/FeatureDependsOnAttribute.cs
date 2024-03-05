// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=true)]
    public sealed class FeatureDependsOnAttribute : Attribute
    {
        public Type FeatureType { get; }

        public FeatureDependsOnAttribute(Type featureType)
        {
            FeatureType = featureType;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that the specified public static boolean get-only property
    /// guards access to the specified feature.
    /// </summary>
    /// <remarks>
    /// Analyzers can use this to prevent warnings on calls to code that is
    /// annotated as requiring that feature, when the callsite is guarded by a
    /// call to the property.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        sealed class FeatureGuardAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureGuardAttribute"/> class
        /// with the specified feature type.
        /// </summary>
        /// <param name="featureType">
        /// The type that represents the feature guarded by the property.
        /// </param>
        public FeatureGuardAttribute(Type featureType)
        {
            FeatureType = featureType;
        }

        /// <summary>
        /// The type that represents the feature guarded by the property.
        /// </summary>
        public Type FeatureType { get; }
    }
}

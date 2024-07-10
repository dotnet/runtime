// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that the specified public static boolean get-only property
    /// corresponds to the feature switch specified by name.
    /// </summary>
    /// <remarks>
    /// IL rewriters and compilers can use this to substitute the return value
    /// of the specified property with the value of the feature switch.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        sealed class FeatureSwitchDefinitionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureSwitchDefinitionAttribute"/> class
        /// with the specified feature switch name.
        /// </summary>
        /// <param name="switchName">
        /// The name of the feature switch that provides the value for the specified property.
        /// </param>
        public FeatureSwitchDefinitionAttribute(string switchName)
        {
            SwitchName = switchName;
        }

        /// <summary>
        /// The name of the feature switch that provides the value for the specified property.
        /// </summary>
        public string SwitchName { get; }
    }
}

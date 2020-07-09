// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Versioning
{
    /// <summary>
    /// Base type for all platform-specific API attributes.
    /// </summary>
#pragma warning disable CS3015 // Type has no accessible constructors which use only CLS-compliant types
    public abstract class OSPlatformAttribute : Attribute
#pragma warning restore CS3015
    {
        private protected OSPlatformAttribute(string platformName)
        {
            PlatformName = platformName;
        }
        public string PlatformName { get; }
    }
}

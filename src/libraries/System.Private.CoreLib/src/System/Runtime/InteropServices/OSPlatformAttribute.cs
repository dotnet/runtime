// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Base type for all platform-specific API attributes.
    /// </summary>
    public abstract class OSPlatformAttribute : Attribute
    {
        private protected OSPlatformAttribute(string platformName)
        {
            PlatformName = platformName;
        }
        public string PlatformName { get; }
    }
}

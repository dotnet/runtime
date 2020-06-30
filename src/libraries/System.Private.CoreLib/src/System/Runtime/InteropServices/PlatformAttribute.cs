// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Base type for all platform-specific attributes. Primarily used to allow grouping
    // in documentation.
    public abstract class PlatformAttribute : Attribute
    {
        internal PlatformAttribute(string platformName)
        {
            PlatformName = platformName;
        }
        public string PlatformName { get; }
    }
}

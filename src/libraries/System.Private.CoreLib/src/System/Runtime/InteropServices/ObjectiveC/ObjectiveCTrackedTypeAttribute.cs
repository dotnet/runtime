// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// Attribute used to indicate a class represents a tracked Objective-C type.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("macos")]
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class ObjectiveCTrackedTypeAttribute : Attribute
    {
        /// <summary>
        /// Instantiate a <see cref="ObjectiveCTrackedTypeAttribute"/> instance.
        /// </summary>
        public ObjectiveCTrackedTypeAttribute() { }
    }
}
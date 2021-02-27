// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// Attribute used to indicate a class is tracked from the native environment.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [AttributeUsage(AttributeTargets.Class)]
    public class TrackedNativeReferenceAttribute : Attribute
    {
        /// <summary>
        /// Instantiate a <see cref="TrackedNativeReferenceAttribute"/> instance.
        /// </summary>
        public TrackedNativeReferenceAttribute() { }
    }
}
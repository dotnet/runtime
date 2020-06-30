// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Marks APIs that were removed in a given operating system version.
    //
    // Primarily used by OS bindings to indicate APIs that are only available in
    // earlier versions.
    [AttributeUsage(AttributeTargets.Assembly |
                    AttributeTargets.Class |
                    AttributeTargets.Constructor |
                    AttributeTargets.Event |
                    AttributeTargets.Method |
                    AttributeTargets.Module |
                    AttributeTargets.Property |
                    AttributeTargets.Struct,
                    AllowMultiple = true, Inherited = false)]
    public sealed class RemovedInPlatformAttribute : PlatformAttribute
    {
        public RemovedInPlatformAttribute(string platformName) : base(platformName)
        {
        }
    }
}

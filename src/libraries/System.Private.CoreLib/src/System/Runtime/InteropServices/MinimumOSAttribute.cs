// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Records the minimum platform that is required in order to the marked thing.
    //
    // * When applied to an assembly, it means the entire assembly cannot be called
    //   into on earlier versions. It records the TargetPlatformMinVersion property.
    //
    // * When applied to an API, it means the API cannot be called from an earlier
    //   version.
    //
    // In either case, the caller can either mark itself with MinimumPlatformAttribute
    // or guard the call with a platform check.
    //
    // The attribute can be applied multiple times for different operating systems.
    // That means the API is supported on multiple operating systems.
    //
    // A given platform should only be specified once.

    [AttributeUsage(AttributeTargets.Assembly |
                    AttributeTargets.Class |
                    AttributeTargets.Constructor |
                    AttributeTargets.Event |
                    AttributeTargets.Method |
                    AttributeTargets.Module |
                    AttributeTargets.Property |
                    AttributeTargets.Struct,
                    AllowMultiple = true, Inherited = false)]
    public sealed class MinimumOSAttribute : PlatformAttribute
    {
        public MinimumOSAttribute(string platformName) : base(platformName)
        {
        }
    }
}

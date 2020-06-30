// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Marks APIs that were obsoleted in a given operating system version.
    //
    // Primarily used by OS bindings to indicate APIs that should only be used in
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
    public sealed class ObsoletedInPlatformAttribute : PlatformAttribute
    {
        public ObsoletedInPlatformAttribute(string platformName, string url, string message) : base(platformName)
        {
            Url = url;
            Message = message;
        }

        public string Url { get; set; }
        public string Message { get; }
    }
}

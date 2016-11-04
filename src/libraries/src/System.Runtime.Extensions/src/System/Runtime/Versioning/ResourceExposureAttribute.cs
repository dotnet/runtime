// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Constructor, Inherited = false)]
    [Conditional("RESOURCE_ANNOTATION_WORK")]
    public sealed class ResourceExposureAttribute : Attribute
    {
        public ResourceScope ResourceExposureLevel { get; }

        public ResourceExposureAttribute(ResourceScope exposureLevel)
        {
            ResourceExposureLevel = exposureLevel;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor, Inherited = false)]
    [Conditional("RESOURCE_ANNOTATION_WORK")]
    public sealed class ResourceConsumptionAttribute : Attribute
    {
        public ResourceScope ResourceScope { get; }
        public ResourceScope ConsumptionScope { get; }

        public ResourceConsumptionAttribute(ResourceScope resourceScope)
        {
            ResourceScope = resourceScope;
            ConsumptionScope = resourceScope;
        }

        public ResourceConsumptionAttribute(ResourceScope resourceScope, ResourceScope consumptionScope)
        {
            ResourceScope = resourceScope;
            ConsumptionScope = consumptionScope;
        }
    }
}

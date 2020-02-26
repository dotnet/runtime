// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class IEnumerableCallSite : ServiceCallSite
    {
        internal Type ItemType { get; }
        internal ServiceCallSite[] ServiceCallSites { get; }

        public IEnumerableCallSite(ResultCache cache, Type itemType, ServiceCallSite[] serviceCallSites) : base(cache)
        {
            ItemType = itemType;
            ServiceCallSites = serviceCallSites;
        }

        public override Type ServiceType => typeof(IEnumerable<>).MakeGenericType(ItemType);
        public override Type ImplementationType  => ItemType.MakeArrayType();
        public override CallSiteKind Kind { get; } = CallSiteKind.IEnumerable;
    }
}

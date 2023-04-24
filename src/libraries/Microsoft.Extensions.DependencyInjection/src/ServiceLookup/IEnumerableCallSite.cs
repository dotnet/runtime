// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class IEnumerableCallSite : ServiceCallSite
    {
        internal Type ItemType { get; }
        internal ServiceCallSite[] ServiceCallSites { get; }

        public IEnumerableCallSite(ResultCache cache, Type itemType, ServiceCallSite[] serviceCallSites) : base(cache)
        {
            Debug.Assert(!ServiceProvider.VerifyAotCompatibility || !itemType.IsValueType, "If VerifyAotCompatibility=true, an IEnumerableCallSite should not be created with a ValueType.");

            ItemType = itemType;
            ServiceCallSites = serviceCallSites;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "When ServiceProvider.VerifyAotCompatibility is true, which it is by default when PublishAot=true, " +
            "CallSiteFactory ensures ItemType is not a ValueType.")]
        public override Type ServiceType => typeof(IEnumerable<>).MakeGenericType(ItemType);

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "When ServiceProvider.VerifyAotCompatibility is true, which it is by default when PublishAot=true, " +
            "CallSiteFactory ensures ItemType is not a ValueType.")]
        public override Type ImplementationType => ItemType.MakeArrayType();

        public override CallSiteKind Kind { get; } = CallSiteKind.IEnumerable;
    }
}

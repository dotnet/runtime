// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    /// <summary>
    /// Summary description for ServiceCallSite
    /// </summary>
    internal abstract class ServiceCallSite
    {
        protected ServiceCallSite(ResultCache cache)
        {
            Cache = cache;
        }

        public abstract Type ServiceType { get; }
        public abstract Type ImplementationType { get; }
        public abstract CallSiteKind Kind { get; }
        public ResultCache Cache { get; }

        public bool CaptureDisposable =>
            ImplementationType == null ||
            typeof(IDisposable).IsAssignableFrom(ImplementationType) ||
            typeof(IAsyncDisposable).IsAssignableFrom(ImplementationType);
    }
}

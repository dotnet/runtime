// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    /// <summary>
    /// Summary description for IServiceCallSite
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

        public bool CaptureDisposable
        {
            get
            {
                return ImplementationType == null ||
                       typeof(IDisposable).GetTypeInfo().IsAssignableFrom(ImplementationType.GetTypeInfo())
#if DISPOSE_ASYNC
                       || typeof(IAsyncDisposable).GetTypeInfo().IsAssignableFrom(ImplementationType.GetTypeInfo())
#endif
                    ;
            }
        }
    }
}

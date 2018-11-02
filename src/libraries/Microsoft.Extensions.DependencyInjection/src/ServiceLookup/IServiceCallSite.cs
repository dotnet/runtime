// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    /// <summary>
    /// Summary description for IServiceCallSite
    /// </summary>
    internal interface IServiceCallSite
    {
        Type ServiceType { get; }
        Type ImplementationType { get; }
        CallSiteKind Kind { get; }
    }
}
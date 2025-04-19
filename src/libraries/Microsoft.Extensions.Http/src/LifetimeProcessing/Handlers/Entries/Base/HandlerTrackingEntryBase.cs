// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.LifetimeProcessing.Handlers.Entries.Base
{
    internal abstract class HandlerTrackingEntryBase
    {
        protected HandlerTrackingEntryBase(
            string name,
            IServiceScope? scope)
        {
            Name = name;
            Scope = scope;
        }

        public string Name { get; }

        public IServiceScope? Scope { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    // Internal contract for configuration sources whose providers need an upstream snapshot at
    // build time. Implementations must throw from IConfigurationSource.Build(IConfigurationBuilder)
    // and are constructed exclusively through Build(IConfigurationBuilder, IReadOnlyList&lt;IConfigurationProvider&gt;).
    // ConfigurationBuilder.Build and ConfigurationManager dispatch through this interface to
    // hand the source the providers that already exist at its declaration point.
    internal interface IContextualConfigurationSource : IConfigurationSource
    {
        IConfigurationProvider Build(IConfigurationBuilder builder, IReadOnlyList<IConfigurationProvider> previousProviders);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.DotEnv;

/// <summary>
/// Represents a .env file as an <see cref="IConfigurationSource"/>.
/// </summary>
public sealed class DotEnvStreamConfigurationSource : StreamConfigurationSource
{
    /// <summary>
    /// Builds the <see cref="DotEnvStreamConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="DotEnvStreamConfigurationProvider"/>.</returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
        => new DotEnvStreamConfigurationProvider(this);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.DotEnv;

/// <summary>
/// Represents a .env file as an <see cref="IConfigurationSource"/>.
/// </summary>
public sealed class DotEnvConfigurationSource : FileConfigurationSource
{
    /// <summary>
    /// Builds the <see cref="DotEnvConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="DotEnvConfigurationProvider"/>.</returns>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new DotEnvConfigurationProvider(this);
    }
}

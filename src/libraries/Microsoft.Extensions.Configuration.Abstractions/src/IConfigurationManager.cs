// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Represents a mutable configuration object.
/// </summary>
/// <remarks>
/// It is both an <see cref="IConfigurationBuilder"/> and an <see cref="IConfiguration"/>.
/// As sources are added, it updates its current view of configuration.
/// </remarks>
public interface IConfigurationManager : IConfiguration, IConfigurationBuilder
{
}

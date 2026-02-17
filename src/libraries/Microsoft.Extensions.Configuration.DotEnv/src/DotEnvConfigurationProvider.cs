// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Configuration.DotEnv;

/// <summary>
/// Provides configuration key-value pairs that are obtained from a .env file.
/// </summary>
/// <remarks>
/// Initializes a new instance with the specified source.
/// </remarks>
/// <param name="source">The source settings.</param>
public sealed class DotEnvConfigurationProvider(DotEnvConfigurationSource source) : FileConfigurationProvider(source)
{
    /// <summary>
    /// Loads the .env data from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    public override void Load(Stream stream)
    {
        try
        {
            Data = DotEnvConfigurationFileParser.Parse(stream);
        }
        catch (Exception e)
        {
            throw new FormatException("Could not parse the .env file.", e);
        }
    }
}

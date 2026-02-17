// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Provides extension methods for registering <see cref="DotEnvConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class DotEnvExtensions
{
    /// <summary>
    /// Adds the default .env configuration provider to <paramref name="builder"/>.
    /// Looks for a ".env" file in the application's base directory.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder)
        => builder.AddDotEnvFile(".env", optional: true);

    /// <summary>
    /// Adds the .env configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in 
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string path)
        => builder.AddDotEnvFile(provider: null, path: path, optional: false, reloadOnChange: false);

    /// <summary>
    /// Adds the .env configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in 
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string path, bool optional)
        => builder.AddDotEnvFile(provider: null, path: path, optional: optional, reloadOnChange: false);

    /// <summary>
    /// Adds the .env configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in 
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        => builder.AddDotEnvFile(provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);

    /// <summary>
    /// Adds a .env configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
    /// <param name="path">Path relative to the base path stored in 
    /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional, bool reloadOnChange)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(path);

        return builder.AddDotEnvFile(s =>
        {
            s.FileProvider = provider;
            s.Path = path;
            s.Optional = optional;
            s.ReloadOnChange = reloadOnChange;
            s.ResolveFileProvider();
        });
    }

    /// <summary>
    /// Adds a .env configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="configureSource">Configures the source.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, Action<DotEnvConfigurationSource>? configureSource)
        => builder.Add(configureSource);

    /// <summary>
    /// Adds a .env configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="stream">The <see cref="Stream"/> to read the .env configuration data from.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddDotEnvStream(this IConfigurationBuilder builder, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(stream);

        return builder.Add<DotEnvStreamConfigurationSource>(s => s.Stream = stream);
    }
}

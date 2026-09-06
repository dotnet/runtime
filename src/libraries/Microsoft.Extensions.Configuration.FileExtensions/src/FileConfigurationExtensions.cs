// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides extension methods for <see cref="FileConfigurationProvider"/>.
    /// </summary>
    public static class FileConfigurationExtensions
    {
        private const string FileProviderKey = "FileProvider";
        private const string FileLoadExceptionHandlerKey = "FileLoadExceptionHandler";

        /// <summary>
        /// Sets the default <see cref="IFileProvider"/> to be used for file-based providers.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="fileProvider">The default file provider instance.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder SetFileProvider(this IConfigurationBuilder builder, IFileProvider fileProvider)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(fileProvider);

            builder.Properties[FileProviderKey] = fileProvider;
            return builder;
        }

        /// <summary>
        /// Gets the default <see cref="IFileProvider"/> to be used for file-based providers.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>The default <see cref="IFileProvider"/>.</returns>
        public static IFileProvider GetFileProvider(this IConfigurationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.GetFileProvider(out _);
        }

        /// <summary>
        /// Gets the default <see cref="IFileProvider"/> to be used for file-based providers, reporting the
        /// <see cref="PhysicalFileProvider"/> created when <paramref name="builder"/> doesn't have one configured.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <param name="created">
        /// When this method returns, contains the file provider created by this call and therefore safe to dispose,
        /// or <see langword="null"/> if the returned file provider was supplied by the caller.
        /// </param>
        /// <returns>The default <see cref="IFileProvider"/>.</returns>
        internal static IFileProvider GetFileProvider(this IConfigurationBuilder builder, out PhysicalFileProvider? created)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (builder.Properties.TryGetValue(FileProviderKey, out object? provider))
            {
                created = null;
                return (IFileProvider)provider;
            }

            return created = new PhysicalFileProvider(AppContext.BaseDirectory ?? string.Empty);
        }

        /// <summary>
        /// Sets the FileProvider for file-based providers to a PhysicalFileProvider with the base path.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="basePath">The absolute path of file-based providers.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder SetBasePath(this IConfigurationBuilder builder, string basePath)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(basePath);

            return builder.SetFileProvider(new PhysicalFileProvider(basePath));
        }

        /// <summary>
        /// Sets a default action to be invoked for file-based providers when an error occurs.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="handler">The Action to be invoked on a file load exception.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder SetFileLoadExceptionHandler(this IConfigurationBuilder builder, Action<FileLoadExceptionContext> handler)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Properties[FileLoadExceptionHandlerKey] = handler;
            return builder;
        }

        /// <summary>
        /// Gets a default action to be invoked for file-based providers when an error occurs.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>The The Action to be invoked on a file load exception, if set.</returns>
        public static Action<FileLoadExceptionContext>? GetFileLoadExceptionHandler(this IConfigurationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (builder.Properties.TryGetValue(FileLoadExceptionHandlerKey, out object? handler))
            {
                return handler as Action<FileLoadExceptionContext>;
            }
            return null;
        }
    }
}

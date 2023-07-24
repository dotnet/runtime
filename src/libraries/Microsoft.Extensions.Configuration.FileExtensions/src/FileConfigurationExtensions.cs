// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="FileConfigurationProvider"/>.
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
            ThrowHelper.ThrowIfNull(builder);
            ThrowHelper.ThrowIfNull(fileProvider);

            builder.Properties[FileProviderKey] = fileProvider;
            return builder;
        }

        internal static IFileProvider? GetUserDefinedFileProvider(this IConfigurationBuilder builder)
            => builder.Properties.TryGetValue(FileProviderKey, out object? provider) ? (IFileProvider)provider : null;

        /// <summary>
        /// Gets the default <see cref="IFileProvider"/> to be used for file-based providers.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>The default <see cref="IFileProvider"/>.</returns>
        public static IFileProvider GetFileProvider(this IConfigurationBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);

            return GetUserDefinedFileProvider(builder) ?? new PhysicalFileProvider(AppContext.BaseDirectory ?? string.Empty);
        }

        /// <summary>
        /// Sets the FileProvider for file-based providers to a PhysicalFileProvider with the base path.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="basePath">The absolute path of file-based providers.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder SetBasePath(this IConfigurationBuilder builder, string basePath)
        {
            ThrowHelper.ThrowIfNull(builder);
            ThrowHelper.ThrowIfNull(basePath);

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
            ThrowHelper.ThrowIfNull(builder);

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
            ThrowHelper.ThrowIfNull(builder);

            if (builder.Properties.TryGetValue(FileLoadExceptionHandlerKey, out object? handler))
            {
                return handler as Action<FileLoadExceptionContext>;
            }
            return null;
        }
    }
}

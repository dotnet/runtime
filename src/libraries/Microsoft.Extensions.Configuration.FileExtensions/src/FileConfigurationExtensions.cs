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
        /// <remarks>The caller retains ownership of <paramref name="fileProvider"/>.</remarks>
        public static IConfigurationBuilder SetFileProvider(this IConfigurationBuilder builder, IFileProvider fileProvider)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(fileProvider);

            SetFileProviderProperty(builder, fileProvider);
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

            if (builder.Properties.TryGetValue(FileProviderKey, out object? provider))
            {
                return provider is FileProviderHandle handle
                    ? handle.GetOrCreate()
                    : (IFileProvider)provider;
            }

            return new PhysicalFileProvider(AppContext.BaseDirectory ?? string.Empty);
        }

        internal static FileProviderHandle GetFileProviderHandle(this IConfigurationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (builder.Properties.TryGetValue(FileProviderKey, out object? provider))
            {
                return provider is FileProviderHandle handle
                    ? handle
                    : FileProviderHandle.CreateBorrowed((IFileProvider)provider);
            }

            return FileProviderHandle.CreateOwned(
                new PhysicalFileProvider(AppContext.BaseDirectory ?? string.Empty));
        }

        /// <summary>
        /// Sets the FileProvider for file-based providers to a PhysicalFileProvider with the base path.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="basePath">The absolute path of file-based providers.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <remarks>
        /// The physical file provider created by this method is disposed once no configuration providers are using it.
        /// </remarks>
        public static IConfigurationBuilder SetBasePath(this IConfigurationBuilder builder, string basePath)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(basePath);

            SetFileProviderProperty(
                builder,
                FileProviderHandle.CreateOwned(new PhysicalFileProvider(basePath)));
            return builder;
        }

        private static void SetFileProviderProperty(IConfigurationBuilder builder, object fileProvider)
        {
            builder.Properties.TryGetValue(FileProviderKey, out object? previous);
            try
            {
                builder.Properties[FileProviderKey] = fileProvider;
            }
            finally
            {
                (previous as FileProviderHandle)?.DiscardIfIdle();
            }
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

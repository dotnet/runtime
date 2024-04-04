// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a base class for file based <see cref="IConfigurationSource"/>.
    /// </summary>
    public abstract class FileConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Used to access the contents of the file.
        /// </summary>
        public IFileProvider? FileProvider { get; set; }

        /// <summary>
        /// Set to true when <see cref="FileProvider"/> was not provided by user and can be safely disposed.
        /// </summary>
        internal bool OwnsFileProvider { get; private set; }

        /// <summary>
        /// The path to the file.
        /// </summary>
        [DisallowNull]
        public string? Path { get; set; }

        /// <summary>
        /// Determines if loading the file is optional.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Determines whether the source will be loaded if the underlying file changes.
        /// </summary>
        public bool ReloadOnChange { get; set; }

        /// <summary>
        /// Number of milliseconds that reload will wait before calling Load.  This helps
        /// avoid triggering reload before a file is completely written. Default is 250.
        /// </summary>
        public int ReloadDelay { get; set; } = 250;

        /// <summary>
        /// Will be called if an uncaught exception occurs in FileConfigurationProvider.Load.
        /// </summary>
        public Action<FileLoadExceptionContext>? OnLoadException { get; set; }

        /// <summary>
        /// Builds the <see cref="IConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="IConfigurationProvider"/></returns>
        public abstract IConfigurationProvider Build(IConfigurationBuilder builder);

        /// <summary>
        /// Called to use any default settings on the builder like the FileProvider or FileLoadExceptionHandler.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        public void EnsureDefaults(IConfigurationBuilder builder)
        {
            // There are two in-box implementations of IConfigurationBuilder: ConfigurationBuilder and ConfigurationManager.
            // 1) ConfigurationBuilder.Build takes a list of IConfigurationSources and builds IConfigurationProvider from them.
            // IConfigurationProvider abstraction does not expose a reference to IConfigurationSources, but most of it implementations do.
            // ConfigurationBuilder.Build creates an instance of ConfigurationRoot by passing it just the list of providers (NOT the sources).
            // ConfigurationRoot implements IDisposable and it has only a list of providers (no sources).
            // When it gets disposed, all providers AND sources that were used to create the providers should be disposed.
            // This is why FileConfigurationProvider disposes the source when it knows that it's not owned by the user.
            // 2) ConfigurationManager is also IDisposable, but it has references both sources and providers.
            // When sources change, it creates new providers and disposes the old ones.
            // It must not dispose the sources! That is why, in such scenario OwnsFileProvider is set to false.
            if (builder is IConfigurationManager)
            {
                OwnsFileProvider = false;
            }
            else if (FileProvider is null && builder.GetUserDefinedFileProvider() is null)
            {
                OwnsFileProvider = true;
            }

            FileProvider ??= builder.GetFileProvider();
            OnLoadException ??= builder.GetFileLoadExceptionHandler();
        }

        /// <summary>
        /// If no file provider has been set, for absolute Path, this will creates a physical file provider
        /// for the nearest existing directory.
        /// </summary>
        public void ResolveFileProvider()
        {
            if (FileProvider == null &&
                !string.IsNullOrEmpty(Path) &&
                System.IO.Path.IsPathRooted(Path))
            {
                string? directory = System.IO.Path.GetDirectoryName(Path);
                string? pathToFile = System.IO.Path.GetFileName(Path);
                while (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    pathToFile = System.IO.Path.Combine(System.IO.Path.GetFileName(directory), pathToFile);
                    directory = System.IO.Path.GetDirectoryName(directory);
                }
                if (Directory.Exists(directory))
                {
                    OwnsFileProvider = true;
                    FileProvider = new PhysicalFileProvider(directory);
                    Path = pathToFile;
                }
            }
        }
    }
}

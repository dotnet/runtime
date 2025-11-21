// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides the base class for file-based <see cref="IConfigurationSource"/>.
    /// </summary>
    public abstract class FileConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Gets or sets the provider used to access the contents of the file.
        /// </summary>
        public IFileProvider? FileProvider { get; set; }

        /// <summary>
        /// Gets or sets the path to the file.
        /// </summary>
        [DisallowNull]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether loading the file is optional.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the source will be loaded if the underlying file changes.
        /// </summary>
        public bool ReloadOnChange { get; set; }

        /// <summary>
        /// Gets or sets the number of milliseconds that reload will wait before calling Load.
        /// </summary>
        /// <value>
        /// The number of milliseconds that reload waits before calling Load. The default is 250.
        /// </value>
        /// <remarks>
        /// This delay helps avoid triggering reload before a file is completely written.
        /// </remarks>
        public int ReloadDelay { get; set; } = 250;

        /// <summary>
        /// Gets or sets the action that's called if an uncaught exception occurs in FileConfigurationProvider.Load.
        /// </summary>
        public Action<FileLoadExceptionContext>? OnLoadException { get; set; }

        /// <summary>
        /// Builds the <see cref="IConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>To be added.</returns>
        public abstract IConfigurationProvider Build(IConfigurationBuilder builder);

        /// <summary>
        /// Called to use any default settings on the builder like the FileProvider or FileLoadExceptionHandler.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        public void EnsureDefaults(IConfigurationBuilder builder)
        {
            FileProvider ??= builder.GetFileProvider();
            OnLoadException ??= builder.GetFileLoadExceptionHandler();
        }

        /// <summary>
        /// Creates a physical file provider for the nearest existing directory if no file provider has been set, for absolute Path.
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
                    FileProvider = new PhysicalFileProvider(directory);
                    Path = pathToFile;
                }
            }
        }
    }
}

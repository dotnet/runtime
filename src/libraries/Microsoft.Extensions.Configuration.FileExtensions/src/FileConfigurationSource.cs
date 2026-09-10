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
        private FileProviderHandle? _fileProvider;

        /// <summary>
        /// Gets or sets the provider used to access the contents of the file.
        /// </summary>
        /// <remarks>
        /// A provider supplied by the caller is not disposed by the configuration system.
        /// A provider created by the configuration system is disposed once nothing is using it.
        /// </remarks>
        public IFileProvider? FileProvider
        {
            get => _fileProvider?.Current;
            set
            {
                if (ReferenceEquals(FileProvider, value) &&
                    (value is not null || _fileProvider is null))
                {
                    return;
                }

                FileProviderHandle? previous = _fileProvider;
                _fileProvider = value is null ? null : FileProviderHandle.CreateBorrowed(value);
                previous?.DiscardIfIdle();
            }
        }

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
        /// <remarks>
        /// When <see cref="ReloadOnChange"/> is enabled, this callback is also invoked on background reload failures.
        /// If the callback is not set or does not set <see cref="FileLoadExceptionContext.Ignore"/> to <see langword="true"/>,
        /// exceptions from background reloads will propagate unhandled on the thread pool.
        /// </remarks>
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
            _fileProvider ??= builder.GetFileProviderHandle();
            OnLoadException ??= builder.GetFileLoadExceptionHandler();
        }

        /// <summary>
        /// Creates a physical file provider for the file's directory if no file provider has been set, for absolute Path.
        /// </summary>
        public void ResolveFileProvider()
        {
            if (_fileProvider is null &&
                !string.IsNullOrEmpty(Path) &&
                System.IO.Path.IsPathRooted(Path) &&
                System.IO.Path.GetDirectoryName(Path) is string directory)
            {
                _fileProvider = FileProviderHandle.CreateOwned(new PhysicalFileProvider(directory));
                Path = System.IO.Path.GetFileName(Path);
            }
        }

        internal FileProviderHandle? AcquireFileProvider()
        {
            FileProviderHandle? fileProvider = _fileProvider;
            fileProvider?.Acquire();
            return fileProvider;
        }
    }
}

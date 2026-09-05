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
        private IFileProvider? _fileProvider;
        private FileProviderOwner? _fileProviderOwner;

        /// <summary>
        /// Gets or sets the provider used to access the contents of the file.
        /// </summary>
        /// <remarks>
        /// A file provider assigned to this property or supplied by the <see cref="IConfigurationBuilder"/>
        /// belongs to the caller and is not disposed by the configuration system. A
        /// <see cref="PhysicalFileProvider"/> created implicitly by <see cref="ResolveFileProvider"/> or
        /// <see cref="EnsureDefaults"/> is disposed once every <see cref="FileConfigurationProvider"/>
        /// using it has been disposed. Building this source again after that replaces it with a fresh instance.
        /// A provider built while using such an implicitly created provider keeps using that instance and
        /// the current <see cref="Path"/> even if this source is subsequently changed.
        /// </remarks>
        public IFileProvider? FileProvider
        {
            get => _fileProvider;
            set
            {
                if (ReferenceEquals(_fileProvider, value))
                {
                    return;
                }

                FileProviderOwner? previousOwner = _fileProviderOwner;
                _fileProviderOwner = null;
                _fileProvider = value;
                previousOwner?.Retire();
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
        /// <remarks>
        /// A file provider set on <paramref name="builder"/> is used without transferring ownership. When
        /// no provider is set, the physical file provider created by this method belongs to the configuration
        /// system and is disposed once nothing is using it. See <see cref="FileProvider"/>.
        /// </remarks>
        public void EnsureDefaults(IConfigurationBuilder builder)
        {
            if (_fileProvider is null)
            {
                IFileProvider fileProvider = builder.GetFileProvider(out PhysicalFileProvider? created);
                if (created is not null)
                {
                    SetOwnedFileProvider(created);
                }
                else
                {
                    _fileProvider = fileProvider;
                }
            }

            OnLoadException ??= builder.GetFileLoadExceptionHandler();
        }

        /// <summary>
        /// Creates a physical file provider for the file's directory if no file provider has been set, for absolute Path.
        /// </summary>
        /// <remarks>
        /// The physical file provider created by this method belongs to the configuration system and is
        /// disposed once nothing is using it. See <see cref="FileProvider"/>.
        /// </remarks>
        public void ResolveFileProvider()
        {
            if (_fileProvider is null &&
                !string.IsNullOrEmpty(Path) &&
                System.IO.Path.IsPathRooted(Path) &&
                System.IO.Path.GetDirectoryName(Path) is string directory)
            {
                SetOwnedFileProvider(new PhysicalFileProvider(directory));
                Path = System.IO.Path.GetFileName(Path);
            }
        }

        internal FileProviderOwner? AcquireFileProvider(out IFileProvider? fileProvider)
        {
            FileProviderOwner? owner = _fileProviderOwner;
            if (owner is null)
            {
                fileProvider = _fileProvider;
                return null;
            }

            fileProvider = owner.Acquire();
            _fileProvider = fileProvider;
            return owner;
        }

        private void SetOwnedFileProvider(PhysicalFileProvider fileProvider)
        {
            FileProviderOwner? previousOwner = _fileProviderOwner;
            _fileProviderOwner = new FileProviderOwner(fileProvider);
            _fileProvider = fileProvider;
            previousOwner?.Retire();
        }
    }
}

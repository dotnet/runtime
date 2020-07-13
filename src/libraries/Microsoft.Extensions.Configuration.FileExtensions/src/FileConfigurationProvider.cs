// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Base class for file based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public abstract class FileConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IDisposable _changeTokenRegistration;

        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public FileConfigurationProvider(FileConfigurationSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            Source = source;

            if (Source.ReloadOnChange && Source.FileProvider != null)
            {
                _changeTokenRegistration = ChangeToken.OnChange(
                    () => Source.FileProvider.Watch(Source.Path),
                    () =>
                    {
                        Thread.Sleep(Source.ReloadDelay);
                        Load(reload: true);
                    });
            }
        }

        /// <summary>
        /// The source settings for this provider.
        /// </summary>
        public FileConfigurationSource Source { get; }

        /// <summary>
        /// Generates a string representing this provider name and relevant details.
        /// </summary>
        /// <returns> The configuration name. </returns>
        public override string ToString()
            => $"{GetType().Name} for '{Source.Path}' ({(Source.Optional ? "Optional" : "Required")})";

        private void Load(bool reload)
        {
            IFileInfo file = Source.FileProvider?.GetFileInfo(Source.Path);
            if (file == null || !file.Exists)
            {
                if (Source.Optional || reload) // Always optional on reload
                {
                    Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    var error = new StringBuilder($"The configuration file '{Source.Path}' was not found and is not optional.");
                    if (!string.IsNullOrEmpty(file?.PhysicalPath))
                    {
                        error.Append($" The physical path is '{file.PhysicalPath}'.");
                    }
                    HandleException(ExceptionDispatchInfo.Capture(new FileNotFoundException(error.ToString())));
                }
            }
            else
            {
                // Always create new Data on reload to drop old keys
                if (reload)
                {
                    Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                static Stream OpenRead(IFileInfo fileInfo)
                {
                    if (fileInfo.PhysicalPath != null)
                    {
                        // The default physical file info assumes asynchronous IO which results in unnecessary overhead
                        // especally since the configuration system is synchronous. This uses the same settings
                        // and disables async IO.
                        return new FileStream(
                            fileInfo.PhysicalPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite,
                            bufferSize: 1,
                            FileOptions.SequentialScan);
                    }

                    return fileInfo.CreateReadStream();
                }

                using Stream stream = OpenRead(file);
                try
                {
                    Load(stream);
                }
                catch (Exception e)
                {
                    HandleException(ExceptionDispatchInfo.Capture(e));
                }
            }
            // REVIEW: Should we raise this in the base as well / instead?
            OnReload();
        }

        /// <summary>
        /// Loads the contents of the file at <see cref="Path"/>.
        /// </summary>
        /// <exception cref="FileNotFoundException">If Optional is <c>false</c> on the source and a
        /// file does not exist at specified Path.</exception>
        public override void Load()
        {
            Load(reload: false);
        }

        /// <summary>
        /// Loads this provider's data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public abstract void Load(Stream stream);

        private void HandleException(ExceptionDispatchInfo info)
        {
            bool ignoreException = false;
            if (Source.OnLoadException != null)
            {
                var exceptionContext = new FileLoadExceptionContext
                {
                    Provider = this,
                    Exception = info.SourceException
                };
                Source.OnLoadException.Invoke(exceptionContext);
                ignoreException = exceptionContext.Ignore;
            }
            if (!ignoreException)
            {
                info.Throw();
            }
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Dispose the provider.
        /// </summary>
        /// <param name="disposing"><c>true</c> if invoked from <see cref="IDisposable.Dispose"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            _changeTokenRegistration?.Dispose();
        }
    }
}

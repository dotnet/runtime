// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders.Physical;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Looks up files using the on-disk file system
    /// </summary>
    /// <remarks>
    /// When the environment variable "DOTNET_USE_POLLING_FILE_WATCHER" is set to "1" or "true", calls to
    /// <see cref="Watch(string)" /> will use <see cref="PollingFileChangeToken" />.
    /// </remarks>
    public partial class PhysicalFileProvider : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await _fileWatcher?.DisposeAsync().ConfigureAwait(false);
        }
    }
}

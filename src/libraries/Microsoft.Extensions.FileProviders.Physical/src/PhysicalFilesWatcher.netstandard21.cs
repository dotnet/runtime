// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public partial class PhysicalFilesWatcher : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_timer is not null)
            {
                await _timer.DisposeAsync().ConfigureAwait(false);
            }
            if (_fileWatcher is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _fileWatcher.Dispose();
            }
        }
    }
}

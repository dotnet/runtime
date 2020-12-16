// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.FileProviders
{
    public partial class PhysicalFileProvider : System.IAsyncDisposable
    {
        public ValueTask DisposeAsync() { throw null; }
        protected virtual ValueTask DisposeAsyncCore() { throw null; }
    }
}

namespace Microsoft.Extensions.FileProviders.Physical
{
    public partial class PhysicalFilesWatcher : System.IAsyncDisposable
    {
        public ValueTask DisposeAsync() { throw null; }
        protected virtual ValueTask DisposeAsyncCore() { throw null; }
    }
}

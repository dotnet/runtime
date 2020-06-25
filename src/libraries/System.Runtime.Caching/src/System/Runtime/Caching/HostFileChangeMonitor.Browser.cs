// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Runtime.Caching
{
    public sealed partial class HostFileChangeMonitor : FileChangeMonitor
    {
        public HostFileChangeMonitor(IList<string> filePaths)
        {
            throw new PlatformNotSupportedException();
        }

        public override ReadOnlyCollection<string> FilePaths => throw new PlatformNotSupportedException();

        public override DateTimeOffset LastModified => throw new PlatformNotSupportedException();

        public override string UniqueId => throw new PlatformNotSupportedException();

        protected override void Dispose(bool disposing)
        {

        }
    }
}
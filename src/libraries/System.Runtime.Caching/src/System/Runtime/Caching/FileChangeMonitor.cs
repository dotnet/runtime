// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;

namespace System.Runtime.Caching
{
    public abstract class FileChangeMonitor : ChangeMonitor
    {
        public abstract ReadOnlyCollection<string> FilePaths { get; }
        public abstract DateTimeOffset LastModified { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

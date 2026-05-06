// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyModel
{
    internal sealed class FileSystemWrapper : IFileSystem
    {
        public static IFileSystem Default { get; } = new FileSystemWrapper();

        public IFile File { get; } = new FileWrapper();

        public IDirectory Directory { get; } = new DirectoryWrapper();
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Extensions.DependencyModel
{
    internal class FileSystemWrapper : IFileSystem
    {
        public static IFileSystem Default { get; } = new FileSystemWrapper();

        public IFile File { get; } = new FileWrapper();

        public IDirectory Directory { get; } = new DirectoryWrapper();
    }
}
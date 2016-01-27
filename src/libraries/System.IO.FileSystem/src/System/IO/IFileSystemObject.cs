// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal interface IFileSystemObject
    {
        FileAttributes Attributes { get; set; }
        DateTimeOffset CreationTime { get; set; }
        bool Exists { get; }
        DateTimeOffset LastAccessTime { get; set; }
        DateTimeOffset LastWriteTime { get; set; }
        long Length { get; }

        void Refresh();
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

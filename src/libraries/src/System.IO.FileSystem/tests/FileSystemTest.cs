// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.FileSystem.Tests
{
    public abstract class FileSystemTest : FileCleanupTestBase
    {
        public static readonly byte[] TestBuffer = { 0xBA, 0x5E, 0xBA, 0x11, 0xF0, 0x07, 0xBA, 0x11 };
    }
}

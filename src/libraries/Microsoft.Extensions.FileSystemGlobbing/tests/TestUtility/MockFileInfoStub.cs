// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility
{
    internal class MockFileInfo : FileInfoBase
    {
        public MockFileInfo(
            DirectoryInfoBase parentDirectory,
            string fullName,
            string name)
        {
            FullName = fullName;
            Name = name;
        }

        public override DirectoryInfoBase ParentDirectory { get; }

        public override string FullName { get; }

        public override string Name { get; }
    }
}

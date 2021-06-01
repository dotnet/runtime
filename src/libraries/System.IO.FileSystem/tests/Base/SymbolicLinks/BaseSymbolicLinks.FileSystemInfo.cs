// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    // Contains test methods that can be used for FileInfo and DirectoryInfo.
    public abstract class BaseSymbolicLinks_FileSystemInfo : BaseSymbolicLinks_FileSystem
    {
        // Creates and returns FileSystemInfo instance by calling either the DirectoryInfo or FileInfo constructor and passing the path.
        protected abstract FileSystemInfo GetFileSystemInfo(string path);

        protected override FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
        {
            FileSystemInfo link = GetFileSystemInfo(path);
            link.CreateAsSymbolicLink(pathToTarget);
            return link;
        }

        protected override FileSystemInfo ResolveLinkTarget(string linkPath, string? expectedLinkTarget, bool returnFinalTarget = false)
        {
            FileSystemInfo link = GetFileSystemInfo(linkPath);

            if (expectedLinkTarget == null)
            {
                // LinkTarget is null when linkPath does not exist or is not a link
                Assert.Null(link.LinkTarget);
            }
            else
            {
                Assert.Equal(link.LinkTarget, expectedLinkTarget);
            }

            FileSystemInfo? target = link.ResolveLinkTarget(returnFinalTarget);

            // When the resolved target is the immediate next, and it does not exist,
            // verify that the link's LinkTarget returns null
            if (!returnFinalTarget && target == null)
            {
                Assert.Null(link.LinkTarget);
            }

            return target;
        }
    }
}
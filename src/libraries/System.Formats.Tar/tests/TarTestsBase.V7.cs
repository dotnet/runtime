// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected void SetRegularFile(V7TarEntry regularFile) => SetCommonRegularFile(regularFile, isV7RegularFile: true);

        protected void SetDirectory(V7TarEntry directory) => SetCommonDirectory(directory);

        protected void SetHardLink(V7TarEntry hardLink) => SetCommonHardLink(hardLink);

        protected void SetSymbolicLink(V7TarEntry symbolicLink) => SetCommonSymbolicLink(symbolicLink);

        protected void VerifyRegularFile(V7TarEntry regularFile, bool isWritable) => VerifyCommonRegularFile(regularFile, isWritable, isV7RegularFile: true);

        protected void VerifyDirectory(V7TarEntry directory) => VerifyCommonDirectory(directory);

        protected void VerifyHardLink(V7TarEntry hardLink) => VerifyCommonHardLink(hardLink);

        protected void VerifySymbolicLink(V7TarEntry symbolicLink) => VerifyCommonSymbolicLink(symbolicLink);
    }
}

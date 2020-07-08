// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class DirectoryInfoExtensions
    {
        public static DirectoryInfoAssertions Should(this DirectoryInfo dir)
        {
            return new DirectoryInfoAssertions(dir);
        }

        public static DirectoryInfo Sub(this DirectoryInfo dir, string name)
        {
            return new DirectoryInfo(Path.Combine(dir.FullName, name));
        }
    }
}

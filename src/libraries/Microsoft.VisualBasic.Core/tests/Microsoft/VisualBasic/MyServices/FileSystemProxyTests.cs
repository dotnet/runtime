// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualBasic.Devices;
using Xunit;

namespace Microsoft.VisualBasic.MyServices.Tests
{
    public class FileSystemProxyTests
    {
        [Fact]
        public void SpecialDirectories()
        {
            FileSystemProxy fileSystem = new ServerComputer().FileSystem;
            var specialDirectories = fileSystem.SpecialDirectories;
            Assert.NotNull(specialDirectories);
            Assert.Same(specialDirectories, fileSystem.SpecialDirectories);
        }
    }
}

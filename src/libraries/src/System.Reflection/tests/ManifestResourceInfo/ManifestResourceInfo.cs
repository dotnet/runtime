// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class FieldInfoManifestResourceInfoTests
    {
        [Fact]
        public void TestFileName()
        {
            Assembly assembly1 = typeof(FieldInfoManifestResourceInfoTests).GetTypeInfo().Assembly;
            System.Reflection.ManifestResourceInfo manifestresourceinfo = assembly1.GetManifestResourceInfo("ResourceTextFile.txt");
            Assert.Null(manifestresourceinfo.FileName);
        }

        [Fact]
        public void TestReferencedAssembly()
        {
            Assembly assembly1 = typeof(FieldInfoManifestResourceInfoTests).GetTypeInfo().Assembly;
            System.Reflection.ManifestResourceInfo manifestresourceinfo = assembly1.GetManifestResourceInfo("ResourceTextFile.txt");
            Assert.Null(manifestresourceinfo.ReferencedAssembly);
        }

        [Fact]
        public void TestResourceLocation()
        {
            Assembly assembly1 = typeof(FieldInfoManifestResourceInfoTests).GetTypeInfo().Assembly;
            System.Reflection.ManifestResourceInfo manifestresourceinfo = assembly1.GetManifestResourceInfo("ResourceTextFile.txt");
            Assert.Equal("Embedded, ContainedInManifestFile", manifestresourceinfo.ResourceLocation.ToString());
        }
    }
}

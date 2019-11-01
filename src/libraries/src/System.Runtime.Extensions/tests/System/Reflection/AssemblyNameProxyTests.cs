// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public static class AssemblyNameProxyTests
    {
        [Fact]
        public static void GetAssemblyName_AssemblyNameProxy()
        {
            AssemblyNameProxy anp = new AssemblyNameProxy();
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => anp.GetAssemblyName(null));
            AssertExtensions.Throws<ArgumentException>("path", null, () => anp.GetAssemblyName(string.Empty));
            Assert.Throws<FileNotFoundException>(() => anp.GetAssemblyName(Guid.NewGuid().ToString("N")));

            Assembly a = typeof(AssemblyNameProxyTests).Assembly;
            Assert.Equal(new AssemblyName(a.FullName).ToString(), anp.GetAssemblyName(Path.GetFullPath(a.Location)).ToString());
        }
    }
}

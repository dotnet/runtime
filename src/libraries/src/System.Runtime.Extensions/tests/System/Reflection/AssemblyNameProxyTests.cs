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
            Assert.Throws<ArgumentNullException>("assemblyFile", () => anp.GetAssemblyName(null));
            Assert.Throws<ArgumentException>(() => anp.GetAssemblyName(string.Empty));
            Assert.Throws<FileNotFoundException>(() => anp.GetAssemblyName(Guid.NewGuid().ToString("N")));

            Assembly a = typeof(AssemblyNameProxyTests).Assembly;
            Assert.Equal(new AssemblyName(a.FullName).ToString(), anp.GetAssemblyName(Path.GetFullPath(a.Location)).ToString());
        }

        public static IEnumerable<object[]> ReferenceMatchesDefinition_TestData()
        {
            yield return new object[] { new AssemblyName(typeof(AssemblyNameProxy).GetTypeInfo().Assembly.FullName), new AssemblyName("System.Runtime.Extensions"), true };   
        }

        [Theory]
        [MemberData(nameof(ReferenceMatchesDefinition_TestData))]
        public static void ReferenceMatchesDefinition(AssemblyName a1, AssemblyName a2, bool expected)
        {
            Assert.Equal(expected, AssemblyName.ReferenceMatchesDefinition(a1, a2));
        }
    }
}

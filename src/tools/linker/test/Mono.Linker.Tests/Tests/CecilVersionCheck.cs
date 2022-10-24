// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Mono.Linker.Tests
{
    [TestFixture]
    public class CecilVersionCheck
    {
        [TestCase]
        public void CecilPackageVersionMatchesAssemblyVersion()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            var cecilPackageVersion = thisAssembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(ca => ca.Key == "CecilPackageVersion")
                .Single().Value;
            // Assume that the test assembly builds against the same cecil as the linker.
            var cecilAssemblyVersion = thisAssembly
                .GetReferencedAssemblies()
                .Where(an => an.Name == "Mono.Cecil")
                .Single().Version;
            Assert.AreEqual(cecilPackageVersion.AsSpan(0, 6).ToString(), cecilAssemblyVersion.ToString(3));
        }
    }
}

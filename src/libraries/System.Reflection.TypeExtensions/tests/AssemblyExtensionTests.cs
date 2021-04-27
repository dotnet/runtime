// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyExtensionTests
    {
        [Fact]
        public void GetExportedTypesTest()
        {
            Assembly executingAssembly = GetType().GetTypeInfo().Assembly;
            Assert.True(AssemblyExtensions.GetExportedTypes(executingAssembly).Length >= 60);
        }

        [Fact]
        public void GetModulesTest()
        {
            Assembly executingAssembly = GetType().GetTypeInfo().Assembly;
            Assert.Equal(1, AssemblyExtensions.GetModules(executingAssembly).Length);
        }

        [Fact]
        public void GetTypes()
        {
            Assembly executingAssembly = GetType().GetTypeInfo().Assembly;
            Assert.True(AssemblyExtensions.GetTypes(executingAssembly).Length >= 140);
        }
    }
}

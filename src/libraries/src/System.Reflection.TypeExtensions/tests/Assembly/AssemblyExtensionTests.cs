// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyExtensionTests
    {
        [Fact]
        public void GetExportedTypesTest()
        {
            Assembly executingAssembly = this.GetType().GetTypeInfo().Assembly;
            Assert.True(executingAssembly.GetExportedTypes().Length >= 183);
        }

        [Fact]
        public void GetModulesTest()
        {
            Assembly executingAssembly = this.GetType().GetTypeInfo().Assembly;
            Assert.Equal(1, executingAssembly.GetModules().Length);
        }

        [Fact]
        public void GetTypes()
        {
            Assembly executingAssembly = this.GetType().GetTypeInfo().Assembly;
            Assert.True(executingAssembly.GetTypes().Length >= 236);
        }
    }
}

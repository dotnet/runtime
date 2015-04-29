// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection.Compatibility.UnitTests.AssemblyTests
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
            Assert.True(executingAssembly.GetModules().Length == 1);
        }

        [Fact]
        public void GetTypes()
        {
            Assembly executingAssembly = this.GetType().GetTypeInfo().Assembly;
            Assert.True(executingAssembly.GetTypes().Length >= 236);
        }
    }
}

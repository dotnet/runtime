// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class LoadTests
    {
        [Fact]
        //Null Assembly
        public void LoadTest1()
        {
            Assert.Throws<ArgumentNullException>("assemblyRef", () => Assembly.Load((AssemblyName)null));
        }

        [Fact]
        //Non existent Assembly
        public void LoadTest2()
        {
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName("no such assembly")));
        }

        [Fact]
        //Try to Load System.Runtime
        public void LoadTest3()
        {
            Assert.NotNull(Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)));
        }

        [Fact]
        //Try to Load assembly that is not already loaded
        public void LoadTest4()
        {
            var assembly = Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));
            Assert.NotNull(assembly);
        }

        [Fact]
        //Try to Load assembly that is not already loaded
        public void LoadTest5()
        {
            var assembly = Assembly.Load(new AssemblyName(typeof(AssemblyName).GetTypeInfo().Assembly.FullName));
            Assert.NotNull(assembly);
        }
    }
}



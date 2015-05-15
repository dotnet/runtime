// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class ToStringTests
    {
        [Fact]
        //Try to Load itself and Verify ToString method
        public void ToStringTest1()
        {
            var assembly = typeof(ToStringTests).GetTypeInfo().Assembly;
            var assemblyString = assembly.ToString();

            Assert.NotNull(assemblyString);
            Assert.True(assemblyString.Contains("System.Reflection.Tests"));
        }

        //Load Assembly and Verify PublicKeyToken is present in FullName
        [Fact]
        public void ToStringTest2()
        {
            var assembly = Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));
            var assemblyString = assembly.ToString();

            Assert.NotNull(assemblyString);
            Assert.True(assemblyString.Contains("PublicKeyToken="));
        }


        [Fact]
        //Try to Load assembly and Verify that ToString() method returns same string as FullName
        public void ToStringTest3()
        {
            var assembly = Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));
            var assemblyString = assembly.ToString();

            Assert.NotNull(assemblyString);
            Assert.Equal(assemblyString, assembly.FullName);
        }
    }
}



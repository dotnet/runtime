// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class EqualsTests
    {
        [Fact]
        //Try to Load System.Runtime and Verify Equals Method returns True
        public void EqualsTest1()
        {
            var assembly1 = Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName));
            var assembly2 = Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName));

            Assert.Equal(assembly1, assembly2);
        }

        [Fact]
        //Try to Load assembly other than System.Runtime and Verify Equals Method returns True
        public void EqualsTest2()
        {
            var assembly1 = Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));
            var assembly2 = Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));

            Assert.Equal(assembly1, assembly2);
        }

        [Fact]
        //Try to Load System.Runtime and currently Executing Assembly and Verify that Equals method returns False
        public void EqualsTest3()
        {
            var assembly1 = Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));
            var assembly2 = typeof(EqualsTests).GetTypeInfo().Assembly;

            Assert.NotEqual(assembly1, assembly2);
        }
    }
}

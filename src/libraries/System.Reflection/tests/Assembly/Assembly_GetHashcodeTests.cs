// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Reflection.Tests
{
    public class HashCodeTests
    {
        [Fact]
        //Try to Load System.Runtime and Verify Assembly.GetHashCode method returns valid HashCode
        public void GetHashCodeTest1()
        {
            Assembly asm = LoadSystemRuntimeAssembly();
            int hashCode = asm.GetHashCode();
            Assert.False((hashCode == -1) || (hashCode == 0));
        }

        [Fact]
        //Try to Load FW assembly Verify HashCode
        public void GetHashCodeTest2()
        {
            Assembly asm = LoadSystemCollectionsAssembly();
            int hashCode = asm.GetHashCode();

            Assert.False((hashCode == -1) || (hashCode == 0));
        }

        [Fact]
        //Try to Load FW assembly and Verify HashCode
        public void GetHashCodeTest3()
        {
            Assembly asm = LoadSystemReflectionAssembly();
            int hashCode = asm.GetHashCode();
            Assert.False((hashCode == -1) || (hashCode == 0));
        }

        [Fact]
        //Verify currently executing Assembly has HashCode
        public void GetHashCodeTest4()
        {
            Type t = typeof(HashCodeTests);
            TypeInfo ti = t.GetTypeInfo();
            Assembly asm = ti.Assembly;
            int hashCode = asm.GetHashCode();
            Assert.False((hashCode == -1) || (hashCode == 0));
        }

        private static Assembly LoadSystemCollectionsAssembly()
        {
            //Force System.collections to be linked statically
            List<int> li = new List<int>();
            li.Add(1);

            //Load System.Collections
            Assembly a = Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));
            return a;
        }

        private static Assembly LoadSystemReflectionAssembly()
        {
            //Force System.Reflection to be linked statically
            AssemblyName name = new AssemblyName("Foo");

            //Load System.Reflection
            Assembly a = Assembly.Load(new AssemblyName(typeof(AssemblyName).GetTypeInfo().Assembly.FullName));
            return a;
        }

        private static Assembly LoadSystemRuntimeAssembly()
        {
            //Load System.Runtime
            Assembly a = Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName));
            return a;
        }
    }
}



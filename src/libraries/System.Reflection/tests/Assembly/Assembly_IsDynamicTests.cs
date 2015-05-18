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
    public class IsDynamicTests
    {
        [Fact]
        //Try to Load itself and Verify IsDynamic Property 
        public void IsDynamicTest1()
        {
            Assembly asm = GetExecutingAssembly();
            Assert.False(asm.IsDynamic);
        }

        [Fact]
        //Try to Load assembly other than System.Runtime that is not already loaded and check IsDynamic property
        public void IsDynamicTest2()
        {
            Assembly asm = LoadSystemCollectionsAssembly();
            Assert.False(asm.IsDynamic);
        }

        private static Assembly LoadSystemCollectionsAssembly()
        {
            //Force System.collections to be linked statically
            List<int> li = new List<int>();
            li.Add(1);

            //Load System.Collections
            Assembly a = Assembly.Load(new AssemblyName("System.Collections, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
            return a;
        }

        private static Assembly GetExecutingAssembly()
        {
            Assembly currentasm = null;

            Type t = typeof(IsDynamicTests);
            TypeInfo ti = t.GetTypeInfo();
            currentasm = ti.Assembly;
            return currentasm;
        }
    }
}




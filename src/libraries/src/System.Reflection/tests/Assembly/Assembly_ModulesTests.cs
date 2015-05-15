// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Reflection.Tests
{
    public class ModuleTests
    {
        //Load an Assembly that is not already loaded and try to find Modules in that
        [Fact]
        public void GetModulesTest1()
        {
            Assembly asm = LoadSystemCollectionsAssembly();
            IEnumerator<Module> mods = asm.Modules.GetEnumerator();
            mods.MoveNext();
            Module mod = mods.Current;
            Assert.NotNull(mod);
        }

        //Load an assembly that is already loaded and try to find Module
        [Fact]
        public void GetModulesTest2()
        {
            Assembly asm = LoadSystemReflectionAssembly();
            // reloading the assembly again to see whether it causes any load error
            IEnumerator<Module> mods = asm.Modules.GetEnumerator();
            mods.MoveNext();
            Module mod = mods.Current;
            Assert.NotNull(mod);
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
    }
}



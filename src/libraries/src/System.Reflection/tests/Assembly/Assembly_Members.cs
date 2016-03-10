// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Reflection.Tests
{
    public class Assembly_Members
    {
        [Fact]
        public void CodeBase()
        {
            string codeBase = GetExecutingAssembly().CodeBase;
            Assert.False(string.IsNullOrEmpty(codeBase));
        }

        [Fact]
        public void ImageRuntimeVersion()
        {
            string ver = GetExecutingAssembly().ImageRuntimeVersion;
            Assert.False(string.IsNullOrEmpty(ver));
        }

        [Fact]
        public void CreateInstance()
        {
            Assert.Throws<ArgumentException>(() => GetExecutingAssembly().CreateInstance(""));
            Assert.Throws<ArgumentNullException>(() => GetExecutingAssembly().CreateInstance(null));
            Assert.Throws<MissingMethodException>(() => GetExecutingAssembly().CreateInstance(typeof(MyClassWithPrivateCtor).FullName));
            Assert.Throws<MissingMethodException>(() => GetExecutingAssembly().CreateInstance(typeof(MyClassNoDefaultCtor).FullName));

            Object obj = GetExecutingAssembly().CreateInstance(typeof(MyClass).FullName);
            Assert.NotNull(obj);
            Assert.Equal(obj.GetType(), typeof(MyClass));

            obj = typeof(int).GetTypeInfo().Assembly.CreateInstance(typeof(int).FullName);
            Assert.NotNull(obj);
            Assert.Equal(obj.GetType(), typeof(int));

            obj = typeof(int).GetTypeInfo().Assembly.CreateInstance(typeof(Dictionary<int, string>).FullName);
            Assert.NotNull(obj);
            Assert.Equal(obj.GetType(), typeof(Dictionary<int, string>));
        }

        [Fact]
        public void CreateInstance_IgnoreCase()
        {
            Assert.Throws<ArgumentException>(() => GetExecutingAssembly().CreateInstance("", false));
            Assert.Throws<ArgumentNullException>(() => GetExecutingAssembly().CreateInstance(null, true));
            Assert.Throws<MissingMethodException>(() => GetExecutingAssembly().CreateInstance(typeof(MyClassWithPrivateCtor).FullName, false));
            Assert.Throws<MissingMethodException>(() => GetExecutingAssembly().CreateInstance(typeof(MyClassNoDefaultCtor).FullName, true));

            Object obj = GetExecutingAssembly().CreateInstance(typeof(MyClass).FullName, false);
            Assert.NotNull(obj);
            Assert.Equal(obj.GetType(), typeof(MyClass));

            obj = GetExecutingAssembly().CreateInstance(typeof(MyClass).FullName.ToLower(), true);
            Assert.NotNull(obj);
            Assert.Equal(obj.GetType(), typeof(MyClass));

            obj = typeof(int).GetTypeInfo().Assembly.CreateInstance(typeof(int).FullName.ToLower(), true);
            Assert.NotNull(obj);
            Assert.Equal(obj.GetType(), typeof(int));

            obj = typeof(Dictionary<,>).GetTypeInfo().Assembly.CreateInstance(typeof(Dictionary<int, string>).FullName.ToUpper(), true);
            Assert.NotNull(obj);
            Assert.Equal(typeof(Dictionary<int, string>), obj.GetType());
        }

        [Fact]
        public static void CreateQualifiedName()
        {
            Assert.Equal(typeof(Assembly_Members).FullName + ", " + GetExecutingAssembly().ToString(), 
                Assembly.CreateQualifiedName(GetExecutingAssembly().ToString(), typeof(Assembly_Members).FullName));
        }

        [Fact]
        public static void GetExportedTypes()
        {
            Type[] types = GetExecutingAssembly().GetExportedTypes();
            Assert.NotNull(types);
            Assert.True(types.Length > 0);
        }

        [Fact]
        public static void GetReferencedAssemblies()
        {
            // It is too brittle to depend on the assmebly references so we simply call the method and check that it does not throw.
            AssemblyName[] assemblies = GetExecutingAssembly().GetReferencedAssemblies();
            Assert.NotNull(assemblies);
            Assert.True(assemblies.Length > 0);
        }

        [Fact]
        public static void GetTypeMethod()
        {
            Type type = GetExecutingAssembly().GetType(typeof(MyClass).FullName);
            Assert.NotNull(type);
            Assert.Equal(type, typeof(MyClass));

            type = GetExecutingAssembly().GetType(typeof(MyPrivateNestedClass).FullName);
            Assert.NotNull(type);
            Assert.Equal(type, typeof(MyPrivateNestedClass));

            type = GetExecutingAssembly().GetType(typeof(MyPrivateClass).FullName);
            Assert.NotNull(type);
            Assert.Equal(type, typeof(MyPrivateClass));

            type = GetExecutingAssembly().GetType(typeof(MyClassWithPrivateCtor).FullName, false);
            Assert.NotNull(type);
            Assert.Equal(type, typeof(MyClassWithPrivateCtor));

            type = GetExecutingAssembly().GetType(typeof(MyClassNoDefaultCtor).FullName, true);
            Assert.NotNull(type);
            Assert.Equal(type, typeof(MyClassNoDefaultCtor));

            type = GetExecutingAssembly().GetType("notfound", false);
            Assert.Null(type);

            Assert.Throws<TypeLoadException>(() => GetExecutingAssembly().GetType("notfound", true));
        }

        public static Assembly GetExecutingAssembly()
        {
            Assembly asm = null;
            Type t = typeof(Assembly_Members);
            TypeInfo ti = t.GetTypeInfo();
            asm = ti.Assembly;

            return asm;
        }

        public class MyClass
        {
            public MyClass() { }
        }

        public class MyClassWithPrivateCtor
        {
            private MyClassWithPrivateCtor() { }
        }

        public class MyClassNoDefaultCtor
        {
            public MyClassNoDefaultCtor(int x) { }
        }

        private class MyPrivateNestedClass{ }
    }

    class MyPrivateClass { }
}


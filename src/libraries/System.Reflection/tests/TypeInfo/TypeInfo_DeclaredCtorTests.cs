// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredCtorTests
    {
        // Even if there is a static ctor, it shouldn't be included in the count because it's private
        // Verify Declared Ctors for a Base class
        [Fact]
        public static void TestTypeInfoBaseClassCtor()
        {
            VerifyConstructors(typeof(TypeInfoBaseClass), 2);
        }

        // Verify Declared Ctors for a Derived Class 
        [Fact]
        public static void TestDerivedClassCtor()
        {
            VerifyConstructors(typeof(TypeInfoSubClass), 2);
        }

        // Verify Declared Ctors for a Static Class 
        [Fact]
        public static void TestStaticClassCtor()
        {
            VerifyConstructors(typeof(StaticClass), 0);
        }

        // Verify Declared Ctors for a Class with multiple Ctors 
        [Fact]
        public static void TestClassWithMultipleCtor()
        {
            VerifyConstructors(typeof(ClassWithMultipleConstructors), 4);
        }

        //private helper methods
        private static void VerifyConstructors(Type t, int count)
        {
            ConstructorInfo[] ctors = getConstructor(t);
            Assert.NotNull(ctors);

            //Verify number of ctors
            Assert.Equal(count, ctors.Length);

            foreach (ConstructorInfo ci in ctors)
                Assert.NotNull(ci);
        }

        //Gets ConstructorInfo object from a Type
        public static ConstructorInfo[] getConstructor(Type t)
        {
            var constructorList = new List<ConstructorInfo>();

            foreach (var constructor in t.GetTypeInfo().DeclaredConstructors)
            {
                if (!constructor.IsStatic)
                    constructorList.Add(constructor);
            }

            return constructorList.ToArray();
        }
    }

    //Metadata for Reflection
    public static class StaticClass
    {
        public static int Members = 3;
        public static int MembersEverything = 9;
        static StaticClass() { }
    }

    public class ClassWithMultipleConstructors
    {
        public static int Members = 9;
        public static int MembersEverything = 15;
        static ClassWithMultipleConstructors() { }

        public ClassWithMultipleConstructors() { }
        public ClassWithMultipleConstructors(TimeSpan ts) { }
        public ClassWithMultipleConstructors(Object o1, Object o2) { }
        public ClassWithMultipleConstructors(Object obj0, Int32 i4) { }
    }

    public class TypeInfoBaseClass
    {
        public static int Members = 5;
        public static int MembersEverything = 11;

        static TypeInfoBaseClass() { }
        public TypeInfoBaseClass() { }
        public TypeInfoBaseClass(Int16 i2) { }
    }

    public class TypeInfoSubClass : TypeInfoBaseClass
    {
        public TypeInfoSubClass(String s) { }
        public TypeInfoSubClass(Int16 i2) { }
    }
}

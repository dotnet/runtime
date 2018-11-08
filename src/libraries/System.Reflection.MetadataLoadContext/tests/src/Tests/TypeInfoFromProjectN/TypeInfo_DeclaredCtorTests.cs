// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
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
            VerifyConstructors(typeof(TypeInfoBaseClass).Project(), 2);
        }

        // Verify Declared Ctors for a Derived Class 
        [Fact]
        public static void TestDerivedClassCtor()
        {
            VerifyConstructors(typeof(TypeInfoSubClass).Project(), 2);
        }

        // Verify Declared Ctors for a Static Class 
        [Fact]
        public static void TestStaticClassCtor()
        {
            VerifyConstructors(typeof(StaticClass).Project(), 0);
        }

        // Verify Declared Ctors for a Class with multiple Ctors 
        [Fact]
        public static void TestClassWithMultipleCtor()
        {
            VerifyConstructors(typeof(ClassWithMultipleConstructors).Project(), 4);
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
            List<ConstructorInfo> constructorList = new List<ConstructorInfo>();

            foreach (ConstructorInfo constructor in t.GetTypeInfo().DeclaredConstructors)
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
        public ClassWithMultipleConstructors(object o1, object o2) { }
        public ClassWithMultipleConstructors(object obj0, int i4) { }
    }

    public class TypeInfoBaseClass
    {
        public static int Members = 5;
        public static int MembersEverything = 11;

        static TypeInfoBaseClass() { }
        public TypeInfoBaseClass() { }
        public TypeInfoBaseClass(short i2) { }
    }

    public class TypeInfoSubClass : TypeInfoBaseClass
    {
        public TypeInfoSubClass(string s) { }
        public TypeInfoSubClass(short i2) { }
    }
}

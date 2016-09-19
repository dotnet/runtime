// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public static class PropertyInfoTests
    {
        [Fact]
        public static void GetRawConstantValueOnProperty()
        {
            //
            // Why does PropertyInfo expose a GetRawConstantValue property?
            //
            //  - ECMA metadata has the ability to specify a "default value" for a property but C# has no way to generate it.
            //  - The CustomConstantAttribute class is not marked as usable on a property.
            // 
            PropertyInfo f = typeof(TestClass).GetTypeInfo().GetDeclaredProperty(nameof(TestClass.MyProp));
            Assert.Throws<InvalidOperationException>(() => f.GetRawConstantValue());
        }
    
        [Fact]
        public static void TestEquality_False()
        {
            PropertyInfo pi1 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");
            PropertyInfo pi2 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropBB");

            Assert.False(pi1 == pi2);
            Assert.True(pi1 != pi2);
        }

        [Fact]
        public static void TestEquality_True()
        {
            PropertyInfo pi1 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");
            PropertyInfo pi2 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");

            Assert.True(pi1 == pi2);
            Assert.False(pi1 != pi2);
        }

        private class TestClass
        {
            public static BindingFlags MyProp { get; }
        }

        //Reflection Metadata
        public class SampleMethod
        {
            public double m_PropBB = 1;
            public short m_PropAA = 2;
            //indexer Property
            public string[] mystrings = { "abc", "def", "ghi", "jkl" };

            public string this[int Index]
            {
                get
                {
                    return mystrings[Index];
                }
                set
                {
                    mystrings[Index] = value;
                }
            }

            // MyPropAA - ReadWrite property
            public String MyPropAA
            {
                get { return m_PropAA.ToString(); }
                set { m_PropAA = Int16.Parse(value); }
            }

            public double MyPropBB
            {
                get { return m_PropBB; }
                set { m_PropBB = value; }
            }
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414
#pragma warning disable 0067
#pragma warning disable 3026
#pragma warning disable 3005

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredNestedTypeTests
    {
        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes1()
        {
            VerifyNestedType(typeof(TestNest).Project(), "NestPublic", true);
        }


        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes2()
        {
            VerifyNestedType(typeof(TestNest).Project(), "NestPublic2", true);
        }

        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes3()
        {
            VerifyNestedType(typeof(TestNestDerived).Project(), "NestPublic", true);
        }

        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes4()
        {
            VerifyNestedType(typeof(TestNestDerived).Project(), "NestPublic3", true);
        }


        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes5()
        {
            VerifyNestedType(typeof(TestNestDerived).Project(), "NESTPUBLIC3", true);
        }

        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes6()
        {
            VerifyNestedType(typeof(TestMultiNest).Project(), "Nest1", true);
        }

        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes7()
        {
            VerifyNestedType(typeof(TestMultiNest.Nest1).Project(), "Nest2", true);
        }

        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes8()
        {
            VerifyNestedType(typeof(TestMultiNest.Nest1.Nest2).Project(), "Nest3", true);
        }

        // Verify NestedTypes
        [Fact]
        public static void TestNestedTypes9()
        {
            VerifyNestedType(typeof(TestNest).Project(), "NoSuchType", false);
        }

        // DeclaredNestedTypes of a generic instantiation returns the same result as
        // DeclaredNestedTypes on the generic type definition.
        [Fact]
        public static void TestNestedTypes10()
        {
            TypeInfo ti = typeof(TestNestGeneric<int>).Project().GetTypeInfo();
            IEnumerable<TypeInfo> nestedTypes = ti.DeclaredNestedTypes;

            using (IEnumerator<TypeInfo> e = nestedTypes.GetEnumerator())
            {
                Assert.True(e.MoveNext());
                TypeInfo nest1 = e.Current;
                Assert.Equal(typeof(TestNestGeneric<>.Nest1).Project(), nest1.AsType());
                Assert.False(e.MoveNext());
            }
        }

        //private helper methods
        private static void VerifyNestedType(Type t, string name, bool present)
        {
            //Fix to initialize Reflection
            string str = typeof(object).Project().Name;

            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<TypeInfo> alltypes = ti.DeclaredNestedTypes.GetEnumerator();
            bool found = false;

            while (alltypes.MoveNext())
            {
                if (alltypes.Current.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    found = true;
            }

            if (present && (!found))
            {
                Assert.Fail(string.Format("Nested Type {0} not found", name));
            }
            else if ((!present) && found)
            {
                Assert.Fail(string.Format("Nested Type {0} was not expected to be found", name));
            }
        }
    }

    //Metadata for Reflection
    public class TestNest
    {
        public static int iDeclaredNests = 6;

        public class NestPublic { }
        public class NestPublic2 { }
        private class NestPrivate { }          // private, so not inherited
        internal class NestInternal { }        // internal members are not inherited
        protected class NestProtected { }
        private class NestAssemblyPrivate { }  // same as private, so not inherited
    }

    public class TestNestDerived : TestNest
    {
        public static new int iDeclaredNests = 4;

        public new class NestPublic { }
        public class NestPublic3 { }
        public class NESTPUBLIC3 { }
        private class NestPrivate2 { }
    }

    public class TestMultiNest
    {
        public class Nest1
        {
            public class Nest2
            {
                public class Nest3
                {
                    // nest ends
                }
            }
        }
    }

    public class TestNestGeneric<T>
    {
        public class Nest1
        {
        }
    }
}

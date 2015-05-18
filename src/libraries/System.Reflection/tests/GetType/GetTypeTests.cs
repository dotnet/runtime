// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public class GetTypeReflectionTests
    {
        [Fact]
        public void GetType1()
        {
            Assembly a = typeof(GetTypeReflectionTests).GetTypeInfo().Assembly;
            String assemblyName = a.FullName;

            PerformTests(null, "NON-EXISTENT-TYPE");
            PerformTests(typeof(MyClass1));
            PerformTests(typeof(MyClass1), "System.Reflection.Tests.mYclAss1");
            PerformTests(null, "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace99.MyClASs3+inNer");
            PerformTests(null, "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace2.MyClASs399+inNer");
            PerformTests(null, "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace2.MyClASs3+inNer99");
            PerformTests(typeof(MyNamespace1.MyNamespace2.MyClass2));
            PerformTests(typeof(MyNamespace1.MyNamespace2.MyClass2));
            PerformTests(typeof(MyNamespace1.MyNamespace2.MyClass3.iNner));
            PerformTests(typeof(MyNamespace1.MyNamespace2.MyClass3.iNner), "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace2.MyClASs3+inNer");
            PerformTests(typeof(MyNamespace1.MyNaMespace3.Foo));
            PerformTests(typeof(MyNamespace1.MyNaMespace3.Foo), "System.Reflection.Tests.mynamespace1.mynamespace3.foo");
            PerformTests(typeof(MyNamespace1.MyNaMespace3.Foo), "System.Reflection.Tests.MYNAMESPACE1.MYNAMESPACE3.FOO");

            {
                Type g = typeof(MyNamespace1.MynAmespace3.Goo<int>);
                String fullName = g.FullName;

                PerformTests(g, fullName);


                PerformTests(g, fullName.ToUpper());
                PerformTests(g, fullName.ToLower());
            }
        }

        [Fact]
        public void GetTypeFromCoreAssembly()
        {
            Type typeInt32 = typeof(Int32);
            Type t;

            t = Type.GetType("System.Int32", throwOnError: true);
            Assert.Equal(t, typeInt32);

            t = Type.GetType("system.int32", throwOnError: true, ignoreCase: true);
            Assert.Equal(t, typeInt32);
        }

        [Fact]
        public void GetTypeEmptyString()
        {
            Assembly a = typeof(GetTypeReflectionTests).GetTypeInfo().Assembly;
            Module m = a.ManifestModule;

            String typeName = "";
            String aqn = ", " + typeof(GetTypeReflectionTests).GetTypeInfo().Assembly.FullName;

            Assert.Null(Type.GetType(typeName));
            Assert.Null(Type.GetType(aqn));

            Assert.Null(Type.GetType(typeName, throwOnError: false));
            Assert.Null(Type.GetType(aqn, throwOnError: false));

            Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true));
            Assert.Throws<ArgumentException>(() => Type.GetType(aqn, throwOnError: true));

            Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: false));
            Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: true));
            Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: false));
            Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: true));

            Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
            Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: true));
            Assert.Throws<ArgumentException>(() => Type.GetType(aqn, throwOnError: true, ignoreCase: false));
            Assert.Throws<ArgumentException>(() => Type.GetType(aqn, throwOnError: true, ignoreCase: true));


            Assert.Throws<ArgumentException>(() => a.GetType(typeName));
            Assert.Null(a.GetType(aqn));

            Assert.Throws<ArgumentException>(() => a.GetType(typeName, throwOnError: false, ignoreCase: false));
            Assert.Throws<ArgumentException>(() => a.GetType(typeName, throwOnError: false, ignoreCase: true));
            Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
            Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

            Assert.Throws<ArgumentException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: false));
            Assert.Throws<ArgumentException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: true));
            Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: false));
            Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: true));


            Assert.Throws<ArgumentException>(() => m.GetType(typeName, throwOnError: false, ignoreCase: false));
            Assert.Throws<ArgumentException>(() => m.GetType(typeName, throwOnError: false, ignoreCase: true));
            Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
            Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

            Assert.Throws<ArgumentException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: false));
            Assert.Throws<ArgumentException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: true));
            Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: false));
            Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: true));
        }

        private static void PerformTests(Type expectedResult, String typeName = null)
        {
            if (typeName == null)
                typeName = expectedResult.FullName;


            Assembly a = typeof(GetTypeReflectionTests).GetTypeInfo().Assembly;
            Module m = a.ManifestModule;

            String aqn = typeName + ", " + a.FullName;

            if (expectedResult == null)
            {
                Assert.Null(Type.GetType(typeName));
                Assert.Null(Type.GetType(aqn));

                Assert.Null(Type.GetType(typeName, throwOnError: false));
                Assert.Null(Type.GetType(aqn, throwOnError: false));

                Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true));
                Assert.Throws<TypeLoadException>(() => Type.GetType(aqn, throwOnError: true));

                Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<TypeLoadException>(() => Type.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Throws<TypeLoadException>(() => Type.GetType(aqn, throwOnError: true, ignoreCase: true));


                Assert.Null(a.GetType(typeName));
                Assert.Null(a.GetType(aqn));

                Assert.Null(a.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Throws<TypeLoadException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: true));


                Assert.Null(m.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Throws<TypeLoadException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: true));
            }
            else if (expectedResult.FullName == typeName)
            {
                // Case-sensitive match.

                Assert.Equal(expectedResult, Type.GetType(typeName));
                Assert.Equal(expectedResult, Type.GetType(aqn));

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: false));
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: false));

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: true));
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: true));

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(aqn, throwOnError: true, ignoreCase: true));


                Assert.Equal(expectedResult, a.GetType(typeName));
                Assert.Null(a.GetType(aqn));

                Assert.Equal(expectedResult, a.GetType(typeName, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, a.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Equal(expectedResult, a.GetType(typeName, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, a.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: true));


                Assert.Equal(expectedResult, m.GetType(typeName, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, m.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Equal(expectedResult, m.GetType(typeName, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, m.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: true));
            }
            else if (expectedResult.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            {
                // Case-insensitive match.

                Assert.Null(Type.GetType(typeName));
                Assert.Null(Type.GetType(aqn));

                Assert.Null(Type.GetType(typeName, throwOnError: false));
                Assert.Null(Type.GetType(aqn, throwOnError: false));

                Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true));
                Assert.Throws<TypeLoadException>(() => Type.GetType(aqn, throwOnError: true));

                Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<TypeLoadException>(() => Type.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, Type.GetType(aqn, throwOnError: true, ignoreCase: true));


                Assert.Null(a.GetType(typeName));
                Assert.Null(a.GetType(aqn));

                Assert.Null(a.GetType(typeName, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, a.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, a.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Throws<ArgumentException>(() => a.GetType(aqn, throwOnError: true, ignoreCase: true));


                Assert.Null(m.GetType(typeName, throwOnError: false, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, m.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: false));
                AssertCaseInsensitiveMatch(expectedResult, m.GetType(typeName, throwOnError: true, ignoreCase: true));
                Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Throws<ArgumentException>(() => m.GetType(aqn, throwOnError: true, ignoreCase: true));
            }
            else
            {
                throw new InvalidOperationException("TEST ERROR.");
            }
        }

        private static void AssertCaseInsensitiveMatch(Type expectedResult, Type actualResult)
        {
            // When called with "ignoreCase: true", GetType() may have a choice of matching items. The one that is chosen
            // is an implementation detail (and on the CLR, *very* implementation-dependent as it's influenced by the internal
            // layout of private hash tables.) As a result, we do not expect the same result across desktop and Project N
            // and so the best we can do is compare the names.
            Assert.True(expectedResult.AssemblyQualifiedName.Equals(actualResult.AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase));
        }
    }

    namespace MyNamespace1
    {
        namespace MyNamespace2
        {
            public class MyClass2 { }
            public class MyClass3
            {
                public class Inner { }
                public class inner { }
                public class iNner { }
                public class inNer { }
            }
            public class MyClass4 { }
            public class mYClass4 { }
            public class Myclass4 { }
            public class myCLass4 { }
            public class myClAss4 { }
        }

        namespace MyNamespace3
        {
            public class Foo { }
        }
        namespace MynAmespace3
        {
            public class Foo { }

            public class Goo<T> { }
            public class gOo<T> { }
            public class goO<T> { }
        }
        namespace MyNaMespace3
        {
            public class Foo { }
        }
        namespace MyNamEspace3
        {
            public class Foo { }
        }
    }

    public class MyClass1 { }
}
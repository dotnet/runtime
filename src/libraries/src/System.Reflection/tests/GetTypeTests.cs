// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class GetTypeTests
    {
        [Fact]
        public void GetType_EmptyString()
        {
            Assembly a = typeof(GetTypeTests).GetTypeInfo().Assembly;
            Module m = a.ManifestModule;

            string typeName = "";
            string aqn = ", " + typeof(GetTypeTests).GetTypeInfo().Assembly.FullName;

            // Type.GetType
            Assert.Null(Type.GetType(typeName));
            Assert.Null(Type.GetType(aqn));

            Assert.Null(Type.GetType(typeName, throwOnError: false));
            Assert.Null(Type.GetType(aqn, throwOnError: false));

            Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true));
            AssertExtensions.Throws<ArgumentException>("typeName@0", () => Type.GetType(aqn, throwOnError: true));

            Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: false));
            Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: true));
            Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: false));
            Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: true));

            Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
            Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: true));
            AssertExtensions.Throws<ArgumentException>("typeName@0", () => Type.GetType(aqn, throwOnError: true, ignoreCase: false));
            AssertExtensions.Throws<ArgumentException>("typeName@0", () => Type.GetType(aqn, throwOnError: true, ignoreCase: true));

            // Assembly.GetType
            AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(typeName));
            Assert.Null(a.GetType(aqn));

            AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(typeName, throwOnError: false, ignoreCase: false));
            AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(typeName, throwOnError: false, ignoreCase: true));
            Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
            Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

            AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(typeName, throwOnError: true, ignoreCase: false));
            AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(typeName, throwOnError: true, ignoreCase: true));
            AssertExtensions.Throws<ArgumentException>("typeName@0", () => a.GetType(aqn, throwOnError: true, ignoreCase: false));
            AssertExtensions.Throws<ArgumentException>("typeName@0", () => a.GetType(aqn, throwOnError: true, ignoreCase: true));

            // Module.GetType
            AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(typeName, throwOnError: false, ignoreCase: false));
            AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(typeName, throwOnError: false, ignoreCase: true));
            Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
            Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

            AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(typeName, throwOnError: true, ignoreCase: false));
            AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(typeName, throwOnError: true, ignoreCase: true));
            AssertExtensions.Throws<ArgumentException>("typeName@0", () => m.GetType(aqn, throwOnError: true, ignoreCase: false));
            AssertExtensions.Throws<ArgumentException>("typeName@0", () => m.GetType(aqn, throwOnError: true, ignoreCase: true));
        }

        public static IEnumerable<object[]> GetType_TestData()
        {
            yield return new object[] { "non-existent-type", null };
            yield return new object[] { typeof(MyClass1).FullName, typeof(MyClass1) };
            yield return new object[] { "System.Reflection.Tests.mYclAss1", typeof(MyClass1) };
            yield return new object[] { "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace99.MyClASs3+inNer", null };
            yield return new object[] { "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace2.MyClASs399+inNer", null };
            yield return new object[] { "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace2.MyClASs3+inNer99", null };
            yield return new object[] { typeof(MyNamespace1.MyNamespace2.MyClass2).FullName, typeof(MyNamespace1.MyNamespace2.MyClass2) };
            yield return new object[] { typeof(MyNamespace1.MyNamespace2.MyClass3.iNner).FullName, typeof(MyNamespace1.MyNamespace2.MyClass3.iNner) };
            yield return new object[] { "System.Reflection.Tests.MyNameSPACe1.MyNAMEspace2.MyClASs3+inNer", typeof(MyNamespace1.MyNamespace2.MyClass3.iNner) };
            yield return new object[] { typeof(MyNamespace1.MyNamespace3.Foo).FullName, typeof(MyNamespace1.MyNamespace3.Foo) };
            yield return new object[] { "System.Reflection.Tests.mynamespace1.mynamespace3.foo", typeof(MyNamespace1.MyNamespace3.Foo) };
            yield return new object[] { "System.Reflection.Tests.MYNAMESPACE1.MYNAMESPACE3.FOO", typeof(MyNamespace1.MyNamespace3.Foo) };

            Type type = typeof(MyNamespace1.MynAmespace3.Goo<int>);
            yield return new object[] { type.FullName, type };
            yield return new object[] { type.FullName.ToUpper(), type };
            yield return new object[] { type.FullName.ToLower(), type };
        }

        [Theory]
        [MemberData(nameof(GetType_TestData))]
        public void GetType(string typeName, Type expectedResult)
        {
            Assembly a = typeof(GetTypeTests).GetTypeInfo().Assembly;
            Module m = a.ManifestModule;

            string aqn = typeName + ", " + a.FullName;
            if (expectedResult == null)
            {
                // Type.GetType
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

                // Assembly.GetType
                Assert.Null(a.GetType(typeName));
                Assert.Null(a.GetType(aqn));

                Assert.Null(a.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Throws<TypeLoadException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: true));
                AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(aqn, throwOnError: true, ignoreCase: true));

                // Module.GetType
                Assert.Null(m.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(typeName, throwOnError: false, ignoreCase: true));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Throws<TypeLoadException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: true));
                AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(aqn, throwOnError: true, ignoreCase: true));
            }
            else if (expectedResult.FullName == typeName)
            {
                // Case-sensitive match.
                // Type.GetType
                Assert.Equal(expectedResult, Type.GetType(typeName));
                Assert.Equal(expectedResult, Type.GetType(aqn));

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: false));
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: false));

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: true));
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: true));

                // When called with "ignoreCase: true", GetType() may have a choice of matching items. The one that is chosen
                // is an implementation detail (and on the CLR, *very* implementation-dependent as it's influenced by the internal
                // layout of private hash tables.) As a result, we do not expect the same result across desktop and Project N
                // and so the best we can do is compare the names.
                string expectedName = expectedResult.AssemblyQualifiedName;

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(typeName, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(aqn, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);

                Assert.Equal(expectedResult, Type.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(typeName, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedResult, Type.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(aqn, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);

                // Assembly.GetType
                Assert.Equal(expectedResult, a.GetType(typeName));
                Assert.Null(a.GetType(aqn));

                Assert.Equal(expectedResult, a.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, a.GetType(typeName, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Equal(expectedResult, a.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, a.GetType(typeName, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(aqn, throwOnError: true, ignoreCase: true));

                // Module.GetType
                Assert.Equal(expectedResult, m.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, m.GetType(typeName, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Equal(expectedResult, m.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, m.GetType(typeName, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(aqn, throwOnError: true, ignoreCase: true));
            }
            else if (expectedResult.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            {
                // Case-insensitive match.
                // Type.GetType
                Assert.Null(Type.GetType(typeName));
                Assert.Null(Type.GetType(aqn));

                Assert.Null(Type.GetType(typeName, throwOnError: false));
                Assert.Null(Type.GetType(aqn, throwOnError: false));

                Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true));
                Assert.Throws<TypeLoadException>(() => Type.GetType(aqn, throwOnError: true));

                // When called with "ignoreCase: true", GetType() may have a choice of matching items. The one that is chosen
                // is an implementation detail (and on the CLR, *very* implementation-dependent as it's influenced by the internal
                // layout of private hash tables.) As a result, we do not expect the same result across desktop and Project N
                // and so the best we can do is compare the names.
                string expectedName = expectedResult.AssemblyQualifiedName;

                Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(typeName, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Null(Type.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(aqn, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);

                Assert.Throws<TypeLoadException>(() => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(typeName, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Throws<TypeLoadException>(() => Type.GetType(aqn, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, Type.GetType(aqn, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);

                // Assembly.GetType
                Assert.Null(a.GetType(typeName));
                Assert.Null(a.GetType(aqn));

                Assert.Null(a.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, a.GetType(typeName, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(a.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => a.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, a.GetType(typeName, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(aqn, throwOnError: true, ignoreCase: true));

                // Module.GetType
                Assert.Null(m.GetType(typeName, throwOnError: false, ignoreCase: false));
                Assert.Equal(expectedName, m.GetType(typeName, throwOnError: false, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: false));
                Assert.Null(m.GetType(aqn, throwOnError: false, ignoreCase: true));

                Assert.Throws<TypeLoadException>(() => m.GetType(typeName, throwOnError: true, ignoreCase: false));
                Assert.Equal(expectedName, m.GetType(typeName, throwOnError: true, ignoreCase: true).AssemblyQualifiedName, StringComparer.OrdinalIgnoreCase);
                AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(aqn, throwOnError: true, ignoreCase: false));
                AssertExtensions.Throws<ArgumentException>(null, () => m.GetType(aqn, throwOnError: true, ignoreCase: true));
            }
            else
            {
                throw new InvalidOperationException("TEST ERROR.");
            }
        }

        [Fact]
        public void GetType_CoreAssembly()
        {
            Assert.Equal(typeof(int), Type.GetType("System.Int32", throwOnError: true));
            Assert.Equal(typeof(int), Type.GetType("system.int32", throwOnError: true, ignoreCase: true));
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

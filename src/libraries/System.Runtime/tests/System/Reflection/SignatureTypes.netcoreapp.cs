// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static class SignatureTypeTests
    {
        [Fact]
        public static void IsSignatureType()
        {
            // Executing [Theory] logic manually. Signature Types cannot be used in theory data because Xunit preemptively invokes an unguarded
            // System.Type pretty printer that invokes members that Signature Types don't support.
            foreach (object[] pair in IsSignatureTypeTestData)
            {
                Type type = (Type)(pair[0]);
                bool expected = (bool)(pair[1]);

                Assert.Equal(expected, type.IsSignatureType);
            }
        }

        public static IEnumerable<object[]> IsSignatureTypeTestData
        {
            get
            {
                yield return new object[] { typeof(int), false };
                yield return new object[] { typeof(int).MakeArrayType(), false };
                yield return new object[] { typeof(int).MakeArrayType(1), false };
                yield return new object[] { typeof(int).MakeArrayType(2), false };
                yield return new object[] { typeof(int).MakeByRefType(), false };
                yield return new object[] { typeof(int).MakePointerType(), false };
                yield return new object[] { typeof(List<>).MakeGenericType(typeof(int)), false };
                yield return new object[] { typeof(List<>).GetGenericArguments()[0], false };

                Type sigType = Type.MakeGenericMethodParameter(2);

                yield return new object[] { sigType, true };
                yield return new object[] { sigType.MakeArrayType(), true };
                yield return new object[] { sigType.MakeArrayType(1), true };
                yield return new object[] { sigType.MakeArrayType(2), true };
                yield return new object[] { sigType.MakeByRefType(), true };
                yield return new object[] { sigType.MakePointerType(), true };
                yield return new object[] { typeof(List<>).MakeGenericType(sigType), true };

                yield return new object[] { Type.MakeGenericSignatureType(typeof(List<>), typeof(int)), true };
                yield return new object[] { Type.MakeGenericSignatureType(typeof(List<>), sigType), true };
            }
        }

        [Fact]
        public static void GetMethodWithGenericParameterCount()
        {
            Type t = typeof(TestClass1);
            const BindingFlags bf = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
            Type[] args = { typeof(int) };
            MethodInfo m;

            Assert.Throws<AmbiguousMatchException>(() => t.GetMethod("Moo", bf, null, args, null));

            for (int genericParameterCount = 0; genericParameterCount < 4; genericParameterCount++)
            {
                m = t.GetMethod("Moo", genericParameterCount, bf, null, args, null);
                Assert.NotNull(m);
                AssertIsMarked(m, genericParameterCount);

                // Verify that generic parameter count filtering occurs before candidates are passed to the binder.
                m = t.GetMethod("Moo", genericParameterCount, bf, new InflexibleBinder(genericParameterCount), args, null);
                Assert.NotNull(m);
                AssertIsMarked(m, genericParameterCount);
            }

            m = t.GetMethod("Moo", 4, bf, null, args, null);
            Assert.Null(m);
        }

        private sealed class InflexibleBinder : Binder
        {
            public InflexibleBinder(int genericParameterCount)
            {
                _genericParameterCount = genericParameterCount;
            }

            public sealed override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture) => throw null;
            public sealed override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state) => throw null;
            public sealed override object ChangeType(object value, Type type, CultureInfo culture) => throw null;
            public sealed override void ReorderArgumentArray(ref object[] args, object state) => throw null;

            public sealed override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
            {
                foreach (MethodBase methodBase in match)
                {
                    Assert.True(methodBase is MethodInfo methodInfo && methodInfo.GetGenericArguments().Length == _genericParameterCount);
                }
                return Type.DefaultBinder.SelectMethod(bindingAttr, match, types, modifiers);
            }

            public sealed override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers) { throw null; }

            private readonly int _genericParameterCount;
        }

        [Fact]
        public static void GetMethodWithNegativeGenericParameterCount()
        {
            Type t = typeof(TestClass1);
            const BindingFlags bf = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
            Type[] args = { typeof(int) };
            Assert.Throws<ArgumentException>(() => t.GetMethod("Moo", -1, bf, null, args, null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void GetMethodOverloadTest(bool exactBinding)
        {
            Type t = typeof(TestClass2);
            BindingFlags bf = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
            if (exactBinding)
            {
                bf |= BindingFlags.ExactBinding;
            }
            Type[] args = { Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1).MakeArrayType() };
            MethodInfo moo = t.GetMethod("Moo", 2, bf, null, args, null);
            AssertIsMarked(moo, 3);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void SignatureTypeComparisonLogicCodeCoverage(bool exactBinding)
        {
            Type t = typeof(TestClass3<,>);
            MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            BindingFlags bf = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
            if (exactBinding)
            {
                bf |= BindingFlags.ExactBinding;
            }

            foreach (MethodInfo m in methods)
            {
                ParameterInfo[] parameters = m.GetParameters();
                Type[] sigTypes = new Type[parameters.Length];
                for (int i = 0; i < sigTypes.Length; i++)
                {
                    sigTypes[i] = parameters[i].ParameterType.ToSignatureType();
                }
                MethodInfo match = t.GetMethod("Moo", m.GetGenericArguments().Length, bf, null, sigTypes, null);
                Assert.NotNull(match);
                Assert.True(m.HasSameMetadataDefinitionAs(match));
            }
        }

        [Fact]
        public static void SigTypeResolutionResilience()
        {
            // Make sure the framework can't be tricked into throwing an exception because it tried to look up a nonexistent method generic parameter
            // or trying to construct a generic type where the constraints don't validate.
            Type t = typeof(TestClass4<>);
            Type[] args = { typeof(TestClass4<>).MakeGenericType(Type.MakeGenericMethodParameter(1)), Type.MakeGenericMethodParameter(500) };
            CountingBinder binder = new CountingBinder();
            Assert.Null(t.GetMethod("Moo", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, binder, args, null));
            Assert.Equal(3, binder.NumCandidatesReceived);
        }

        private sealed class CountingBinder : Binder
        {
            public sealed override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture) => throw null;
            public sealed override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state) => throw null;
            public sealed override object ChangeType(object value, Type type, CultureInfo culture) => throw null;
            public sealed override void ReorderArgumentArray(ref object[] args, object state) => throw null;

            public sealed override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
            {
                NumCandidatesReceived += match.Length;
                return Type.DefaultBinder.SelectMethod(bindingAttr, match, types, modifiers);
            }

            public sealed override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers) { throw null; }

            public int NumCandidatesReceived { get; private set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(400)]
        [InlineData(int.MaxValue)]
        public static void MakeGenericMethodParameter(int position)
        {
            Type t = Type.MakeGenericMethodParameter(position);
            Assert.True(t.IsGenericParameter);
            Assert.False(t.IsGenericTypeParameter);
            Assert.True(t.IsGenericMethodParameter);
            Assert.Equal(position, t.GenericParameterPosition);
            TestSignatureTypeInvariants(t);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-5)]
        [InlineData(int.MinValue)]
        public static void MakeGenericMethodParameterNegative(int position)
        {
            Assert.Throws<ArgumentException>(() => Type.MakeGenericMethodParameter(position));
        }

        [Fact]
        public static void MakeSignatureArrayType()
        {
            Type t = Type.MakeGenericMethodParameter(5);
            t = t.MakeArrayType();
            Assert.True(t.IsArray);
            Assert.True(t.IsSZArray);
            Assert.Equal(1, t.GetArrayRank());

            Type et = t.GetElementType();
            Assert.True(et.IsSignatureType);
            Assert.True(et.IsGenericParameter);
            Assert.False(et.IsGenericTypeParameter);
            Assert.True(et.IsGenericMethodParameter);
            Assert.Equal(5, et.GenericParameterPosition);

            TestSignatureTypeInvariants(t);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public static void MakeSignatureMdArrayType(int rank)
        {
            Type t = Type.MakeGenericMethodParameter(5);
            t = t.MakeArrayType(rank);
            Assert.True(t.IsArray);
            Assert.True(t.IsVariableBoundArray);
            Assert.Equal(rank, t.GetArrayRank());

            TestSignatureTypeInvariants(t);
        }

        [Fact]
        public static void MakeSignatureByRefType()
        {
            Type t = Type.MakeGenericMethodParameter(5);
            t = t.MakeByRefType();
            Assert.True(t.IsByRef);

            Type et = t.GetElementType();
            Assert.True(et.IsSignatureType);
            Assert.True(et.IsGenericParameter);
            Assert.False(et.IsGenericTypeParameter);
            Assert.True(et.IsGenericMethodParameter);
            Assert.Equal(5, et.GenericParameterPosition);

            TestSignatureTypeInvariants(t);
        }

        [Fact]
        public static void MakeSignaturePointerType()
        {
            Type t = Type.MakeGenericMethodParameter(5);
            t = t.MakePointerType();
            Assert.True(t.IsPointer);

            Type et = t.GetElementType();
            Assert.True(et.IsSignatureType);
            Assert.True(et.IsGenericParameter);
            Assert.False(et.IsGenericTypeParameter);
            Assert.True(et.IsGenericMethodParameter);
            Assert.Equal(5, et.GenericParameterPosition);

            TestSignatureTypeInvariants(t);
        }

        [Theory]
        [InlineData(typeof(List<>))]
        [InlineData(typeof(Span<>))]
        public static void MakeSignatureConstructedGenericType(Type genericTypeDefinition)
        {
            Type gmp = Type.MakeGenericMethodParameter(5);

            Type[] testTypes = { genericTypeDefinition.MakeGenericType(gmp), Type.MakeGenericSignatureType(genericTypeDefinition, gmp) };
            Assert.All(testTypes,
                (Type t) =>
                {
                    Assert.True(t.IsConstructedGenericType);
                    Assert.Equal(genericTypeDefinition, t.GetGenericTypeDefinition());
                    Assert.Equal(1, t.GenericTypeArguments.Length);

                    Type et = t.GenericTypeArguments[0];
                    Assert.True(et.IsSignatureType);
                    Assert.True(et.IsGenericParameter);
                    Assert.False(et.IsGenericTypeParameter);
                    Assert.True(et.IsGenericMethodParameter);
                    Assert.Equal(5, et.GenericParameterPosition);

                    TestSignatureTypeInvariants(t);
                });
        }

        [Fact]
        public static void MakeGenericSignatureTypeValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("genericTypeDefinition", () => Type.MakeGenericSignatureType(null));
            AssertExtensions.Throws<ArgumentNullException>("typeArguments", () => Type.MakeGenericSignatureType(typeof(IList<>), typeArguments: null));
            AssertExtensions.Throws<ArgumentNullException>("typeArguments", () => Type.MakeGenericSignatureType(typeof(IList<>), new Type[] { null }));
        }

        private static Type ToSignatureType(this Type type)
        {
            if (type.IsTypeDefinition)
                return type;
            if (type.IsSZArray)
                return type.GetElementType().ToSignatureType().MakeArrayType();
            if (type.IsVariableBoundArray)
                return type.GetElementType().ToSignatureType().MakeArrayType(type.GetArrayRank());
            if (type.IsByRef)
                return type.GetElementType().ToSignatureType().MakeByRefType();
            if (type.IsPointer)
                return type.GetElementType().ToSignatureType().MakePointerType();
            if (type.IsConstructedGenericType)
            {
                Type[] genericTypeArguments = type.GenericTypeArguments.Select(t => t.ToSignatureType()).ToArray();
                return type.GetGenericTypeDefinition().MakeGenericType(genericTypeArguments);
            }
            if (type.IsGenericTypeParameter)
                return type;
            if (type.IsGenericMethodParameter)
                return Type.MakeGenericMethodParameter(type.GenericParameterPosition);

            throw new Exception("Unknown type flavor.");
        }

        private static void AssertIsMarked(MemberInfo member, int value)
        {
            MarkerAttribute marker = member.GetCustomAttribute<MarkerAttribute>(inherit: false);
            Assert.NotNull(marker);
            Assert.Equal(value, marker.Value);
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false)]
        private sealed class MarkerAttribute : Attribute
        {
            public MarkerAttribute(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        private sealed class TestClass1
        {
            [Marker(0)] public static void Moo(int x) { }
            [Marker(1)] public static void Moo<T1>(int x) { }
            [Marker(2)] public static void Moo<T1, T2>(int x) { }
            [Marker(3)] public static void Moo<T1, T2, T3>(int x) {}
        }

        private class TestClass2
        {
            [Marker(0)] public static void Moo(int x, int[] y) { }
            [Marker(1)] public static void Moo<T>(T x, T[] y) { }
            [Marker(2)] public static void Moo<T>(int x, int[] y) { }
            [Marker(3)] public static void Moo<T, U>(T x, U[] y) { } 
            [Marker(4)] public static void Moo<T, U>(int x, int[] y) { }
        }

        private class TestClass3<T,U>
        {
            public static void Moo(T p1, T[] p2, T[,] p3, ref T p4, TestClass3<T, T> p5, ref TestClass3<T, T[]>[,] p6) { }
            public static void Moo(U p1, U[] p2, U[,] p3, ref U p4, TestClass3<U, U> p5, ref TestClass3<U, U[]>[,] p6) { }
            public static void Moo<M>(T p1, T[] p2, T[,] p3, ref T p4, TestClass3<T, T> p5, ref TestClass3<T, T[]>[,] p6) { }
            public static void Moo<M>(U p1, U[] p2, U[,] p3, ref U p4, TestClass3<U, U> p5, ref TestClass3<U, U[]>[,] p6) { }
            public static void Moo<M>(M p1, M[] p2, M[,] p3, ref M p4, TestClass3<M, M> p5, ref TestClass3<M, M[]>[,] p6) { }
            public static void Moo<M, N>(T p1, T[] p2, T[,] p3, ref T p4, TestClass3<T, T> p5, ref TestClass3<T, T[]>[,] p6) { }
            public static void Moo<M, N>(U p1, U[] p2, U[,] p3, ref U p4, TestClass3<U, U> p5, ref TestClass3<U, U[]>[,] p6) { }
            public static void Moo<M, N>(M p1, M[] p2, M[,] p3, ref M p4, TestClass3<M, M> p5, ref TestClass3<M, M[]>[,] p6) { }
            public static void Moo<M, N>(N p1, N[] p2, N[,] p3, ref N p4, TestClass3<N, N> p5, ref TestClass3<N, N[]>[,] p6) { }
        }

        private class TestClass4<T> where T: NoOneSubclasses, new()
        {
            public static void Moo<M>(int p1, int p2) where M : NoOneSubclassesThisEither { }
            public static void Moo<N, O>(TestClass4<N> p1, int p2) where N : NoOneSubclasses, new() { }
            public static void Moo<N, O>(O p1, int p2) where N : NoOneSubclasses, new() { }
        }

        private class NoOneSubclasses { }
        private class NoOneSubclassesThisEither { }

        private static void TestSignatureTypeInvariants(Type type)
        {
            Assert.True(type.IsSignatureType);
            Assert.False(type.IsTypeDefinition);
            Assert.False(type.IsGenericTypeDefinition);
            Assert.NotNull(type.Name);
            Assert.Null(type.FullName);
            Assert.Null(type.AssemblyQualifiedName);
            Assert.NotNull(type.ToString());
            Assert.Equal(MemberTypes.TypeInfo, type.MemberType);
            Assert.Same(type, type.UnderlyingSystemType);

            // SignatureTypes don't override Equality/GetHashCode at this time, but they don't promise never to do so either. 
            // Thus, we'll only test the most basic behavior.
            Assert.True(type.Equals((object)type));
            Assert.True(type.Equals((Type)type));
            Assert.False(type.Equals((object)null));
            Assert.False(type.Equals((Type)null));
            int _ = type.GetHashCode();

            bool categorized = false;
            if (type.IsArray)
            {
                Assert.False(categorized);
                categorized = true;
                Assert.True(type.HasElementType);
                Assert.True(type.IsSZArray != type.IsVariableBoundArray);
                Assert.Equal(type.GetElementType().ContainsGenericParameters, type.ContainsGenericParameters);
                string elementTypeName = type.GetElementType().Name;
                if (type.IsSZArray)
                {
                    Assert.Equal(1, type.GetArrayRank());

                    Assert.Equal(elementTypeName + "[]", type.Name);
                }
                else
                {
                    int rank = type.GetArrayRank();
                    Assert.True(rank >= 1);
                    if (rank == 1)
                    {
                        Assert.Equal(elementTypeName + "[*]", type.Name);
                    }
                    else
                    {
                        Assert.Equal(elementTypeName + "[" + new string(',', rank - 1) + "]", type.Name);
                    }
                }
            }

            if (type.IsByRef)
            {
                Assert.False(categorized);
                categorized = true;

                Assert.True(type.HasElementType);
                Assert.Equal(type.GetElementType().ContainsGenericParameters, type.ContainsGenericParameters);
                string elementTypeName = type.GetElementType().Name;
                Assert.Equal(elementTypeName + "&", type.Name);
            }

            if (type.IsPointer)
            {
                Assert.False(categorized);
                categorized = true;

                Assert.True(type.HasElementType);
                Assert.Equal(type.GetElementType().ContainsGenericParameters, type.ContainsGenericParameters);
                string elementTypeName = type.GetElementType().Name;
                Assert.Equal(elementTypeName + "*", type.Name);
            }

            if (type.IsConstructedGenericType)
            {
                Assert.False(categorized);
                categorized = true;

                Assert.True(type.IsGenericType);
                Assert.False(type.HasElementType);
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                Assert.Equal(genericTypeDefinition.IsByRefLike, type.IsByRefLike);
                Assert.NotNull(genericTypeDefinition);
                Assert.True(genericTypeDefinition.IsGenericTypeDefinition);
                Type[] genericTypeArguments = type.GetGenericArguments();
                Type[] genericTypeArgumentsClone = type.GetGenericArguments();
                Assert.NotSame(genericTypeArguments, genericTypeArgumentsClone);
                Type[] genericTypeArgumentsFromProperty = type.GenericTypeArguments;
                Type[] genericTypeArgumentsFromPropertyClone = type.GenericTypeArguments;
                Assert.NotSame(genericTypeArgumentsFromProperty, genericTypeArgumentsFromPropertyClone);
                for (int i = 0; i < genericTypeArguments.Length; i++)
                {
                    if (genericTypeArguments[i].IsSignatureType)
                    {
                        TestSignatureTypeInvariants(genericTypeArguments[i]);
                    }
                }
                Assert.Equal(genericTypeDefinition.Name, type.Name);
                Assert.Equal(genericTypeDefinition.Namespace, type.Namespace);


                bool containsGenericParameters = false;
                for (int i = 0; i < genericTypeArguments.Length; i++)
                {
                    containsGenericParameters = containsGenericParameters || genericTypeArguments[i].ContainsGenericParameters;
                }
                Assert.Equal(containsGenericParameters, type.ContainsGenericParameters);
            }

            if (type.IsGenericParameter)
            {
                Assert.False(categorized);
                categorized = true;

                Assert.False(type.HasElementType);
                Assert.True(type.ContainsGenericParameters);
                Assert.Null(type.Namespace);

                int position = type.GenericParameterPosition;
                Assert.True(position >= 0);

                if (type.IsGenericTypeParameter)
                {
                    throw new Exception("Unexpected: There is no mechanism at this time to create Signature Types of generic parameters on types.");    
                }
                else
                {
                    Assert.True(type.IsGenericMethodParameter);
                    Assert.Equal("!!" + position, type.Name);
                }
            }

            Assert.True(categorized);

            if (type.HasElementType)
            {
                TestSignatureTypeInvariants(type.GetElementType());
                Assert.Equal(type.GetElementType().Namespace, type.Namespace);
            }
            else
            {
                Assert.Null(type.GetElementType());
            }

            if (!type.IsConstructedGenericType)
            {
                Assert.Throws<InvalidOperationException>(() => type.GetGenericTypeDefinition());
                Assert.Equal(0, type.GetGenericArguments().Length);
                Assert.Equal(0, type.GenericTypeArguments.Length);
                Assert.False(type.IsGenericType);
                Assert.False(type.IsByRefLike);
            }

            if (!type.IsArray)
            {
                Assert.Throws<ArgumentException>(() => type.GetArrayRank());
            }

            if (!type.IsGenericParameter)
            {
                Assert.False(type.IsGenericTypeParameter);
                Assert.False(type.IsGenericMethodParameter);
                Assert.Throws<InvalidOperationException>(() => type.GenericParameterPosition);
            }
        }
    }
}

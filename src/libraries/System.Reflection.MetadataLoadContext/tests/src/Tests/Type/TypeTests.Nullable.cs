// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        [Fact]
        public static void GetNullableUnderlyingType_MetadataLoadContext_NullableInt_ReturnsUnderlyingType()
        {
            string coreAssemblyPath = TestUtils.GetPathToCoreAssembly();
            var resolver = new PathAssemblyResolver([coreAssemblyPath]);
            using var mlc = new MetadataLoadContext(resolver, TestUtils.GetNameOfCoreAssembly());

            Assembly coreAssembly = mlc.LoadFromAssemblyPath(coreAssemblyPath);
            Type intType = coreAssembly.GetType("System.Int32", throwOnError: true)!;
            Type nullableIntType = coreAssembly.GetType("System.Nullable`1", throwOnError: true)!.MakeGenericType(intType);

            Type? underlying = Nullable.GetUnderlyingType(nullableIntType);
            Assert.NotNull(underlying);
            Assert.Equal("System.Int32", underlying.FullName);
            Assert.Same(intType, underlying);
            Assert.NotSame(typeof(int), underlying);

            Type? underlyingDirect = nullableIntType.GetNullableUnderlyingType();
            Assert.NotNull(underlyingDirect);
            Assert.Equal("System.Int32", underlyingDirect.FullName);
            Assert.Same(intType, underlyingDirect);
            Assert.NotSame(typeof(int), underlyingDirect);
        }

        [Fact]
        public static void GetNullableUnderlyingType_MetadataLoadContext_NonNullableTypes_ReturnsNull()
        {
            string coreAssemblyPath = TestUtils.GetPathToCoreAssembly();
            var resolver = new PathAssemblyResolver([coreAssemblyPath]);
            using var mlc = new MetadataLoadContext(resolver, TestUtils.GetNameOfCoreAssembly());

            Assembly coreAssembly = mlc.LoadFromAssemblyPath(coreAssemblyPath);
            Type intType = coreAssembly.GetType("System.Int32", throwOnError: true)!;
            Type stringType = coreAssembly.GetType("System.String", throwOnError: true)!;

            Assert.Null(Nullable.GetUnderlyingType(intType));
            Assert.Null(Nullable.GetUnderlyingType(stringType));

            Assert.Null(intType.GetNullableUnderlyingType());
            Assert.Null(stringType.GetNullableUnderlyingType());
        }

        [Fact]
        public static void GetNullableUnderlyingType_MetadataLoadContext_OpenNullable()
        {
            string coreAssemblyPath = TestUtils.GetPathToCoreAssembly();
            var resolver = new PathAssemblyResolver([coreAssemblyPath]);
            using var mlc = new MetadataLoadContext(resolver, TestUtils.GetNameOfCoreAssembly());

            Assembly coreAssembly = mlc.LoadFromAssemblyPath(coreAssemblyPath);
            Type openNullableType = coreAssembly.GetType("System.Nullable`1", throwOnError: true)!;

            // Nullable.GetUnderlyingType returns null for generic type definitions (COMPAT).
            Assert.Null(Nullable.GetUnderlyingType(openNullableType));

            // Type.GetNullableUnderlyingType returns the generic type parameter T for Nullable<>.
            Type? underlying = openNullableType.GetNullableUnderlyingType();
            Assert.NotNull(underlying);
            Assert.Same(openNullableType.GetGenericArguments()[0], underlying);
        }
    }
}

#endif // NET

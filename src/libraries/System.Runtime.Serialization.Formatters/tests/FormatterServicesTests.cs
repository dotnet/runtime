// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public partial class FormatterServicesTests
    {
        [Fact]
        public void CheckTypeSecurity_Nop()
        {
            FormatterServices.CheckTypeSecurity(typeof(int), TypeFilterLevel.Full);
            FormatterServices.CheckTypeSecurity(typeof(int), TypeFilterLevel.Low);
        }

        [Fact]
        public void GetSerializableMembers_InvalidArguments_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => FormatterServices.GetSerializableMembers(null));
        }

        [Fact]
        public void GetSerializableMembers_Interface()
        {
            Assert.Equal<MemberInfo>(new MemberInfo[0], FormatterServices.GetSerializableMembers(typeof(IDisposable)));
        }

        [Fact]
        public void GetUninitializedObject_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => FormatterServices.GetUninitializedObject(null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => FormatterServices.GetSafeUninitializedObject(null));
        }

        [Fact]
        public void GetUninitializedObject_NonRuntimeType_ThrowsSerializationException()
        {
            Assert.Throws<SerializationException>(() => FormatterServices.GetUninitializedObject(new NonRuntimeType()));
            Assert.Throws<SerializationException>(() => FormatterServices.GetSafeUninitializedObject(new NonRuntimeType()));
        }

        public static IEnumerable<object[]> GetUninitializedObject_NotSupportedType_TestData()
        {
            yield return new object[] { typeof(int[]) };
            yield return new object[] { typeof(int[,]) };
            yield return new object[] { typeof(int).MakePointerType() };
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15113", TestRuntimes.Mono)]
        [MemberData(nameof(GetUninitializedObject_NotSupportedType_TestData))]
        public void GetUninitializedObject_NotSupportedType_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => FormatterServices.GetUninitializedObject(type));
            AssertExtensions.Throws<ArgumentException>(null, () => FormatterServices.GetSafeUninitializedObject(type));
        }

        [Theory]
        [InlineData(typeof(AbstractClass))]
        [InlineData(typeof(StaticClass))]
        [InlineData(typeof(Interface))]
        [InlineData(typeof(Array))]
        public void GetUninitializedObject_AbstractClass_ThrowsMemberAccessException(Type type)
        {
            Assert.Throws<MemberAccessException>(() => FormatterServices.GetUninitializedObject(type));
            Assert.Throws<MemberAccessException>(() => FormatterServices.GetSafeUninitializedObject(type));
        }

        public abstract class AbstractClass { }
        public static class StaticClass { }
        public interface Interface { }

        public static IEnumerable<object[]> GetUninitializedObject_OpenGenericClass_TestData()
        {
            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>)) };
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15111", TestRuntimes.Mono)]
        [MemberData(nameof(GetUninitializedObject_OpenGenericClass_TestData))]
        public void GetUninitializedObject_OpenGenericClass_ThrowsMemberAccessException(Type type)
        {
            Assert.Throws<MemberAccessException>(() => FormatterServices.GetUninitializedObject(type));
            Assert.Throws<MemberAccessException>(() => FormatterServices.GetSafeUninitializedObject(type));
        }

        public interface IGenericClass
        {
            int Value { get; set; }
        }

        public class GenericClass<T> : IGenericClass
        {
            public int Value { get; set; }
        }

        public static IEnumerable<object[]> GetUninitializedObject_ByRefLikeType_TestData()
        {
            yield return new object[] { typeof(TypedReference) };

            yield return new object[] { typeof(RuntimeArgumentHandle) };

            // .NET Standard 2.0 doesn't have ArgIterator, but .NET Core 2.0 does
            Type argIterator = typeof(object).Assembly.GetType("System.ArgIterator");
            if (argIterator != null)
            {
                yield return new object[] { argIterator };
            }
        }

        public static IEnumerable<object[]> GetUninitializedObject_ByRefLikeType_NetCore_TestData()
        {
            yield return new object[] { typeof(Span<int>) };
            yield return new object[] { typeof(ReadOnlySpan<int>) };
            yield return new object[] { typeof(StructWithSpanField) };
        }

#pragma warning disable 0169 // The private field 'class member' is never used
        private ref struct StructWithSpanField
        {
            Span<byte> _bytes;
            int _position;
        }
#pragma warning restore 0169

        [Theory]
        [MemberData(nameof(GetUninitializedObject_ByRefLikeType_NetCore_TestData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp, "Some runtimes don't support or recognise Span<T>, ReadOnlySpan<T> or ByReference<T> as ref types.")]
        public void GetUninitializedObject_ByRefLikeType_NetCore_ThrowsNotSupportedException(Type type)
        {
            Assert.Throws<NotSupportedException>(() => FormatterServices.GetUninitializedObject(type));
            Assert.Throws<NotSupportedException>(() => FormatterServices.GetSafeUninitializedObject(type));
        }

        [Theory]
        [MemberData(nameof(GetUninitializedObject_ByRefLikeType_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework has bug that allows allocating instances of byref-like types.")]
        public void GetUninitializedObject_ByRefLikeType_NonNetfx_ThrowsNotSupportedException(Type type)
        {
            Assert.Throws<NotSupportedException>(() => FormatterServices.GetUninitializedObject(type));
            Assert.Throws<NotSupportedException>(() => FormatterServices.GetSafeUninitializedObject(type));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp, "The coreclr doesn't support GetUninitializedObject for shared generic instances")]
        [SkipOnMonoAttribute("System.__Canon doesn't exist on Mono")]
        public void GetUninitializedObject_SharedGenericInstance_NetCore_ThrowsNotSupportedException()
        {
            Type canonType = Type.GetType("System.__Canon");
            Assert.NotNull(canonType);
            Type sharedGenericInstance = typeof(GenericClass<>).MakeGenericType(canonType);
            Assert.Throws<NotSupportedException>(() => FormatterServices.GetUninitializedObject(sharedGenericInstance));
            Assert.Throws<NotSupportedException>(() => FormatterServices.GetSafeUninitializedObject(sharedGenericInstance));
        }

        [Fact]
        public void GetUninitializedObject_NullableType_InitializesValue()
        {
            int? nullableUnsafe = (int?)FormatterServices.GetUninitializedObject(typeof(int?));
            Assert.True(nullableUnsafe.HasValue);
            Assert.Equal(0, nullableUnsafe.Value);

            int? nullableSafe = (int?)FormatterServices.GetSafeUninitializedObject(typeof(int?));
            Assert.True(nullableSafe.HasValue);
            Assert.Equal(0, nullableSafe.Value);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The .NET Framework doesn't support GetUninitializedObject for subclasses of ContextBoundObject")]
        public void GetUninitializedObject_ContextBoundObjectSubclass_NetCore_InitializesValue()
        {
            Assert.Equal(0, ((ContextBoundSubclass)FormatterServices.GetUninitializedObject(typeof(ContextBoundSubclass))).Value);
            Assert.Equal(0, ((ContextBoundSubclass)FormatterServices.GetSafeUninitializedObject(typeof(ContextBoundSubclass))).Value);
        }

        public class ContextBoundSubclass : ContextBoundObject
        {
            public int Value { get; set; }
        }

        [Fact]
        public void GetUninitializedObject_TypeHasDefaultConstructor_DoesNotRunConstructor()
        {
            Assert.Equal(42, new ObjectWithDefaultConstructor().Value);
            Assert.Equal(0, ((ObjectWithDefaultConstructor)FormatterServices.GetUninitializedObject(typeof(ObjectWithDefaultConstructor))).Value);
            Assert.Equal(0, ((ObjectWithDefaultConstructor)FormatterServices.GetSafeUninitializedObject(typeof(ObjectWithDefaultConstructor))).Value);
        }

        private class ObjectWithDefaultConstructor
        {
            public ObjectWithDefaultConstructor()
            {
                Value = 42;
            }

            public int Value;
        }

        [Fact]
        public void GetUninitializedObject_StaticConstructor_CallsStaticConstructor()
        {
            Assert.Equal(2, ((ObjectWithStaticConstructor)FormatterServices.GetUninitializedObject(typeof(ObjectWithStaticConstructor))).GetValue());
        }

        private class ObjectWithStaticConstructor
        {
            private static int s_value = 1;

            static ObjectWithStaticConstructor()
            {
                s_value = 2;
            }

            public int GetValue() => s_value;
        }

        [Fact]
        public void GetUninitializedObject_StaticField_InitializesStaticFields()
        {
            Assert.Equal(1, ((ObjectWithStaticField)FormatterServices.GetUninitializedObject(typeof(ObjectWithStaticField))).GetValue());
        }

        private class ObjectWithStaticField
        {
            private static int s_value = 1;

            public int GetValue() => s_value;
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15114", TestRuntimes.Mono)]
        public void GetUninitializedObject_StaticConstructorThrows_ThrowsTypeInitializationException()
        {
            TypeInitializationException ex = Assert.Throws<TypeInitializationException>(() => FormatterServices.GetUninitializedObject(typeof(StaticConstructorThrows)));
            Assert.IsType<DivideByZeroException>(ex.InnerException);
        }

        private class StaticConstructorThrows
        {
            static StaticConstructorThrows()
            {
                throw new DivideByZeroException();
            }
        }

        [Fact]
        public void GetUninitializedObject_ClassFieldWithDefaultValue_DefaultValueIgnored()
        {
            Assert.Equal(42, new ObjectWithStructDefaultField().Value);
            Assert.Null(((ObjectWithClassDefaultField)FormatterServices.GetUninitializedObject(typeof(ObjectWithClassDefaultField))).Value);
            Assert.Null(((ObjectWithClassDefaultField)FormatterServices.GetSafeUninitializedObject(typeof(ObjectWithClassDefaultField))).Value);
        }

        private class ObjectWithClassDefaultField
        {
            public string Value = "abc";
        }

        [Fact]
        public void GetUninitializedObject_StructFieldWithDefaultValue_DefaultValueIgnored()
        {
            Assert.Equal(42, new ObjectWithStructDefaultField().Value);
            Assert.Equal(0, ((ObjectWithStructDefaultField)FormatterServices.GetUninitializedObject(typeof(ObjectWithStructDefaultField))).Value);
            Assert.Equal(0, ((ObjectWithStructDefaultField)FormatterServices.GetSafeUninitializedObject(typeof(ObjectWithStructDefaultField))).Value);
        }

        private class ObjectWithStructDefaultField
        {
            public int Value = 42;
        }

        [Fact]
        public void PopulateObjectMembers_InvalidArguments_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => FormatterServices.PopulateObjectMembers(null, new MemberInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("members", () => FormatterServices.PopulateObjectMembers(new object(), null, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("data", () => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[0], null));
            AssertExtensions.Throws<ArgumentException>(null, () => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[1], new object[2]));
            AssertExtensions.Throws<ArgumentNullException>("members", () => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[1], new object[1]));
            Assert.Throws<SerializationException>(() => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[] { typeof(object).GetMethod("GetHashCode") }, new object[] { new object() }));
        }

        [Fact]
        public void GetObjectData_InvalidArguments_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => FormatterServices.GetObjectData(null, new MemberInfo[0]));
            AssertExtensions.Throws<ArgumentNullException>("members", () => FormatterServices.GetObjectData(new object(), null));
            AssertExtensions.Throws<ArgumentNullException>("members", () => FormatterServices.GetObjectData(new object(), new MemberInfo[1]));
            Assert.Throws<SerializationException>(() => FormatterServices.GetObjectData(new object(), new MethodInfo[] { typeof(object).GetMethod("GetHashCode") }));
        }

        [Fact]
        public void GetSurrogateForCyclicalReference_InvalidArguments_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("innerSurrogate", () => FormatterServices.GetSurrogateForCyclicalReference(null));
        }

        [Fact]
        public void GetSurrogateForCyclicalReference_ValidSurrogate_GetsObject()
        {
            var surrogate = new NonSerializablePairSurrogate();
            ISerializationSurrogate newSurrogate = FormatterServices.GetSurrogateForCyclicalReference(surrogate);
            Assert.NotNull(newSurrogate);
            Assert.NotSame(surrogate, newSurrogate);
        }

        [Fact]
        public void GetTypeFromAssembly_InvalidArguments_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("assem", () => FormatterServices.GetTypeFromAssembly(null, "name"));
            Assert.Null(FormatterServices.GetTypeFromAssembly(GetType().Assembly, Guid.NewGuid().ToString("N"))); // non-existing type doesn't throw
        }
    }
}

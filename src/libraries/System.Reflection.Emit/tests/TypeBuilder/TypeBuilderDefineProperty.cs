// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineProperty
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { "PropertyName", PropertyAttributes.None, typeof(int), new Type[] { typeof(int) }, "PropertyName", PropertyAttributes.None };
            yield return new object[] { "a\0b\0c", PropertyAttributes.HasDefault, typeof(int[]), new Type[] { typeof(int), typeof(double) }, "a", PropertyAttributes.None };
            yield return new object[] { "\uD800\uDC00", PropertyAttributes.RTSpecialName, typeof(EmptyNonGenericInterface1), new Type[0], "\uD800\uDC00", PropertyAttributes.None };
            yield return new object[] { "\u043F\u0440\u0438\u0432\u0435\u0442", PropertyAttributes.SpecialName, typeof(EmptyNonGenericStruct), null, "\u043F\u0440\u0438\u0432\u0435\u0442", PropertyAttributes.SpecialName };
            yield return new object[] { "class", (PropertyAttributes)(-1), null, new Type[] { typeof(void) }, "class", PropertyAttributes.None };
            yield return new object[] { "Test Name With Spaces", (PropertyAttributes)8192, typeof(string), new Type[] { typeof(string) }, "Test Name With Spaces", PropertyAttributes.None };
            yield return new object[] { "PropertyName", PropertyAttributes.None, typeof(BasicDelegate), new Type[] { typeof(int) }, "PropertyName", PropertyAttributes.None };
            yield return new object[] { "PropertyName", PropertyAttributes.None, typeof(EmptyEnum), new Type[] { typeof(int) }, "PropertyName", PropertyAttributes.None };
            yield return new object[] { "PropertyName", PropertyAttributes.None, typeof(DateTime), new Type[] { typeof(int) }, "PropertyName", PropertyAttributes.None };
            yield return new object[] { "PropertyName", PropertyAttributes.None, typeof(EmptyGenericStruct<int>), new Type[] { typeof(int) }, "PropertyName", PropertyAttributes.None };
            yield return new object[] { "PropertyName", PropertyAttributes.None, typeof(EmptyGenericStruct<int>).GetGenericArguments()[0], new Type[] { typeof(int) }, "PropertyName", PropertyAttributes.None };

            // Invalid unicode
            yield return new object[] { "\uDC00", (PropertyAttributes)0x8000, typeof(EmptyGenericStruct<string>), new Type[] { typeof(EmptyGenericClass<string>) }, "\uFFFD", PropertyAttributes.None };
            yield return new object[] { "\uD800", PropertyAttributes.None, typeof(int).MakeByRefType(), new Type[] { typeof(int).MakeByRefType() }, "\uFFFD", PropertyAttributes.None };
            yield return new object[] { "1A\0\t\v\r\n\n\uDC81\uDC91", PropertyAttributes.None, typeof(int).MakePointerType(), new Type[] { typeof(int).MakePointerType() }, "1A", PropertyAttributes.None };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(TestData))]
        public void DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[] parameterTypes, string expectedName, PropertyAttributes expectedPropertyAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty(name, attributes, returnType, parameterTypes);
            Assert.Equal(name, property.Name);
            Assert.Equal(attributes, property.Attributes);
            Assert.Equal(returnType, property.PropertyType);

            Type createdType = type.CreateType();
            Assert.Equal(type.AsType().GetProperties(Helpers.AllFlags), createdType.GetProperties(Helpers.AllFlags));

            PropertyInfo createdProperty = createdType.GetProperty(expectedName, Helpers.AllFlags);
            Assert.Equal(expectedName, createdProperty.Name);
            Assert.Equal(expectedPropertyAttributes, createdProperty.Attributes);
            Assert.Equal(returnType ?? typeof(void), createdProperty.PropertyType);
        }

        [Fact]
        public void DefineProperty_NullCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = { typeof(int), typeof(double) };
            PropertyBuilder property = type.DefineProperty("propertyname", PropertyAttributes.None, typeof(int), null, null, parameterTypes, null, null);

            Assert.Equal("propertyname", property.Name);
            Assert.Equal(PropertyAttributes.None, property.Attributes);
            Assert.Equal(typeof(int), property.PropertyType);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void DefineProperty_GetAccessor_NoCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
            type.SetParent(typeof(DefinePropertyClass));

            PropertyBuilder property = type.DefineProperty("Property", PropertyAttributes.None, CallingConventions.HasThis | CallingConventions.Standard, typeof(int), new Type[0]);

            MethodAttributes methodAttr = MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.ReuseSlot;
            CallingConventions conventions = CallingConventions.Standard | CallingConventions.HasThis;

            MethodBuilder getMethod = type.DefineMethod("get_Property", methodAttr, conventions, typeof(int), new Type[0]);
            ILGenerator ilGenerator = getMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 5);
            ilGenerator.Emit(OpCodes.Ret);
            property.SetGetMethod(getMethod);

            Type createdType = type.CreateType();
            object obj = Activator.CreateInstance(createdType);
            Assert.Equal(5, createdType.GetProperty("Property").GetGetMethod().Invoke(obj, null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void DefineProperty_GetAccessor_NullCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
            type.SetParent(typeof(DefinePropertyClass));

            PropertyBuilder property = type.DefineProperty("Property", PropertyAttributes.None, CallingConventions.HasThis | CallingConventions.Standard, typeof(int), null, null, null, null, null);

            MethodAttributes methodAttr = MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.ReuseSlot;
            CallingConventions conventions = CallingConventions.Standard | CallingConventions.HasThis;

            MethodBuilder getMethod = type.DefineMethod("get_Property", methodAttr, conventions, typeof(int), new Type[0]);
            ILGenerator ilGenerator = getMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, 5);
            ilGenerator.Emit(OpCodes.Ret);
            property.SetGetMethod(getMethod);

            Type createdType = type.CreateType();
            object obj = Activator.CreateInstance(createdType);
            Assert.Equal(5, createdType.GetProperty("Property").GetGetMethod().Invoke(obj, null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        [InlineData("\0TestProperty")]
        public void DefineProperty_InvalidName_ThrowsArgumentException(string name)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>("name", () => type.DefineProperty(name, PropertyAttributes.HasDefault, typeof(int), null, null, new Type[] { typeof(int) }, null, null));

            AssertExtensions.Throws<ArgumentException>("name", () => type.DefineProperty(name, PropertyAttributes.None, typeof(int), new Type[] { typeof(int) }));
        }

        [Fact]
        public void DefineProperty_NullString_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("name", () => type.DefineProperty(null, PropertyAttributes.HasDefault, typeof(int), null, null, new Type[] { typeof(int) }, null, null));

            AssertExtensions.Throws<ArgumentNullException>("name", () => type.DefineProperty(null, PropertyAttributes.None, typeof(int), new Type[] { typeof(int) }));
        }

        [Fact]
        public void DefineProperty_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null, null, new Type[] { typeof(int) }, null, null));

            Assert.Throws<InvalidOperationException>(() => type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), new Type[] { typeof(int) }));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineProperty_OpenGenericReturnType_ThrowsBadImageFormatExceptionGettingCreatedPropertyType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineProperty("Name", PropertyAttributes.None, typeof(EmptyGenericStruct<>), new Type[0]);

            Type createdType = type.CreateType();
            PropertyInfo createdProperty = createdType.GetProperty("Name", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.Throws<BadImageFormatException>(() => createdProperty.PropertyType);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineProperty_NullParameterType_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("argument", () => type.DefineProperty("Name", PropertyAttributes.None, typeof(void), new Type[] { null }));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineProperty_OpenGenericParameterType_ThrowsBadImageFormatExceptionGettingCreatedPropertyType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.DefineProperty("Name", PropertyAttributes.None, typeof(int), new Type[] { typeof(EmptyGenericStruct<>) });

            Type createdType = type.CreateType();
            PropertyInfo createdProperty = createdType.GetProperty("Name", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.Throws<BadImageFormatException>(() => createdProperty.PropertyType);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineProperty_DynamicPropertyTypeNotCreated_ThrowsTypeLoadException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("Name", TypeAttributes.Public);
            TypeBuilder propertyType = module.DefineType("PropertyType", TypeAttributes.Public);
            type.DefineProperty("Name", PropertyAttributes.None, propertyType.AsType(), new Type[0]);

            Type createdType = type.CreateType();
            PropertyInfo property = createdType.GetProperty("Name", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Throws<TypeLoadException>(() => property.PropertyType);

            Type createdPropertyType = propertyType.CreateType();
            Assert.Equal(createdPropertyType, property.PropertyType);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineProperty_DynamicParameterTypeNotCreated_ThrowsTypeLoadException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("Name", TypeAttributes.Public);
            TypeBuilder propertyType = module.DefineType("PropertyType", TypeAttributes.Public);
            type.DefineProperty("Name", PropertyAttributes.None, typeof(int), new Type[] { propertyType.AsType() });

            Type createdType = type.CreateType();
            PropertyInfo property = createdType.GetProperty("Name", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Throws<TypeLoadException>(() => property.PropertyType);

            Type createdPropertyType = propertyType.CreateType();
            Assert.Equal(typeof(int), property.PropertyType);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineProperty_CalledMultipleTimes_Works()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineProperty("PropertyName", PropertyAttributes.None, typeof(int), new Type[0]);
            type.DefineProperty("PropertyName", PropertyAttributes.None, typeof(int), new Type[0]);

            Type createdType = type.CreateType();
            PropertyInfo[] properties = createdType.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.Equal(1, properties.Length);
            Assert.Equal("PropertyName", properties[0].Name);
        }

        [Fact]
        public void GetProperty_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetProperty("Any"));
        }

        [Fact]
        public void GetProperty_TypeCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            type.CreateTypeInfo();

            Assert.Throws<NotSupportedException>(() => type.AsType().GetProperty("Name"));
        }

        [Fact]
        public void GetProperties_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetProperties());
        }

        public class DefinePropertyClass
        {
            public int Property { get { return 10; } }
        }
    }
}

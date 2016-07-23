// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineProperty
    {
        [Theory]
        [InlineData(PropertyAttributes.None, PropertyAttributes.None)]
        [InlineData(PropertyAttributes.HasDefault, PropertyAttributes.None)]
        [InlineData(PropertyAttributes.RTSpecialName, PropertyAttributes.None)]
        [InlineData(PropertyAttributes.SpecialName, PropertyAttributes.SpecialName)]
        [InlineData((PropertyAttributes)(-1), PropertyAttributes.None)]
        [InlineData((PropertyAttributes)0x8000, PropertyAttributes.None)]
        public void DefineProperty(PropertyAttributes attributes, PropertyAttributes expected)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            type.DefineProperty("TestProperty", attributes, typeof(int), null, null, new Type[] { typeof(int) }, null, null);

            Type createdType = type.CreateTypeInfo().AsType();
            PropertyInfo createdProperty = createdType.GetProperty("TestProperty", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            Assert.Equal(typeof(int), createdProperty.PropertyType);
            Assert.Equal(expected, createdProperty.Attributes);
        }

        [Fact]
        public void DefineProperty_NoCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = { typeof(int), typeof(double) };
            PropertyBuilder property = type.DefineProperty("propertyname", PropertyAttributes.None, typeof(int), null, null, parameterTypes, null, null);

            Assert.Equal("propertyname", property.Name);
            Assert.Equal(PropertyAttributes.None, property.Attributes);
            Assert.Equal(typeof(int), property.PropertyType);
        }

        [Fact]
        public void DefineProperty_NullCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = { typeof(int), typeof(double) };
            PropertyBuilder property = type.DefineProperty("propertyname", PropertyAttributes.None, typeof(int), parameterTypes);

            Assert.Equal("propertyname", property.Name);
            Assert.Equal(PropertyAttributes.None, property.Attributes);
            Assert.Equal(typeof(int), property.PropertyType);
        }

        [Fact]
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

            Type createdType = type.CreateTypeInfo().AsType();
            object obj = Activator.CreateInstance(createdType);
            Assert.Equal(5, createdType.GetProperty("Property").GetGetMethod().Invoke(obj, null));
        }

        [Fact]
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

            Type createdType = type.CreateTypeInfo().AsType();
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
            Assert.Throws<ArgumentException>("name", () => type.DefineProperty(name, PropertyAttributes.HasDefault, typeof(int), null, null, new Type[] { typeof(int) }, null, null));

            Assert.Throws<ArgumentException>("name", () => type.DefineProperty(name, PropertyAttributes.None, typeof(int), new Type[] { typeof(int) }));
        }

        [Fact]
        public void DefineProperty_NullString_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            Assert.Throws<ArgumentNullException>("name", () => type.DefineProperty(null, PropertyAttributes.HasDefault, typeof(int), null, null, new Type[] { typeof(int) }, null, null));

            Assert.Throws<ArgumentNullException>("name", () => type.DefineProperty(null, PropertyAttributes.None, typeof(int), new Type[] { typeof(int) }));
        }

        [Fact]
        public void DefineProperty_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null, null, new Type[] { typeof(int) }, null, null));

            Assert.Throws<InvalidOperationException>(() => type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), new Type[] { typeof(int) }));
        }

        public class DefinePropertyClass
        {
            public int Property { get { return 10; } }
        }
    }
}

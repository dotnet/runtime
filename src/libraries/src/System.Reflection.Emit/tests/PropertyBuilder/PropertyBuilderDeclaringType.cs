// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest6
    {
        [Fact]
        public void DeclaringType_RootClass()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            Assert.Equal(type.AsType(), property.DeclaringType);
        }

        [Fact]
        public void DeclaringType_NestedClass()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            TypeBuilder nestedType = type.DefineNestedType("NestedType", TypeAttributes.Class | TypeAttributes.NestedPublic);

            PropertyBuilder property = nestedType.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            Assert.Equal(nestedType.AsType(), property.DeclaringType);
        }

        [Fact]
        public void DeclaringType_RootInterface()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.Public);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            Assert.Equal(type.AsType(), property.DeclaringType);
        }

        [Fact]
        public void DeclaringType_DerivedClass()
        {
            TypeBuilder baseClass = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public, typeName: "BaseClass");
            TypeBuilder subClass = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public, typeName: "SubClass");
            subClass.SetParent(baseClass.AsType());

            PropertyBuilder property = subClass.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            Assert.Equal(subClass.AsType(), property.DeclaringType);
        }

        [Fact]
        public void DeclaringType_BaseClass()
        {
            TypeBuilder baseClass = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Public, typeName: "BaseClass");
            TypeBuilder subClass = Helpers.DynamicType(TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Interface, typeName: "SubClass");
            subClass.SetParent(baseClass.AsType());

            PropertyBuilder property = baseClass.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);
            Assert.Equal(baseClass.AsType(), property.DeclaringType);
        }
    }
}

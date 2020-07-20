// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public class PropertyInfoTests
    {
        [Theory]
        [InlineData(typeof(PI_BaseClass), nameof(PI_BaseClass.PublicGetPublicSetStaticProperty))]
        [InlineData(typeof(PI_BaseClass), nameof(PI_BaseClass.PublicGetPublicSetProperty))]
        [InlineData(typeof(string), nameof(string.Length))]
        public void Properties(Type type, string name)
        {
            PropertyInfo property = TypeExtensions.GetProperty(type, name, Helpers.AllFlags);
            Assert.Equal(type, property.DeclaringType);
            Assert.Equal(type.GetTypeInfo().Module, property.Module);
            Assert.Equal(name, property.Name);

            Assert.Equal(MemberTypes.Property, property.MemberType);
        }

        [Fact]
        public void SetValue_CantWrite_ThrowsArgumentException()
        {
            PropertyInfo property = TypeExtensions.GetProperty(typeof(PI_SubClass), nameof(PI_BaseClass.PublicGetPrivateSetProperty), Helpers.AllFlags);
            Assert.False(property.CanWrite);
            AssertExtensions.Throws<ArgumentException>(null, () => property.SetValue(new PI_SubClass(), 5));
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15319", TestRuntimes.Mono)]
        [InlineData(typeof(PI_BaseClass), nameof(PI_BaseClass.PublicGetPublicSetProperty), true, false, true, false)]
        [InlineData(typeof(PI_BaseClass), nameof(PI_BaseClass.PublicGetProperty1), true, false, false, false)]
        [InlineData(typeof(PI_BaseClass), nameof(PI_BaseClass.PublicSetProperty), false, false, true, false)]
        [InlineData(typeof(PI_BaseClass), "ProtectedGetProtectedSetProperty", true, true, true, true)]
        [InlineData(typeof(PI_BaseClass), nameof(PI_BaseClass.PublicGetPublicSetStaticProperty), true, false, true, false)]
        [InlineData(typeof(PI_BaseClass), "ProtectedGetProtectedSetStaticProperty", true, true, true, true)]
        // Misc
        [InlineData(typeof(PropertyInfoTests), nameof(PropertyInfoTests.GetSetProperty), true, false, true, false)]
        [InlineData(typeof(PI_Interface), nameof(PI_Interface.GetSetProperty), true, false, true, false)]
        [InlineData(typeof(PI_SubClass), nameof(PI_SubClass.PublicGetProperty2), true, false, false, false)]
        [InlineData(typeof(PI_AbstractClass), nameof(PI_AbstractClass.PublicGetPublicSetProperty), true, false, true, false)]
        [InlineData(typeof(PI_SubClass), nameof(PI_BaseClass.PublicGetPrivateSetProperty), true, false, false, false)]
        public void GetGetMethod_GetSetMethod(Type type, string name, bool hasGetter, bool nonPublicGetter, bool hasSetter, bool nonPublicSetter)
        {
            PropertyInfo property = TypeExtensions.GetProperty(type, name, Helpers.AllFlags);

            VerifyGetMethod(property, property.GetGetMethod(), hasGetter && !nonPublicGetter, nonPublicGetter);
            Assert.Equal(property.GetGetMethod(), property.GetGetMethod(false));
            VerifyGetMethod(property, property.GetGetMethod(true), hasGetter, nonPublicGetter);

            VerifySetMethod(property, property.GetSetMethod(), hasSetter && !nonPublicSetter, nonPublicSetter);
            Assert.Equal(property.GetSetMethod(), property.GetSetMethod(false));
            VerifySetMethod(property, property.GetSetMethod(true), hasSetter, nonPublicSetter);

            Assert.Equal(ExcludeNulls(property.GetGetMethod(), property.GetSetMethod()), property.GetAccessors());
            Assert.Equal(property.GetAccessors(), property.GetAccessors(false));
            Assert.Equal(ExcludeNulls(property.GetGetMethod(true), property.GetSetMethod(true)), property.GetAccessors(true));
        }

        private static void VerifyGetMethod(PropertyInfo property, MethodInfo method, bool exists, bool nonPublic)
        {
            Assert.Equal(exists, method != null);
            if (exists)
            {
                Assert.Equal(MemberTypes.Method, method.MemberType);
                Assert.Equal("get_" + property.Name, method.Name);
                Assert.Equal(!nonPublic, method.IsPublic);

                Assert.Equal(property.PropertyType, method.ReturnType);
                Assert.Empty(method.GetParameters());
            }
        }

        private static void VerifySetMethod(PropertyInfo property, MethodInfo method, bool exists, bool nonPublic)
        {
            Assert.Equal(exists, method != null);
            if (exists)
            {
                Assert.Equal(MemberTypes.Method, method.MemberType);
                Assert.Equal("set_" + property.Name, method.Name);
                Assert.Equal(!nonPublic, method.IsPublic);

                Assert.Equal(typeof(void), method.ReturnType);
                Assert.Equal(new Type[] { property.PropertyType }, method.GetParameters().Select(parameter => parameter.ParameterType));
            }
        }

        public static IEnumerable<object> ExcludeNulls(params object[] array)
        {
            return array.Where(obj => obj != null);
        }

        public int GetSetProperty { get { return 0; } set { } }
    }

    public class PI_BaseClass
    {
        public static int PublicGetPublicSetStaticProperty { get { return 0; } set { } }
        protected static int ProtectedGetProtectedSetStaticProperty { get { return 0; } set { } }

        public int PublicGetPublicSetProperty { get { return 0; } set { } }
        public int PublicGetProperty1 { get { return 0; } }
        public int PublicSetProperty { set { } }

        protected int ProtectedGetProtectedSetProperty { get { return 0; } set { } }
        public int PublicGetPrivateSetProperty { get; private set; }
    }

    public interface PI_Interface
    {
        int GetSetProperty { get; set; }
    }

    public class PI_SubClass : PI_BaseClass
    {
        public int PublicGetProperty2 { get { return 0; } }
    }

    public class PI_AbstractClass
    {
        public int PublicGetPublicSetProperty { get { return 0; } set { } }
    }
}

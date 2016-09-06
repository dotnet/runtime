// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class PropertyInfoTests
    {
        [Theory]
        [InlineData(typeof(PI_Class), "StaticProperty")]
        [InlineData(typeof(PI_Class), "IntGetSetProperty")]
        [InlineData(typeof(string), "Length")]
        public void Properties(Type type, string name)
        {
            PropertyInfo property = Helpers.GetProperty(type, name);
            Assert.Equal(type, property.DeclaringType);
            Assert.Equal(type.GetTypeInfo().Module, property.Module);
            Assert.Equal(name, property.Name);

            Assert.Equal(MemberTypes.Property, property.MemberType);
        }

        [Theory]
        [InlineData("IntGetProperty", true, false)]
        [InlineData("IntSetProperty", false, true)]
        public void GetGetMethod_GetSetMethod(string name, bool hasGetter, bool hasSetter)
        {
            PropertyInfo property = Helpers.GetProperty(typeof(PI_Class), name);

            MethodInfo getMethod = property.GetGetMethod();
            Assert.Equal(hasGetter, getMethod != null);
            if (hasGetter)
            {
                Assert.Equal(MemberTypes.Method, getMethod.MemberType);
                Assert.Equal("get_" + name, getMethod.Name);
            }

            MethodInfo setMethod = property.GetSetMethod();
            Assert.Equal(hasSetter, setMethod != null);
            if (hasSetter)
            {
                Assert.Equal(MemberTypes.Method, setMethod.MemberType);
                Assert.Equal("set_" + name, setMethod.Name);
            }
        }
    }

    public class PI_Class
    {
        public static int StaticProperty { get { return 0; } }
        public int IntGetSetProperty { get { return 0; } set { } }
        public int IntGetProperty { get { return 0; } }
        public int IntSetProperty { set { } }
    }
}

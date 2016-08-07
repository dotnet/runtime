// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class MethodInfoTests
    {
        [Theory]
        [InlineData(typeof(MI_Class), "PublicMethod")]
        [InlineData(typeof(MI_Class), "PublicStaticMethod")]
        [InlineData(typeof(string), "IsNullOrEmpty")]
        public void Properties(Type type, string name)
        {
            MethodInfo method = Helpers.GetMethod(type, name);
            Assert.Equal(type, method.DeclaringType);
            Assert.Equal(type.GetTypeInfo().Module, method.Module);

            Assert.Equal(name, method.Name);
            Assert.Equal(MemberTypes.Method, method.MemberType);
        }
    }

    public class MI_Class
    {
        public void PublicMethod() { }
        public static void PublicStaticMethod() { }
    }
}

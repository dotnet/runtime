// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class CustomAttributeBuilderCtor1
    {
        [Theory]
        [InlineData(new Type[] { typeof(string) }, new object[] { "TestString" })]
        [InlineData(new Type[0], new object[0])]
        [InlineData(new Type[] { typeof(string), typeof(bool) }, new object[] { "TestString", true })]
        public void ConstructorInfo_ObjectArray(Type[] paramTypes, object[] paramValues)
        {
            ConstructorInfo constructor = typeof(ObsoleteAttribute).GetConstructor(paramTypes);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(constructor, paramValues);
        }

        [Fact]
        public void ConstructorInfo_ObjectArray_StaticCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsStatic).First();
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[0]));
        }

        [Fact]
        public void ConstructorInfo_ObjectArray_PrivateCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsPrivate).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[] { false }));
        }

        [Theory]
        [InlineData(new Type[] { typeof(string) }, new object[] { "TestString", false })]
        [InlineData(new Type[] { typeof(string), typeof(bool) }, new object[] { false, "TestString" })]
        public void ConstructorInfo_ObjectArray_NonMatching_ThrowsArgumentException(Type[] paramTypes, object[] paramValues)
        {
            ConstructorInfo constructor = typeof(ObsoleteAttribute).GetConstructor(paramTypes);

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, paramValues));
        }

        [Fact]
        public void ConstructorInfo_ObjectArray_NullCtor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0]));
        }

        [Fact]
        public void ConstructorInfo_ObjectArray_NullConstructorArgs_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(ObsoleteAttribute).GetConstructor(new Type[] { typeof(string), typeof(bool) });
            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(constructor, null));
        }
    }
}

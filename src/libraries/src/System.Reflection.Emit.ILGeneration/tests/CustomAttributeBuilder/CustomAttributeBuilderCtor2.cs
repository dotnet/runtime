// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class CustomAttributeBuilderCtor2
    {
        private const string IntField = "TestInt";
        private const string StringField = "TestStringField";
        private const string GetStringField = "GetString";
        private const string GetIntField = "GetInt";

        [Theory]
        [InlineData(new string[] { IntField }, new object[0], "namedFields, fieldValues")]
        [InlineData(new string[] { IntField }, new object[] { "TestString", 10 }, "namedFields, fieldValues")]
        [InlineData(new string[] { IntField, StringField }, new object[] { "TestString", 10 }, null)]
        [InlineData(new string[] { StringField }, new object[] { 10 }, null)]
        public void NamedFieldAndFieldValuesDifferentLengths_ThrowsArgumentException(string[] fieldNames, object[] fieldValues, string paramName)
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(fieldNames);

            Assert.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(constructor, new object[0], namedFields, fieldValues));
        }

        [Fact]
        public void StaticCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsStatic).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void PrivateCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsPrivate).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[] { false }, new FieldInfo[0], new object[0]));
        }

        [Theory]
        [InlineData(new Type[] { typeof(string), typeof(int), typeof(string), typeof(int) }, new object[] { "TestString", 10 })]
        [InlineData(new Type[] { typeof(string), typeof(int) }, new object[] { 10, "TestString" })]
        public void ConstructorArgsDontMatchConstructor_ThrowsArgumentException(Type[] ctorParams, object[] constructorArgs)
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(ctorParams);

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, constructorArgs, new FieldInfo[0], new object[0]));
        }
        
        [Fact]
        public void NullConstructor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullConstructorArgs_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(constructor, null, new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedFields", () => new CustomAttributeBuilder(constructor, new object[0], (FieldInfo[])null, new object[0]));
        }

        [Fact]
        public void NullFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("fieldValues", () => new CustomAttributeBuilder(constructor, new object[0], new FieldInfo[0], null));
        }

        [Fact]
        public void NullObjectInFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("fieldValues[0]", () => new CustomAttributeBuilder(constructor, new object[0], Helpers.GetFields(IntField, StringField), new object[] { null, 10 }));
        }

        [Fact]
        public void NullObjectInNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedFields[0]", () => new CustomAttributeBuilder(constructor, new object[0], Helpers.GetFields(null, IntField), new object[] { "TestString", 10 }));
        }
    }
}

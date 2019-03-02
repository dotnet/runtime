// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class PropertyVisibilityTests
    {
        [Fact]
        public static void NoSetter()
        {
            var obj = new ClassWithNoSetter();

            string json = JsonSerializer.ToString(obj);
            Assert.Equal(@"{""MyString"":""DefaultValue""}", json);

            ClassWithNoSetter objCopy = JsonSerializer.Parse<ClassWithNoSetter>(json);
            Assert.Equal("DefaultValue", objCopy.MyString);
        }

        [Fact]
        public static void NoGetter()
        {
            var objNoSetter = new ClassWithNoSetter();

            string json = JsonSerializer.ToString(objNoSetter);
            Assert.Equal(@"{""MyString"":""DefaultValue""}", json);

            ClassWithNoGetter objNoGetter = JsonSerializer.Parse<ClassWithNoGetter>(json);
            Assert.Equal("DefaultValue", objNoGetter.GetMyString());
        }

        [Fact]
        public static void PrivateGetter()
        {
            var obj = new ClassWithPrivateSetterAndGetter();
            obj.SetMyString("Hello");

            string json = JsonSerializer.ToString(obj);
            Assert.Equal(@"{}", json);
        }

        [Fact]
        public static void PrivateSetter()
        {
            string json = @"{""MyString"":""Hello""}";

            ClassWithPrivateSetterAndGetter objCopy = JsonSerializer.Parse<ClassWithPrivateSetterAndGetter>(json);
            Assert.Null(objCopy.GetMyString());
        }

        // Todo: add tests with missing object property and missing collection property.

        public class ClassWithNoSetter
        {
            public ClassWithNoSetter()
            {
                MyString = "DefaultValue";
            }

            public string MyString { get; }
        }

        public class ClassWithNoGetter
        {
            string _myString = "";

            public string MyString
            {
                set
                {
                    _myString = value;
                }
            }

            public string GetMyString()
            {
                return _myString;
            }
        }

        public class ClassWithPrivateSetterAndGetter
        {
            private string MyString { get; set; }

            public string GetMyString()
            {
                return MyString;
            }

            public void SetMyString(string value)
            {
                MyString = value;
            }
        }
    }
}

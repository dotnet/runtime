// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Concurrent;
using Xunit;
using System.Numerics;

namespace System.Text.Json.Serialization.Tests
{
    public static class PropertyVisibilityTests
    {
        [Fact]
        public static void NoSetter()
        {
            var obj = new ClassWithNoSetter();

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyString"":""DefaultValue""", json);
            Assert.Contains(@"""MyInts"":[1,2]", json);

            obj = JsonSerializer.Deserialize<ClassWithNoSetter>(@"{""MyString"":""IgnoreMe"",""MyInts"":[0]}");
            Assert.Equal("DefaultValue", obj.MyString);
            Assert.Equal(2, obj.MyInts.Length);
        }

        [Fact]
        public static void IgnoreReadOnlyProperties()
        {
            var options = new JsonSerializerOptions();
            options.IgnoreReadOnlyProperties = true;

            var obj = new ClassWithNoSetter();

            string json = JsonSerializer.Serialize(obj, options);

            // Collections are always serialized unless they have [JsonIgnore].
            Assert.Equal(@"{""MyInts"":[1,2]}", json);
        }

        [Fact]
        public static void NoGetter()
        {
            ClassWithNoGetter objWithNoGetter = JsonSerializer.Deserialize<ClassWithNoGetter>(
                @"{""MyString"":""Hello"",""MyIntArray"":[0],""MyIntList"":[0]}");

            Assert.Equal("Hello", objWithNoGetter.GetMyString());

            // Currently we don't support setters without getters.
            Assert.Equal(0, objWithNoGetter.GetMyIntArray().Length);
            Assert.Equal(0, objWithNoGetter.GetMyIntList().Count);
        }

        [Fact]
        public static void PrivateGetter()
        {
            var obj = new ClassWithPrivateSetterAndGetter();
            obj.SetMyString("Hello");

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{}", json);
        }

        [Fact]
        public static void PrivateSetter()
        {
            string json = @"{""MyString"":""Hello""}";

            ClassWithPrivateSetterAndGetter objCopy = JsonSerializer.Deserialize<ClassWithPrivateSetterAndGetter>(json);
            Assert.Null(objCopy.GetMyString());
        }

        [Fact]
        public static void PrivateSetterPublicGetter()
        {
            // https://github.com/dotnet/corefx/issues/37567
            ClassWithPublicGetterAndPrivateSetter obj
                = JsonSerializer.Deserialize<ClassWithPublicGetterAndPrivateSetter>(@"{ ""Class"": {} }");

            Assert.NotNull(obj);
            Assert.Null(obj.Class);
        }

        private class ClassWithPublicGetterAndPrivateSetter
        {
            public NestedClass Class { get; private set; }
        }

        private class NestedClass
        {
        }

        [Fact]
        public static void JsonIgnoreAttribute()
        {
            // Verify default state.
            var obj = new ClassWithIgnoreAttributeProperty();
            Assert.Equal(@"MyString", obj.MyString);
            Assert.Equal(@"MyStringWithIgnore", obj.MyStringWithIgnore);
            Assert.Equal(2, obj.MyStringsWithIgnore.Length);
            Assert.Equal(1, obj.MyDictionaryWithIgnore["Key"]);

            // Verify serialize.
            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyString""", json);
            Assert.DoesNotContain(@"MyStringWithIgnore", json);
            Assert.DoesNotContain(@"MyStringsWithIgnore", json);
            Assert.DoesNotContain(@"MyDictionaryWithIgnore", json);

            // Verify deserialize default.
            obj = JsonSerializer.Deserialize<ClassWithIgnoreAttributeProperty>(@"{}");
            Assert.Equal(@"MyString", obj.MyString);
            Assert.Equal(@"MyStringWithIgnore", obj.MyStringWithIgnore);
            Assert.Equal(2, obj.MyStringsWithIgnore.Length);
            Assert.Equal(1, obj.MyDictionaryWithIgnore["Key"]);

            // Verify deserialize ignores the json for MyStringWithIgnore and MyStringsWithIgnore.
            obj = JsonSerializer.Deserialize<ClassWithIgnoreAttributeProperty>(
                @"{""MyString"":""Hello"", ""MyStringWithIgnore"":""IgnoreMe"", ""MyStringsWithIgnore"":[""IgnoreMe""], ""MyDictionaryWithIgnore"":{""Key"":9}}");
            Assert.Contains(@"Hello", obj.MyString);
            Assert.Equal(@"MyStringWithIgnore", obj.MyStringWithIgnore);
            Assert.Equal(2, obj.MyStringsWithIgnore.Length);
            Assert.Equal(1, obj.MyDictionaryWithIgnore["Key"]);
        }

        [Fact]
        public static void JsonIgnoreAttribute_UnsupportedCollection()
        {
            string json =
                    @"{
                        ""MyConcurrentDict"":{
                            ""key"":""value""
                        },
                        ""MyIDict"":{
                            ""key"":""value""
                        },
                        ""MyDict"":{
                            ""key"":""value""
                        }
                    }";
            string wrapperJson =
                    @"{
                        ""MyClass"":{
                            ""MyConcurrentDict"":{
                                ""key"":""value""
                            },
                            ""MyIDict"":{
                                ""key"":""value""
                            },
                            ""MyDict"":{
                                ""key"":""value""
                            }
                        }
                    }";

            // Unsupported collections will throw on deserialize by default.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithUnsupportedDictionary>(json));
            
            // Using new options instance to prevent using previously cached metadata.
            JsonSerializerOptions options = new JsonSerializerOptions();
            string serialized = JsonSerializer.Serialize(new ClassWithUnsupportedDictionary(), options);
            
            // Object keys are fine on serialization if the keys are strings.
            Assert.Contains(@"""MyConcurrentDict"":null", serialized);
            Assert.Contains(@"""MyIDict"":null", serialized);
            Assert.Contains(@"""MyDict"":null", serialized);

            // Unsupported collections will throw on deserialize by default.
            options = new JsonSerializerOptions();
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<WrapperForClassWithUnsupportedDictionary>(wrapperJson, options));
            
            options = new JsonSerializerOptions();
            serialized = JsonSerializer.Serialize(new WrapperForClassWithUnsupportedDictionary(), options);

            // Object keys are fine on serialization if the keys are strings.
            Assert.Contains(@"{""MyClass"":{", serialized);
            Assert.Contains(@"""MyConcurrentDict"":null", serialized);
            Assert.Contains(@"""MyIDict"":null", serialized);
            Assert.Contains(@"""MyDict"":null", serialized);
            Assert.Contains("}}", serialized);

            // When ignored, we can serialize and deserialize without exceptions.
            options = new JsonSerializerOptions();
            ClassWithIgnoredUnsupportedDictionary obj = JsonSerializer.Deserialize<ClassWithIgnoredUnsupportedDictionary>(json, options);
            Assert.Null(obj.MyDict);

            options = new JsonSerializerOptions();
            Assert.Equal("{}", JsonSerializer.Serialize(new ClassWithIgnoredUnsupportedDictionary()));

            options = new JsonSerializerOptions();
            WrapperForClassWithIgnoredUnsupportedDictionary wrapperObj = JsonSerializer.Deserialize<WrapperForClassWithIgnoredUnsupportedDictionary>(wrapperJson, options);
            Assert.Null(wrapperObj.MyClass.MyDict);

            options = new JsonSerializerOptions();
            Assert.Equal(@"{""MyClass"":{}}", JsonSerializer.Serialize(new WrapperForClassWithIgnoredUnsupportedDictionary()
            {
                MyClass = new ClassWithIgnoredUnsupportedDictionary(),
            }, options)); ;
        }

        [Fact]
        public static void JsonIgnoreAttribute_UnsupportedBigInteger()
        {
            string json = @"{""MyBigInteger"":1}";
            string wrapperJson = @"{""MyClass"":{""MyBigInteger"":1}}";

            // Unsupported types will throw by default.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithUnsupportedBigInteger>(json));
            // Using new options instance to prevent using previously cached metadata.
            JsonSerializerOptions options = new JsonSerializerOptions();
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WrapperForClassWithUnsupportedBigInteger>(wrapperJson, options));

            // When ignored, we can serialize and deserialize without exceptions.
            options = new JsonSerializerOptions();
            ClassWithIgnoredUnsupportedBigInteger obj = JsonSerializer.Deserialize<ClassWithIgnoredUnsupportedBigInteger>(json, options);
            Assert.Null(obj.MyBigInteger);

            options = new JsonSerializerOptions();
            Assert.Equal("{}", JsonSerializer.Serialize(new ClassWithIgnoredUnsupportedBigInteger()));

            options = new JsonSerializerOptions();
            WrapperForClassWithIgnoredUnsupportedBigInteger wrapperObj = JsonSerializer.Deserialize<WrapperForClassWithIgnoredUnsupportedBigInteger>(wrapperJson, options);
            Assert.Null(wrapperObj.MyClass.MyBigInteger);

            options = new JsonSerializerOptions();
            Assert.Equal(@"{""MyClass"":{}}", JsonSerializer.Serialize(new WrapperForClassWithIgnoredUnsupportedBigInteger()
            {
                MyClass = new ClassWithIgnoredUnsupportedBigInteger(),
            }, options));
        }

        public class ObjectDictWrapper : Dictionary<int, string> { }

        public class ClassWithUnsupportedDictionary
        {
            public ConcurrentDictionary<object, object> MyConcurrentDict { get; set; }
            public IDictionary<object, object> MyIDict { get; set; }
            public ObjectDictWrapper MyDict { get; set; }
        }

        public class WrapperForClassWithUnsupportedDictionary
        {
            public ClassWithUnsupportedDictionary MyClass { get; set; } = new ClassWithUnsupportedDictionary();
        }

        public class ClassWithIgnoredUnsupportedDictionary
        {
            [JsonIgnore]
            public ConcurrentDictionary<object, object> MyConcurrentDict { get; set; }
            [JsonIgnore]
            public IDictionary<object, object> MyIDict { get; set; }
            [JsonIgnore]
            public ObjectDictWrapper MyDict { get; set; }
        }

        public class WrapperForClassWithIgnoredUnsupportedDictionary
        {
            public ClassWithIgnoredUnsupportedDictionary MyClass { get; set; }
        }

        public class ClassWithUnsupportedBigInteger
        {
            public BigInteger? MyBigInteger { get; set; }
        }

        public class WrapperForClassWithUnsupportedBigInteger
        {
            public ClassWithUnsupportedBigInteger MyClass { get; set; } = new ClassWithUnsupportedBigInteger();
        }

        public class ClassWithIgnoredUnsupportedBigInteger
        {
            [JsonIgnore]
            public BigInteger? MyBigInteger { get; set; }
        }

        public class WrapperForClassWithIgnoredUnsupportedBigInteger
        {
            public ClassWithIgnoredUnsupportedBigInteger MyClass { get; set; }
        }

        // Todo: add tests with missing object property and missing collection property.

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

        public class ClassWithNoSetter
        {
            public ClassWithNoSetter()
            {
                MyString = "DefaultValue";
                MyInts = new int[] { 1, 2 };
            }

            public string MyString { get; }
            public int[] MyInts { get; }
        }

        public class ClassWithNoGetter
        {
            string _myString = "";
            int[] _myIntArray = new int[] { };
            List<int> _myIntList = new List<int> { };

            public string MyString
            {
                set
                {
                    _myString = value;
                }
            }

            public int[] MyIntArray
            {
                set
                {
                    _myIntArray = value;
                }
            }

            public List<int> MyList
            {
                set
                {
                    _myIntList = value;
                }
            }

            public string GetMyString()
            {
                return _myString;
            }

            public int[] GetMyIntArray()
            {
                return _myIntArray;
            }

            public List<int> GetMyIntList()
            {
                return _myIntList;
            }
        }

        public class ClassWithIgnoreAttributeProperty
        {
            public ClassWithIgnoreAttributeProperty()
            {
                MyDictionaryWithIgnore = new Dictionary<string, int> { { "Key", 1 } };
                MyString = "MyString";
                MyStringWithIgnore = "MyStringWithIgnore";
                MyStringsWithIgnore = new string[] { "1", "2" };
            }

            [JsonIgnore]
            public Dictionary<string, int> MyDictionaryWithIgnore { get; set; }

            [JsonIgnore]
            public string MyStringWithIgnore { get; set; }

            public string MyString { get; set; }

            [JsonIgnore]
            public string[] MyStringsWithIgnore { get; set; }
        }

        private enum MyEnum
        {
            Case1 = 0,
            Case2 = 1,
        }

        private struct StructWithOverride
        {
            [JsonIgnore]
            public MyEnum EnumValue { get; set; }

            [JsonPropertyName("EnumValue")]
            public string EnumString
            {
                get => EnumValue.ToString();
                set
                {
                    if (value == "Case1")
                    {
                        EnumValue = MyEnum.Case1;
                    }
                    else if (value == "Case2")
                    {
                        EnumValue = MyEnum.Case2;
                    }
                    else
                    {
                        throw new Exception("Unknown value!");
                    }
                }
            }
        }

        [Fact]
        public static void OverrideJsonIgnorePropertyUsingJsonPropertyName()
        {
            const string json = @"{""EnumValue"":""Case2""}";

            StructWithOverride obj = JsonSerializer.Deserialize<StructWithOverride>(json);

            Assert.Equal(MyEnum.Case2, obj.EnumValue);
            Assert.Equal("Case2", obj.EnumString);

            string jsonSerialized = JsonSerializer.Serialize(obj);
            Assert.Equal(json, jsonSerialized);
        }

        private struct ClassWithOverrideReversed
        {
            // Same as ClassWithOverride except the order of the properties is different, which should cause different reflection order.
            [JsonPropertyName("EnumValue")]
            public string EnumString
            {
                get => EnumValue.ToString();
                set
                {
                    if (value == "Case1")
                    {
                        EnumValue = MyEnum.Case1;
                    }
                    if (value == "Case2")
                    {
                        EnumValue = MyEnum.Case2;
                    }
                    else
                    {
                        throw new Exception("Unknown value!");
                    }
                }
            }

            [JsonIgnore]
            public MyEnum EnumValue { get; set; }
        }

        [Fact]
        public static void OverrideJsonIgnorePropertyUsingJsonPropertyNameReversed()
        {
            const string json = @"{""EnumValue"":""Case2""}";

            ClassWithOverrideReversed obj = JsonSerializer.Deserialize<ClassWithOverrideReversed>(json);

            Assert.Equal(MyEnum.Case2, obj.EnumValue);
            Assert.Equal("Case2", obj.EnumString);

            string jsonSerialized = JsonSerializer.Serialize(obj);
            Assert.Equal(json, jsonSerialized);
        }
    }
}

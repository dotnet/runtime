// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class OnSerializeTests
    {
        private class MyClass :
            IJsonOnDeserializing,
            IJsonOnDeserialized,
            IJsonOnSerializing,
            IJsonOnSerialized
        {
            public int MyInt { get; set; }

            internal int _onSerializingCount;
            internal int _onSerializedCount;
            internal int _onDeserializingCount;
            internal int _onDeserializedCount;

            public void OnSerializing()
            {
                _onSerializingCount++;
                Assert.Equal(1, MyInt);
                MyInt++;
            }

            public void OnSerialized()
            {
                Assert.Equal(1, _onSerializingCount);
                _onSerializedCount++;
                Assert.Equal(2, MyInt);
                MyInt++;
            }

            public void OnDeserializing()
            {
                _onDeserializingCount++;
                Assert.Equal(0, MyInt);
                MyInt = 100; // Gets replaced by the serializer.
            }

            public void OnDeserialized()
            {
                Assert.Equal(1, _onDeserializingCount);
                _onDeserializedCount++;
                Assert.Equal(1, MyInt);
                MyInt++;
            }
        }

        [Fact]
        public static void Test_MyClass()
        {
            MyClass obj = new();
            obj.MyInt = 1;

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("{\"MyInt\":2}", json);
            Assert.Equal(3, obj.MyInt);
            Assert.Equal(1, obj._onSerializingCount);
            Assert.Equal(1, obj._onSerializedCount);
            Assert.Equal(0, obj._onDeserializingCount);
            Assert.Equal(0, obj._onDeserializedCount);

            obj = JsonSerializer.Deserialize<MyClass>("{\"MyInt\":1}");
            Assert.Equal(2, obj.MyInt);
            Assert.Equal(0, obj._onSerializingCount);
            Assert.Equal(0, obj._onSerializedCount);
            Assert.Equal(1, obj._onDeserializingCount);
            Assert.Equal(1, obj._onDeserializedCount);
        }

        private struct MyStruct :
            IJsonOnDeserializing,
            IJsonOnDeserialized,
            IJsonOnSerializing,
            IJsonOnSerialized
        {
            public int MyInt { get; set; }

            internal int _onSerializingCount;
            internal int _onSerializedCount;
            internal int _onDeserializingCount;
            internal int _onDeserializedCount;

            public void OnSerializing()
            {
                _onSerializingCount++;
                Assert.Equal(1, MyInt);
                MyInt++;
            }

            public void OnSerialized()
            {
                Assert.Equal(1, _onSerializingCount);
                _onSerializedCount++;
                MyInt++;    // should not affect serialization
            }

            public void OnDeserializing()
            {
                Assert.Equal(0, MyInt);
                _onDeserializingCount++;
                MyInt = 100; // Gets replaced by the serializer.
            }

            public void OnDeserialized()
            {
                Assert.Equal(1, _onDeserializingCount);
                Assert.Equal(1, MyInt);
                _onDeserializedCount++;
            }
        }


        [Fact]
        public static void Test_MyStruct()
        {
            MyStruct obj = new();
            obj.MyInt = 1;

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("{\"MyInt\":2}", json);

            // Although the OnSerialize* callbacks are invoked, a struct is passed to the serializer byvalue.
            Assert.Equal(0, obj._onSerializingCount);
            Assert.Equal(0, obj._onSerializedCount);

            Assert.Equal(0, obj._onDeserializingCount);
            Assert.Equal(0, obj._onDeserializedCount);

            obj = JsonSerializer.Deserialize<MyStruct>("{\"MyInt\":1}");
            Assert.Equal(1, obj.MyInt);
            Assert.Equal(0, obj._onSerializingCount);
            Assert.Equal(0, obj._onSerializedCount);
            Assert.Equal(1, obj._onDeserializingCount);
            Assert.Equal(1, obj._onDeserializedCount);
        }

        private class MyClassWithSmallConstructor :
            IJsonOnDeserializing,
            IJsonOnDeserialized,
            IJsonOnSerializing,
            IJsonOnSerialized
        {
            public int MyInt { get; set; }

            public MyClassWithSmallConstructor(int myInt)
            {
                MyInt = myInt;
                _constructorCalled = true;
            }

            internal bool _constructorCalled;
            internal int _onSerializingCount;
            internal int _onSerializedCount;
            internal int _onDeserializingCount;
            internal int _onDeserializedCount;

            public void OnSerializing()
            {
                _onSerializingCount++;
                Assert.Equal(1, MyInt);
                MyInt++;
            }

            public void OnSerialized()
            {
                Assert.Equal(1, _onSerializingCount);
                _onSerializedCount++;
                Assert.Equal(2, MyInt);
                MyInt++;
            }

            public void OnDeserializing()
            {
                _onDeserializingCount++;
                Assert.Equal(1, MyInt);
                MyInt++; // Does not get replaced by the serializer since it was passed into the ctor.
            }

            public void OnDeserialized()
            {
                Assert.Equal(1, _onDeserializingCount);
                _onDeserializedCount++;
                Assert.Equal(2, MyInt);
                MyInt++;
            }
        }

        [Fact]
        public static void Test_MyClassWithSmallConstructor()
        {
            MyClassWithSmallConstructor obj = new(1);
            Assert.Equal(1, obj.MyInt);

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("{\"MyInt\":2}", json);
            Assert.Equal(3, obj.MyInt);
            Assert.True(obj._constructorCalled);
            Assert.Equal(1, obj._onSerializingCount);
            Assert.Equal(1, obj._onSerializedCount);
            Assert.Equal(0, obj._onDeserializingCount);
            Assert.Equal(0, obj._onDeserializedCount);

            obj = JsonSerializer.Deserialize<MyClassWithSmallConstructor>("{\"MyInt\":1}");
            Assert.True(obj._constructorCalled);
            Assert.Equal(3, obj.MyInt);
            Assert.Equal(0, obj._onSerializingCount);
            Assert.Equal(0, obj._onSerializedCount);
            Assert.Equal(1, obj._onDeserializingCount);
            Assert.Equal(1, obj._onDeserializedCount);
        }

        private class MyClassWithLargeConstructor :
            IJsonOnDeserializing,
            IJsonOnDeserialized,
            IJsonOnSerializing,
            IJsonOnSerialized
        {
            public int MyInt1 { get; set; }
            public int MyInt2 { get; set; }
            public int MyInt3 { get; set; }
            public int MyInt4 { get; set; }
            public int MyInt5 { get; set; }

            public MyClassWithLargeConstructor(int myInt1, int myInt2, int myInt3, int myInt4, int myInt5)
            {
                MyInt1 = myInt1;
                MyInt2 = myInt2;
                MyInt3 = myInt3;
                MyInt4 = myInt4;
                MyInt5 = myInt5;
                _constructorCalled = true;
            }

            internal bool _constructorCalled;
            internal int _onSerializingCount;
            internal int _onSerializedCount;
            internal int _onDeserializingCount;
            internal int _onDeserializedCount;

            public void OnSerializing()
            {
                _onSerializingCount++;
                Assert.Equal(1, MyInt1);
                MyInt1++;
            }

            public void OnSerialized()
            {
                Assert.Equal(1, _onSerializingCount);
                _onSerializedCount++;
                Assert.Equal(2, MyInt1);
                MyInt1++;
            }

            public void OnDeserializing()
            {
                _onDeserializingCount++;
                Assert.Equal(1, MyInt1);
                MyInt1++; // Does not get replaced by the serializer since it was passed into the ctor.
            }

            public void OnDeserialized()
            {
                Assert.Equal(1, _onDeserializingCount);
                _onDeserializedCount++;
                Assert.Equal(2, MyInt1);
                MyInt1++;
            }
        }

        [Fact]
        public static void Test_MyClassWithLargeConstructor()
        {
            const string Json = "{\"MyInt1\":1,\"MyInt2\":2,\"MyInt3\":3,\"MyInt4\":4,\"MyInt5\":5}";

            MyClassWithLargeConstructor obj = new(1, 2, 3, 4, 5);
            Assert.Equal(1, obj.MyInt1);
            Assert.Equal(2, obj.MyInt2);
            Assert.Equal(3, obj.MyInt3);
            Assert.Equal(4, obj.MyInt4);
            Assert.Equal(5, obj.MyInt5);

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains("\"MyInt1\":2", json);
            Assert.Equal(3, obj.MyInt1); // Is updated in the callback
            Assert.True(obj._constructorCalled);
            Assert.Equal(1, obj._onSerializingCount);
            Assert.Equal(1, obj._onSerializedCount);
            Assert.Equal(0, obj._onDeserializingCount);
            Assert.Equal(0, obj._onDeserializedCount);

            obj = JsonSerializer.Deserialize<MyClassWithLargeConstructor>(Json);
            Assert.True(obj._constructorCalled);
            Assert.Equal(3, obj.MyInt1);
            Assert.Equal(0, obj._onSerializingCount);
            Assert.Equal(0, obj._onSerializedCount);
            Assert.Equal(1, obj._onDeserializingCount);
            Assert.Equal(1, obj._onDeserializedCount);
        }

        private class MyCyclicClass :
            IJsonOnDeserializing,
            IJsonOnDeserialized,
            IJsonOnSerializing,
            IJsonOnSerialized
        {
            public int MyInt { get; set; }
            public MyCyclicClass Cycle { get; set; }

            internal int _onSerializingCount;
            internal int _onSerializedCount;
            internal int _onDeserializingCount;
            internal int _onDeserializedCount;

            public void OnSerializing()
            {
                _onSerializingCount++;
                Assert.Equal(1, MyInt);
                MyInt++;
            }

            public void OnSerialized()
            {
                Assert.Equal(1, _onSerializingCount);
                _onSerializedCount++;
                Assert.Equal(2, MyInt);
                MyInt++;
            }

            public void OnDeserializing()
            {
                _onDeserializingCount++;
                Assert.Equal(0, MyInt);
                MyInt = 100; // Gets replaced by the serializer.
            }

            public void OnDeserialized()
            {
                Assert.Equal(1, _onDeserializingCount);
                _onDeserializedCount++;
                Assert.Equal(1, MyInt);
                MyInt++;
            }
        }

        [Fact]
        public static void Test_MyCyclicClass()
        {
            const string Json = "{\"$id\":\"1\",\"MyInt\":1,\"Cycle\":{\"$ref\":\"1\"}}";

            MyCyclicClass obj = new();
            obj.MyInt = 1;
            obj.Cycle = obj;

            JsonSerializerOptions options = new();
            options.ReferenceHandler = ReferenceHandler.Preserve;

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Contains("\"MyInt\":2", json);
            Assert.Equal(1, obj._onSerializingCount);
            Assert.Equal(1, obj._onSerializedCount);
            Assert.Equal(0, obj._onDeserializingCount);
            Assert.Equal(0, obj._onDeserializedCount);

            obj = JsonSerializer.Deserialize<MyCyclicClass>(Json, options);
            Assert.Equal(2, obj.MyInt);
            Assert.Equal(obj, obj.Cycle);
            Assert.Equal(0, obj._onSerializingCount);
            Assert.Equal(0, obj._onSerializedCount);
            Assert.Equal(1, obj._onDeserializingCount);
            Assert.Equal(1, obj._onDeserializedCount);
        }

        private class MyCollection : List<int>,
            IJsonOnDeserializing,
            IJsonOnDeserialized,
            IJsonOnSerializing,
            IJsonOnSerialized
        {
            public void OnDeserialized() => Assert.Fail("Not expected");
            public void OnDeserializing() => Assert.Fail("Not expected");
            public void OnSerialized() => Assert.Fail("Not expected");
            public void OnSerializing() => Assert.Fail("Not expected");
        }

        [JsonConverter(converterType: typeof(MyValueConverter))]
        private class MyValue :
            IJsonOnDeserializing,
            IJsonOnDeserialized,
            IJsonOnSerializing,
            IJsonOnSerialized
        {
            public void OnDeserialized() => Assert.Fail("Not expected");
            public void OnDeserializing() => Assert.Fail("Not expected");
            public void OnSerialized() => Assert.Fail("Not expected");
            public void OnSerializing() => Assert.Fail("Not expected");
        }

        private class MyValueConverter : JsonConverter<MyValue>
        {
            public override MyValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new MyValue();
            }

            public override void Write(Utf8JsonWriter writer, MyValue value, JsonSerializerOptions options)
            {
                writer.WriteStringValue("dummy");
            }
        }

        [Fact]
        public static void NonPocosIgnored()
        {
            JsonSerializer.Serialize(new MyCollection());
            JsonSerializer.Deserialize<MyCollection>("[]");
            JsonSerializer.Serialize(new MyValue());
            JsonSerializer.Deserialize<MyCollection>("[]");
        }
    }
}

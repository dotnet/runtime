// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class ConstructorTests_String : ConstructorTests
    {
        public ConstructorTests_String() : base(DeserializationWrapper.StringDeserializer) { }
    }

    public class ConstructorTests_Stream : ConstructorTests
    {
        public ConstructorTests_Stream() : base(DeserializationWrapper.StreamDeserializer) { }
    }

    public abstract partial class ConstructorTests
    {
        private DeserializationWrapper Serializer { get; }

        public ConstructorTests(DeserializationWrapper serializer)
        {
            Serializer = serializer;
        }

        [Fact]
        public void ReturnNullForNullObjects()
        {
            Assert.Null(Serializer.Deserialize<Point_2D>("null"));
            Assert.Null(Serializer.Deserialize<Point_3D>("null"));
        }

        [Fact]
        public void JsonExceptionWhenAssigningNullToStruct()
        {
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D_With_ExtData>("null"));
        }

        [Fact]
        public void MatchJsonPropertyToConstructorParameters()
        {
            Point_2D point = Serializer.Deserialize<Point_2D>(@"{""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""Y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public void UseDefaultValues_When_NoJsonMatch()
        {
            // Using CLR value when `ParameterInfo.DefaultValue` is not set.
            Point_2D point = Serializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""y"":2,""x"":1}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            // Using `ParameterInfo.DefaultValue` when set; using CLR value as fallback.
            Point_3D point3d = Serializer.Deserialize<Point_3D>(@"{""X"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""X"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""Y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""Z"":3,""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""x"":1,""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = Serializer.Deserialize<Point_3D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);
        }

        [Fact]
        public void CaseInsensitivityWorks()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            Point_2D point = Serializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""y"":2,""x"":1}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""x"":1,""Y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = Serializer.Deserialize<Point_2D>(@"{""y"":2,""X"":1}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public void VaryingOrderingOfJson()
        {
            Point_3D point = Serializer.Deserialize<Point_3D>(@"{""X"":1,""Y"":2,""Z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = Serializer.Deserialize<Point_3D>(@"{""X"":1,""Z"":3,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = Serializer.Deserialize<Point_3D>(@"{""Y"":2,""Z"":3,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = Serializer.Deserialize<Point_3D>(@"{""Y"":2,""X"":1,""Z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = Serializer.Deserialize<Point_3D>(@"{""Z"":3,""Y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = Serializer.Deserialize<Point_3D>(@"{""Z"":3,""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public void AsListElement()
        {
            List<Point_3D> list = Serializer.Deserialize<List<Point_3D>>(@"[{""Y"":2,""Z"":3,""X"":1},{""Z"":10,""Y"":30,""X"":20}]");
            Assert.Equal(1, list[0].X);
            Assert.Equal(2, list[0].Y);
            Assert.Equal(3, list[0].Z);
            Assert.Equal(20, list[1].X);
            Assert.Equal(30, list[1].Y);
            Assert.Equal(10, list[1].Z);
        }

        [Fact]
        public void AsDictionaryValue()
        {
            Dictionary<string, Point_3D> dict = Serializer.Deserialize<Dictionary<string, Point_3D>>(@"{""0"":{""Y"":2,""Z"":3,""X"":1},""1"":{""Z"":10,""Y"":30,""X"":20}}");
            Assert.Equal(1, dict["0"].X);
            Assert.Equal(2, dict["0"].Y);
            Assert.Equal(3, dict["0"].Z);
            Assert.Equal(20, dict["1"].X);
            Assert.Equal(30, dict["1"].Y);
            Assert.Equal(10, dict["1"].Z);
        }

        [Fact]
        public void AsProperty_Of_ObjectWithParameterlessCtor()
        {
            WrapperForPoint_3D obj = Serializer.Deserialize<WrapperForPoint_3D>(@"{""Point_3D"":{""Y"":2,""Z"":3,""X"":1}}");
            Point_3D point = obj.Point_3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public void AsProperty_Of_ObjectWithParameterizedCtor()
        {
            ClassWrapperForPoint_3D obj = Serializer.Deserialize<ClassWrapperForPoint_3D>(@"{""Point3D"":{""Y"":2,""Z"":3,""X"":1}}");
            Point_3D point = obj.Point3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public void At_Symbol_As_ParameterNamePrefix()
        {
            ClassWrapper_For_Int_String obj = Serializer.Deserialize<ClassWrapper_For_Int_String>(@"{""Int"":1,""String"":""1""}");
            Assert.Equal(1, obj.Int);
            Assert.Equal("1", obj.String);
        }

        [Fact]
        public void At_Symbol_As_ParameterNamePrefix_UseDefaultValues()
        {
            ClassWrapper_For_Int_String obj = Serializer.Deserialize<ClassWrapper_For_Int_String>(@"{""@Int"":1,""@String"":""1""}");
            Assert.Equal(0, obj.Int);
            Assert.Null(obj.String);
        }

        [Fact]
        public void PassDefaultValueToComplexStruct()
        {
            ClassWrapperForPoint_3D obj = Serializer.Deserialize<ClassWrapperForPoint_3D>(@"{}");
            Assert.True(obj.Point3D == default);

            ClassWrapper_For_Int_Point_3D_String obj1 = Serializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{}");
            Assert.Equal(0, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(0, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        [Fact]
        public void Null_AsArgument_To_ParameterThat_CanBeNull()
        {
            ClassWrapper_For_Int_Point_3D_String obj1 = Serializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":1,""MyPoint3DStruct"":{},""MyString"":null}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(50, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        [Fact]
        public void Null_AsArgument_To_ParameterThat_CanNotBeNull()
        {
            Assert.Throws<JsonException>(() => Serializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":null,""MyString"":""1""}"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyPoint3DStruct"":null,""MyString"":""1""}"));
        }

        [Fact]
        public void OtherPropertiesAreSet()
        {
            var personClass = Serializer.Deserialize<Person_Class>(Person_Class.s_json);
            personClass.Verify();

            var personStruct = Serializer.Deserialize<Person_Struct>(Person_Struct.s_json);
            personStruct.Verify();
        }

        [Fact]
        public void ExtraProperties_AreIgnored()
        {
            Point_2D point = Serializer.Deserialize<Point_2D>(@"{ ""x"":1,""y"":2,""b"":3}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
        }

        [Fact]
        public void ExtraProperties_GoInExtensionData_IfPresent()
        {
            Point_2D_With_ExtData point = Serializer.Deserialize<Point_2D_With_ExtData>(@"{""X"":1,""y"":2,""b"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.ExtensionData["y"].GetInt32());
            Assert.Equal(3, point.ExtensionData["b"].GetInt32());
        }

        [Fact]
        public void PropertiesNotSet_WhenJSON_MapsToConstructorParameters()
        {
            var obj = Serializer.Deserialize<Point_CtorsIgnoreJson>(@"{""X"":1,""Y"":2}");
            Assert.Equal(40, obj.X); // Would be 1 if property were set directly after object construction.
            Assert.Equal(60, obj.Y); // Would be 2 if property were set directly after object construction.
        }

        [Fact]
        public void IgnoreNullValues_DontSetNull_ToConstructorArguments_ThatCantBeNull()
        {
            // Throw JsonException when null applied to types that can't be null. Behavior should align with properties deserialized with setters.

            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}"));

            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null}"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""Point3DStruct"":null}"));

            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""Int"":null}"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""Int"":null}"));

            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""ImmutableArray"":null}"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""ImmutableArray"":null}"));

            // Throw even when IgnoreNullValues is true for symmetry with property deserialization,
            // until https://github.com/dotnet/runtime/issues/30795 is addressed.

            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));

            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null}", options));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));

            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""Int"":null}", options));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));

            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester>(@"{""ImmutableArray"":null}", options));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));
        }

        [Fact]
        public void NumerousSimpleAndComplexParameters()
        {
            var obj = Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_json);
            obj.Verify();
        }

        [Fact]
        public void ClassWithPrimitives_Parameterless()
        {
            var point = new Parameterless_ClassWithPrimitives();

            point.FirstInt = 348943;
            point.SecondInt = 348943;
            point.FirstString = "934sdkjfskdfssf";
            point.SecondString = "sdad9434243242";
            point.FirstDateTime = DateTime.Now;
            point.SecondDateTime = DateTime.Now.AddHours(1).AddYears(1);

            point.X = 234235;
            point.Y = 912874;
            point.Z = 434934;

            point.ThirdInt = 348943;
            point.FourthInt = 348943;
            point.ThirdString = "934sdkjfskdfssf";
            point.FourthString = "sdad9434243242";
            point.ThirdDateTime = DateTime.Now;
            point.FourthDateTime = DateTime.Now.AddHours(1).AddYears(1);

            string json = JsonSerializer.Serialize(point);

            var deserialized = Serializer.Deserialize<Parameterless_ClassWithPrimitives>(json);
            Assert.Equal(point.FirstInt, deserialized.FirstInt);
            Assert.Equal(point.SecondInt, deserialized.SecondInt);
            Assert.Equal(point.FirstString, deserialized.FirstString);
            Assert.Equal(point.SecondString, deserialized.SecondString);
            Assert.Equal(point.FirstDateTime, deserialized.FirstDateTime);
            Assert.Equal(point.SecondDateTime, deserialized.SecondDateTime);

            Assert.Equal(point.X, deserialized.X);
            Assert.Equal(point.Y, deserialized.Y);
            Assert.Equal(point.Z, deserialized.Z);

            Assert.Equal(point.ThirdInt, deserialized.ThirdInt);
            Assert.Equal(point.FourthInt, deserialized.FourthInt);
            Assert.Equal(point.ThirdString, deserialized.ThirdString);
            Assert.Equal(point.FourthString, deserialized.FourthString);
            Assert.Equal(point.ThirdDateTime, deserialized.ThirdDateTime);
            Assert.Equal(point.FourthDateTime, deserialized.FourthDateTime);
        }

        [Fact]
        public void ClassWithPrimitives()
        {
            var point = new Parameterized_ClassWithPrimitives_3Args(x: 234235, y: 912874, z: 434934);

            point.FirstInt = 348943;
            point.SecondInt = 348943;
            point.FirstString = "934sdkjfskdfssf";
            point.SecondString = "sdad9434243242";
            point.FirstDateTime = DateTime.Now;
            point.SecondDateTime = DateTime.Now.AddHours(1).AddYears(1);

            point.ThirdInt = 348943;
            point.FourthInt = 348943;
            point.ThirdString = "934sdkjfskdfssf";
            point.FourthString = "sdad9434243242";
            point.ThirdDateTime = DateTime.Now;
            point.FourthDateTime = DateTime.Now.AddHours(1).AddYears(1);

            string json = JsonSerializer.Serialize(point);

            var deserialized = Serializer.Deserialize<Parameterized_ClassWithPrimitives_3Args>(json);
            Assert.Equal(point.FirstInt, deserialized.FirstInt);
            Assert.Equal(point.SecondInt, deserialized.SecondInt);
            Assert.Equal(point.FirstString, deserialized.FirstString);
            Assert.Equal(point.SecondString, deserialized.SecondString);
            Assert.Equal(point.FirstDateTime, deserialized.FirstDateTime);
            Assert.Equal(point.SecondDateTime, deserialized.SecondDateTime);

            Assert.Equal(point.X, deserialized.X);
            Assert.Equal(point.Y, deserialized.Y);
            Assert.Equal(point.Z, deserialized.Z);

            Assert.Equal(point.ThirdInt, deserialized.ThirdInt);
            Assert.Equal(point.FourthInt, deserialized.FourthInt);
            Assert.Equal(point.ThirdString, deserialized.ThirdString);
            Assert.Equal(point.FourthString, deserialized.FourthString);
            Assert.Equal(point.ThirdDateTime, deserialized.ThirdDateTime);
            Assert.Equal(point.FourthDateTime, deserialized.FourthDateTime);
        }

        [Fact]
        public void ClassWithPrimitivesPerf()
        {
            var point = new Parameterized_ClassWithPrimitives_3Args(x: 234235, y: 912874, z: 434934);

            point.FirstInt = 348943;
            point.SecondInt = 348943;
            point.FirstString = "934sdkjfskdfssf";
            point.SecondString = "sdad9434243242";
            point.FirstDateTime = DateTime.Now;
            point.SecondDateTime = DateTime.Now.AddHours(1).AddYears(1);

            point.ThirdInt = 348943;
            point.FourthInt = 348943;
            point.ThirdString = "934sdkjfskdfssf";
            point.FourthString = "sdad9434243242";
            point.ThirdDateTime = DateTime.Now;
            point.FourthDateTime = DateTime.Now.AddHours(1).AddYears(1);

            string json = JsonSerializer.Serialize(point);

            Serializer.Deserialize<Parameterized_ClassWithPrimitives_3Args>(json);
            Serializer.Deserialize<Parameterized_ClassWithPrimitives_3Args>(json);
        }

        [Fact]
        public void TupleDeserializationWorks()
        {
            var tuple = Serializer.Deserialize<Tuple<string, double>>(@"{""Item1"":""New York"",""Item2"":32.68}");
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleWrapper = Serializer.Deserialize<TupleWrapper>(@"{""Tuple"":{""Item1"":""New York"",""Item2"":32.68}}");
            tuple = tupleWrapper.Tuple;
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleList = Serializer.Deserialize<List<Tuple<string, double>>>(@"[{""Item1"":""New York"",""Item2"":32.68}]");
            tuple = tupleList[0];
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);
        }

        [Fact]
        public void TupleDeserialization_MoreThanSevenItems()
        {
            // Seven is okay
            string json = JsonSerializer.Serialize(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
            var obj = Serializer.Deserialize<Tuple<int, int, int, int, int, int, int>>(json);
            Assert.Equal(json, JsonSerializer.Serialize(obj));

            // More than seven arguments needs special casing and can be revisted.
            // Newtonsoft.Json fails in the same way.
            json = JsonSerializer.Serialize(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Tuple<int, int, int, int, int, int, int, int>>(json));

            // Invalid JSON representing a tuple with more than seven items yields an ArgumentException from the constructor.
            // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
            // We pass the number 8, not a new Tuple<int>(8).
            // Fixing this needs special casing. Newtonsoft behaves the same way.
            string invalidJson = @"{""Item1"":1,""Item2"":2,""Item3"":3,""Item4"":4,""Item5"":5,""Item6"":6,""Item7"":7,""Item1"":8}";
            Assert.Throws<ArgumentException>(() => Serializer.Deserialize<Tuple<int, int, int, int, int, int, int, int>>(invalidJson));
        }

        [Fact]
        public void TupleDeserialization_DefaultValuesUsed_WhenJsonMissing()
        {
            // Seven items; only three provided.
            string input = @"{""Item2"":""2"",""Item3"":3,""Item6"":6}";
            var obj = Serializer.Deserialize<Tuple<int, string, int, string, string, int, Point_3D_Struct>>(input);

            string serialized = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""Item1"":0", serialized);
            Assert.Contains(@"""Item2"":""2""", serialized);
            Assert.Contains(@"""Item3"":3", serialized);
            Assert.Contains(@"""Item4"":null", serialized);
            Assert.Contains(@"""Item5"":null", serialized);
            Assert.Contains(@"""Item6"":6", serialized);
            Assert.Contains(@"""Item7"":{", serialized);

            serialized = JsonSerializer.Serialize(obj.Item7);
            Assert.Contains(@"""X"":0", serialized);
            Assert.Contains(@"""Y"":0", serialized);
            Assert.Contains(@"""Z"":0", serialized);

            // Although no Json is provided for the 8th item, ArgumentException is still thrown as we use default(int) as the argument/
            // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
            // We pass the number 8, not a new Tuple<int>(default(int)).
            // Fixing this needs special casing. Newtonsoft behaves the same way.
            Assert.Throws<ArgumentException>(() => Serializer.Deserialize<Tuple<int, string, int, string, string, int, Point_3D_Struct, int>>(input));
        }

        [Fact]
        public void TupleDeserializationWorks_ClassWithParameterizedCtor()
        {
            string classJson = ClassWithConstructor_SimpleAndComplexParameters.s_json;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(@$"""Item{i + 1}"":{classJson},");
            }
            sb.Append(@$"""Item7"":{classJson}");
            sb.Append("}");

            string complexTupleJson = sb.ToString();

            var complexTuple = Serializer.Deserialize<Tuple<
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters>>(complexTupleJson);

            complexTuple.Item1.Verify();
            complexTuple.Item2.Verify();
            complexTuple.Item3.Verify();
            complexTuple.Item4.Verify();
            complexTuple.Item5.Verify();
            complexTuple.Item6.Verify();
            complexTuple.Item7.Verify();
        }

        [Fact]
        public void TupleDeserializationWorks_ClassWithParameterlessCtor()
        {
            string classJson = SimpleTestClass.s_json;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(@$"""Item{i + 1}"":{classJson},");
            }
            sb.Append(@$"""Item7"":{classJson}");
            sb.Append("}");

            string complexTupleJson = sb.ToString();

            var complexTuple = Serializer.Deserialize<Tuple<
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass>>(complexTupleJson);

            complexTuple.Item1.Verify();
            complexTuple.Item2.Verify();
            complexTuple.Item3.Verify();
            complexTuple.Item4.Verify();
            complexTuple.Item5.Verify();
            complexTuple.Item6.Verify();
            complexTuple.Item7.Verify();
        }

        [Fact]
        public void NoConstructorHandlingWhenObjectHasConverter()
        {
            // Baseline without converter
            string serialized = JsonSerializer.Serialize(new Point_3D(10, 6));

            Point_3D point = Serializer.Deserialize<Point_3D>(serialized);
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = JsonSerializer.Serialize(new[] { new Point_3D(10, 6) });

            point = Serializer.Deserialize<Point_3D[]>(serialized)[0];
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = JsonSerializer.Serialize(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = Serializer.Deserialize<WrapperForPoint_3D>(serialized).Point_3D;
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            // Converters for objects with parameterized ctors are honored

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForPoint3D());

            serialized = JsonSerializer.Serialize(new Point_3D(10, 6));

            point = Serializer.Deserialize<Point_3D>(serialized, options);
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = JsonSerializer.Serialize(new[] { new Point_3D(10, 6) });

            point = Serializer.Deserialize<Point_3D[]>(serialized, options)[0];
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = JsonSerializer.Serialize(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = Serializer.Deserialize<WrapperForPoint_3D>(serialized, options).Point_3D;
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);
        }

        [Fact]
        public void ConstructorHandlingHonorsCustomConverters()
        {
            // Baseline, use internal converters for primitives
            Point_2D point = Serializer.Deserialize<Point_2D>(@"{""X"":2,""Y"":3}");
            Assert.Equal(2, point.X);
            Assert.Equal(3, point.Y);

            // Honor custom converters
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForInt32());

            point = Serializer.Deserialize<Point_2D>(@"{""X"":2,""Y"":3}", options);
            Assert.Equal(25, point.X);
            Assert.Equal(25, point.X);
        }

        [Fact]
        public void CanDeserialize_ObjectWith_Ctor_With_64_Params()
        {
            void RunTest<T>()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                for (int i = 0; i < 63; i++)
                {
                    sb.Append($@"""Int{i}"":{i},");
                }
                sb.Append($@"""Int63"":63");
                sb.Append("}");

                string input = sb.ToString();

                object obj = Serializer.Deserialize<T>(input);
                for (int i = 0; i < 64; i++)
                {
                    Assert.Equal(i, (int)typeof(T).GetProperty($"Int{i}").GetValue(obj));
                }
            }

            RunTest<Struct_With_Ctor_With_64_Params>();
            RunTest<Class_With_Ctor_With_64_Params>();
        }

        [Fact]
        public void Cannot_Deserialize_ObjectWith_Ctor_With_65_Params()
        {
            void RunTest<T>()
            {
                Type type = typeof(T);

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                for (int i = 0; i < 64; i++)
                {
                    sb.Append($@"""Int{i}"":{i},");
                }
                sb.Append($@"""Int64"":64");
                sb.Append("}");

                string input = sb.ToString();

                sb = new StringBuilder();
                sb.Append("(");
                for (int i = 0; i < 64; i++)
                {
                    sb.Append("Int32, ");
                }
                sb.Append("Int32");
                sb.Append(")");

                string ctorAsString = sb.ToString();

                NotSupportedException ex = Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<T>(input));
                string strEx = ex.ToString();
                Assert.Contains(ctorAsString, strEx);
                Assert.Contains(type.ToString(), strEx);

                ex = Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<T>("{}"));
                strEx = ex.ToString();
                Assert.Contains(ctorAsString, strEx);
                Assert.Contains(type.ToString(), strEx);
            }

            RunTest<Class_With_Ctor_With_65_Params>();
            RunTest<Struct_With_Ctor_With_65_Params>();
        }

        [Fact]
        public void Deserialize_ObjectWith_Ctor_With_65_Params_IfNull()
        {
            Assert.Null(Serializer.Deserialize<Class_With_Ctor_With_65_Params>("null"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Struct_With_Ctor_With_65_Params>("null"));
        }

        [Fact]
        public void Escaped_ParameterNames_Work()
        {
            Point_2D point = Serializer.Deserialize<Point_2D>(@"{""\u0058"":1,""\u0059"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public void FirstParameterWins()
        {
            Point_2D point = Serializer.Deserialize<Point_2D>(@"{""X"":1,""Y"":2,""X"":4}");
            Assert.Equal(4, point.X); // Not 1.
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public void SubsequentParameter_GoesToExtensionData()
        {
            string json = @"{
                ""FirstName"":""Jet"",
                ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
                ""EmailAddress"":""jetdoe@outlook.com"",
                ""Id"":""0b3aa420-2e98-47f7-8a49-fea233b89416"",
                ""LastName"":""Doe"",
                ""Id"":""63cf821d-fd47-4782-8345-576d9228a534""
                }";

            Parameterized_Person person = Serializer.Deserialize<Parameterized_Person>(json);
            Assert.Equal("Jet", person.FirstName);
            Assert.Equal("Doe", person.LastName);
            Assert.Equal("63cf821d-fd47-4782-8345-576d9228a534", person.Id.ToString());
            Assert.Equal("jetdoe@outlook.com", person.ExtensionData["EmailAddress"].GetString());
            Assert.False(person.ExtensionData.ContainsKey("Id"));
        }

        [Fact]
        public void BitVector32_UsesStructDefaultCtor_MultipleParameterizedCtor()
        {
            string serialized = JsonSerializer.Serialize(new BitVector32(1));
            Assert.Equal(0, Serializer.Deserialize<BitVector32>(serialized).Data);
        }

        [Fact]
        public void HonorExtensionDataGeneric()
        {
            var obj1 = Serializer.Deserialize<SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt>(@"{""key"": ""value""}");
            Assert.Equal("value", obj1.ExtensionData["key"].GetString());

            var obj2 = Serializer.Deserialize<SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt>(@"{""key"": ""value""}");
            Assert.Equal("value", ((JsonElement)obj2.ExtensionData["key"]).GetString());

            var obj3 = Serializer.Deserialize<SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt>(@"{""key"": ""value""}");
            Assert.Equal("value", obj3.ExtensionData["key"].GetString());

            var obj4 = Serializer.Deserialize<SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt>(@"{""key"": ""value""}");
            Assert.Equal("value", ((JsonElement)obj4.ExtensionData["key"]).GetString());
        }

        [Fact]
        public void ArgumentDeserialization_Honors_JsonPropertyName()
        {
            Point_MembersHave_JsonPropertyName point = new Point_MembersHave_JsonPropertyName(1, 2);

            string json = JsonSerializer.Serialize(point);
            Assert.Contains(@"""XValue"":1", json);
            Assert.Contains(@"""YValue"":2", json);

            point = Serializer.Deserialize<Point_MembersHave_JsonPropertyName>(json);
            point.Verify();
        }

        [Fact]
        public void ArgumentDeserialization_Honors_JsonPropertyName_CaseInsensitiveWorks()
        {
            string json = @"{""XVALUE"":1,""yvalue"":2}";

            // Without case insensitivity, there's no match.
            Point_MembersHave_JsonPropertyName point = Serializer.Deserialize<Point_MembersHave_JsonPropertyName>(json);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            point = Serializer.Deserialize<Point_MembersHave_JsonPropertyName>(json, options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public void ArgumentDeserialization_Honors_ConverterOnProperty()
        {
            var point = Serializer.Deserialize<Point_MembersHave_JsonConverter>(Point_MembersHave_JsonConverter.s_json);
            point.Verify();
        }

        [Fact]
        public void ArgumentDeserialization_Honors_JsonIgnore()
        {
            var point = Serializer.Deserialize<Point_MembersHave_JsonIgnore>(Point_MembersHave_JsonIgnore.s_json);
            point.Verify();
        }

        [Fact]
        public void ArgumentDeserialization_UseNamingPolicy_ToMatch()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LowerCaseNamingPolicy()
            };

            string json = JsonSerializer.Serialize(new Point_ExtendedPropNames(1, 2), options);

            // If we don't use naming policy, then we can't match serialized properties to constructor parameters on deserialization.
            var point = Serializer.Deserialize<Point_ExtendedPropNames>(json);
            Assert.Equal(0, point.XValue);
            Assert.Equal(0, point.YValue);

            point = Serializer.Deserialize<Point_ExtendedPropNames>(json, options);
            Assert.Equal(1, point.XValue);
            Assert.Equal(2, point.YValue);
        }

        [Fact]
        public void ArgumentDeserialization_UseNamingPolicy_ToMatch_CaseInsensitiveWorks()
        {
            var options1 = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SimpleSnakeCasePolicy()
            };

            string json = @"{""x_VaLUE"":1,""Y_vALue"":2}";

            // If we don't use case sensitivity, then we can't match serialized properties to constructor parameters on deserialization.
            Point_ExtendedPropNames point = Serializer.Deserialize<Point_ExtendedPropNames>(json, options1);
            Assert.Equal(0, point.XValue);
            Assert.Equal(0, point.YValue);

            var options2 = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SimpleSnakeCasePolicy(),
                PropertyNameCaseInsensitive = true,
            };

            point = Serializer.Deserialize<Point_ExtendedPropNames>(json, options2);
            Assert.Equal(1, point.XValue);
            Assert.Equal(2, point.YValue);
        }

        [Fact]
        public void ArgumentDeserialization_UseNamingPolicy_InvalidPolicyFails()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new NullNamingPolicy()
            };

            Assert.Throws<InvalidOperationException>(() => Serializer.Deserialize<Point_ExtendedPropNames>("{}", options));
        }

        [Fact]
        public void ComplexJson_As_LastCtorArg()
        {
            Point_With_Array obj1 = Serializer.Deserialize<Point_With_Array>(Point_With_Array.s_json);
            ((ITestClass)obj1).Verify();

            Point_With_Dictionary obj2 = Serializer.Deserialize<Point_With_Dictionary>(Point_With_Dictionary.s_json);
            ((ITestClass)obj2).Verify();

            Point_With_Object obj3 = Serializer.Deserialize<Point_With_Object>(Point_With_Object.s_json);
            ((ITestClass)obj3).Verify();
        }

        [Fact]
        public void NumerousPropertiesWork()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append(@"""X"":1,");
            sb.Append(@"""Y"":2,");

            for (int i = 0; i < 65; i++)
            {
                sb.Append($@"""Z"":{i},");
            }

            sb.Append(@"""Z"":66");
            sb.Append("}");

            string json = sb.ToString();

            var point = Serializer.Deserialize<Point_With_Property>(json);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(66, point.Z);
        }

        [Fact]
        public void ArgumentStateNotOverwritten()
        {
            ClassWithNestedClass obj = new ClassWithNestedClass(myClass: null, myPoint: default);
            ClassWithNestedClass obj1 = new ClassWithNestedClass(myClass: obj, myPoint: new Point_2D_Struct_WithAttribute(1, 2));
            ClassWithNestedClass obj2 = new ClassWithNestedClass(myClass: obj1, myPoint: new Point_2D_Struct_WithAttribute(3, 4));

            string json = JsonSerializer.Serialize(obj2);

            obj2 = Serializer.Deserialize<ClassWithNestedClass>(json);
            Assert.Equal(3, obj2.MyPoint.X);
            Assert.Equal(4, obj2.MyPoint.Y);

            obj1 = obj2.MyClass;
            Assert.Equal(1, obj1.MyPoint.X);
            Assert.Equal(2, obj1.MyPoint.Y);

            obj = obj1.MyClass;
            Assert.Equal(0, obj.MyPoint.X);
            Assert.Equal(0, obj.MyPoint.Y);

            Assert.Null(obj.MyClass);
        }

        [Fact]
        public void FourArgsWork()
        {
            string json = JsonSerializer.Serialize(new StructWithFourArgs(1, 2, 3, 4));

            var obj = Serializer.Deserialize<StructWithFourArgs>(json);
            Assert.Equal(1, obj.W);
            Assert.Equal(2, obj.X);
            Assert.Equal(3, obj.Y);
            Assert.Equal(4, obj.Z);
        }

        [Fact]
        public void InvalidJsonFails()
        {
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D>("{1"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D>("{x"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D>("{{"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D>("{true"));

            // Also test deserialization of objects with parameterless ctors
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D_Struct>("{1"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D_Struct>("{x"));
            Assert.Throws<JsonException>(() => Serializer.Deserialize<Point_2D_Struct>("{true"));
        }
    }
}

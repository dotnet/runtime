// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstructorTests
    {
        [Theory]
        [InlineData(typeof(Point_2D))]
        [InlineData(typeof(Point_3D))]
        public static void ReturnNullForNullObjects(Type type)
        {
            Assert.Null(JsonSerializer.Deserialize("null", type));
        }

        [Fact]
        public static void JsonExceptionWhenAssigningNullToStruct()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Point_2D_With_ExtData>("null"));
        }

        [Fact]
        public static void MatchJsonPropertyToConstructorParameters()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""Y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void UseDefaultValues_When_NoJsonMatch()
        {
            // Using CLR value when `ParameterInfo.DefaultValue` is not set.
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""y"":2,""x"":1}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            // Using `ParameterInfo.DefaultValue` when set; using CLR value as fallback.
            Point_3D point3d = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""Y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3,""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);
        }

        [Fact]
        public static void CaseInsensitivityWorks()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""y"":2,""x"":1}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""Y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""y"":2,""X"":1}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void VaryingOrderingOfJson()
        {
            Point_3D point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""Y"":2,""Z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""Z"":3,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""Y"":2,""Z"":3,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""Y"":2,""X"":1,""Z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3,""Y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3,""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public static void AsListElement()
        {
            List<Point_3D> list = JsonSerializer.Deserialize<List<Point_3D>>(@"[{""Y"":2,""Z"":3,""X"":1},{""Z"":10,""Y"":30,""X"":20}]");
            Assert.Equal(1, list[0].X);
            Assert.Equal(2, list[0].Y);
            Assert.Equal(3, list[0].Z);
            Assert.Equal(20, list[1].X);
            Assert.Equal(30, list[1].Y);
            Assert.Equal(10, list[1].Z);
        }

        [Fact]
        public static void AsDictionaryValue()
        {
            Dictionary<string, Point_3D> dict = JsonSerializer.Deserialize<Dictionary<string, Point_3D>>(@"{""0"":{""Y"":2,""Z"":3,""X"":1},""1"":{""Z"":10,""Y"":30,""X"":20}}");
            Assert.Equal(1, dict["0"].X);
            Assert.Equal(2, dict["0"].Y);
            Assert.Equal(3, dict["0"].Z);
            Assert.Equal(20, dict["1"].X);
            Assert.Equal(30, dict["1"].Y);
            Assert.Equal(10, dict["1"].Z);
        }

        [Fact]
        public static void AsProperty_Of_ObjectWithParameterlessCtor()
        {
            WrapperForPoint_3D obj = JsonSerializer.Deserialize<WrapperForPoint_3D>(@"{""Point_3D"":{""Y"":2,""Z"":3,""X"":1}}");
            Point_3D point = obj.Point_3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public static void AsProperty_Of_ObjectWithParameterizedCtor()
        {
            ClassWrapperForPoint_3D obj = JsonSerializer.Deserialize<ClassWrapperForPoint_3D>(@"{""Point3D"":{""Y"":2,""Z"":3,""X"":1}}");
            Point_3D point = obj.Point3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public static void At_Symbol_As_ParameterNamePrefix()
        {
            ClassWrapper_For_Int_String obj = JsonSerializer.Deserialize<ClassWrapper_For_Int_String>(@"{""Int"":1,""String"":""1""}");
            Assert.Equal(1, obj.Int);
            Assert.Equal("1", obj.String);
        }

        [Fact]
        public static void At_Symbol_As_ParameterNamePrefix_UseDefaultValues()
        {
            ClassWrapper_For_Int_String obj = JsonSerializer.Deserialize<ClassWrapper_For_Int_String>(@"{""@Int"":1,""@String"":""1""}");
            Assert.Equal(0, obj.Int);
            Assert.Null(obj.String);
        }

        [Fact]
        public static void PassDefaultValueToComplexStruct()
        {
            ClassWrapperForPoint_3D obj = JsonSerializer.Deserialize<ClassWrapperForPoint_3D>(@"{}");
            Assert.True(obj.Point3D == default);

            ClassWrapper_For_Int_Point_3D_String obj1 = JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{}");
            Assert.Equal(0, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(0, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        [Fact]
        public static void Null_AsArgument_To_ParameterThat_CanBeNull()
        {
            ClassWrapper_For_Int_Point_3D_String obj1 = JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":1,""MyPoint3DStruct"":{},""MyString"":null}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(50, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        [Fact]
        public static void Null_AsArgument_To_ParameterThat_CanNotBeNull()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":null,""MyString"":""1""}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyPoint3DStruct"":null,""MyString"":""1""}"));
        }

        [Theory]
        [MemberData(nameof(PersonTestData))]
        public static void OtherPropertiesAreSet(Type type, string json)
        {
            object person = JsonSerializer.Deserialize(json, type);
            ((ITestClass)person).Verify();
        }

        private static IEnumerable<object[]> PersonTestData()
        {
            yield return new object[] { typeof(Person_Class), Person_Class.s_json };
            yield return new object[] { typeof(Person_Struct), Person_Struct.s_json };
        }

        [Fact]
        public static void ExtraProperties_AreIgnored()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{ ""x"":1,""y"":2,""b"":3}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
        }

        [Fact]
        public static void ExtraProperties_GoInExtensionData_IfPresent()
        {
            Point_2D_With_ExtData point = JsonSerializer.Deserialize<Point_2D_With_ExtData>(@"{""X"":1,""y"":2,""b"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.ExtensionData["y"].GetInt32());
            Assert.Equal(3, point.ExtensionData["b"].GetInt32());
        }

        [Fact]
        public static void PropertiesNotSet_WhenJSON_MapsToConstructorParameters()
        {
            var obj = JsonSerializer.Deserialize<Point_CtorsIgnoreJson>(@"{""X"":1,""Y"":2}");
            Assert.Equal(40, obj.X); // Would be 1 if property were set directly after object construction.
            Assert.Equal(60, obj.Y); // Would be 2 if property were set directly after object construction.
        }

        [Fact]
        public static void IgnoreNullValues_DontSetNull_ToConstructorArguments_ThatCantBeNull()
        {
            // Default is to throw JsonException when null applied to types that can't be null.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""Int"":null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""ImmutableArray"":null}"));

            // Set arguments to default values when IgnoreNullValues is on.
            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            var obj = JsonSerializer.Deserialize<NullArgTester>(@"{""Int"":null,""Point3DStruct"":null,""ImmutableArray"":null}", options);
            Assert.Equal(0, obj.Point3DStruct.X);
            Assert.Equal(0, obj.Point3DStruct.Y);
            Assert.Equal(0, obj.Point3DStruct.Z);
            Assert.True(obj.ImmutableArray.IsDefault);
            Assert.Equal(50, obj.Int);
        }

        [Fact]
        public static void NumerousSimpleAndComplexParameters()
        {
            var obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ClassWithPrimitives_Parameterless()
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

            var deserialized = JsonSerializer.Deserialize<Parameterless_ClassWithPrimitives>(json);
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
        public static void ClassWithPrimitives()
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

            var deserialized = JsonSerializer.Deserialize<Parameterized_ClassWithPrimitives_3Args>(json);
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
        public static void ClassWithPrimitivesPerf()
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

            JsonSerializer.Deserialize<Parameterized_ClassWithPrimitives_3Args>(json);
            JsonSerializer.Deserialize<Parameterized_ClassWithPrimitives_3Args>(json);
        }

        [Fact]
        public static void TupleDeserializationWorks()
        {
            var tuple = JsonSerializer.Deserialize<Tuple<string, double>>(@"{""Item1"":""New York"",""Item2"":32.68}");
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleWrapper = JsonSerializer.Deserialize<TupleWrapper>(@"{""Tuple"":{""Item1"":""New York"",""Item2"":32.68}}");
            tuple = tupleWrapper.Tuple;
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleList = JsonSerializer.Deserialize<List<Tuple<string, double>>>(@"[{""Item1"":""New York"",""Item2"":32.68}]");
            tuple = tupleList[0];
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);
        }

        [Fact]
        public static void TupleDeserialization_MoreThanSevenItems()
        {
            // Seven is okay
            string json = JsonSerializer.Serialize(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
            var obj = JsonSerializer.Deserialize<Tuple<int, int, int, int, int, int, int>>(json);
            Assert.Equal(json, JsonSerializer.Serialize(obj));

            // More than seven arguments needs special casing and can be revisted.
            // Newtonsoft.Json fails in the same way.
            json = JsonSerializer.Serialize(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Tuple<int, int, int, int, int, int, int, int>>(json));

            // Invalid JSON representing a tuple with more than seven items yields an ArgumentException from the constructor.
            // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
            // We pass the number 8, not a new Tuple<int>(8).
            // Fixing this needs special casing. Newtonsoft behaves the same way.
            string invalidJson = @"{""Item1"":1,""Item2"":2,""Item3"":3,""Item4"":4,""Item5"":5,""Item6"":6,""Item7"":7,""Item1"":8}";
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Tuple<int, int, int, int, int, int, int, int>>(invalidJson));
        }

        [Fact]
        public static void TupleDeserialization_DefaultValuesUsed_WhenJsonMissing()
        {
            // Seven items; only three provided.
            string input = @"{""Item2"":""2"",""Item3"":3,""Item6"":6}";
            var obj = JsonSerializer.Deserialize<Tuple<int, string, int, string, string, int, Point_3D_Struct>>(input);

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
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Tuple<int, string, int, string, string, int, Point_3D_Struct, int>>(input));
        }

        [Fact]
        public static void TupleDeserializationWorks_ClassWithParameterizedCtor()
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

            var complexTuple = JsonSerializer.Deserialize<Tuple<
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
        public static void TupleDeserializationWorks_ClassWithParameterlessCtor()
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

            var complexTuple = JsonSerializer.Deserialize<Tuple<
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
        public static void NoConstructorHandlingWhenObjectHasConverter()
        {
            // Baseline without converter
            string serialized = JsonSerializer.Serialize(new Point_3D(10, 6));

            Point_3D point = JsonSerializer.Deserialize<Point_3D>(serialized);
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = JsonSerializer.Serialize(new[] { new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<Point_3D[]>(serialized)[0];
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = JsonSerializer.Serialize(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<WrapperForPoint_3D>(serialized).Point_3D;
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            // Converters for objects with parameterized ctors are honored

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForPoint3D());

            serialized = JsonSerializer.Serialize(new Point_3D(10, 6));

            point = JsonSerializer.Deserialize<Point_3D>(serialized, options);
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = JsonSerializer.Serialize(new[] { new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<Point_3D[]>(serialized, options)[0];
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = JsonSerializer.Serialize(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<WrapperForPoint_3D>(serialized, options).Point_3D;
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);
        }

        [Fact]
        public static void ConstructorHandlingHonorsCustomConverters()
        {
            // Baseline, use internal converters for primitives
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":2,""Y"":3}");
            Assert.Equal(2, point.X);
            Assert.Equal(3, point.Y);

            // Honor custom converters
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForInt32());

            point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":2,""Y"":3}", options);
            Assert.Equal(25, point.X);
            Assert.Equal(25, point.X);
        }

        [Theory]
        [InlineData(typeof(Struct_With_Ctor_With_64_Params))]
        [InlineData(typeof(Class_With_Ctor_With_64_Params))]
        public static void CanDeserialize_ObjectWith_Ctor_With_64_Params(Type type)
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

            object obj = JsonSerializer.Deserialize(input, type);
            for (int i = 0; i < 64; i++)
            {
                Assert.Equal(i, (int)type.GetProperty($"Int{i}").GetValue(obj));
            }
        }

        [Theory]
        [InlineData(typeof(Class_With_Ctor_With_65_Params))]
        [InlineData(typeof(Struct_With_Ctor_With_65_Params))]
        public static void Cannot_Deserialize_ObjectWith_Ctor_With_65_Params(Type type)
        {
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

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(input, type));
            string strEx = ex.ToString();
            Assert.Contains(ctorAsString, strEx);
            Assert.Contains(type.ToString(), strEx);

            ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("{}", type));
            strEx = ex.ToString();
            Assert.Contains(ctorAsString, strEx);
            Assert.Contains(type.ToString(), strEx);
        }

        [Fact]
        public static void Deserialize_ObjectWith_Ctor_With_65_Params_IfNull()
        {
            Assert.Null(JsonSerializer.Deserialize<Class_With_Ctor_With_65_Params>("null"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Struct_With_Ctor_With_65_Params>("null"));
        }

        [Fact]
        public static void Escaped_ParameterNames_Work()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""\u0058"":1,""\u0059"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void FirstParameterWins()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1,""Y"":2,""X"":4}");
            Assert.Equal(4, point.X); // Not 1.
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void SubsequentParameter_GoesToExtensionData()
        {
            string json = @"{
                ""FirstName"":""Jet"",
                ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
                ""EmailAddress"":""jetdoe@outlook.com"",
                ""Id"":""0b3aa420-2e98-47f7-8a49-fea233b89416"",
                ""LastName"":""Doe"",
                ""Id"":""63cf821d-fd47-4782-8345-576d9228a534""
                }";

            Parameterized_Person person = JsonSerializer.Deserialize<Parameterized_Person>(json);
            Assert.Equal("Jet", person.FirstName);
            Assert.Equal("Doe", person.LastName);
            Assert.Equal("63cf821d-fd47-4782-8345-576d9228a534", person.Id.ToString());
            Assert.Equal("jetdoe@outlook.com", person.ExtensionData["EmailAddress"].GetString());
            Assert.False(person.ExtensionData.ContainsKey("Id"));
        }

        [Fact]
        public static void BitVector32_UsesStructDefaultCtor_MultipleParameterizedCtor()
        {
            string serialized = JsonSerializer.Serialize(new BitVector32(1));
            Assert.Equal(0, JsonSerializer.Deserialize<BitVector32>(serialized).Data);
        }

        [Theory]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericIDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericIDictionary_ObjectExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericDictionary_ObjectExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt))]
        public static void HonorExtensionData(Type type)
        {
            var obj1 = JsonSerializer.Deserialize(@"{""key"": ""value""}", type);

            object extensionData = type.GetProperty("ExtensionData").GetValue(obj1);

            JsonElement value;
            if (extensionData is IDictionary<string, JsonElement> typedExtensionData)
            {
                value = typedExtensionData["key"];
            }
            else
            {
                value = (JsonElement)((IDictionary<string, object>)extensionData)["key"];
            }

            Assert.Equal("value", value.GetString());
        }

        [Fact]
        public static void ArgumentDeserialization_Honors_JsonPropertyName()
        {
            Point_MembersHave_JsonPropertyName point = new Point_MembersHave_JsonPropertyName(1, 2);

            string json = JsonSerializer.Serialize(point);
            Assert.Contains(@"""XValue"":1", json);
            Assert.Contains(@"""YValue"":2", json);

            point = JsonSerializer.Deserialize<Point_MembersHave_JsonPropertyName>(json);
            point.Verify();
        }

        [Fact]
        public static void ArgumentDeserialization_Honors_JsonPropertyName_CaseInsensitiveWorks()
        {
            string json = @"{""XVALUE"":1,""yvalue"":2}";

            // Without case insensitivity, there's no match.
            Point_MembersHave_JsonPropertyName point = JsonSerializer.Deserialize<Point_MembersHave_JsonPropertyName>(json);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            point = JsonSerializer.Deserialize<Point_MembersHave_JsonPropertyName>(json, options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void ArgumentDeserialization_Honors_ConverterOnProperty()
        {
            var point = JsonSerializer.Deserialize<Point_MembersHave_JsonConverter>(Point_MembersHave_JsonConverter.s_json);
            point.Verify();
        }

        [Fact]
        public static void ArgumentDeserialization_Honors_JsonIgnore()
        {
            var point = JsonSerializer.Deserialize<Point_MembersHave_JsonIgnore>(Point_MembersHave_JsonIgnore.s_json);
            point.Verify();
        }

        [Fact]
        public static void ArgumentDeserialization_UseNamingPolicy_ToMatch()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LowerCaseNamingPolicy()
            };

            string json = JsonSerializer.Serialize(new Point_ExtendedPropNames(1, 2), options);

            // If we don't use naming policy, then we can't match serialized properties to constructor parameters on deserialization.
            var point = JsonSerializer.Deserialize<Point_ExtendedPropNames>(json);
            Assert.Equal(0, point.XValue);
            Assert.Equal(0, point.YValue);

            point = JsonSerializer.Deserialize<Point_ExtendedPropNames>(json, options);
            Assert.Equal(1, point.XValue);
            Assert.Equal(2, point.YValue);
        }

        [Fact]
        public static void ArgumentDeserialization_UseNamingPolicy_ToMatch_CaseInsensitiveWorks()
        {
            var options1 = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SimpleSnakeCasePolicy()
            };

            string json = @"{""x_VaLUE"":1,""Y_vALue"":2}";

            // If we don't use case sensitivity, then we can't match serialized properties to constructor parameters on deserialization.
            Point_ExtendedPropNames point = JsonSerializer.Deserialize<Point_ExtendedPropNames>(json, options1);
            Assert.Equal(0, point.XValue);
            Assert.Equal(0, point.YValue);

            var options2 = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SimpleSnakeCasePolicy(),
                PropertyNameCaseInsensitive = true,
            };

            point = JsonSerializer.Deserialize<Point_ExtendedPropNames>(json, options2);
            Assert.Equal(1, point.XValue);
            Assert.Equal(2, point.YValue);
        }

        [Fact]
        public static void ArgumentDeserialization_UseNamingPolicy_InvalidPolicyFails()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new NullNamingPolicy()
            };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<Point_ExtendedPropNames>("{}", options));
        }

        [Theory]
        [MemberData(nameof(ComplexJsonAsLasArgTests))]
        public static void ComplexJson_As_LastCtorArg(Type type, string json)
        {
            object obj = JsonSerializer.Deserialize(json, type);
            ((ITestClass)obj).Verify();
        }

        private static IEnumerable<object[]> ComplexJsonAsLasArgTests()
        {
            yield return new object[] { typeof(Point_With_Array), Point_With_Array.s_json };
            yield return new object[] { typeof(Point_With_Dictionary), Point_With_Dictionary.s_json };
            yield return new object[] { typeof(Point_With_Object), Point_With_Object.s_json };
        }
    }
}

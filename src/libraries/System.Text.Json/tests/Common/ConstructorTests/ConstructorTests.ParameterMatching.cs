// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests : SerializerTests
    {
        public ConstructorTests(JsonSerializerWrapperForString stringSerializer, JsonSerializerWrapperForStream streamSerializer)
            : base(stringSerializer, streamSerializer)
        {
        }

        [Fact]
        public async Task ReturnNullForNullObjects()
        {
            Assert.Null(await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>("null"));
            Assert.Null(await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>("null"));
        }

        [Fact]
        public Task JsonExceptionWhenAssigningNullToStruct()
        {
            return Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D_With_ExtData>("null"));
        }

        [Fact]
        public async Task MatchJsonPropertyToConstructorParameters()
        {
            Point_2D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""Y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public async Task UseDefaultValues_When_NoJsonMatch()
        {
            // Using CLR value when `ParameterInfo.DefaultValue` is not set.
            Point_2D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""x"":1,""y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""y"":2,""x"":1}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            // Using `ParameterInfo.DefaultValue` when set; using CLR value as fallback.
            Point_3D point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""X"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""X"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Z"":3,""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""x"":1,""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);
        }

        [Fact]
        public async Task CaseInsensitivityWorks()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            Point_2D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""x"":1,""y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""y"":2,""x"":1}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""x"":1,""Y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""y"":2,""X"":1}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public async Task VaryingOrderingOfJson()
        {
            Point_3D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""X"":1,""Y"":2,""Z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""X"":1,""Z"":3,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Y"":2,""Z"":3,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Y"":2,""X"":1,""Z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Z"":3,""Y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(@"{""Z"":3,""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public async Task AsListElement()
        {
            List<Point_3D> list = await JsonSerializerWrapperForString.DeserializeWrapper<List<Point_3D>>(@"[{""Y"":2,""Z"":3,""X"":1},{""Z"":10,""Y"":30,""X"":20}]");
            Assert.Equal(1, list[0].X);
            Assert.Equal(2, list[0].Y);
            Assert.Equal(3, list[0].Z);
            Assert.Equal(20, list[1].X);
            Assert.Equal(30, list[1].Y);
            Assert.Equal(10, list[1].Z);
        }

        [Fact]
        public async Task AsDictionaryValue()
        {
            Dictionary<string, Point_3D> dict = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, Point_3D>>(@"{""0"":{""Y"":2,""Z"":3,""X"":1},""1"":{""Z"":10,""Y"":30,""X"":20}}");
            Assert.Equal(1, dict["0"].X);
            Assert.Equal(2, dict["0"].Y);
            Assert.Equal(3, dict["0"].Z);
            Assert.Equal(20, dict["1"].X);
            Assert.Equal(30, dict["1"].Y);
            Assert.Equal(10, dict["1"].Z);
        }

        [Fact]
        public async Task AsProperty_Of_ObjectWithParameterlessCtor()
        {
            WrapperForPoint_3D obj = await JsonSerializerWrapperForString.DeserializeWrapper<WrapperForPoint_3D>(@"{""Point_3D"":{""Y"":2,""Z"":3,""X"":1}}");
            Point_3D point = obj.Point_3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public async Task AsProperty_Of_ObjectWithParameterizedCtor()
        {
            ClassWrapperForPoint_3D obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapperForPoint_3D>(@"{""Point3D"":{""Y"":2,""Z"":3,""X"":1}}");
            Point_3D point = obj.Point3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public async Task At_Symbol_As_ParameterNamePrefix()
        {
            ClassWrapper_For_Int_String obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapper_For_Int_String>(@"{""Int"":1,""String"":""1""}");
            Assert.Equal(1, obj.Int);
            Assert.Equal("1", obj.String);
        }

        [Fact]
        public async Task At_Symbol_As_ParameterNamePrefix_UseDefaultValues()
        {
            ClassWrapper_For_Int_String obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapper_For_Int_String>(@"{""@Int"":1,""@String"":""1""}");
            Assert.Equal(0, obj.Int);
            Assert.Null(obj.String);
        }

        [Fact]
        public async Task PassDefaultValueToComplexStruct()
        {
            ClassWrapperForPoint_3D obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapperForPoint_3D>(@"{}");
            Assert.True(obj.Point3D == default);

            ClassWrapper_For_Int_Point_3D_String obj1 = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapper_For_Int_Point_3D_String>(@"{}");
            Assert.Equal(0, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(0, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        [Fact]
        public async Task Null_AsArgument_To_ParameterThat_CanBeNull()
        {
            ClassWrapper_For_Int_Point_3D_String obj1 = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":1,""MyPoint3DStruct"":{},""MyString"":null}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(50, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        [Fact]
        public async Task Null_AsArgument_To_ParameterThat_CanNotBeNull()
        {
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":null,""MyString"":""1""}"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWrapper_For_Int_Point_3D_String>(@"{""MyPoint3DStruct"":null,""MyString"":""1""}"));
        }

        [Fact]
        public async Task OtherPropertiesAreSet()
        {
            var personClass = await JsonSerializerWrapperForString.DeserializeWrapper<Person_Class>(Person_Class.s_json);
            personClass.Verify();

            var personStruct = await JsonSerializerWrapperForString.DeserializeWrapper<Person_Struct>(Person_Struct.s_json);
            personStruct.Verify();
        }

        [Fact]
        public async Task ExtraProperties_AreIgnored()
        {
            Point_2D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{ ""x"":1,""y"":2,""b"":3}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
        }

        [Fact]
        public async Task ExtraProperties_GoInExtensionData_IfPresent()
        {
            Point_2D_With_ExtData point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D_With_ExtData>(@"{""X"":1,""y"":2,""b"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.ExtensionData["y"].GetInt32());
            Assert.Equal(3, point.ExtensionData["b"].GetInt32());
        }

        [Fact]
        public async Task PropertiesNotSet_WhenJSON_MapsToConstructorParameters()
        {
            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<Point_CtorsIgnoreJson>(@"{""X"":1,""Y"":2}");
            Assert.Equal(40, obj.X); // Would be 1 if property were set directly after object construction.
            Assert.Equal(60, obj.Y); // Would be 2 if property were set directly after object construction.
        }

        [Fact]
        public async Task IgnoreNullValues_DontSetNull_ToConstructorArguments_ThatCantBeNull()
        {
            // Throw JsonException when null applied to types that can't be null. Behavior should align with properties deserialized with setters.

            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}"));

            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""Point3DStruct"":null}"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""Point3DStruct"":null}"));

            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""Int"":null}"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""Int"":null}"));

            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""ImmutableArray"":null}"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""ImmutableArray"":null}"));

            // Throw even when IgnoreNullValues is true for symmetry with property deserialization,
            // until https://github.com/dotnet/runtime/issues/30795 is addressed.

            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));

            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""Point3DStruct"":null}", options));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));

            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""Int"":null}", options));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));

            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester>(@"{""ImmutableArray"":null}", options));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<NullArgTester_Mutable>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}", options));
        }

        [Fact]
        public async Task NumerousSimpleAndComplexParameters()
        {
            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<ObjWCtorMixedParams>(ObjWCtorMixedParams.s_json);
            obj.Verify();
        }

        [Fact]
        public async Task ClassWithPrimitives_Parameterless()
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

            string json = await JsonSerializerWrapperForString.SerializeWrapper(point);

            var deserialized = await JsonSerializerWrapperForString.DeserializeWrapper<Parameterless_ClassWithPrimitives>(json);
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
        public async Task ClassWithPrimitives()
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

            string json = await JsonSerializerWrapperForString.SerializeWrapper(point);

            var deserialized = await JsonSerializerWrapperForString.DeserializeWrapper<Parameterized_ClassWithPrimitives_3Args>(json);
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
        public async Task ClassWithPrimitivesPerf()
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

            string json = await JsonSerializerWrapperForString.SerializeWrapper(point);

            await JsonSerializerWrapperForString.DeserializeWrapper<Parameterized_ClassWithPrimitives_3Args>(json);
            await JsonSerializerWrapperForString.DeserializeWrapper<Parameterized_ClassWithPrimitives_3Args>(json);
        }

        [Fact]
        public async Task TupleDeserializationWorks()
        {
            var dont_trim_ctor = typeof(Tuple<,>).GetConstructors();

            var tuple = await JsonSerializerWrapperForString.DeserializeWrapper<Tuple<string, double>>(@"{""Item1"":""New York"",""Item2"":32.68}");
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleWrapper = await JsonSerializerWrapperForString.DeserializeWrapper<TupleWrapper>(@"{""Tuple"":{""Item1"":""New York"",""Item2"":32.68}}");
            tuple = tupleWrapper.Tuple;
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleList = await JsonSerializerWrapperForString.DeserializeWrapper<List<Tuple<string, double>>>(@"[{""Item1"":""New York"",""Item2"":32.68}]");
            tuple = tupleList[0];
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(Tuple<,,,,,,,>))]
        public async Task TupleDeserialization_MoreThanSevenItems()
        {
            var dont_trim_ctor = typeof(Tuple<,,,,,,>).GetConstructors();
            dont_trim_ctor = typeof(Tuple<,,,,,,,>).GetConstructors();

            // Seven is okay
            string json = await JsonSerializerWrapperForString.SerializeWrapper(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<Tuple<int, int, int, int, int, int, int>>(json);
            Assert.Equal(json, await JsonSerializerWrapperForString.SerializeWrapper(obj));

#if !BUILDING_SOURCE_GENERATOR_TESTS // Source-gen implementations aren't binding with tuples with more than 7 generic args
            // More than seven arguments needs special casing and can be revisted.
            // Newtonsoft.Json fails in the same way.
            json = await JsonSerializerWrapperForString.SerializeWrapper(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Tuple<int, int, int, int, int, int, int, int>>(json));

            // Invalid JSON representing a tuple with more than seven items yields an ArgumentException from the constructor.
            // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
            // We pass the number 8, not a new Tuple<int>(8).
            // Fixing this needs special casing. Newtonsoft behaves the same way.
            string invalidJson = @"{""Item1"":1,""Item2"":2,""Item3"":3,""Item4"":4,""Item5"":5,""Item6"":6,""Item7"":7,""Item1"":8}";
            await Assert.ThrowsAsync<ArgumentException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Tuple<int, int, int, int, int, int, int, int>>(invalidJson));
#endif
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(Tuple<,,,,,,,>))]
        public async Task TupleDeserialization_DefaultValuesUsed_WhenJsonMissing()
        {
            // Seven items; only three provided.
            string input = @"{""Item2"":""2"",""Item3"":3,""Item6"":6}";
            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<Tuple<int, string, int, string, string, int, Point_3D_Struct>>(input);

            string serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Contains(@"""Item1"":0", serialized);
            Assert.Contains(@"""Item2"":""2""", serialized);
            Assert.Contains(@"""Item3"":3", serialized);
            Assert.Contains(@"""Item4"":null", serialized);
            Assert.Contains(@"""Item5"":null", serialized);
            Assert.Contains(@"""Item6"":6", serialized);
            Assert.Contains(@"""Item7"":{", serialized);

            serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj.Item7);
            Assert.Contains(@"""X"":0", serialized);
            Assert.Contains(@"""Y"":0", serialized);
            Assert.Contains(@"""Z"":0", serialized);

            // Although no Json is provided for the 8th item, ArgumentException is still thrown as we use default(int) as the argument/
            // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
            // We pass the number 8, not a new Tuple<int>(default(int)).
            // Fixing this needs special casing. Newtonsoft behaves the same way.
            await Assert.ThrowsAsync<ArgumentException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Tuple<int, string, int, string, string, int, Point_3D_Struct, int>>(input));
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs full SimpleTestClass support.")]
#endif
        public async Task TupleDeserializationWorks_ClassWithParameterizedCtor()
        {
            string classJson = ObjWCtorMixedParams.s_json;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(@$"""Item{i + 1}"":{classJson},");
            }
            sb.Append(@$"""Item7"":{classJson}");
            sb.Append("}");

            string complexTupleJson = sb.ToString();

            var complexTuple = await JsonSerializerWrapperForString.DeserializeWrapper<Tuple<
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams>>(complexTupleJson);

            complexTuple.Item1.Verify();
            complexTuple.Item2.Verify();
            complexTuple.Item3.Verify();
            complexTuple.Item4.Verify();
            complexTuple.Item5.Verify();
            complexTuple.Item6.Verify();
            complexTuple.Item7.Verify();
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs full SimpleTestClass support.")]
#endif
        public async Task TupleDeserializationWorks_ClassWithParameterlessCtor()
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

            var dont_trim_ctor = typeof(Tuple<,,,,,,>).GetConstructors();

            var complexTuple = await JsonSerializerWrapperForString.DeserializeWrapper<Tuple<
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
        public async Task NoConstructorHandlingWhenObjectHasConverter()
        {
            // Baseline without converter
            string serialized = await JsonSerializerWrapperForString.SerializeWrapper(new Point_3D(10, 6));

            Point_3D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(serialized);
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = await JsonSerializerWrapperForString.SerializeWrapper(new[] { new Point_3D(10, 6) });

            point = (await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D[]>(serialized))[0];
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = await JsonSerializerWrapperForString.SerializeWrapper(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = (await JsonSerializerWrapperForString.DeserializeWrapper<WrapperForPoint_3D>(serialized)).Point_3D;
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            // Converters for objects with parameterized ctors are honored

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForPoint3D());

            serialized = await JsonSerializerWrapperForString.SerializeWrapper(new Point_3D(10, 6));

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D>(serialized, options);
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = await JsonSerializerWrapperForString.SerializeWrapper(new[] { new Point_3D(10, 6) });

            point = (await JsonSerializerWrapperForString.DeserializeWrapper<Point_3D[]>(serialized, options))[0];
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = await JsonSerializerWrapperForString.SerializeWrapper(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = (await JsonSerializerWrapperForString.DeserializeWrapper<WrapperForPoint_3D>(serialized, options)).Point_3D;
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);
        }

        [Fact]
        public async Task ConstructorHandlingHonorsCustomConverters()
        {
            // Baseline, use internal converters for primitives
            Point_2D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""X"":2,""Y"":3}");
            Assert.Equal(2, point.X);
            Assert.Equal(3, point.Y);

            // Honor custom converters
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForInt32());

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""X"":2,""Y"":3}", options);
            Assert.Equal(25, point.X);
            Assert.Equal(25, point.X);
        }

        [Fact]
        public async Task CanDeserialize_ObjectWith_Ctor_With_64_Params()
        {
            async Task RunTestAsync<T>()
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

                object obj = await JsonSerializerWrapperForString.DeserializeWrapper<T>(input);
                for (int i = 0; i < 64; i++)
                {
                    Assert.Equal(i, (int)typeof(T).GetProperty($"Int{i}").GetValue(obj));
                }
            }

            await RunTestAsync<Struct_With_Ctor_With_64_Params>();
            await RunTestAsync<Class_With_Ctor_With_64_Params>();
        }

        [Fact]
        public async Task Cannot_Deserialize_ObjectWith_Ctor_With_65_Params()
        {
            async Task RunTestAsync<T>()
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

                NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializerWrapperForString.DeserializeWrapper<T>(input));
                Assert.Contains(type.ToString(), ex.ToString());

                ex = await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializerWrapperForString.DeserializeWrapper<T>("{}"));
                Assert.Contains(type.ToString(), ex.ToString());
            }

            await RunTestAsync<Class_With_Ctor_With_65_Params>();
            await RunTestAsync<Struct_With_Ctor_With_65_Params>();
        }

        [Fact]
        public async Task Deserialize_ObjectWith_Ctor_With_65_Params_IfNull()
        {
            Assert.Null(await JsonSerializerWrapperForString.DeserializeWrapper<Class_With_Ctor_With_65_Params>("null"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Struct_With_Ctor_With_65_Params>("null"));
        }

        [Fact]
        public async Task Escaped_ParameterNames_Work()
        {
            Point_2D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""\u0058"":1,""\u0059"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public async Task LastParameterWins()
        {
            Point_2D point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>(@"{""X"":1,""Y"":2,""X"":4}");
            Assert.Equal(4, point.X); // Not 1.
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public async Task LastParameterWins_DoesNotGoToExtensionData()
        {
            string json = @"{
                ""FirstName"":""Jet"",
                ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
                ""EmailAddress"":""jetdoe@outlook.com"",
                ""Id"":""0b3aa420-2e98-47f7-8a49-fea233b89416"",
                ""LastName"":""Doe"",
                ""Id"":""63cf821d-fd47-4782-8345-576d9228a534""
                }";

            Parameterized_Person person = await JsonSerializerWrapperForString.DeserializeWrapper<Parameterized_Person>(json);
            Assert.Equal("Jet", person.FirstName);
            Assert.Equal("Doe", person.LastName);
            Assert.Equal("63cf821d-fd47-4782-8345-576d9228a534", person.Id.ToString());
            Assert.Equal("jetdoe@outlook.com", person.ExtensionData["EmailAddress"].GetString());
            Assert.False(person.ExtensionData.ContainsKey("Id"));
        }

        [Fact]
        public async Task BitVector32_UsesStructDefaultCtor_MultipleParameterizedCtor()
        {
            string serialized = await JsonSerializerWrapperForString.SerializeWrapper(new BitVector32(1));
            Assert.Equal(0, (await JsonSerializerWrapperForString.DeserializeWrapper<BitVector32>(serialized)).Data);
        }

        [Fact]
        public async Task HonorExtensionDataGeneric()
        {
            var obj1 = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt>(@"{""key"": ""value""}");
            Assert.Equal("value", obj1.ExtensionData["key"].GetString());

            var obj2 = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt>(@"{""key"": ""value""}");
            Assert.Equal("value", ((JsonElement)obj2.ExtensionData["key"]).GetString());

            var obj3 = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt>(@"{""key"": ""value""}");
            Assert.Equal("value", obj3.ExtensionData["key"].GetString());

            var obj4 = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt>(@"{""key"": ""value""}");
            Assert.Equal("value", ((JsonElement)obj4.ExtensionData["key"]).GetString());
        }

        [Fact]
        public async Task ArgumentDeserialization_Honors_JsonInclude()
        {
            Point_MembersHave_JsonInclude point = new Point_MembersHave_JsonInclude(1, 2,3);

            string json = await JsonSerializerWrapperForString.SerializeWrapper(point);
            Assert.Contains(@"""X"":1", json);
            Assert.Contains(@"""Y"":2", json);
            //We should add another test for non-public members
            //when https://github.com/dotnet/runtime/issues/31511 is implemented
            Assert.Contains(@"""Z"":3", json);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_MembersHave_JsonInclude>(json);
            point.Verify();
        }

        [Fact]
        public async Task ArgumentDeserialization_Honors_JsonNumberHandling()
        {
            ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes>(ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes.s_json);
            obj.Verify();

            string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Contains(@"""A"":1", json);
            Assert.Contains(@"""B"":""NaN""", json);
            Assert.Contains(@"""C"":2", json);
            Assert.Contains(@"""D"":""3""", json);
            Assert.Contains(@"""E"":""4""", json);
        }

        [Fact]
        public async Task ArgumentDeserialization_Honors_JsonPropertyName()
        {
            Point_MembersHave_JsonPropertyName point = new Point_MembersHave_JsonPropertyName(1, 2);

            string json = await JsonSerializerWrapperForString.SerializeWrapper(point);
            Assert.Contains(@"""XValue"":1", json);
            Assert.Contains(@"""YValue"":2", json);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_MembersHave_JsonPropertyName>(json);
            point.Verify();
        }

        [Fact]
        public async Task ArgumentDeserialization_Honors_JsonPropertyName_CaseInsensitiveWorks()
        {
            string json = @"{""XVALUE"":1,""yvalue"":2}";

            // Without case insensitivity, there's no match.
            Point_MembersHave_JsonPropertyName point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_MembersHave_JsonPropertyName>(json);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_MembersHave_JsonPropertyName>(json, options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public async Task ArgumentDeserialization_Honors_ConverterOnProperty()
        {
            var point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_MembersHave_JsonConverter>(Point_MembersHave_JsonConverter.s_json);
            point.Verify();
        }

        [Fact]
        public async Task ArgumentDeserialization_Honors_JsonIgnore()
        {
            var point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_MembersHave_JsonIgnore>(Point_MembersHave_JsonIgnore.s_json);
            point.Verify();
        }

        [Fact]
        public async Task ArgumentDeserialization_UseNamingPolicy_ToMatch()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LowerCaseNamingPolicy()
            };

            string json = await JsonSerializerWrapperForString.SerializeWrapper(new Point_ExtendedPropNames(1, 2), options);

            // If we don't use naming policy, then we can't match serialized properties to constructor parameters on deserialization.
            var point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_ExtendedPropNames>(json);
            Assert.Equal(0, point.XValue);
            Assert.Equal(0, point.YValue);

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_ExtendedPropNames>(json, options);
            Assert.Equal(1, point.XValue);
            Assert.Equal(2, point.YValue);
        }

        [Fact]
        public async Task ArgumentDeserialization_UseNamingPolicy_ToMatch_CaseInsensitiveWorks()
        {
            var options1 = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SimpleSnakeCasePolicy()
            };

            string json = @"{""x_VaLUE"":1,""Y_vALue"":2}";

            // If we don't use case sensitivity, then we can't match serialized properties to constructor parameters on deserialization.
            Point_ExtendedPropNames point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_ExtendedPropNames>(json, options1);
            Assert.Equal(0, point.XValue);
            Assert.Equal(0, point.YValue);

            var options2 = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SimpleSnakeCasePolicy(),
                PropertyNameCaseInsensitive = true,
            };

            point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_ExtendedPropNames>(json, options2);
            Assert.Equal(1, point.XValue);
            Assert.Equal(2, point.YValue);
        }

        [Fact]
        public async Task ArgumentDeserialization_UseNamingPolicy_InvalidPolicyFails()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new NullNamingPolicy()
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_ExtendedPropNames>("{}", options));
        }

        [Fact]
        public async Task ComplexJson_As_LastCtorArg()
        {
            Point_With_Array obj1 = await JsonSerializerWrapperForString.DeserializeWrapper<Point_With_Array>(Point_With_Array.s_json);
            ((ITestClass)obj1).Verify();

            Point_With_Dictionary obj2 = await JsonSerializerWrapperForString.DeserializeWrapper<Point_With_Dictionary>(Point_With_Dictionary.s_json);
            ((ITestClass)obj2).Verify();

            Point_With_Object obj3 = await JsonSerializerWrapperForString.DeserializeWrapper<Point_With_Object>(Point_With_Object.s_json);
            ((ITestClass)obj3).Verify();
        }

        [Fact]
        public async Task NumerousPropertiesWork()
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

            var point = await JsonSerializerWrapperForString.DeserializeWrapper<Point_With_Property>(json);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(66, point.Z);
        }

        [Fact]
        public async Task ArgumentStateNotOverwritten()
        {
            ClassWithNestedClass obj = new ClassWithNestedClass(myClass: null, myPoint: default);
            ClassWithNestedClass obj1 = new ClassWithNestedClass(myClass: obj, myPoint: new Point_2D_Struct_WithAttribute(1, 2));
            ClassWithNestedClass obj2 = new ClassWithNestedClass(myClass: obj1, myPoint: new Point_2D_Struct_WithAttribute(3, 4));

            string json = await JsonSerializerWrapperForString.SerializeWrapper(obj2);

            obj2 = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithNestedClass>(json);
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
        public async Task FourArgsWork()
        {
            string json = await JsonSerializerWrapperForString.SerializeWrapper(new StructWithFourArgs(1, 2, 3, 4));

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<StructWithFourArgs>(json);
            Assert.Equal(1, obj.W);
            Assert.Equal(2, obj.X);
            Assert.Equal(3, obj.Y);
            Assert.Equal(4, obj.Z);
        }

        [Fact]
        public async Task InvalidJsonFails()
        {
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>("{1"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>("{x"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>("{{"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D>("{true"));

            // Also test deserialization of objects with parameterless ctors
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D_Struct>("{1"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D_Struct>("{x"));
            await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Point_2D_Struct>("{true"));
        }

        [Fact]
        public void AnonymousObject()
        {
            var obj = new { Prop = 5 };
            Type objType = obj.GetType();

            // 'Prop' property binds with a ctor arg called 'Prop'.

            object newObj = JsonSerializer.Deserialize("{}", objType);
            Assert.Equal(0, objType.GetProperty("Prop").GetValue(newObj));

            newObj = JsonSerializer.Deserialize(@"{""Prop"":5}", objType);
            Assert.Equal(5, objType.GetProperty("Prop").GetValue(newObj));
        }

        [Fact]
        public void AnonymousObject_NamingPolicy()
        {
            const string Json = @"{""prop"":5}";

            var obj = new { Prop = 5 };
            Type objType = obj.GetType();

            // 'Prop' property binds with a ctor arg called 'Prop'.

            object newObj = JsonSerializer.Deserialize(Json, objType);
            // Verify no match if no naming policy
            Assert.Equal(0, objType.GetProperty("Prop").GetValue(newObj));

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            newObj = JsonSerializer.Deserialize(Json, objType, options);
            // Verify match with naming policy
            Assert.Equal(5, objType.GetProperty("Prop").GetValue(newObj));
        }

        public record MyRecord(int Prop);

        [Fact]
        public async Task Record()
        {
            // 'Prop' property binds with a ctor arg called 'Prop'.
            MyRecord obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyRecord>("{}");
            Assert.Equal(0, obj.Prop);

            obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyRecord>(@"{""Prop"":5}");
            Assert.Equal(5, obj.Prop);
        }

        public record AgeRecord(int age)
        {
            public string Age { get; set; } = age.ToString();
        }

        [Fact]
        public async Task RecordWithSamePropertyNameDifferentTypes()
        {
            AgeRecord obj = await JsonSerializerWrapperForString.DeserializeWrapper<AgeRecord>(@"{""age"":1}");
            Assert.Equal(1, obj.age);
        }

        public record MyRecordWithUnboundCtorProperty(int IntProp1, int IntProp2)
        {
            public string StringProp { get; set; }
        }

        [Fact]
        public async Task RecordWithAdditionalProperty()
        {
            MyRecordWithUnboundCtorProperty obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyRecordWithUnboundCtorProperty>(
                @"{""IntProp1"":1,""IntProp2"":2,""StringProp"":""hello""}");

            Assert.Equal(1, obj.IntProp1);
            Assert.Equal(2, obj.IntProp2);

            // StringProp is not bound to any constructor property.
            Assert.Equal("hello", obj.StringProp);
        }

        [Fact]
        public async Task Record_NamingPolicy()
        {
            const string Json = @"{""prop"":5}";

            // 'Prop' property binds with a ctor arg called 'Prop'.

            // Verify no match if no naming policy
            MyRecord obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyRecord>(Json);
            Assert.Equal(0, obj.Prop);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Verify match with naming policy
            obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyRecord>(Json, options);
            Assert.Equal(5, obj.Prop);
        }

        public class AgePoco
        {
            public AgePoco(int age)
            {
                this.age = age;
            }

            public int age { get; set; }
            public string Age { get; set; }
        }

        [Fact]
        public async Task PocoWithSamePropertyNameDifferentTypes()
        {
            AgePoco obj = await JsonSerializerWrapperForString.DeserializeWrapper<AgePoco>(@"{""age"":1}");
            Assert.Equal(1, obj.age);
        }

        [Theory]
        [InlineData(typeof(TypeWithGuid))]
        [InlineData(typeof(TypeWithNullableGuid))]
        public async Task DefaultForValueTypeCtorParam(Type type)
        {
            string json = @"{""MyGuid"":""edc421bf-782a-4a95-ad67-3d73b5d7db6f""}";
            object obj = await JsonSerializerWrapperForString.DeserializeWrapper(json, type);
            Assert.Equal(json, await JsonSerializerWrapperForString.SerializeWrapper(obj));

            json = @"{}";
            obj = await JsonSerializerWrapperForString.DeserializeWrapper(json, type);

            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
            Assert.Equal(json, await JsonSerializerWrapperForString.SerializeWrapper(obj, options));
        }

        public class TypeWithGuid
        {
            public Guid MyGuid { get; }

            public TypeWithGuid(Guid myGuid = default) => MyGuid = myGuid;
        }

        public struct TypeWithNullableGuid
        {
            public Guid? MyGuid { get; }

            [JsonConstructor]
            public TypeWithNullableGuid(Guid? myGuid = default) => MyGuid = myGuid;
        }

        [Fact]
        public async Task DefaultForReferenceTypeCtorParam()
        {
            string json = @"{""MyUri"":""http://hello""}";
            object obj = await JsonSerializerWrapperForString.DeserializeWrapper(json, typeof(TypeWithUri));
            Assert.Equal(json, await JsonSerializerWrapperForString.SerializeWrapper(obj));

            json = @"{}";
            obj = await JsonSerializerWrapperForString.DeserializeWrapper(json, typeof(TypeWithUri));

            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
            Assert.Equal(json, await JsonSerializerWrapperForString.SerializeWrapper(obj, options));
        }

        public class TypeWithUri
        {
            public Uri MyUri { get; }

            public TypeWithUri(Uri myUri = default) => MyUri = myUri;
        }

        [Fact]
        public async Task SmallObject_ClrDefaultParamValueUsed_WhenMatchingPropIgnored()
        {
            string json = @"{""Prop"":20}";
            var obj1 = await JsonSerializerWrapperForString.DeserializeWrapper<SmallType_IgnoredProp_Bind_ParamWithDefaultValue>(json);
            Assert.Equal(0, obj1.Prop);

            var obj2 = await JsonSerializerWrapperForString.DeserializeWrapper<SmallType_IgnoredProp_Bind_Param>(json);
            Assert.Equal(0, obj2.Prop);
        }

        public class SmallType_IgnoredProp_Bind_ParamWithDefaultValue
        {
            [JsonIgnore]
            public int Prop { get; set; }

            public SmallType_IgnoredProp_Bind_ParamWithDefaultValue(int prop = 5)
                => Prop = prop;
        }

        public class SmallType_IgnoredProp_Bind_Param
        {
            [JsonIgnore]
            public int Prop { get; set; }

            public SmallType_IgnoredProp_Bind_Param(int prop)
                => Prop = prop;
        }

        [Fact]
        public async Task LargeObject_ClrDefaultParamValueUsed_WhenMatchingPropIgnored()
        {
            string json = @"{""Prop"":20}";
            var obj1 = await JsonSerializerWrapperForString.DeserializeWrapper<LargeType_IgnoredProp_Bind_ParamWithDefaultValue>(json);
            Assert.Equal(0, obj1.Prop);

            var obj2 = await JsonSerializerWrapperForString.DeserializeWrapper<LargeType_IgnoredProp_Bind_Param>(json);
            Assert.Equal(0, obj2.Prop);
        }

        public class LargeType_IgnoredProp_Bind_ParamWithDefaultValue
        {
            public int W { get; set; }

            public int X { get; set; }

            public int Y { get; set; }

            public int Z { get; set; }

            [JsonIgnore]
            public int Prop { get; set; }

            public LargeType_IgnoredProp_Bind_ParamWithDefaultValue(int w, int x, int y, int z, int prop = 5)
                => Prop = prop;
        }

        public class LargeType_IgnoredProp_Bind_Param
        {
            public int W { get; set; }

            public int X { get; set; }

            public int Y { get; set; }

            public int Z { get; set; }

            [JsonIgnore]
            public int Prop { get; set; }

            public LargeType_IgnoredProp_Bind_Param(int w, int x, int y, int z, int prop)
                => Prop = prop;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34779")]
        public async Task BindingBetweenRefProps()
        {
            string json = @"{""NameRef"":""John""}";
            await JsonSerializerWrapperForString.DeserializeWrapper<TypeWith_RefStringProp_ParamCtor>(json);
        }

        public class TypeWith_RefStringProp_ParamCtor
        {
            private string _name;

            public ref string NameRef => ref _name;

            public TypeWith_RefStringProp_ParamCtor(ref string nameRef) => _name = nameRef;
        }

        [Fact]
        public async Task BindToIgnoredPropOfSameType()
        {
            string json = @"{""Prop"":{}}";
            Assert.NotNull(await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithIgnoredSameType>(json));
        }

        public class ClassWithIgnoredSameType
        {
            [JsonIgnore]
            public ClassWithIgnoredSameType Prop { get; }

            public ClassWithIgnoredSameType(ClassWithIgnoredSameType prop) { }
        }

        [Fact]
        public void StructWithPropertyInit_DeseralizeEmptyObject()
        {
            string json = @"{}";
            var obj = JsonSerializer.Deserialize<StructWithPropertyInit>(json);
            Assert.Equal(42, obj.A);
            Assert.Equal(0, obj.B);
        }

        [Fact]
        public void StructWithPropertyInit_OverrideInitedProperty()
        {
            string json = @"{""A"":43}";
            var obj = JsonSerializer.Deserialize<StructWithPropertyInit>(json);
            Assert.Equal(43, obj.A);
            Assert.Equal(0, obj.B);
            
            json = @"{""A"":0,""B"":44}";
            obj = JsonSerializer.Deserialize<StructWithPropertyInit>(json);
            Assert.Equal(0, obj.A);
            Assert.Equal(44, obj.B);
            
            json = @"{""B"":45}";
            obj = JsonSerializer.Deserialize<StructWithPropertyInit>(json);
            Assert.Equal(42, obj.A); // JSON doesn't set A property so it's expected to be 42
            Assert.Equal(45, obj.B);
        }

        public struct StructWithPropertyInit
        {
            public long A { get; set; } = 42;
            public long B { get; set; }
        }

        [Fact]
        public void StructWithFieldInit_DeseralizeEmptyObject()
        {
            string json = @"{}";
            var obj = JsonSerializer.Deserialize<StructWithFieldInit>(json);
            Assert.Equal(0, obj.A);
            Assert.Equal(42, obj.B);
        }

        public struct StructWithFieldInit
        {
            public long A;
            public long B = 42;
        }

        [Fact]
        public void StructWithExplicitParameterlessCtor_DeseralizeEmptyObject()
        {
            string json = @"{}";
            var obj = JsonSerializer.Deserialize<StructWithExplicitParameterlessCtor>(json);
            Assert.Equal(42, obj.A);
        }

        public struct StructWithExplicitParameterlessCtor
        {
            public long A;
            public StructWithExplicitParameterlessCtor() => A = 42;
        }

        public async Task TestClassWithDefaultCtorParams()
        {
            ClassWithDefaultCtorParams obj = new ClassWithDefaultCtorParams(
                new Point_2D_Struct_WithAttribute(1, 2),
                DayOfWeek.Sunday,
                (DayOfWeek)(-2),
                (DayOfWeek)19,
                BindingFlags.CreateInstance | BindingFlags.FlattenHierarchy,
                SampleEnumUInt32.MinZero,
                "Hello world!",
                null,
                "xzy",
                'c',
                ' ',
                23,
                4,
                -40,
                double.Epsilon,
                23,
                4,
                -40,
                float.MinValue,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10);

            string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithDefaultCtorParams>(json);
            JsonTestHelper.AssertJsonEqual(json, await JsonSerializerWrapperForString.SerializeWrapper(obj));
        }

        public class ClassWithDefaultCtorParams
        {
            public Point_2D_Struct_WithAttribute Struct { get; }
            public DayOfWeek Enum1 { get; }
            public DayOfWeek Enum2 { get; }
            public DayOfWeek Enum3 { get; }
            public BindingFlags Enum4 { get; }
            public SampleEnumUInt32 Enum5 { get; }
            public string Str1 { get; }
            public string Str2 { get; }
            public string Str3 { get; }
            public char Char1 { get; }
            public char Char2 { get; }
            public double Double1 { get; }
            public double Double2 { get; }
            public double Double3 { get; }
            public double Double4 { get; }
            public double Double5 { get; }
            public float Float1 { get; }
            public float Float2 { get; }
            public float Float3 { get; }
            public float Float4 { get; }
            public float Float5 { get; }
            public byte Byte { get; }
            public decimal Decimal1 { get; }
            public decimal Decimal2 { get; }
            public short Short { get; }
            public sbyte Sbyte { get; }
            public int Int { get; }
            public long Long { get; }
            public ushort UShort { get; }
            public uint UInt { get; }
            public ulong ULong { get; }

            public ClassWithDefaultCtorParams(
                Point_2D_Struct_WithAttribute @struct = default,
                DayOfWeek enum1 = default,
                DayOfWeek enum2 = DayOfWeek.Sunday,
                DayOfWeek enum3 = DayOfWeek.Sunday | DayOfWeek.Monday,
                BindingFlags enum4 = BindingFlags.CreateInstance | BindingFlags.ExactBinding,
                SampleEnumUInt32 enum5 = SampleEnumUInt32.MinZero,
                string str1 = "abc",
                string str2 = "",
                string str3 = "\n\r⁉️\'\"\u200D\f\t\v\0\a\b\\\'\"",
                char char1 = 'a',
                char char2 = '\u200D',
                double double1 = double.NegativeInfinity,
                double double2 = double.PositiveInfinity,
                double double3 = double.NaN,
                double double4 = double.MaxValue,
                double double5 = double.Epsilon,
                float float1 = float.NegativeInfinity,
                float float2 = float.PositiveInfinity,
                float float3 = float.NaN,
                float float4 = float.MinValue,
                float float5 = float.Epsilon,
                byte @byte = byte.MinValue,
                decimal @decimal1 = decimal.MinValue,
                decimal @decimal2 = decimal.MaxValue,
                short @short = short.MinValue,
                sbyte @sbyte = sbyte.MaxValue,
                int @int = int.MinValue,
                long @long = long.MaxValue,
                ushort @ushort = ushort.MinValue,
                uint @uint = uint.MaxValue,
                ulong @ulong = ulong.MinValue)
            {
                Struct = @struct;
                Enum1 = enum1;
                Enum2 = enum2;
                Enum3 = enum3;
                Enum4 = enum4;
                Enum5 = enum5;
                Str1 = str1;
                Str2 = str2;
                Str3 = str3;
                Char1 = char1;
                Char2 = char2;
                Double1 = double1;
                Double2 = double2;
                Double3 = double3;
                Double4 = double4;
                Float1 = float1;
                Float2 = float2;
                Float3 = float3;
                Float4 = float4;
                Byte = @byte;
                Decimal1 = @decimal1;
                Decimal2 = @decimal2;
                Short = @short;
                Sbyte = @sbyte;
                Int = @int;
                Long = @long;
                UShort = @ushort;
                UInt = @uint;
                ULong = @ulong;
            }
        }
    }
}

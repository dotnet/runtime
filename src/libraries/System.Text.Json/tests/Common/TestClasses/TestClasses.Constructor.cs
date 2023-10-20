// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public interface ITestClassWithParameterizedCtor : ITestClass
    {
        void VerifyMinimal();
    }

    public class PrivateParameterlessCtor
    {
        private PrivateParameterlessCtor() { }
    }

    public class PrivateParameterizedCtor
    {
        public int X { get; }

        private PrivateParameterizedCtor(int x) { }
    }

    public class PrivateParameterizedCtor_WithAttribute
    {
        public int X { get; }

        [JsonConstructor]
        private PrivateParameterizedCtor_WithAttribute(int x) => X = x;
    }

    public class InternalParameterlessCtor
    {
        internal InternalParameterlessCtor() { }
    }

    public class InternalParameterizedCtor
    {
        public int X { get; }

        internal InternalParameterizedCtor(int x) { }
    }

    public class InternalParameterizedCtor_WithAttribute
    {
        public int X { get; }

        [JsonConstructor]
        internal InternalParameterizedCtor_WithAttribute(int x) => X = x;
    }

    public class ProtectedParameterlessCtor
    {
        protected ProtectedParameterlessCtor() { }
    }

    public class ProtectedParameterizedCtor
    {
        public int X { get; }

        protected ProtectedParameterizedCtor(int x) { }
    }

    public class ProtectedParameterizedCtor_WithAttribute
    {
        public int X { get; }

        [JsonConstructor]
        protected ProtectedParameterizedCtor_WithAttribute(int x) => X = x;
    }

    public class PrivateParameterlessCtor_WithAttribute
    {
        public int X { get; }

        [JsonConstructor]
        private PrivateParameterlessCtor_WithAttribute()
            => X = 42;
    }

    public class ProtectedParameterlessCtor_WithAttribute
    {
        public int X { get; }

        [JsonConstructor]
        protected ProtectedParameterlessCtor_WithAttribute()
            => X = 42;
    }

    public class InternalParameterlessCtor_WithAttribute
    {
        public int X { get; }

        [JsonConstructor]
        internal InternalParameterlessCtor_WithAttribute()
            => X = 42;
    }

    public class PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes
    {
        [JsonConstructor]
        private PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes() { }

        [JsonConstructor]
        internal PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes(int value) { }
    }

    public class ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes
    {
        [JsonConstructor]
        protected ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes() { }

        [JsonConstructor]
        private ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes(int value) { }
    }

    public class PublicParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes
    {
        [JsonConstructor]
        public PublicParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes() { }

        [JsonConstructor]
        private PublicParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes(int value) { }
    }

    public class PublicParameterizedCtor_PublicParameterizedCtor_WithMultipleAttributes
    {
        [JsonConstructor]
        public PublicParameterizedCtor_PublicParameterizedCtor_WithMultipleAttributes(int value) { }

        [JsonConstructor]
        public PublicParameterizedCtor_PublicParameterizedCtor_WithMultipleAttributes(float value) { }
    }

    public struct Struct_PublicParameterizedCtor_PrivateParameterizedCtor_WithMultipleAttributes
    {
        [JsonConstructor]
        public Struct_PublicParameterizedCtor_PrivateParameterizedCtor_WithMultipleAttributes(float value) { }

        [JsonConstructor]
        private Struct_PublicParameterizedCtor_PrivateParameterizedCtor_WithMultipleAttributes(int value) { }
    }

    public struct Point_2D_Struct
    {
        public int X { get; }

        public int Y { get; }

        public Point_2D_Struct(int x, int y) => (X, Y) = (x, y);
    }

    public struct Point_2D_Struct_WithAttribute
    {
        public int X { get; }

        public int Y { get; }

        [JsonConstructor]
        public Point_2D_Struct_WithAttribute(int x, int y) => (X, Y) = (x, y);
    }

    public struct Point_2D_Struct_WithMultipleAttributes
    {
        public int X { get; }

        public int Y { get; }

        [JsonConstructor]
        public Point_2D_Struct_WithMultipleAttributes(int x) => (X, Y) = (x, 0);

        [JsonConstructor]
        public Point_2D_Struct_WithMultipleAttributes(int x, int y) => (X, Y) = (x, y);
    }

    public struct Point_2D_Struct_WithMultipleAttributes_OneNonPublic
    {
        public int X { get; }

        public int Y { get; }

        [JsonConstructor]
        public Point_2D_Struct_WithMultipleAttributes_OneNonPublic(int x) => (X, Y) = (x, 0);

        [JsonConstructor]
        private Point_2D_Struct_WithMultipleAttributes_OneNonPublic(int x, int y) => (X, Y) = (x, y);
    }

    public class SinglePublicParameterizedCtor
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public SinglePublicParameterizedCtor() { }

        public SinglePublicParameterizedCtor(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class SingleParameterlessCtor_MultiplePublicParameterizedCtor
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor() { }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public struct SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct(int myInt)
        {
            MyInt = myInt;
            MyString = null;
        }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class PublicParameterizedCtor
    {
        public int MyInt { get; private set; }

        public PublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }
    }

    public struct Struct_PublicParameterizedConstructor
    {
        public int MyInt { get; }

        public Struct_PublicParameterizedConstructor(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class PrivateParameterlessConstructor_PublicParameterizedCtor
    {
        public int MyInt { get; private set; }

        private PrivateParameterlessConstructor_PublicParameterizedCtor() { }

        public PrivateParameterlessConstructor_PublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class PublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }

        [JsonConstructor]
        public PublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }
    }

    public struct Struct_PublicParameterizedConstructor_WithAttribute
    {
        public int MyInt { get; }

        [JsonConstructor]
        public Struct_PublicParameterizedConstructor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }

        private PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute() { }

        [JsonConstructor]
        public PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class MultiplePublicParameterizedCtor
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public MultiplePublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }

        public MultiplePublicParameterizedCtor(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public struct MultiplePublicParameterizedCtor_Struct
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public MultiplePublicParameterizedCtor_Struct(int myInt)
        {
            MyInt = myInt;
            MyString = null;
        }

        public MultiplePublicParameterizedCtor_Struct(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class MultiplePublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }

        public MultiplePublicParameterizedCtor_WithAttribute(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public struct MultiplePublicParameterizedCtor_WithAttribute_Struct
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public MultiplePublicParameterizedCtor_WithAttribute_Struct(int myInt)
        {
            MyInt = myInt;
            MyString = null;
        }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithAttribute_Struct(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute() { }

        [JsonConstructor]
        public ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }

        public ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class MultiplePublicParameterizedCtor_WithMultipleAttributes
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithMultipleAttributes(int myInt)
        {
            MyInt = myInt;
        }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithMultipleAttributes(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        [JsonConstructor]
        public PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes() { }

        [JsonConstructor]
        public PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class Parameterized_StackWrapper : Stack
    {
        [JsonConstructor]
        public Parameterized_StackWrapper(object[] elements)
        {
            foreach (object element in elements)
            {
                Push(element);
            }
        }
    }

    public class Parameterized_WrapperForICollection : ICollection
    {
        private List<object> _list = new List<object>();

        public Parameterized_WrapperForICollection(object[] elements)
        {
            _list.AddRange(elements);
        }

        public int Count => ((ICollection)_list).Count;

        public bool IsSynchronized => ((ICollection)_list).IsSynchronized;

        public object SyncRoot => ((ICollection)_list).SyncRoot;

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_list).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return ((ICollection)_list).GetEnumerator();
        }
    }

    public class Point_2D : ITestClass
    {
        public int X { get; }

        public int Y { get; }

        [JsonConstructor]
        public Point_2D(int x, int y) => (X, Y) = (x, y);

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
        }
    }

    public class Point_3D : ITestClass
    {
        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        [JsonConstructor]
        public Point_3D(int x, int y, int z = 50) => (X, Y, Z) = (x, y, z);

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
            Assert.Equal(3, Z);
        }
    }

    public struct Point_2D_With_ExtData : ITestClass
    {
        public int X { get; }

        public int Y { get; }

        [JsonConstructor]
        public Point_2D_With_ExtData(int x, int y)
        {
            X = x;
            Y = y;
            ExtensionData = new Dictionary<string, JsonElement>();
        }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
        }
    }

    public struct WrapperForPoint_3D
    {
        public Point_3D Point_3D { get; set; }
    }

    public class ClassWrapperForPoint_3D
    {
        public Point_3D Point3D { get; }

        public ClassWrapperForPoint_3D(Point_3D point3D)
        {
            Point3D = point3D;
        }
    }

    public class ClassWrapper_For_Int_String
    {
        public int Int { get; }

        public string String { get; }

        public ClassWrapper_For_Int_String(int @int, string @string) // Parameter names are "int" and "string"
        {
            Int = @int;
            String = @string;
        }
    }

    public class ClassWrapper_For_Int_Point_3D_String
    {
        public int MyInt { get; }

        public Point_3D_Struct MyPoint3DStruct { get; }

        public string MyString { get; }

        public ClassWrapper_For_Int_Point_3D_String(Point_3D_Struct myPoint3DStruct)
        {
            MyInt = 0;
            MyPoint3DStruct = myPoint3DStruct;
            MyString = null;
        }

        [JsonConstructor]
        public ClassWrapper_For_Int_Point_3D_String(int myInt, Point_3D_Struct myPoint3DStruct, string myString)
        {
            MyInt = myInt;
            MyPoint3DStruct = myPoint3DStruct;
            MyString = myString;
        }
    }

    public struct Point_3D_Struct
    {
        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        [JsonConstructor]
        public Point_3D_Struct(int x, int y, int z = 50) => (X, Y, Z) = (x, y, z);
    }

    public class Person_Class : ITestClassWithParameterizedCtor
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; }
        public Guid Id { get; }
        public int Age { get; }

        public Point_2D Point2D { get; set; }
        public Point_2D ReadOnlyPoint2D { get; }

        public Point_2D_With_ExtData_Class Point2DWithExtDataClass { get; set; }
        public Point_2D_With_ExtData_Class ReadOnlyPoint2DWithExtDataClass { get; }

        public Point_3D_Struct Point3DStruct { get; set; }
        public Point_3D_Struct ReadOnlyPoint3DStruct { get; }

        public Point_2D_With_ExtData Point2DWithExtData { get; set; }
        public Point_2D_With_ExtData ReadOnlyPoint2DWithExtData { get; }

        // Test that objects deserialized with parameterless still work fine as properties
        public SinglePublicParameterizedCtor SinglePublicParameterizedCtor { get; set; }
        public SinglePublicParameterizedCtor ReadOnlySinglePublicParameterizedCtor { get; }

        public Person_Class(
            string emailAddress,
            Guid id,
            int age,
            Point_2D readOnlyPoint2D,
            Point_2D_With_ExtData_Class readOnlyPoint2DWithExtDataClass,
            Point_3D_Struct readOnlyPoint3DStruct,
            Point_2D_With_ExtData readOnlyPoint2DWithExtData,
            SinglePublicParameterizedCtor readOnlySinglePublicParameterizedCtor)
        {
            EmailAddress = emailAddress;
            Id = id;
            Age = age;
            ReadOnlyPoint2D = readOnlyPoint2D;
            ReadOnlyPoint2DWithExtDataClass = readOnlyPoint2DWithExtDataClass;
            ReadOnlyPoint3DStruct = readOnlyPoint3DStruct;
            ReadOnlyPoint2DWithExtData = readOnlyPoint2DWithExtData;
            ReadOnlySinglePublicParameterizedCtor = readOnlySinglePublicParameterizedCtor;
        }

        public static string s_json => $"{{{s_partialJson1},{s_partialJson2}}}";

        public static string s_json_flipped => $"{{{s_partialJson2},{s_partialJson1}}}";

        public static string s_json_minimal => @"{""ReadOnlyPoint2D"":{""X"":1,""Y"":2}}";

        private const string s_partialJson1 =
             @"
                ""FirstName"":""John"",
                ""LastName"":""Doe"",
                ""EmailAddress"":""johndoe@live.com"",
                ""Id"":""f2c92fcc-459f-4287-90b6-a7cbd82aeb0e"",
                ""Age"":24,
                ""Point2D"":{""X"":1,""Y"":2},
                ""ReadOnlyPoint2D"":{""X"":1,""Y"":2}
            ";

        private const string s_partialJson2 =
            @"
                ""Point2DWithExtDataClass"":{""X"":1,""Y"":2,""b"":3},
                ""ReadOnlyPoint2DWithExtDataClass"":{""X"":1,""Y"":2,""b"":3},
                ""Point3DStruct"":{""X"":1,""Y"":2,""Z"":3},
                ""ReadOnlyPoint3DStruct"":{""X"":1,""Y"":2,""Z"":3},
                ""Point2DWithExtData"":{""X"":1,""Y"":2,""b"":3},
                ""ReadOnlyPoint2DWithExtData"":{""X"":1,""Y"":2,""b"":3}
            ";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal("John", FirstName);
            Assert.Equal("Doe", LastName);
            Assert.Equal("johndoe@live.com", EmailAddress);
            Assert.Equal("f2c92fcc-459f-4287-90b6-a7cbd82aeb0e", Id.ToString());
            Assert.Equal(24, Age);

            Point2D.Verify();
            ReadOnlyPoint2D.Verify();

            Assert.Equal(1, Point2DWithExtDataClass.X);
            Assert.Equal(2, Point2DWithExtDataClass.Y);
            Assert.True(Point2DWithExtDataClass.ExtensionData.ContainsKey("b"));

            Assert.Equal(1, Point3DStruct.X);
            Assert.Equal(2, Point3DStruct.Y);
            Assert.Equal(3, Point3DStruct.Z);

            Assert.Equal(1, ReadOnlyPoint3DStruct.X);
            Assert.Equal(2, ReadOnlyPoint3DStruct.Y);
            Assert.Equal(3, ReadOnlyPoint3DStruct.Z);

            Assert.Equal(1, Point2DWithExtData.X);
            Assert.Equal(2, Point2DWithExtData.Y);
            Assert.True(Point2DWithExtData.ExtensionData.ContainsKey("b"));

            Assert.Equal(1, ReadOnlyPoint2DWithExtData.X);
            Assert.Equal(2, ReadOnlyPoint2DWithExtData.Y);
            Assert.True(ReadOnlyPoint2DWithExtData.ExtensionData.ContainsKey("b"));
        }

        public void VerifyMinimal()
        {
            ReadOnlyPoint2D.Verify();
        }
    }

    public struct Person_Struct : ITestClass
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; }
        public Guid Id { get; }
        public int Age { get; }

        public Point_2D Point2D { get; set; }
        public Point_2D ReadOnlyPoint2D { get; }

        public Point_2D_With_ExtData_Class Point2DWithExtDataClass { get; set; }
        public Point_2D_With_ExtData_Class ReadOnlyPoint2DWithExtDataClass { get; }

        public Point_3D_Struct Point3DStruct { get; set; }
        public Point_3D_Struct ReadOnlyPoint3DStruct { get; }

        public Point_2D_With_ExtData Point2DWithExtData { get; set; }
        public Point_2D_With_ExtData ReadOnlyPoint2DWithExtData { get; }

        // Test that objects deserialized with parameterless still work fine as properties
        public SinglePublicParameterizedCtor SinglePublicParameterizedCtor { get; set; }
        public SinglePublicParameterizedCtor ReadOnlySinglePublicParameterizedCtor { get; }

        [JsonConstructor]
        public Person_Struct(
            string emailAddress,
            Guid id,
            int age,
            Point_2D readOnlyPoint2D,
            Point_2D_With_ExtData_Class readOnlyPoint2DWithExtDataClass,
            Point_3D_Struct readOnlyPoint3DStruct,
            Point_2D_With_ExtData readOnlyPoint2DWithExtData,
            SinglePublicParameterizedCtor readOnlySinglePublicParameterizedCtor)
        {
            // Readonly, setting in ctor.
            EmailAddress = emailAddress;
            Id = id;
            Age = age;
            ReadOnlyPoint2D = readOnlyPoint2D;
            ReadOnlyPoint2DWithExtDataClass = readOnlyPoint2DWithExtDataClass;
            ReadOnlyPoint3DStruct = readOnlyPoint3DStruct;
            ReadOnlyPoint2DWithExtData = readOnlyPoint2DWithExtData;
            ReadOnlySinglePublicParameterizedCtor = readOnlySinglePublicParameterizedCtor;

            // These properties will be set by serializer.
            FirstName = null;
            LastName = null;
            Point2D = null;
            Point2DWithExtDataClass = null;
            Point3DStruct = default;
            Point2DWithExtData = default;
            SinglePublicParameterizedCtor = default;
        }

        public static readonly string s_json =
             @"{
                ""FirstName"":""John"",
                ""LastName"":""Doe"",
                ""EmailAddress"":""johndoe@live.com"",
                ""Id"":""f2c92fcc-459f-4287-90b6-a7cbd82aeb0e"",
                ""Age"":24,
                ""Point2D"":{""X"":1,""Y"":2},
                ""Junk"":""Data"",
                ""ReadOnlyPoint2D"":{""X"":1,""Y"":2},
                ""Point2DWithExtDataClass"":{""X"":1,""Y"":2,""b"":3},
                ""ReadOnlyPoint2DWithExtDataClass"":{""X"":1,""Y"":2,""b"":3},
                ""Point3DStruct"":{""X"":1,""Y"":2,""Z"":3},
                ""More"":""Junk"",
                ""ReadOnlyPoint3DStruct"":{""X"":1,""Y"":2,""Z"":3},
                ""Point2DWithExtData"":{""X"":1,""Y"":2,""b"":3},
                ""ReadOnlyPoint2DWithExtData"":{""X"":1,""Y"":2,""b"":3}
            }";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal("John", FirstName);
            Assert.Equal("Doe", LastName);
            Assert.Equal("johndoe@live.com", EmailAddress);
            Assert.Equal("f2c92fcc-459f-4287-90b6-a7cbd82aeb0e", Id.ToString());
            Assert.Equal(24, Age);

            Point2D.Verify();
            ReadOnlyPoint2D.Verify();

            Assert.Equal(1, Point2DWithExtDataClass.X);
            Assert.Equal(2, Point2DWithExtDataClass.Y);
            Assert.True(Point2DWithExtDataClass.ExtensionData.ContainsKey("b"));

            Assert.Equal(1, Point3DStruct.X);
            Assert.Equal(2, Point3DStruct.Y);
            Assert.Equal(3, Point3DStruct.Z);

            Assert.Equal(1, ReadOnlyPoint3DStruct.X);
            Assert.Equal(2, ReadOnlyPoint3DStruct.Y);
            Assert.Equal(3, ReadOnlyPoint3DStruct.Z);

            Assert.Equal(1, Point2DWithExtData.X);
            Assert.Equal(2, Point2DWithExtData.Y);
            Assert.True(Point2DWithExtData.ExtensionData.ContainsKey("b"));

            Assert.Equal(1, ReadOnlyPoint2DWithExtData.X);
            Assert.Equal(2, ReadOnlyPoint2DWithExtData.Y);
            Assert.True(ReadOnlyPoint2DWithExtData.ExtensionData.ContainsKey("b"));
        }
    }



    public class Point_2D_With_ExtData_Class
    {
        public int X { get; }

        public int Y { get; }

        [JsonConstructor]
        public Point_2D_With_ExtData_Class(int x, int y)
        {
            X = x;
            Y = y;
        }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    public class Point_CtorsIgnoreJson
    {
        public int X { get; set; }

        public int Y { get; set; }

        [JsonConstructor]
        public Point_CtorsIgnoreJson(int x, int y)
        {
            X = 40;
            Y = 60;
        }
    }

    public class PointPropertyNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (name == "X")
            {
                return "A";
            }
            else if (name == "Y")
            {
                return "B";
            }

            return name;
        }
    }

    public class NullArgTester
    {
        public Point_3D_Struct Point3DStruct { get; }
        public ImmutableArray<int> ImmutableArray { get; }
        public int Int { get; }
        public string String { get; }

        public NullArgTester(Point_3D_Struct point3DStruct, ImmutableArray<int> immutableArray, int @int = 50, string @string = "defaultStr")
        {
            Point3DStruct = point3DStruct;
            ImmutableArray = immutableArray;
            Int = @int;
            String = @string;
        }
    }

    public class NullArgTester_Mutable
    {
        public Point_3D_Struct Point3DStruct { get; set; }
        public ImmutableArray<int> ImmutableArray { get; set; }
        public int Int { get; set; }
    }

    public class ObjWCtorMixedParams : ITestClassWithParameterizedCtor
    {
        public byte MyByte { get; }
        public sbyte MySByte { get; set; }
        public char MyChar { get; }
        public string MyString { get; }
        public decimal MyDecimal { get; }
        public bool MyBooleanTrue { get; set; }
        public bool MyBooleanFalse { get; }
        public float MySingle { get; set; }
        public double MyDouble { get; }
        public DateTime MyDateTime { get; set; }
        public DateTimeOffset MyDateTimeOffset { get; }
        public Guid MyGuid { get; }
        public Uri MyUri { get; set; }
        public SampleEnum MyEnum { get; }
        public SampleEnumInt64 MyInt64Enum { get; }
        public SampleEnumUInt64 MyUInt64Enum { get; }
        public SimpleStruct MySimpleStruct { get; }
        public SimpleTestStruct MySimpleTestStruct { get; set; }
        public int[][][] MyInt16ThreeDimensionArray { get; }
        public List<List<List<int>>> MyInt16ThreeDimensionList { get; }
        public List<string> MyStringList { get; }
        public IEnumerable MyStringIEnumerable { get; set; }
        public IList MyStringIList { get; }
        public ICollection MyStringICollection { get; set; }
        public IEnumerable<string> MyStringIEnumerableT { get; }
        public IReadOnlyList<string> MyStringIReadOnlyListT { get; }
        public ISet<string> MyStringISetT { get; set; }
        public KeyValuePair<string, string> MyStringToStringKeyValuePair { get; }
        public IDictionary MyStringToStringIDict { get; set; }
        public Dictionary<string, string> MyStringToStringGenericDict { get; }
        public IDictionary<string, string> MyStringToStringGenericIDict { get; set; }
        public IImmutableDictionary<string, string> MyStringToStringIImmutableDict { get; }
        public ImmutableQueue<string> MyStringImmutablQueueT { get; set; }
        public ImmutableSortedSet<string> MyStringImmutableSortedSetT { get; }
        public List<string> MyListOfNullString { get; }

        public ObjWCtorMixedParams(
            byte myByte,
            char myChar,
            string myString,
            decimal myDecimal,
            bool myBooleanFalse,
            double myDouble,
            DateTimeOffset myDateTimeOffset,
            Guid myGuid,
            SampleEnum myEnum,
            SampleEnumInt64 myInt64Enum,
            SampleEnumUInt64 myUInt64Enum,
            SimpleStruct mySimpleStruct,
            int[][][] myInt16ThreeDimensionArray,
            List<List<List<int>>> myInt16ThreeDimensionList,
            List<string> myStringList,
            IList myStringIList,
            IEnumerable<string> myStringIEnumerableT,
            IReadOnlyList<string> myStringIReadOnlyListT,
            KeyValuePair<string, string> myStringToStringKeyValuePair,
            Dictionary<string, string> myStringToStringGenericDict,
            IImmutableDictionary<string, string> myStringToStringIImmutableDict,
            ImmutableSortedSet<string> myStringImmutableSortedSetT,
            List<string> myListOfNullString)
        {
            MyByte = myByte;
            MyChar = myChar;
            MyString = myString;
            MyDecimal = myDecimal;
            MyBooleanFalse = myBooleanFalse;
            MyDouble = myDouble;
            MyDateTimeOffset = myDateTimeOffset;
            MyGuid = myGuid;
            MyEnum = myEnum;
            MyInt64Enum = myInt64Enum;
            MyUInt64Enum = myUInt64Enum;
            MySimpleStruct = mySimpleStruct;
            MyInt16ThreeDimensionArray = myInt16ThreeDimensionArray;
            MyInt16ThreeDimensionList = myInt16ThreeDimensionList;
            MyStringList = myStringList;
            MyStringIList = myStringIList;
            MyStringIEnumerableT = myStringIEnumerableT;
            MyStringIReadOnlyListT = myStringIReadOnlyListT;
            MyStringToStringKeyValuePair = myStringToStringKeyValuePair;
            MyStringToStringGenericDict = myStringToStringGenericDict;
            MyStringToStringIImmutableDict = myStringToStringIImmutableDict;
            MyStringImmutableSortedSetT = myStringImmutableSortedSetT;
            MyListOfNullString = myListOfNullString;
        }

        public static ObjWCtorMixedParams GetInstance() =>
#if BUILDING_SOURCE_GENERATOR_TESTS
            JsonSerializer.Deserialize(s_json, System.Text.Json.SourceGeneration.Tests.ConstructorTests_Default.ConstructorTestsContext_Default.Default.ObjWCtorMixedParams);
#else
            JsonSerializer.Deserialize<ObjWCtorMixedParams>(s_json);
#endif

        public static string s_json => $"{{{s_partialJson1},{s_partialJson2}}}";

        public static string s_json_flipped => $"{{{s_partialJson2},{s_partialJson1}}}";

        public static string s_json_minimal => @"{""MyDecimal"" : 3.3}";

        private const string s_partialJson1 =
            @"""MyByte"" : 7," +
            @"""MySByte"" : 8," +
            @"""MyChar"" : ""a""," +
            @"""MyString"" : ""Hello""," +
            @"""MyBooleanTrue"" : true," +
            @"""MyBooleanFalse"" : false," +
            @"""MySingle"" : 1.1," +
            @"""MyDouble"" : 2.2," +
            @"""MyDecimal"" : 3.3," +
            @"""MyDateTime"" : ""2019-01-30T12:01:02.0000000Z""," +
            @"""MyDateTimeOffset"" : ""2019-01-30T12:01:02.0000000+01:00""," +
            @"""MyGuid"" : ""1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6""," +
            @"""MyUri"" : ""https://github.com/dotnet/runtime""," +
            @"""MyEnum"" : 2," + // int by default
            @"""MyInt64Enum"" : -9223372036854775808," +
            @"""MyUInt64Enum"" : 18446744073709551615," +
            @"""MySimpleStruct"" : {""One"" : 11, ""Two"" : 1.9999, ""Three"" : 33}," +
            @"""MySimpleTestStruct"" : {""MyInt64"" : 64, ""MyString"" :""Hello"", ""MyInt32Array"" : [32]}," +
            @"""MyInt16ThreeDimensionArray"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]";

        private const string s_partialJson2 =
            @"""MyInt16ThreeDimensionList"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]," +
            @"""MyStringList"" : [""Hello""]," +
            @"""MyStringIEnumerable"" : [""Hello""]," +
            @"""MyStringIList"" : [""Hello""]," +
            @"""MyStringICollection"" : [""Hello""]," +
            @"""MyStringIEnumerableT"" : [""Hello""]," +
            @"""MyStringIReadOnlyListT"" : [""Hello""]," +
            @"""MyStringISetT"" : [""Hello""]," +
            @"""MyStringToStringKeyValuePair"" : {""Key"" : ""myKey"", ""Value"" : ""myValue""}," +
            @"""MyStringToStringIDict"" : {""key"" : ""value""}," +
            @"""MyStringToStringGenericDict"" : {""key"" : ""value""}," +
            @"""MyStringToStringGenericIDict"" : {""key"" : ""value""}," +
            @"""MyStringToStringIImmutableDict"" : {""key"" : ""value""}," +
            @"""MyStringImmutablQueueT"" : [""Hello""]," +
            @"""MyStringImmutableSortedSetT"" : [""Hello""]," +
            @"""MyListOfNullString"" : [null]";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal((byte)7, MyByte);
            Assert.Equal((sbyte)8, MySByte);
            Assert.Equal('a', MyChar);
            Assert.Equal("Hello", MyString);
            Assert.Equal(3.3m, MyDecimal);
            Assert.False(MyBooleanFalse);
            Assert.True(MyBooleanTrue);
            Assert.Equal(1.1f, MySingle);
            Assert.Equal(2.2d, MyDouble);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTime);
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), MyDateTimeOffset);
            Assert.Equal(SampleEnum.Two, MyEnum);
            Assert.Equal(SampleEnumInt64.MinNegative, MyInt64Enum);
            Assert.Equal(SampleEnumUInt64.Max, MyUInt64Enum);
            Assert.Equal(11, MySimpleStruct.One);
            Assert.Equal(1.9999, MySimpleStruct.Two);
            Assert.Equal(64, MySimpleTestStruct.MyInt64);
            Assert.Equal("Hello", MySimpleTestStruct.MyString);
            Assert.Equal(32, MySimpleTestStruct.MyInt32Array[0]);

            Assert.Equal(11, MyInt16ThreeDimensionArray[0][0][0]);
            Assert.Equal(12, MyInt16ThreeDimensionArray[0][0][1]);
            Assert.Equal(13, MyInt16ThreeDimensionArray[0][1][0]);
            Assert.Equal(14, MyInt16ThreeDimensionArray[0][1][1]);
            Assert.Equal(21, MyInt16ThreeDimensionArray[1][0][0]);
            Assert.Equal(22, MyInt16ThreeDimensionArray[1][0][1]);
            Assert.Equal(23, MyInt16ThreeDimensionArray[1][1][0]);
            Assert.Equal(24, MyInt16ThreeDimensionArray[1][1][1]);

            Assert.Equal(11, MyInt16ThreeDimensionList[0][0][0]);
            Assert.Equal(12, MyInt16ThreeDimensionList[0][0][1]);
            Assert.Equal(13, MyInt16ThreeDimensionList[0][1][0]);
            Assert.Equal(14, MyInt16ThreeDimensionList[0][1][1]);
            Assert.Equal(21, MyInt16ThreeDimensionList[1][0][0]);
            Assert.Equal(22, MyInt16ThreeDimensionList[1][0][1]);
            Assert.Equal(23, MyInt16ThreeDimensionList[1][1][0]);
            Assert.Equal(24, MyInt16ThreeDimensionList[1][1][1]);

            Assert.Equal("Hello", MyStringList[0]);

            IEnumerator enumerator = MyStringIEnumerable.GetEnumerator();
            enumerator.MoveNext();
            Assert.Equal("Hello", ((JsonElement)enumerator.Current).GetString());

            Assert.Equal("Hello", ((JsonElement)MyStringIList[0]).GetString());

            enumerator = MyStringICollection.GetEnumerator();
            enumerator.MoveNext();
            Assert.Equal("Hello", ((JsonElement)enumerator.Current).GetString());

            Assert.Equal("Hello", MyStringIEnumerableT.First());

            Assert.Equal("Hello", MyStringIReadOnlyListT[0]);
            Assert.Equal("Hello", MyStringISetT.First());

            Assert.Equal("myKey", MyStringToStringKeyValuePair.Key);
            Assert.Equal("myValue", MyStringToStringKeyValuePair.Value);

            enumerator = MyStringToStringIDict.GetEnumerator();
            enumerator.MoveNext();
            DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
            Assert.Equal("key", entry.Key);
            Assert.Equal("value", ((JsonElement)entry.Value).GetString());

            Assert.Equal("value", MyStringToStringGenericDict["key"]);
            Assert.Equal("value", MyStringToStringGenericIDict["key"]);
            Assert.Equal("value", MyStringToStringIImmutableDict["key"]);

            Assert.Equal("Hello", MyStringImmutablQueueT.First());
            Assert.Equal("Hello", MyStringImmutableSortedSetT.First());

            Assert.Null(MyListOfNullString[0]);
        }

        public void VerifyMinimal() => Assert.Equal(3.3m, MyDecimal);
    }
    public class Parameterless_ClassWithPrimitives
    {
        public int FirstInt { get; set; }
        public int SecondInt { get; set; }

        public string FirstString { get; set; }
        public string SecondString { get; set; }

        public DateTime FirstDateTime { get; set; }
        public DateTime SecondDateTime { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public int ThirdInt { get; set; }
        public int FourthInt { get; set; }

        public string ThirdString { get; set; }
        public string FourthString { get; set; }

        public DateTime ThirdDateTime { get; set; }
        public DateTime FourthDateTime { get; set; }
    }

    public class Parameterized_ClassWithPrimitives_3Args
    {
        public int FirstInt { get; set; }
        public int SecondInt { get; set; }

        public string FirstString { get; set; }
        public string SecondString { get; set; }

        public DateTime FirstDateTime { get; set; }
        public DateTime SecondDateTime { get; set; }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public int ThirdInt { get; set; }
        public int FourthInt { get; set; }

        public string ThirdString { get; set; }
        public string FourthString { get; set; }

        public DateTime ThirdDateTime { get; set; }
        public DateTime FourthDateTime { get; set; }


        public Parameterized_ClassWithPrimitives_3Args(int x, int y, int z) => (X, Y, Z) = (x, y, z);
    }

    public class TupleWrapper
    {
        public Tuple<string, double> Tuple { get; set; }
    }

    public class ConverterForPoint3D : JsonConverter<Point_3D>
    {
        public override Point_3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.Read();
            }

            return new Point_3D(4, 4, 4);
        }

        public override void Write(Utf8JsonWriter writer, Point_3D value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class Class_With_Ctor_With_64_Params : ITestClass
    {
        public int Int0 { get; }
        public int Int1 { get; }
        public int Int2 { get; }
        public int Int3 { get; }
        public int Int4 { get; }
        public int Int5 { get; }
        public int Int6 { get; }
        public int Int7 { get; }
        public int Int8 { get; }
        public int Int9 { get; }
        public int Int10 { get; }
        public int Int11 { get; }
        public int Int12 { get; }
        public int Int13 { get; }
        public int Int14 { get; }
        public int Int15 { get; }
        public int Int16 { get; }
        public int Int17 { get; }
        public int Int18 { get; }
        public int Int19 { get; }
        public int Int20 { get; }
        public int Int21 { get; }
        public int Int22 { get; }
        public int Int23 { get; }
        public int Int24 { get; }
        public int Int25 { get; }
        public int Int26 { get; }
        public int Int27 { get; }
        public int Int28 { get; }
        public int Int29 { get; }
        public int Int30 { get; }
        public int Int31 { get; }
        public int Int32 { get; }
        public int Int33 { get; }
        public int Int34 { get; }
        public int Int35 { get; }
        public int Int36 { get; }
        public int Int37 { get; }
        public int Int38 { get; }
        public int Int39 { get; }
        public int Int40 { get; }
        public int Int41 { get; }
        public int Int42 { get; }
        public int Int43 { get; }
        public int Int44 { get; }
        public int Int45 { get; }
        public int Int46 { get; }
        public int Int47 { get; }
        public int Int48 { get; }
        public int Int49 { get; }
        public int Int50 { get; }
        public int Int51 { get; }
        public int Int52 { get; }
        public int Int53 { get; }
        public int Int54 { get; }
        public int Int55 { get; }
        public int Int56 { get; }
        public int Int57 { get; }
        public int Int58 { get; }
        public int Int59 { get; }
        public int Int60 { get; }
        public int Int61 { get; }
        public int Int62 { get; }
        public int Int63 { get; }

        public Class_With_Ctor_With_64_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                             int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                             int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                             int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                             int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                             int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                             int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                             int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63)
        {
            Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
            Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
            Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
            Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
            Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
            Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
            Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
            Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
        }

        public static string Json
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                for (int i = 0; i < 63; i++)
                {
                    sb.Append($@"""Int{i}"":{i},");
                }
                sb.Append($@"""Int63"":63");
                sb.Append("}");

                return sb.ToString();
            }
        }

        public static byte[] Data => Encoding.UTF8.GetBytes(Json);

        public void Initialize() { }

        public void Verify()
        {
            for (int i = 0; i < 64; i++)
            {
                Assert.Equal(i, (int)typeof(Class_With_Ctor_With_64_Params).GetProperty($"Int{i}").GetValue(this));
            }
        }
    }

    public struct Struct_With_Ctor_With_64_Params
    {
        public int Int0 { get; }
        public int Int1 { get; }
        public int Int2 { get; }
        public int Int3 { get; }
        public int Int4 { get; }
        public int Int5 { get; }
        public int Int6 { get; }
        public int Int7 { get; }
        public int Int8 { get; }
        public int Int9 { get; }
        public int Int10 { get; }
        public int Int11 { get; }
        public int Int12 { get; }
        public int Int13 { get; }
        public int Int14 { get; }
        public int Int15 { get; }
        public int Int16 { get; }
        public int Int17 { get; }
        public int Int18 { get; }
        public int Int19 { get; }
        public int Int20 { get; }
        public int Int21 { get; }
        public int Int22 { get; }
        public int Int23 { get; }
        public int Int24 { get; }
        public int Int25 { get; }
        public int Int26 { get; }
        public int Int27 { get; }
        public int Int28 { get; }
        public int Int29 { get; }
        public int Int30 { get; }
        public int Int31 { get; }
        public int Int32 { get; }
        public int Int33 { get; }
        public int Int34 { get; }
        public int Int35 { get; }
        public int Int36 { get; }
        public int Int37 { get; }
        public int Int38 { get; }
        public int Int39 { get; }
        public int Int40 { get; }
        public int Int41 { get; }
        public int Int42 { get; }
        public int Int43 { get; }
        public int Int44 { get; }
        public int Int45 { get; }
        public int Int46 { get; }
        public int Int47 { get; }
        public int Int48 { get; }
        public int Int49 { get; }
        public int Int50 { get; }
        public int Int51 { get; }
        public int Int52 { get; }
        public int Int53 { get; }
        public int Int54 { get; }
        public int Int55 { get; }
        public int Int56 { get; }
        public int Int57 { get; }
        public int Int58 { get; }
        public int Int59 { get; }
        public int Int60 { get; }
        public int Int61 { get; }
        public int Int62 { get; }
        public int Int63 { get; }

        [JsonConstructor]
        public Struct_With_Ctor_With_64_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                             int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                             int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                             int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                             int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                             int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                             int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                             int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63)
        {
            Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
            Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
            Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
            Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
            Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
            Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
            Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
            Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
        }
    }

    public class Class_With_Ctor_With_65_Params
    {
        public int Int0 { get; }
        public int Int1 { get; }
        public int Int2 { get; }
        public int Int3 { get; }
        public int Int4 { get; }
        public int Int5 { get; }
        public int Int6 { get; }
        public int Int7 { get; }
        public int Int8 { get; }
        public int Int9 { get; }
        public int Int10 { get; }
        public int Int11 { get; }
        public int Int12 { get; }
        public int Int13 { get; }
        public int Int14 { get; }
        public int Int15 { get; }
        public int Int16 { get; }
        public int Int17 { get; }
        public int Int18 { get; }
        public int Int19 { get; }
        public int Int20 { get; }
        public int Int21 { get; }
        public int Int22 { get; }
        public int Int23 { get; }
        public int Int24 { get; }
        public int Int25 { get; }
        public int Int26 { get; }
        public int Int27 { get; }
        public int Int28 { get; }
        public int Int29 { get; }
        public int Int30 { get; }
        public int Int31 { get; }
        public int Int32 { get; }
        public int Int33 { get; }
        public int Int34 { get; }
        public int Int35 { get; }
        public int Int36 { get; }
        public int Int37 { get; }
        public int Int38 { get; }
        public int Int39 { get; }
        public int Int40 { get; }
        public int Int41 { get; }
        public int Int42 { get; }
        public int Int43 { get; }
        public int Int44 { get; }
        public int Int45 { get; }
        public int Int46 { get; }
        public int Int47 { get; }
        public int Int48 { get; }
        public int Int49 { get; }
        public int Int50 { get; }
        public int Int51 { get; }
        public int Int52 { get; }
        public int Int53 { get; }
        public int Int54 { get; }
        public int Int55 { get; }
        public int Int56 { get; }
        public int Int57 { get; }
        public int Int58 { get; }
        public int Int59 { get; }
        public int Int60 { get; }
        public int Int61 { get; }
        public int Int62 { get; }
        public int Int63 { get; }
        public int Int64 { get; }

        public Class_With_Ctor_With_65_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                             int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                             int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                             int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                             int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                             int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                             int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                             int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63,
                                             int int64)
        {
            Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
            Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
            Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
            Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
            Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
            Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
            Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
            Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
            Int64 = int64;
        }
    }

    public struct Struct_With_Ctor_With_65_Params
    {
        public int Int0 { get; }
        public int Int1 { get; }
        public int Int2 { get; }
        public int Int3 { get; }
        public int Int4 { get; }
        public int Int5 { get; }
        public int Int6 { get; }
        public int Int7 { get; }
        public int Int8 { get; }
        public int Int9 { get; }
        public int Int10 { get; }
        public int Int11 { get; }
        public int Int12 { get; }
        public int Int13 { get; }
        public int Int14 { get; }
        public int Int15 { get; }
        public int Int16 { get; }
        public int Int17 { get; }
        public int Int18 { get; }
        public int Int19 { get; }
        public int Int20 { get; }
        public int Int21 { get; }
        public int Int22 { get; }
        public int Int23 { get; }
        public int Int24 { get; }
        public int Int25 { get; }
        public int Int26 { get; }
        public int Int27 { get; }
        public int Int28 { get; }
        public int Int29 { get; }
        public int Int30 { get; }
        public int Int31 { get; }
        public int Int32 { get; }
        public int Int33 { get; }
        public int Int34 { get; }
        public int Int35 { get; }
        public int Int36 { get; }
        public int Int37 { get; }
        public int Int38 { get; }
        public int Int39 { get; }
        public int Int40 { get; }
        public int Int41 { get; }
        public int Int42 { get; }
        public int Int43 { get; }
        public int Int44 { get; }
        public int Int45 { get; }
        public int Int46 { get; }
        public int Int47 { get; }
        public int Int48 { get; }
        public int Int49 { get; }
        public int Int50 { get; }
        public int Int51 { get; }
        public int Int52 { get; }
        public int Int53 { get; }
        public int Int54 { get; }
        public int Int55 { get; }
        public int Int56 { get; }
        public int Int57 { get; }
        public int Int58 { get; }
        public int Int59 { get; }
        public int Int60 { get; }
        public int Int61 { get; }
        public int Int62 { get; }
        public int Int63 { get; }
        public int Int64 { get; }

        [JsonConstructor]
        public Struct_With_Ctor_With_65_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                             int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                             int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                             int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                             int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                             int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                             int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                             int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63,
                                             int int64)
        {
            Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
            Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
            Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
            Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
            Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
            Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
            Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
            Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
            Int64 = int64;
        }
    }

    public class Parameterized_Person : ITestClass
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public Guid Id { get; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }

        public Parameterized_Person(Guid id) => Id = id;

        public static readonly string s_json = @"{
            ""FirstName"":""Jet"",
            ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
            ""EmailAddress"":""jetdoe@outlook.com"",
            ""Id"":""0b3aa420-2e98-47f7-8a49-fea233b89416"",
            ""LastName"":""Doe"",
            ""Id"":""63cf821d-fd47-4782-8345-576d9228a534""
            }";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal("Jet", FirstName);
            Assert.Equal("Doe", LastName);
            Assert.Equal("63cf821d-fd47-4782-8345-576d9228a534", Id.ToString());
            Assert.Equal("jetdoe@outlook.com", ExtensionData["EmailAddress"].GetString());
            Assert.False(ExtensionData.ContainsKey("Id"));
        }
    }

    public class Parameterized_Person_ObjExtData : ITestClass
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public Guid Id { get; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

        public Parameterized_Person_ObjExtData(Guid id) => Id = id;

        public static readonly string s_json = @"{
            ""FirstName"":""Jet"",
            ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
            ""EmailAddress"":""jetdoe@outlook.com"",
            ""Id"":""0b3aa420-2e98-47f7-8a49-fea233b89416"",
            ""LastName"":""Doe"",
            ""Id"":""63cf821d-fd47-4782-8345-576d9228a534""
            }";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal("Jet", FirstName);
            Assert.Equal("Doe", LastName);
            Assert.Equal("63cf821d-fd47-4782-8345-576d9228a534", Id.ToString());
            Assert.Equal("jetdoe@outlook.com", ((JsonElement)ExtensionData["EmailAddress"]).GetString());
            Assert.False(ExtensionData.ContainsKey("Id"));
        }
    }

    public class Parameterized_Person_Simple : ITestClass
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public Guid Id { get; }

        public Parameterized_Person_Simple(Guid id) => Id = id;

        public static readonly string s_json = @"{
            ""FirstName"":""Jet"",
            ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
            ""LastName"":""Doe""
            }";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            FirstName = "Jet";
            LastName = "Doe";
        }

        public void Verify()
        {
            Assert.Equal("Jet", FirstName);
            Assert.Equal("Doe", LastName);
            Assert.Equal("270bb22b-4816-4bd9-9acd-8ec5b1a896d3", Id.ToString());
        }
    }

    public class SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt
    {
        public int X { get; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt
    {
        public int X { get; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt
    {
        public int X { get; }

        [JsonExtensionData]
        public GenericIDictionaryWrapper<string, JsonElement> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt
    {
        public int X { get; }

        [JsonExtensionData]
        public GenericIDictionaryWrapper<string, object> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt(int x) { }
    }

    public class Parameterized_IndexViewModel_Immutable : ITestClass
    {
        public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; }
        public CampaignSummaryViewModel FeaturedCampaign { get; }
        public bool IsNewAccount { get; }
        public bool HasFeaturedCampaign => FeaturedCampaign != null;

        public Parameterized_IndexViewModel_Immutable(
            List<ActiveOrUpcomingEvent> activeOrUpcomingEvents,
            CampaignSummaryViewModel featuredCampaign,
            bool isNewAccount)
        {
            ActiveOrUpcomingEvents = activeOrUpcomingEvents;
            FeaturedCampaign = featuredCampaign;
            IsNewAccount = isNewAccount;
        }

        public static readonly string s_json =
            @"{
              ""ActiveOrUpcomingEvents"": [
                {
                  ""Id"": 10,
                  ""ImageUrl"": ""https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png"",
                  ""Name"": ""Just a name"",
                  ""CampaignName"": ""The very new campaign"",
                  ""CampaignManagedOrganizerName"": ""Name FamilyName"",
                  ""Description"": ""The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities."",
                  ""StartDate"": ""2019-01-30T12:01:02+00:00"",
                  ""EndDate"": ""2019-01-30T12:01:02+00:00""
                }
              ],
              ""FeaturedCampaign"": {
                ""Id"": 234235,
                ""Title"": ""Promoting Open Source"",
                ""Description"": ""Very nice campaign"",
                ""ImageUrl"": ""https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png"",
                ""OrganizationName"": ""The Company XYZ"",
                ""Headline"": ""The Headline""
              },
              ""IsNewAccount"": false,
              ""HasFeaturedCampaign"": true
            }";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize() { }

        public void Verify()
        {
            Assert.False(IsNewAccount);

            ActiveOrUpcomingEvent @event = ActiveOrUpcomingEvents.First();
            Assert.Equal(10, @event.Id);
            Assert.Equal("Name FamilyName", @event.CampaignManagedOrganizerName);
            Assert.Equal("The very new campaign", @event.CampaignName);
            Assert.Equal("The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.", @event.Description);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), @event.EndDate);
            Assert.Equal("Just a name", @event.Name);
            Assert.Equal("https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png", @event.ImageUrl);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), @event.StartDate);

            Assert.Equal("Very nice campaign", FeaturedCampaign.Description);
            Assert.Equal("The Headline", FeaturedCampaign.Headline);
            Assert.Equal(234235, FeaturedCampaign.Id);
            Assert.Equal("The Company XYZ", FeaturedCampaign.OrganizationName);
            Assert.Equal("https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png", FeaturedCampaign.ImageUrl);
            Assert.Equal("Promoting Open Source", FeaturedCampaign.Title);
        }
    }

    public class ActiveOrUpcomingEvent
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public string CampaignName { get; set; }
        public string CampaignManagedOrganizerName { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }

    public class CampaignSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string OrganizationName { get; set; }
        public string Headline { get; set; }
    }

    public class Parameterized_Class_With_ComplexTuple : ITestClassWithParameterizedCtor
    {
        public Tuple<
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams> MyTuple { get; }

        public Parameterized_Class_With_ComplexTuple(
            Tuple<
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams,
                ObjWCtorMixedParams> myTuple) => MyTuple = myTuple;

        private const string s_inner_json = @"
            {
                ""MyByte"": 7,
                ""MyChar"": ""a"",
                ""MyString"": ""Hello"",
                ""MyDecimal"": 3.3,
                ""MyBooleanFalse"": false,
                ""MyDouble"": 2.2,
                ""MyDateTimeOffset"": ""2019-01-30T12:01:02+01:00"",
                ""MyGuid"": ""1b33498a-7b7d-4dda-9c13-f6aa4ab449a6"",
                ""MyEnum"": 2,
                ""MyInt64Enum"": -9223372036854775808,
                ""MyUInt64Enum"": 18446744073709551615,
                ""MySimpleStruct"": { ""One"": 11, ""Two"": 1.9999 },
                ""MyInt16ThreeDimensionArray"": [ [ [ 11, 12 ], [ 13, 14 ] ], [ [ 21, 22 ], [ 23, 24 ] ] ],
                ""MyInt16ThreeDimensionList"": [ [ [ 11, 12 ], [ 13, 14 ] ], [ [ 21, 22 ], [ 23, 24 ] ] ],
                ""MyStringList"": [ ""Hello"" ],
                ""MyStringIList"": [ ""Hello"" ],
                ""MyStringIEnumerableT"": [ ""Hello"" ],
                ""MyStringIReadOnlyListT"": [ ""Hello"" ],
                ""MyStringToStringKeyValuePair"": { ""Key"": ""myKey"", ""Value"": ""myValue"" },
                ""MyStringToStringGenericDict"": { ""key"": ""value"" },
                ""MyStringToStringIImmutableDict"": { ""key"": ""value"" },
                ""MyStringImmutableSortedSetT"": [ ""Hello"" ],
                ""MyListOfNullString"": [ null ],
                ""MySByte"": 8,
                ""MyBooleanTrue"": true,
                ""MySingle"": 1.1,
                ""MyDateTime"": ""2019-01-30T12:01:02Z"",
                ""MyUri"": ""https://github.com/dotnet/runtime"",
                ""MySimpleTestStruct"": {
                  ""MyInt16"": 0,
                  ""MyInt32"": 0,
                  ""MyInt64"": 64,
                  ""MyUInt16"": 0,
                  ""MyUInt32"": 0,
                  ""MyUInt64"": 0,
                  ""MyByte"": 0,
                  ""MySByte"": 0,
                  ""MyChar"": ""\u0000"",
                  ""MyString"": ""Hello"",
                  ""MyDecimal"": 0,
                  ""MyBooleanTrue"": false,
                  ""MyBooleanFalse"": false,
                  ""MySingle"": 0,
                  ""MyDouble"": 0,
                  ""MyDateTime"": ""0001-01-01T00:00:00"",
                  ""MyDateTimeOffset"": ""0001-01-01T00:00:00+00:00"",
                  ""MyEnum"": 0,
                  ""MyInt64Enum"": 0,
                  ""MyUInt64Enum"": 0,
                  ""MySimpleStruct"": {
                    ""One"": 0,
                    ""Two"": 0
                  },
                  ""MyInt32Array"": [ 32 ]
                },
                ""MyStringIEnumerable"": [ ""Hello"" ],
                ""MyStringICollection"": [ ""Hello"" ],
                ""MyStringISetT"": [ ""Hello"" ],
                ""MyStringToStringIDict"": { ""key"": ""value"" },
                ""MyStringToStringGenericIDict"": { ""key"": ""value"" },
                ""MyStringImmutablQueueT"": [ ""Hello"" ]
              }";

        public static string s_json =>
            $@"{{
                ""MyTuple"": {{
                    {s_partialJson1},
                    {s_partialJson2}
                }}
            }}";

        public static string s_json_flipped =>
            $@"{{
                ""MyTuple"": {{
                    {s_partialJson2},
                    {s_partialJson1}
                }}
            }}";

        public static string s_json_minimal =>
            $@"{{
                ""MyTuple"": {{
                    ""Item4"":{s_inner_json}
                }}
            }}";

        private static string s_partialJson1 =>
            $@"
                ""Item1"":{s_inner_json},
                ""Item2"":{s_inner_json},
                ""Item3"":{s_inner_json},
                ""Item4"":{s_inner_json}
            ";

        private static string s_partialJson2 =>
            $@"
                ""Item5"":{s_inner_json},
                ""Item6"":{s_inner_json},
                ""Item7"":{s_inner_json}
            ";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize() { }

        public void Verify()
        {
            MyTuple.Item1.Verify();
            MyTuple.Item2.Verify();
            MyTuple.Item3.Verify();
            MyTuple.Item4.Verify();
            MyTuple.Item5.Verify();
            MyTuple.Item6.Verify();
            MyTuple.Item7.Verify();
        }

        public void VerifyMinimal()
        {
            MyTuple.Item4.Verify();
        }
    }
    public class Class_With_Parameters_Default_Values
    {
        public int I { get; }
        public float F { get; }
        public double D { get; }
        public decimal M { get; }
        public StringComparison SC { get; }
        public char C { get; }
        public int? NI { get; }
        public float? NF { get; }
        public double? ND { get; }
        public decimal? NM { get; }
        public StringComparison? NSC { get; }
        public char? NC { get; }

        public Class_With_Parameters_Default_Values(
                int i = 21, float f = 42.0f, double d = 3.14159, decimal m = 3.1415926535897932384626433M, StringComparison sc = StringComparison.Ordinal, char c = 'q',
                int? ni = 21, float? nf = 42.0f, double? nd = 3.14159, decimal? nm = 3.1415926535897932384626433M, StringComparison? nsc = StringComparison.Ordinal, char? nc = 'q')
        {
            I = i;
            F = f;
            D = d;
            M = m;
            SC = sc;
            C = c;
            NI = ni;
            NF = nf;
            ND = nd;
            NM = nm;
            NSC = nsc;
            NC = nc;
        }

        public void Initialize() { }

        public static readonly string s_json = @"{}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Verify()
        {
            Assert.Equal(21, I);
            Assert.Equal(42.0f, F);
            Assert.Equal(3.14159, D);
            Assert.Equal(3.1415926535897932384626433M, M);
            Assert.Equal(StringComparison.Ordinal, SC);
            Assert.Equal('q', C);
            Assert.Equal(21, NI);
            Assert.Equal(42.0f, NF);
            Assert.Equal(3.14159, ND);
            Assert.Equal(3.1415926535897932384626433M, NM);
            Assert.Equal(StringComparison.Ordinal, NSC);
            Assert.Equal('q', NC);
        }
    }

    public class Point_MembersHave_JsonPropertyName : ITestClass
    {
        [JsonPropertyName("XValue")]
        public int X { get; }

        [JsonPropertyName("YValue")]
        public int Y { get; }

        public Point_MembersHave_JsonPropertyName(int x, int y) => (X, Y) = (x, y);

        public void Initialize() { }

        public static readonly string s_json = @"{""XValue"":1,""YValue"":2}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
        }
    }

    public class Point_MembersHave_JsonConverter : ITestClass
    {
        [JsonConverter(typeof(ConverterForInt32))]
        public int X { get; }

        [JsonConverter(typeof(ConverterForInt32))]
        public int Y { get; }

        public Point_MembersHave_JsonConverter(int x, int y) => (X, Y) = (x, y);

        public void Initialize() { }

        public static readonly string s_json = @"{""X"":1,""Y"":2}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Verify()
        {
            Assert.Equal(25, X);
            Assert.Equal(25, Y);
        }
    }

    public class Point_MembersHave_JsonIgnore : ITestClass
    {
        [JsonIgnore]
        public int X { get; }

        [JsonIgnore]
        public int Y { get; }

        public Point_MembersHave_JsonIgnore(int x, int y = 5) => (X, Y) = (x, y);

        public void Initialize() { }

        public static readonly string s_json = @"{""X"":1,""Y"":2}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Verify()
        {
            Assert.Equal(0, X);
            Assert.Equal(5, Y); // We use the default parameter of the constructor.
        }
    }
    
    public class Point_MembersHave_JsonInclude : ITestClass
    {
        [JsonInclude]
        public int X { get; }

        [JsonInclude]
        public int Y { get; private set; }

        public int Z { get; private set; }

        public Point_MembersHave_JsonInclude(int x, int y, int z) => (X, Y, Z) = (x, y, z);

        public void Initialize() { }

        public static readonly string s_json = @"{""X"":1,""Y"":2,""Z"":3}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
            Assert.Equal(3, Z);
        }
    }

    public class ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes : ITestClass
    {
        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public int A { get; }

        [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
        public float B { get; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int C { get; }

        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public int D { get; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public int E { get; }

        public ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes(int a, float b, int c, int d, int e) => (A, B, C, D, E) = (a, b, c, d, e);

        public void Initialize() { }

        public static readonly string s_json = @"{""A"":1,""B"":""NaN"",""C"":""2"",""D"": 3,""E"":""4""}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Verify()
        {
            Assert.Equal(1, A);
            Assert.Equal(float.NaN, B);
            Assert.Equal(2, C);
            Assert.Equal(3, D);
            Assert.Equal(4, E);
        }
    }

    public class Point_MultipleMembers_BindTo_OneConstructorParameter
    {
        public int X { get; }

        public int x { get; }

        public Point_MultipleMembers_BindTo_OneConstructorParameter(int X, int x)
        {
            this.X = X;
            this.x = x;
        }
    }

    public class Url_BindTo_OneConstructorParameter
    {
        public int URL { get; }

        public int Url { get; }

        public Url_BindTo_OneConstructorParameter(int url) { }
    }

    public class Point_MultipleMembers_BindTo_OneConstructorParameter_Variant
    {
        public int X { get; }

        public int x { get; }

        public Point_MultipleMembers_BindTo_OneConstructorParameter_Variant(int x) { }
    }

    public class Point_Without_Members
    {
        public Point_Without_Members(int x, int y) { }
    }

    public class Point_With_MismatchedMembers
    {
        public int X { get; }
        public float Y { get; }

        public Point_With_MismatchedMembers(int x, int y) { }
    }

    public class WrapperFor_Point_With_MismatchedMembers
    {
        public int MyInt { get; set; }
        public Point_With_MismatchedMembers MyPoint { get; set; }
    }


    public class Point_ExtendedPropNames
    {
        public int XValue { get; }
        public int YValue { get; }

        public Point_ExtendedPropNames(int xValue, int yValue)
        {
            XValue = xValue;
            YValue = yValue;
        }
    }

    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLowerInvariant();
        }
    }

    public class Point_With_Array : ITestClass
    {
        public int X { get; }
        public int Y { get; }

        public int[] Arr { get; }

        public static readonly string s_json = @"{""X"":1,""Y"":2,""Arr"":[1,2]}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public Point_With_Array(int x, int y, int[] arr)
        {
            X = x;
            Y = y;
            Arr = arr;
        }

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
            Assert.Equal(1, Arr[0]);
            Assert.Equal(2, Arr[1]);
        }
    }

    public class Point_With_Dictionary : ITestClass
    {
        public int X { get; }
        public int Y { get; }

        public Dictionary<string, int> Dict { get; }

        public static readonly string s_json = @"{""X"":1,""Y"":2,""Dict"":{""1"":1,""2"":2}}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public Point_With_Dictionary(int x, int y, Dictionary<string, int> dict)
        {
            X = x;
            Y = y;
            Dict = dict;
        }

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
            Assert.Equal(1, Dict["1"]);
            Assert.Equal(2, Dict["2"]);
        }
    }

    public class Point_With_Object : ITestClass
    {
        public int X { get; }
        public int Y { get; }

        public Point_With_Array Obj { get; }

        public static readonly string s_json = @$"{{""X"":1,""Y"":2,""Obj"":{Point_With_Array.s_json}}}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public Point_With_Object(int x, int y, Point_With_Array obj)
        {
            X = x;
            Y = y;
            Obj = obj;
        }

        public void Initialize() { }

        public void Verify()
        {
            Assert.Equal(1, X);
            Assert.Equal(2, Y);
            Obj.Verify();
        }
    }

    public struct Point_With_Property
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; set; }

        [JsonConstructor]
        public Point_With_Property(int x, int y)
        {
            X = x;
            Y = y;
            Z = 0;
        }
    }

    public class MyEventsListerViewModel
    {
        public List<MyEventsListerItem> CurrentEvents { get; set; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> FutureEvents { get; set; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> PastEvents { get; set; } = new List<MyEventsListerItem>();

        public static MyEventsListerViewModel Instance
            = new MyEventsListerViewModel
            {
                CurrentEvents = Enumerable.Repeat(MyEventsListerItem.Instance, 3).ToList(),
                FutureEvents = Enumerable.Repeat(MyEventsListerItem.Instance, 9).ToList(),
                PastEvents = Enumerable.Repeat(MyEventsListerItem.Instance, 60).ToList() // usually  there is a lot of historical data
            };
    }

    public class MyEventsListerItem
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string TimeZone { get; set; }
        public string Campaign { get; set; }
        public string Organization { get; set; }
        public int VolunteerCount { get; set; }

        public List<MyEventsListerItemTask> Tasks { get; set; } = new List<MyEventsListerItemTask>();

        public static MyEventsListerItem Instance
            = new MyEventsListerItem
            {
                Campaign = "A very nice campaign",
                EndDate = DateTimeTestHelpers.FixedDateTimeValue.AddDays(7),
                EventId = 321,
                EventName = "wonderful name",
                Organization = "Local Animal Shelter",
                StartDate = DateTimeTestHelpers.FixedDateTimeValue.AddDays(-7),
                TimeZone = TimeZoneInfo.Utc.DisplayName,
                VolunteerCount = 15,
                Tasks = Enumerable.Repeat(
                    new MyEventsListerItemTask
                    {
                        StartDate = DateTimeTestHelpers.FixedDateTimeValue,
                        EndDate = DateTimeTestHelpers.FixedDateTimeValue.AddDays(1),
                        Name = "A very nice task to have"
                    }, 4).ToList()
            };
    }

    public class MyEventsListerItemTask
    {
        public string Name { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        public string FormattedDate
        {
            get
            {
                if (!StartDate.HasValue || !EndDate.HasValue)
                {
                    return null;
                }

                var startDateString = string.Format("{0:g}", StartDate.Value);
                var endDateString = string.Format("{0:g}", EndDate.Value);

                return $"From {startDateString} to {endDateString}";
            }
        }
    }

    public class ClassWithNestedClass
    {
        public ClassWithNestedClass MyClass { get; }

        public Point_2D_Struct_WithAttribute MyPoint { get; }

        public ClassWithNestedClass(ClassWithNestedClass myClass, Point_2D_Struct_WithAttribute myPoint)
        {
            MyClass = myClass;
            MyPoint = myPoint;
        }
    }

    public struct StructWithFourArgs
    {
        public int W { get; }
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        [JsonConstructor]
        public StructWithFourArgs(int w, int x, int y, int z) => (W, X, Y, Z) = (w, x, y, z);
    }

    public record ClassWithManyConstructorParameters(
        int P000, int P001, int P002, int P003, int P004, int P005, int P006, int P007, int P008, int P009,
        int P010, int P011, int P012, int P013, int P014, int P015, int P016, int P017, int P018, int P019,
        int P020, int P021, int P022, int P023, int P024, int P025, int P026, int P027, int P028, int P029,
        int P030, int P031, int P032, int P033, int P034, int P035, int P036, int P037, int P038, int P039,
        int P040, int P041, int P042, int P043, int P044, int P045, int P046, int P047, int P048, int P049,
        int P050, int P051, int P052, int P053, int P054, int P055, int P056, int P057, int P058, int P059,
        int P060, int P061, int P062, int P063, int P064, int P065, int P066, int P067, int P068, int P069,
        int P070, int P071, int P072, int P073, int P074, int P075, int P076, int P077, int P078, int P079,
        int P080, int P081, int P082, int P083, int P084, int P085, int P086, int P087, int P088, int P089,
        int P090, int P091, int P092, int P093, int P094, int P095, int P096, int P097, int P098, int P099,
        int P100, int P101, int P102, int P103, int P104, int P105, int P106, int P107, int P108, int P109,
        int P110, int P111, int P112, int P113, int P114, int P115, int P116, int P117, int P118, int P119,
        int P120, int P121, int P122, int P123, int P124, int P125, int P126, int P127, int P128, int P129,
        int P130, int P131, int P132, int P133, int P134, int P135, int P136, int P137, int P138, int P139,
        int P140, int P141, int P142, int P143, int P144, int P145, int P146, int P147, int P148, int P149,
        int P150, int P151, int P152, int P153, int P154, int P155, int P156, int P157, int P158, int P159,
        int P160, int P161, int P162, int P163, int P164, int P165, int P166, int P167, int P168, int P169,
        int P170, int P171, int P172, int P173, int P174, int P175, int P176, int P177, int P178, int P179,
        int P180, int P181, int P182, int P183, int P184, int P185, int P186, int P187, int P188, int P189,
        int P190, int P191, int P192, int P193, int P194, int P195, int P196, int P197, int P198, int P199,
        int P200, int P201, int P202, int P203, int P204, int P205, int P206, int P207, int P208, int P209,
        int P210, int P211, int P212, int P213, int P214, int P215, int P216, int P217, int P218, int P219,
        int P220, int P221, int P222, int P223, int P224, int P225, int P226, int P227, int P228, int P229,
        int P230, int P231, int P232, int P233, int P234, int P235, int P236, int P237, int P238, int P239,
        int P240, int P241, int P242, int P243, int P244, int P245, int P246, int P247, int P248, int P249,
        int P250, int P251, int P252, int P253, int P254, int P255, int P256, int P257, int P258, int P259,
        int P260, int P261, int P262, int P263, int P264, int P265, int P266, int P267, int P268, int P269,
        int P270, int P271, int P272, int P273, int P274, int P275, int P276, int P277, int P278, int P279,
        int P280, int P281, int P282, int P283, int P284, int P285, int P286, int P287, int P288, int P289,
        int P290, int P291, int P292, int P293, int P294, int P295, int P296, int P297, int P298, int P299,
        int P300, int P301, int P302, int P303, int P304, int P305, int P306, int P307, int P308, int P309,
        int P310, int P311, int P312, int P313, int P314, int P315, int P316, int P317, int P318, int P319,
        int P320, int P321, int P322, int P323, int P324, int P325, int P326, int P327, int P328, int P329,
        int P330, int P331, int P332, int P333, int P334, int P335, int P336, int P337, int P338, int P339,
        int P340, int P341, int P342, int P343, int P344, int P345, int P346, int P347, int P348, int P349,
        int P350, int P351, int P352, int P353, int P354, int P355, int P356, int P357, int P358, int P359,
        int P360, int P361, int P362, int P363, int P364, int P365, int P366, int P367, int P368, int P369,
        int P370, int P371, int P372, int P373, int P374, int P375, int P376, int P377, int P378, int P379,
        int P380, int P381, int P382, int P383, int P384, int P385, int P386, int P387, int P388, int P389,
        int P390, int P391, int P392, int P393, int P394, int P395, int P396, int P397, int P398, int P399,
        int P400, int P401, int P402, int P403, int P404, int P405, int P406, int P407, int P408, int P409,
        int P410, int P411, int P412, int P413, int P414, int P415, int P416, int P417, int P418, int P419,
        int P420, int P421, int P422, int P423, int P424, int P425, int P426, int P427, int P428, int P429,
        int P430, int P431, int P432, int P433, int P434, int P435, int P436, int P437, int P438, int P439,
        int P440, int P441, int P442, int P443, int P444, int P445, int P446, int P447, int P448, int P449,
        int P450, int P451, int P452, int P453, int P454, int P455, int P456, int P457, int P458, int P459,
        int P460, int P461, int P462, int P463, int P464, int P465, int P466, int P467, int P468, int P469,
        int P470, int P471, int P472, int P473, int P474, int P475, int P476, int P477, int P478, int P479,
        int P480, int P481, int P482, int P483, int P484, int P485, int P486, int P487, int P488, int P489,
        int P490, int P491, int P492, int P493, int P494, int P495, int P496, int P497, int P498, int P499,
        int P500, int P501, int P502, int P503, int P504, int P505, int P506, int P507, int P508, int P509,
        int P510, int P511, int P512, int P513, int P514, int P515, int P516, int P517, int P518, int P519,
        int P520, int P521, int P522, int P523, int P524, int P525, int P526, int P527, int P528, int P529,
        int P530, int P531, int P532, int P533, int P534, int P535, int P536, int P537, int P538, int P539,
        int P540, int P541, int P542, int P543, int P544, int P545, int P546, int P547, int P548, int P549,
        int P550, int P551, int P552, int P553, int P554, int P555, int P556, int P557, int P558, int P559,
        int P560, int P561, int P562, int P563, int P564, int P565, int P566, int P567, int P568, int P569,
        int P570, int P571, int P572, int P573, int P574, int P575, int P576, int P577, int P578, int P579,
        int P580, int P581, int P582, int P583, int P584, int P585, int P586, int P587, int P588, int P589,
        int P590, int P591, int P592, int P593, int P594, int P595, int P596, int P597, int P598, int P599,
        int P600, int P601, int P602, int P603, int P604, int P605, int P606, int P607, int P608, int P609,
        int P610, int P611, int P612, int P613, int P614, int P615, int P616, int P617, int P618, int P619,
        int P620, int P621, int P622, int P623, int P624, int P625, int P626, int P627, int P628, int P629,
        int P630, int P631, int P632, int P633, int P634, int P635, int P636, int P637, int P638, int P639,
        int P640, int P641, int P642, int P643, int P644, int P645, int P646, int P647, int P648, int P649,
        int P650, int P651, int P652, int P653, int P654, int P655, int P656, int P657, int P658, int P659,
        int P660, int P661, int P662, int P663, int P664, int P665, int P666, int P667, int P668, int P669,
        int P670, int P671, int P672, int P673, int P674, int P675, int P676, int P677, int P678, int P679,
        int P680, int P681, int P682, int P683, int P684, int P685, int P686, int P687, int P688, int P689,
        int P690, int P691, int P692, int P693, int P694, int P695, int P696, int P697, int P698, int P699,
        int P700, int P701, int P702, int P703, int P704, int P705, int P706, int P707, int P708, int P709,
        int P710, int P711, int P712, int P713, int P714, int P715, int P716, int P717, int P718, int P719,
        int P720, int P721, int P722, int P723, int P724, int P725, int P726, int P727, int P728, int P729,
        int P730, int P731, int P732, int P733, int P734, int P735, int P736, int P737, int P738, int P739,
        int P740, int P741, int P742, int P743, int P744, int P745, int P746, int P747, int P748, int P749,
        int P750, int P751, int P752, int P753, int P754, int P755, int P756, int P757, int P758, int P759,
        int P760, int P761, int P762, int P763, int P764, int P765, int P766, int P767, int P768, int P769,
        int P770, int P771, int P772, int P773, int P774, int P775, int P776, int P777, int P778, int P779,
        int P780, int P781, int P782, int P783, int P784, int P785, int P786, int P787, int P788, int P789,
        int P790, int P791, int P792, int P793, int P794, int P795, int P796, int P797, int P798, int P799,
        int P800, int P801, int P802, int P803, int P804, int P805, int P806, int P807, int P808, int P809,
        int P810, int P811, int P812, int P813, int P814, int P815, int P816, int P817, int P818, int P819,
        int P820, int P821, int P822, int P823, int P824, int P825, int P826, int P827, int P828, int P829,
        int P830, int P831, int P832, int P833, int P834, int P835, int P836, int P837, int P838, int P839,
        int P840, int P841, int P842, int P843, int P844, int P845, int P846, int P847, int P848, int P849,
        int P850, int P851, int P852, int P853, int P854, int P855, int P856, int P857, int P858, int P859,
        int P860, int P861, int P862, int P863, int P864, int P865, int P866, int P867, int P868, int P869,
        int P870, int P871, int P872, int P873, int P874, int P875, int P876, int P877, int P878, int P879,
        int P880, int P881, int P882, int P883, int P884, int P885, int P886, int P887, int P888, int P889,
        int P890, int P891, int P892, int P893, int P894, int P895, int P896, int P897, int P898, int P899,
        int P900, int P901, int P902, int P903, int P904, int P905, int P906, int P907, int P908, int P909,
        int P910, int P911, int P912, int P913, int P914, int P915, int P916, int P917, int P918, int P919,
        int P920, int P921, int P922, int P923, int P924, int P925, int P926, int P927, int P928, int P929,
        int P930, int P931, int P932, int P933, int P934, int P935, int P936, int P937, int P938, int P939,
        int P940, int P941, int P942, int P943, int P944, int P945, int P946, int P947, int P948, int P949,
        int P950, int P951, int P952, int P953, int P954, int P955, int P956, int P957, int P958, int P959,
        int P960, int P961, int P962, int P963, int P964, int P965, int P966, int P967, int P968, int P969,
        int P970, int P971, int P972, int P973, int P974, int P975, int P976, int P977, int P978, int P979,
        int P980, int P981, int P982, int P983, int P984, int P985, int P986, int P987, int P988, int P989,
        int P990, int P991, int P992, int P993, int P994, int P995, int P996, int P997, int P998, int P999)
    {
        public static ClassWithManyConstructorParameters Create()
        {
            return new ClassWithManyConstructorParameters(
                P000: 000, P001: 001, P002: 002, P003: 003, P004: 004, P005: 005, P006: 006, P007: 007, P008: 008, P009: 009,
                P010: 010, P011: 011, P012: 012, P013: 013, P014: 014, P015: 015, P016: 016, P017: 017, P018: 018, P019: 019,
                P020: 020, P021: 021, P022: 022, P023: 023, P024: 024, P025: 025, P026: 026, P027: 027, P028: 028, P029: 029,
                P030: 030, P031: 031, P032: 032, P033: 033, P034: 034, P035: 035, P036: 036, P037: 037, P038: 038, P039: 039,
                P040: 040, P041: 041, P042: 042, P043: 043, P044: 044, P045: 045, P046: 046, P047: 047, P048: 048, P049: 049,
                P050: 050, P051: 051, P052: 052, P053: 053, P054: 054, P055: 055, P056: 056, P057: 057, P058: 058, P059: 059,
                P060: 060, P061: 061, P062: 062, P063: 063, P064: 064, P065: 065, P066: 066, P067: 067, P068: 068, P069: 069,
                P070: 070, P071: 071, P072: 072, P073: 073, P074: 074, P075: 075, P076: 076, P077: 077, P078: 078, P079: 079,
                P080: 080, P081: 081, P082: 082, P083: 083, P084: 084, P085: 085, P086: 086, P087: 087, P088: 088, P089: 089,
                P090: 090, P091: 091, P092: 092, P093: 093, P094: 094, P095: 095, P096: 096, P097: 097, P098: 098, P099: 099,
                P100: 100, P101: 101, P102: 102, P103: 103, P104: 104, P105: 105, P106: 106, P107: 107, P108: 108, P109: 109,
                P110: 110, P111: 111, P112: 112, P113: 113, P114: 114, P115: 115, P116: 116, P117: 117, P118: 118, P119: 119,
                P120: 120, P121: 121, P122: 122, P123: 123, P124: 124, P125: 125, P126: 126, P127: 127, P128: 128, P129: 129,
                P130: 130, P131: 131, P132: 132, P133: 133, P134: 134, P135: 135, P136: 136, P137: 137, P138: 138, P139: 139,
                P140: 140, P141: 141, P142: 142, P143: 143, P144: 144, P145: 145, P146: 146, P147: 147, P148: 148, P149: 149,
                P150: 150, P151: 151, P152: 152, P153: 153, P154: 154, P155: 155, P156: 156, P157: 157, P158: 158, P159: 159,
                P160: 160, P161: 161, P162: 162, P163: 163, P164: 164, P165: 165, P166: 166, P167: 167, P168: 168, P169: 169,
                P170: 170, P171: 171, P172: 172, P173: 173, P174: 174, P175: 175, P176: 176, P177: 177, P178: 178, P179: 179,
                P180: 180, P181: 181, P182: 182, P183: 183, P184: 184, P185: 185, P186: 186, P187: 187, P188: 188, P189: 189,
                P190: 190, P191: 191, P192: 192, P193: 193, P194: 194, P195: 195, P196: 196, P197: 197, P198: 198, P199: 199,
                P200: 200, P201: 201, P202: 202, P203: 203, P204: 204, P205: 205, P206: 206, P207: 207, P208: 208, P209: 209,
                P210: 210, P211: 211, P212: 212, P213: 213, P214: 214, P215: 215, P216: 216, P217: 217, P218: 218, P219: 219,
                P220: 220, P221: 221, P222: 222, P223: 223, P224: 224, P225: 225, P226: 226, P227: 227, P228: 228, P229: 229,
                P230: 230, P231: 231, P232: 232, P233: 233, P234: 234, P235: 235, P236: 236, P237: 237, P238: 238, P239: 239,
                P240: 240, P241: 241, P242: 242, P243: 243, P244: 244, P245: 245, P246: 246, P247: 247, P248: 248, P249: 249,
                P250: 250, P251: 251, P252: 252, P253: 253, P254: 254, P255: 255, P256: 256, P257: 257, P258: 258, P259: 259,
                P260: 260, P261: 261, P262: 262, P263: 263, P264: 264, P265: 265, P266: 266, P267: 267, P268: 268, P269: 269,
                P270: 270, P271: 271, P272: 272, P273: 273, P274: 274, P275: 275, P276: 276, P277: 277, P278: 278, P279: 279,
                P280: 280, P281: 281, P282: 282, P283: 283, P284: 284, P285: 285, P286: 286, P287: 287, P288: 288, P289: 289,
                P290: 290, P291: 291, P292: 292, P293: 293, P294: 294, P295: 295, P296: 296, P297: 297, P298: 298, P299: 299,
                P300: 300, P301: 301, P302: 302, P303: 303, P304: 304, P305: 305, P306: 306, P307: 307, P308: 308, P309: 309,
                P310: 310, P311: 311, P312: 312, P313: 313, P314: 314, P315: 315, P316: 316, P317: 317, P318: 318, P319: 319,
                P320: 320, P321: 321, P322: 322, P323: 323, P324: 324, P325: 325, P326: 326, P327: 327, P328: 328, P329: 329,
                P330: 330, P331: 331, P332: 332, P333: 333, P334: 334, P335: 335, P336: 336, P337: 337, P338: 338, P339: 339,
                P340: 340, P341: 341, P342: 342, P343: 343, P344: 344, P345: 345, P346: 346, P347: 347, P348: 348, P349: 349,
                P350: 350, P351: 351, P352: 352, P353: 353, P354: 354, P355: 355, P356: 356, P357: 357, P358: 358, P359: 359,
                P360: 360, P361: 361, P362: 362, P363: 363, P364: 364, P365: 365, P366: 366, P367: 367, P368: 368, P369: 369,
                P370: 370, P371: 371, P372: 372, P373: 373, P374: 374, P375: 375, P376: 376, P377: 377, P378: 378, P379: 379,
                P380: 380, P381: 381, P382: 382, P383: 383, P384: 384, P385: 385, P386: 386, P387: 387, P388: 388, P389: 389,
                P390: 390, P391: 391, P392: 392, P393: 393, P394: 394, P395: 395, P396: 396, P397: 397, P398: 398, P399: 399,
                P400: 400, P401: 401, P402: 402, P403: 403, P404: 404, P405: 405, P406: 406, P407: 407, P408: 408, P409: 409,
                P410: 410, P411: 411, P412: 412, P413: 413, P414: 414, P415: 415, P416: 416, P417: 417, P418: 418, P419: 419,
                P420: 420, P421: 421, P422: 422, P423: 423, P424: 424, P425: 425, P426: 426, P427: 427, P428: 428, P429: 429,
                P430: 430, P431: 431, P432: 432, P433: 433, P434: 434, P435: 435, P436: 436, P437: 437, P438: 438, P439: 439,
                P440: 440, P441: 441, P442: 442, P443: 443, P444: 444, P445: 445, P446: 446, P447: 447, P448: 448, P449: 449,
                P450: 450, P451: 451, P452: 452, P453: 453, P454: 454, P455: 455, P456: 456, P457: 457, P458: 458, P459: 459,
                P460: 460, P461: 461, P462: 462, P463: 463, P464: 464, P465: 465, P466: 466, P467: 467, P468: 468, P469: 469,
                P470: 470, P471: 471, P472: 472, P473: 473, P474: 474, P475: 475, P476: 476, P477: 477, P478: 478, P479: 479,
                P480: 480, P481: 481, P482: 482, P483: 483, P484: 484, P485: 485, P486: 486, P487: 487, P488: 488, P489: 489,
                P490: 490, P491: 491, P492: 492, P493: 493, P494: 494, P495: 495, P496: 496, P497: 497, P498: 498, P499: 499,
                P500: 500, P501: 501, P502: 502, P503: 503, P504: 504, P505: 505, P506: 506, P507: 507, P508: 508, P509: 509,
                P510: 510, P511: 511, P512: 512, P513: 513, P514: 514, P515: 515, P516: 516, P517: 517, P518: 518, P519: 519,
                P520: 520, P521: 521, P522: 522, P523: 523, P524: 524, P525: 525, P526: 526, P527: 527, P528: 528, P529: 529,
                P530: 530, P531: 531, P532: 532, P533: 533, P534: 534, P535: 535, P536: 536, P537: 537, P538: 538, P539: 539,
                P540: 540, P541: 541, P542: 542, P543: 543, P544: 544, P545: 545, P546: 546, P547: 547, P548: 548, P549: 549,
                P550: 550, P551: 551, P552: 552, P553: 553, P554: 554, P555: 555, P556: 556, P557: 557, P558: 558, P559: 559,
                P560: 560, P561: 561, P562: 562, P563: 563, P564: 564, P565: 565, P566: 566, P567: 567, P568: 568, P569: 569,
                P570: 570, P571: 571, P572: 572, P573: 573, P574: 574, P575: 575, P576: 576, P577: 577, P578: 578, P579: 579,
                P580: 580, P581: 581, P582: 582, P583: 583, P584: 584, P585: 585, P586: 586, P587: 587, P588: 588, P589: 589,
                P590: 590, P591: 591, P592: 592, P593: 593, P594: 594, P595: 595, P596: 596, P597: 597, P598: 598, P599: 599,
                P600: 600, P601: 601, P602: 602, P603: 603, P604: 604, P605: 605, P606: 606, P607: 607, P608: 608, P609: 609,
                P610: 610, P611: 611, P612: 612, P613: 613, P614: 614, P615: 615, P616: 616, P617: 617, P618: 618, P619: 619,
                P620: 620, P621: 621, P622: 622, P623: 623, P624: 624, P625: 625, P626: 626, P627: 627, P628: 628, P629: 629,
                P630: 630, P631: 631, P632: 632, P633: 633, P634: 634, P635: 635, P636: 636, P637: 637, P638: 638, P639: 639,
                P640: 640, P641: 641, P642: 642, P643: 643, P644: 644, P645: 645, P646: 646, P647: 647, P648: 648, P649: 649,
                P650: 650, P651: 651, P652: 652, P653: 653, P654: 654, P655: 655, P656: 656, P657: 657, P658: 658, P659: 659,
                P660: 660, P661: 661, P662: 662, P663: 663, P664: 664, P665: 665, P666: 666, P667: 667, P668: 668, P669: 669,
                P670: 670, P671: 671, P672: 672, P673: 673, P674: 674, P675: 675, P676: 676, P677: 677, P678: 678, P679: 679,
                P680: 680, P681: 681, P682: 682, P683: 683, P684: 684, P685: 685, P686: 686, P687: 687, P688: 688, P689: 689,
                P690: 690, P691: 691, P692: 692, P693: 693, P694: 694, P695: 695, P696: 696, P697: 697, P698: 698, P699: 699,
                P700: 700, P701: 701, P702: 702, P703: 703, P704: 704, P705: 705, P706: 706, P707: 707, P708: 708, P709: 709,
                P710: 710, P711: 711, P712: 712, P713: 713, P714: 714, P715: 715, P716: 716, P717: 717, P718: 718, P719: 719,
                P720: 720, P721: 721, P722: 722, P723: 723, P724: 724, P725: 725, P726: 726, P727: 727, P728: 728, P729: 729,
                P730: 730, P731: 731, P732: 732, P733: 733, P734: 734, P735: 735, P736: 736, P737: 737, P738: 738, P739: 739,
                P740: 740, P741: 741, P742: 742, P743: 743, P744: 744, P745: 745, P746: 746, P747: 747, P748: 748, P749: 749,
                P750: 750, P751: 751, P752: 752, P753: 753, P754: 754, P755: 755, P756: 756, P757: 757, P758: 758, P759: 759,
                P760: 760, P761: 761, P762: 762, P763: 763, P764: 764, P765: 765, P766: 766, P767: 767, P768: 768, P769: 769,
                P770: 770, P771: 771, P772: 772, P773: 773, P774: 774, P775: 775, P776: 776, P777: 777, P778: 778, P779: 779,
                P780: 780, P781: 781, P782: 782, P783: 783, P784: 784, P785: 785, P786: 786, P787: 787, P788: 788, P789: 789,
                P790: 790, P791: 791, P792: 792, P793: 793, P794: 794, P795: 795, P796: 796, P797: 797, P798: 798, P799: 799,
                P800: 800, P801: 801, P802: 802, P803: 803, P804: 804, P805: 805, P806: 806, P807: 807, P808: 808, P809: 809,
                P810: 810, P811: 811, P812: 812, P813: 813, P814: 814, P815: 815, P816: 816, P817: 817, P818: 818, P819: 819,
                P820: 820, P821: 821, P822: 822, P823: 823, P824: 824, P825: 825, P826: 826, P827: 827, P828: 828, P829: 829,
                P830: 830, P831: 831, P832: 832, P833: 833, P834: 834, P835: 835, P836: 836, P837: 837, P838: 838, P839: 839,
                P840: 840, P841: 841, P842: 842, P843: 843, P844: 844, P845: 845, P846: 846, P847: 847, P848: 848, P849: 849,
                P850: 850, P851: 851, P852: 852, P853: 853, P854: 854, P855: 855, P856: 856, P857: 857, P858: 858, P859: 859,
                P860: 860, P861: 861, P862: 862, P863: 863, P864: 864, P865: 865, P866: 866, P867: 867, P868: 868, P869: 869,
                P870: 870, P871: 871, P872: 872, P873: 873, P874: 874, P875: 875, P876: 876, P877: 877, P878: 878, P879: 879,
                P880: 880, P881: 881, P882: 882, P883: 883, P884: 884, P885: 885, P886: 886, P887: 887, P888: 888, P889: 889,
                P890: 890, P891: 891, P892: 892, P893: 893, P894: 894, P895: 895, P896: 896, P897: 897, P898: 898, P899: 899,
                P900: 900, P901: 901, P902: 902, P903: 903, P904: 904, P905: 905, P906: 906, P907: 907, P908: 908, P909: 909,
                P910: 910, P911: 911, P912: 912, P913: 913, P914: 914, P915: 915, P916: 916, P917: 917, P918: 918, P919: 919,
                P920: 920, P921: 921, P922: 922, P923: 923, P924: 924, P925: 925, P926: 926, P927: 927, P928: 928, P929: 929,
                P930: 930, P931: 931, P932: 932, P933: 933, P934: 934, P935: 935, P936: 936, P937: 937, P938: 938, P939: 939,
                P940: 940, P941: 941, P942: 942, P943: 943, P944: 944, P945: 945, P946: 946, P947: 947, P948: 948, P949: 949,
                P950: 950, P951: 951, P952: 952, P953: 953, P954: 954, P955: 955, P956: 956, P957: 957, P958: 958, P959: 959,
                P960: 960, P961: 961, P962: 962, P963: 963, P964: 964, P965: 965, P966: 966, P967: 967, P968: 968, P969: 969,
                P970: 970, P971: 971, P972: 972, P973: 973, P974: 974, P975: 975, P976: 976, P977: 977, P978: 978, P979: 979,
                P980: 980, P981: 981, P982: 982, P983: 983, P984: 984, P985: 985, P986: 986, P987: 987, P988: 988, P989: 989,
                P990: 990, P991: 991, P992: 992, P993: 993, P994: 994, P995: 995, P996: 996, P997: 997, P998: 998, P999: 999);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            string serialized = JsonSerializer.Serialize(this);
            Assert.Contains(@"""Point2D"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2D"":{", serialized);
            Assert.Contains(@"""Point2DWithExtDataClass"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2DWithExtDataClass"":{", serialized);
            Assert.Contains(@"""Point3DStruct"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint3DStruct"":{", serialized);
            Assert.Contains(@"""Point2DWithExtData"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2DWithExtData"":{", serialized);

            serialized = JsonSerializer.Serialize(Point2D);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint2D);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);

            serialized = JsonSerializer.Serialize(Point2DWithExtDataClass);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint2DWithExtDataClass);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            serialized = JsonSerializer.Serialize(Point3DStruct);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""Z"":3", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint3DStruct);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""Z"":3", serialized);

            serialized = JsonSerializer.Serialize(Point2DWithExtData);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint2DWithExtData);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);
        }

        public void VerifyMinimal()
        {
            string serialized = JsonSerializer.Serialize(ReadOnlyPoint2D);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
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

            string serialized = JsonSerializer.Serialize(this);
            Assert.Contains(@"""Point2D"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2D"":{", serialized);
            Assert.Contains(@"""Point2DWithExtDataClass"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2DWithExtDataClass"":{", serialized);
            Assert.Contains(@"""Point3DStruct"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint3DStruct"":{", serialized);
            Assert.Contains(@"""Point2DWithExtData"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2DWithExtData"":{", serialized);

            serialized = JsonSerializer.Serialize(Point2D);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint2D);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);

            serialized = JsonSerializer.Serialize(Point2DWithExtDataClass);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint2DWithExtDataClass);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            serialized = JsonSerializer.Serialize(Point3DStruct);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""Z"":3", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint3DStruct);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""Z"":3", serialized);

            serialized = JsonSerializer.Serialize(Point2DWithExtData);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            serialized = JsonSerializer.Serialize(ReadOnlyPoint2DWithExtData);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);
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

        public NullArgTester(Point_3D_Struct point3DStruct, ImmutableArray<int> immutableArray, int @int = 50)
        {
            Point3DStruct = point3DStruct;
            ImmutableArray = immutableArray;
            Int = @int;
        }
    }

    public class NullArgTester_Mutable
    {
        public Point_3D_Struct Point3DStruct { get; set; }
        public ImmutableArray<int> ImmutableArray { get; set; }
        public int Int { get; set; }
    }

    public class ClassWithConstructor_SimpleAndComplexParameters : ITestClassWithParameterizedCtor
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

        public ClassWithConstructor_SimpleAndComplexParameters(
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

        public static ClassWithConstructor_SimpleAndComplexParameters GetInstance() =>
            JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(s_json);

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

        public static string Json {
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
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters> MyTuple { get; }

        public Parameterized_Class_With_ComplexTuple(
            Tuple<
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters> myTuple) => MyTuple = myTuple;

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
            Assert.Equal(0, Y); // We don't set parameter default value here.
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
        public int Z { get; set;  }

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
                EndDate = DateTime.UtcNow.AddDays(7),
                EventId = 321,
                EventName = "wonderful name",
                Organization = "Local Animal Shelter",
                StartDate = DateTime.UtcNow.AddDays(-7),
                TimeZone = TimeZoneInfo.Utc.DisplayName,
                VolunteerCount = 15,
                Tasks = Enumerable.Repeat(
                    new MyEventsListerItemTask
                    {
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(1),
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
}

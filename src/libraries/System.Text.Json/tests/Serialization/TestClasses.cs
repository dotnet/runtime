// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public interface ITestClass
    {
        void Initialize();
        void Verify();
    }

    public enum SampleEnum
    {
        One = 1,
        Two = 2
    }

    public class SimpleTestClass : ITestClass
    {
        public short MyInt16 { get; set; }
        public int MyInt32 { get; set; }
        public long MyInt64 { get; set; }
        public ushort MyUInt16 { get; set; }
        public uint MyUInt32 { get; set; }
        public ulong MyUInt64 { get; set; }
        public byte MyByte { get; set; }
        public sbyte MySByte { get; set; }
        public char MyChar { get; set; }
        public string MyString { get; set; }
        public decimal MyDecimal { get; set; }
        public bool MyBooleanTrue { get; set; }
        public bool MyBooleanFalse { get; set; }
        public float MySingle { get; set; }
        public double MyDouble { get; set; }
        public DateTime MyDateTime { get; set; }
        public SampleEnum MyEnum { get; set; }
        public short[] MyInt16Array{ get; set; }
        public int[] MyInt32Array{ get; set; }
        public long[] MyInt64Array{ get; set; }
        public ushort[] MyUInt16Array{ get; set; }
        public uint[] MyUInt32Array{ get; set; }
        public ulong[] MyUInt64Array{ get; set; }
        public byte[] MyByteArray{ get; set; }
        public sbyte[] MySByteArray{ get; set; }
        public char[] MyCharArray{ get; set; }
        public string[] MyStringArray{ get; set; }
        public decimal[] MyDecimalArray{ get; set; }
        public bool[] MyBooleanTrueArray{ get; set; }
        public bool[] MyBooleanFalseArray{ get; set; }
        public float[] MySingleArray{ get; set; }
        public double[] MyDoubleArray{ get; set; }
        public DateTime[] MyDateTimeArray{ get; set; }
        public SampleEnum[] MyEnumArray{ get; set; }

        public static readonly string s_json =
                @"{" +
                @"""MyInt16"" : 1," +
                @"""MyInt32"" : 2," +
                @"""MyInt64"" : 3," +
                @"""MyUInt16"" : 4," +
                @"""MyUInt32"" : 5," +
                @"""MyUInt64"" : 6," +
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
                @"""MyEnum"" : 2," + // int by default
                @"""MyInt16Array"" : [1]," +
                @"""MyInt32Array"" : [2]," +
                @"""MyInt64Array"" : [3]," +
                @"""MyUInt16Array"" : [4]," +
                @"""MyUInt32Array"" : [5]," +
                @"""MyUInt64Array"" : [6]," +
                @"""MyByteArray"" : [7]," +
                @"""MySByteArray"" : [8]," +
                @"""MyCharArray"" : [""a""]," +
                @"""MyStringArray"" : [""Hello""]," +
                @"""MyBooleanTrueArray"" : [true]," +
                @"""MyBooleanFalseArray"" : [false]," +
                @"""MySingleArray"" : [1.1]," +
                @"""MyDoubleArray"" : [2.2]," +
                @"""MyDecimalArray"" : [3.3]," +
                @"""MyDateTimeArray"" : [""2019-01-30T12:01:02.0000000Z""]," +
                @"""MyEnumArray"" : [2]" + // int by default
                @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyInt16 = 1;
            MyInt32 = 2;
            MyInt64 = 3;
            MyUInt16 = 4;
            MyUInt32 = 5;
            MyUInt64 = 6;
            MyByte = 7;
            MySByte = 8;
            MyChar = 'a';
            MyString = "Hello";
            MyBooleanTrue = true;
            MyBooleanFalse = false;
            MySingle = 1.1f;
            MyDouble = 2.2d;
            MyDecimal = 3.3m;
            MyDateTime = new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc);
            MyEnum = SampleEnum.Two;

            MyInt16Array = new short[] { 1 };
            MyInt32Array = new int[] { 2 };
            MyInt64Array = new long[] { 3 };
            MyUInt16Array = new ushort[] { 4 };
            MyUInt32Array = new uint[] { 5 };
            MyUInt64Array = new ulong[] { 6 };
            MyByteArray = new byte[] { 7 };
            MySByteArray = new sbyte[] { 8 };
            MyCharArray = new char[] { 'a' };
            MyStringArray = new string[] { "Hello" };
            MyBooleanTrueArray = new bool[] { true };
            MyBooleanFalseArray = new bool[] { false };
            MySingleArray = new float[] { 1.1f };
            MyDoubleArray = new double[] { 2.2d };
            MyDecimalArray = new decimal[] { 3.3m };
            MyDateTimeArray = new DateTime[] { new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc) };
            MyEnumArray = new SampleEnum[] { SampleEnum.Two };
        }

        public void Verify()
        {
            Assert.Equal((short)1, MyInt16);
            Assert.Equal((int)2, MyInt32);
            Assert.Equal((long)3, MyInt64);
            Assert.Equal((ushort)4, MyUInt16);
            Assert.Equal((uint)5, MyUInt32);
            Assert.Equal((ulong)6, MyUInt64);
            Assert.Equal((byte)7, MyByte);
            Assert.Equal((sbyte)8, MySByte);
            Assert.Equal('a', MyChar);
            Assert.Equal("Hello", MyString);
            Assert.Equal(3.3m, MyDecimal);
            Assert.Equal(false, MyBooleanFalse);
            Assert.Equal(true, MyBooleanTrue);
            Assert.Equal(1.1f, MySingle);
            Assert.Equal(2.2d, MyDouble);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTime);
            Assert.Equal(SampleEnum.Two, MyEnum);

            Assert.Equal((short)1, MyInt16Array[0]);
            Assert.Equal((int)2, MyInt32Array[0]);
            Assert.Equal((long)3, MyInt64Array[0]);
            Assert.Equal((ushort)4, MyUInt16Array[0]);
            Assert.Equal((uint)5, MyUInt32Array[0]);
            Assert.Equal((ulong)6, MyUInt64Array[0]);
            Assert.Equal((byte)7, MyByteArray[0]);
            Assert.Equal((sbyte)8, MySByteArray[0]);
            Assert.Equal('a', MyCharArray[0]);
            Assert.Equal("Hello", MyStringArray[0]);
            Assert.Equal(3.3m, MyDecimalArray[0]);
            Assert.Equal(false, MyBooleanFalseArray[0]);
            Assert.Equal(true, MyBooleanTrueArray[0]);
            Assert.Equal(1.1f, MySingleArray[0]);
            Assert.Equal(2.2d, MyDoubleArray[0]);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTimeArray[0]);
            Assert.Equal(SampleEnum.Two, MyEnumArray[0]);
        }
    }

    public abstract class SimpleBaseClassWithNullables
    {
        public short? MyInt16 { get; set; }
        public int? MyInt32 { get; set; }
        public long? MyInt64 { get; set; }
        public ushort? MyUInt16 { get; set; }
        public uint? MyUInt32 { get; set; }
        public ulong? MyUInt64 { get; set; }
        public byte? MyByte { get; set; }
        public sbyte? MySByte { get; set; }
        public char? MyChar { get; set; }
        public decimal? MyDecimal { get; set; }
        public bool? MyBooleanTrue { get; set; }
        public bool? MyBooleanFalse { get; set; }
        public float? MySingle { get; set; }
        public double? MyDouble { get; set; }
        public DateTime? MyDateTime { get; set; }
        public SampleEnum? MyEnum { get; set; }
    }

    public class SimpleTestClassWithNulls : SimpleBaseClassWithNullables, ITestClass
    {
        public void Initialize()
        {
        }

        public void Verify()
        {
            Assert.Null(MyInt16);
            Assert.Null(MyInt32);
            Assert.Null(MyInt64);
            Assert.Null(MyUInt16);
            Assert.Null(MyUInt32);
            Assert.Null(MyUInt64);
            Assert.Null(MyByte);
            Assert.Null(MySByte);
            Assert.Null(MyChar);
            Assert.Null(MyDecimal);
            Assert.Null(MyBooleanFalse);
            Assert.Null(MyBooleanTrue);
            Assert.Null(MySingle);
            Assert.Null(MyDouble);
            Assert.Null(MyDateTime);
            Assert.Null(MyEnum);
        }
        public static readonly string s_json =
                @"{" +
                @"""MyInt16"" : null," +
                @"""MyInt32"" : null," +
                @"""MyInt64"" : null," +
                @"""MyUInt16"" : null," +
                @"""MyUInt32"" : null," +
                @"""MyUInt64"" : null," +
                @"""MyByte"" : null," +
                @"""MySByte"" : null," +
                @"""MyChar"" : null," +
                @"""MyBooleanTrue"" : null," +
                @"""MyBooleanFalse"" : null," +
                @"""MySingle"" : null," +
                @"""MyDouble"" : null," +
                @"""MyDecimal"" : null," +
                @"""MyDateTime"" : null," +
                @"""MyEnum"" : null" +
                @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);
    }

    public class SimpleTestClassWithNullables : SimpleBaseClassWithNullables, ITestClass
    {
        public static readonly string s_json =
                @"{" +
                @"""MyInt16"" : 1," +
                @"""MyInt32"" : 2," +
                @"""MyInt64"" : 3," +
                @"""MyUInt16"" : 4," +
                @"""MyUInt32"" : 5," +
                @"""MyUInt64"" : 6," +
                @"""MyByte"" : 7," +
                @"""MySByte"" : 8," +
                @"""MyChar"" : ""a""," +
                @"""MyBooleanTrue"" : true," +
                @"""MyBooleanFalse"" : false," +
                @"""MySingle"" : 1.1," +
                @"""MyDouble"" : 2.2," +
                @"""MyDecimal"" : 3.3," +
                @"""MyDateTime"" : ""2019-01-30T12:01:02.0000000Z""," +
                @"""MyEnum"" : 2" + // int by default
                @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyInt16 = 1;
            MyInt32 = 2;
            MyInt64 = 3;
            MyUInt16 = 4;
            MyUInt32 = 5;
            MyUInt64 = 6;
            MyByte = 7;
            MySByte = 8;
            MyChar = 'a';
            MyBooleanTrue = true;
            MyBooleanFalse = false;
            MySingle = 1.1f;
            MyDouble = 2.2d;
            MyDecimal = 3.3m;
            MyDateTime = new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc);
            MyEnum = SampleEnum.Two;
        }

        public void Verify()
        {
            Assert.Equal(MyInt16, (short)1);
            Assert.Equal(MyInt32, (int)2);
            Assert.Equal(MyInt64, (long)3);
            Assert.Equal(MyUInt16, (ushort)4);
            Assert.Equal(MyUInt32, (uint)5);
            Assert.Equal(MyUInt64, (ulong)6);
            Assert.Equal(MyByte, (byte)7);
            Assert.Equal(MySByte, (sbyte)8);
            Assert.Equal(MyChar, 'a');
            Assert.Equal(MyDecimal, 3.3m);
            Assert.Equal(MyBooleanFalse, false);
            Assert.Equal(MyBooleanTrue, true);
            Assert.Equal(MySingle, 1.1f);
            Assert.Equal(MyDouble, 2.2d);
            Assert.Equal(MyDateTime, new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc));
            Assert.Equal(MyEnum, SampleEnum.Two);
        }
    }

    public class TestClassWithNull
    {
        public string MyString { get; set; }
        public static readonly string s_json =
                @"{" +
                @"""MyString"" : null" +
                @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyString = null;
        }

        public void Verify()
        {
            Assert.Equal(MyString, null);
        }
    }

    public class TestClassWithNullButInitialized
    {
        public string MyString { get; set; } = "Hello";
        public int? MyInt { get; set; } = 1;
        public static readonly string s_json =
                @"{" +
                @"""MyString"" : null," +
                @"""MyInt"" : null" +
                @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);
    }

    public class TestClassWithNestedObjectInner : ITestClass
    {
        public SimpleTestClass MyData { get; set; }

        public static readonly string s_json =
            @"{" +
                @"""MyData"":" + SimpleTestClass.s_json +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyData = new SimpleTestClass();
            MyData.Initialize();
        }

        public void Verify()
        {
            Assert.NotNull(MyData);
            MyData.Verify();
        }
    }

    public class TestClassWithNestedObjectOuter : ITestClass
    {
        public TestClassWithNestedObjectInner MyData { get; set; }

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(
            @"{" +
                @"""MyData"":" + TestClassWithNestedObjectInner.s_json +
            @"}");

        public void Initialize()
        {
            MyData = new TestClassWithNestedObjectInner();
            MyData.Initialize();
        }

        public void Verify()
        {
            Assert.NotNull(MyData);
            MyData.Verify();
        }
    }

    public class TestClassWithObjectList : ITestClass
    {
        public List<SimpleTestClass> MyData { get; set; }

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(
            @"{" +
                @"""MyData"":[" +
                    SimpleTestClass.s_json + "," +
                    SimpleTestClass.s_json +
                @"]" +
            @"}");

        public void Initialize()
        {
            MyData = new List<SimpleTestClass>();

            {
                SimpleTestClass obj = new SimpleTestClass();
                obj.Initialize();
                MyData.Add(obj);
            }

            {
                SimpleTestClass obj = new SimpleTestClass();
                obj.Initialize();
                MyData.Add(obj);
            }
        }

        public void Verify()
        {
            Assert.Equal(2, MyData.Count);
            MyData[0].Verify();
            MyData[1].Verify();
        }
    }

    public class TestClassWithObjectArray : ITestClass
    {
        public SimpleTestClass[] MyData { get; set; }

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(
            @"{" +
                @"""MyData"":[" +
                    SimpleTestClass.s_json + "," +
                    SimpleTestClass.s_json +
                @"]" +
            @"}");

        public void Initialize()
        {
            SimpleTestClass obj1 = new SimpleTestClass();
            obj1.Initialize();

            SimpleTestClass obj2 = new SimpleTestClass();
            obj2.Initialize();

            MyData = new SimpleTestClass[2] { obj1, obj2 };
        }

        public void Verify()
        {
            MyData[0].Verify();
            MyData[1].Verify();
            Assert.Equal(2, MyData.Length);
        }
    }

    public class TestClassWithStringArray : ITestClass
    {
        public string[] MyData { get; set; }

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(
            @"{" +
                @"""MyData"":[" +
                    @"""Hello""," +
                    @"""World""" +
                @"]" +
            @"}");

        public void Initialize()
        {
            MyData = new string[] { "Hello", "World" };
        }

        public void Verify()
        {
            Assert.Equal("Hello", MyData[0]);
            Assert.Equal("World", MyData[1]);
            Assert.Equal(2, MyData.Length);
        }
    }

    public class TestClassWithCycle
    {
        public TestClassWithCycle Parent { get; set; }

        public void Initialize()
        {
            Parent = this;
        }
    }

    public class TestClassWithGenericList : ITestClass
    {
        public List<string> MyData { get; set; }

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(
            @"{" +
                @"""MyData"":[" +
                    @"""Hello""," +
                    @"""World""" +
                @"]" +
            @"}");

        public void Initialize()
        {
            MyData = new List<string>
            {
                "Hello",
                "World"
            };
            Assert.Equal(2, MyData.Count);
        }

        public void Verify()
        {
            Assert.Equal("Hello", MyData[0]);
            Assert.Equal("World", MyData[1]);
            Assert.Equal(2, MyData.Count);
        }
    }

    public class SimpleDerivedTestClass : SimpleTestClass
    {
    }

    public class OverridePropertyNameRuntime_TestClass
    {
        public Int16 MyInt16 { get; set; }

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(
            @"{" +
            @"""blah"" : 1" +
            @"}"
        );
    }

    public class LargeDataTestClass : ITestClass
    {
        public List<LargeDataChildTestClass> Children { get; set; } = new List<LargeDataChildTestClass>();
        public const int ChildrenCount = 10;

        public string MyString { get; set; }
        public const int MyStringLength = 1000;

        public void Initialize()
        {
            MyString = new string('1', MyStringLength);

            for (int i = 0; i < ChildrenCount; i++)
            {
                var child = new LargeDataChildTestClass
                {
                    MyString = new string('2', LargeDataChildTestClass.MyStringLength),
                    MyStringArray = new string[LargeDataChildTestClass.MyStringArrayArrayCount]
                };
                for (int j = 0; j < child.MyStringArray.Length; j++)
                {
                    child.MyStringArray[j] = new string('3', LargeDataChildTestClass.MyStringArrayElementStringLength);
                }

                Children.Add(child);
            }
        }

        public void Verify()
        {
            Assert.Equal(MyStringLength, MyString.Length);
            Assert.Equal('1', MyString[0]);
            Assert.Equal('1', MyString[MyStringLength - 1]);

            Assert.Equal(ChildrenCount, Children.Count);
            for (int i = 0; i < ChildrenCount; i++)
            {
                LargeDataChildTestClass child = Children[i];
                Assert.Equal(LargeDataChildTestClass.MyStringLength, child.MyString.Length);
                Assert.Equal('2', child.MyString[0]);
                Assert.Equal('2', child.MyString[LargeDataChildTestClass.MyStringLength - 1]);

                Assert.Equal(LargeDataChildTestClass.MyStringArrayArrayCount, child.MyStringArray.Length);
                for (int j = 0; j < LargeDataChildTestClass.MyStringArrayArrayCount; j++)
                {
                    Assert.Equal('3', child.MyStringArray[j][0]);
                    Assert.Equal('3', child.MyStringArray[j][LargeDataChildTestClass.MyStringArrayElementStringLength - 1]);
                }
            }
        }
    }

    public class LargeDataChildTestClass
    {
        public const int MyStringLength = 2000;
        public string MyString { get; set; }

        public string[] MyStringArray { get; set; }
        public const int MyStringArrayArrayCount = 1000;
        public const int MyStringArrayElementStringLength = 50;
    }

    public class EmptyClass { }

    public class BasicJson : ITestClass
    {
        public int age { get; set; }
        public string first { get; set; }
        public string last { get; set; }
        public List<string> phoneNumbers { get; set; }
        public BasicJsonAddress address { get; set; }

        public void Initialize()
        {
            age = 30;
            first = "John";
            last = "Smith";
            phoneNumbers = new List<string> { "425-000-0000", "425-000-0001" };
            address = new BasicJsonAddress
            {
                street = "1 Microsoft Way",
                city = "Redmond",
                zip = 98052
            };
        }

        public void Verify()
        {
            Assert.Equal(30, age);
            Assert.Equal("John", first);
            Assert.Equal("Smith", last);
            Assert.Equal("425-000-0000", phoneNumbers[0]);
            Assert.Equal("425-000-0001", phoneNumbers[1]);
            Assert.Equal("1 Microsoft Way", address.street);
            Assert.Equal("Redmond", address.city);
            Assert.Equal(98052, address.zip);
        }

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(
            "{" +
                @"""age"" : 30," +
                @"""first"" : ""John""," +
                @"""last"" : ""Smith""," +
                @"""phoneNumbers"" : [" +
                    @"""425-000-0000""," +
                    @"""425-000-0001""" +
                @"]," +
                @"""address"" : {" +
                    @"""street"" : ""1 Microsoft Way""," +
                    @"""city"" : ""Redmond""," +
                    @"""zip"" : 98052" +
                "}" +
            "}");
    }

    public class BasicJsonAddress
    {
        public string street { get; set; }
        public string city { get; set; }
        public int zip { get; set; }
    }
}

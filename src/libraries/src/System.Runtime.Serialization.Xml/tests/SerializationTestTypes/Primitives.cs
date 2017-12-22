// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;

namespace SerializationTestTypes
{
    [DataContract]
    public struct VT
    {
        [DataMember]
        public int b;

        public VT(int v)
        {
            b = v;
        }
    }

    public struct NotSer
    {
        public int a;
    }

    [DataContract]
    public enum MyEnum1 : byte
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    [Flags]
    public enum Seasons1 : byte
    {
        None = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 4,
        Spring = 8,
        All = Summer | Autumn | Winter | Spring,
    }

    [Flags]
    public enum Seasons2 : sbyte
    {
        None = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 4,
        Spring = 8,
        All = Summer | Autumn | Winter | Spring,
    }

    public struct MyStruct
    {
        public int value;
        public string globName;

        public MyStruct(bool init)
        {
            value = 10;
            globName = "\uFEA5\uFEA3\uFEA8\uFEAA\uFEA3\uFEBB\uFEC2\uFEC3";
        }
    }

    [DataContract]
    public class EnumStructContainer
    {
        [DataMember]
        public object p1 = new VT(10);

        [DataMember]
        public object p2 = new NotSer();

        [DataMember]
        public object[] enumArrayData = new object[] { MyEnum1.red, MyEnum1.black, MyEnum1.blue, Seasons1.Autumn, Seasons2.Spring };

        [DataMember]
        public object p3 = new MyStruct();
    }

    [DataContract]
    public class Person
    {
        public Person(string variation)
        {
            Age = 6;
            Name = "smith";
        }

        public Person()
        {
        }

        [DataMember]
        public int Age;

        [DataMember]
        public string Name;
    }

    [DataContract]
    public class CharClass
    {
        public CharClass()
        {
            c = default(Char);
            c1 = char.MaxValue;
            c2 = char.MinValue;
            c3 = 'c';
        }

        [DataMember]
        public char c;

        [DataMember]
        public char c1;

        [DataMember]
        public char c2;

        [DataMember]
        public char c3;
    }

    [DataContract]
    [KnownType(typeof(MyEnum1))]
    [KnownType(typeof(PublicDCStruct))]
    public class AllTypes
    {
        public AllTypes()
        {
            a = false;
            b = Byte.MaxValue;
            c = Byte.MinValue;
            e = Decimal.MaxValue;
            f = Decimal.MinusOne;
            g = Decimal.MinValue;
            h = Decimal.One;
            i = Decimal.Zero;
            j = default(Decimal);
            k = default(Double);
            l = Double.Epsilon;
            m = Double.MaxValue;
            n = Double.MinValue;
            o = Double.NaN;
            p = Double.NegativeInfinity;
            q = Double.PositiveInfinity;
            r = default(Single);
            s = Single.Epsilon;
            t = Single.MinValue;
            u = Single.MaxValue;
            v = Single.NaN;
            w = Single.NegativeInfinity;
            x = Single.PositiveInfinity;
            y = default(Int32);
            z = Int32.MaxValue;
            z1 = Int32.MinValue;
            z2 = default(Int64);
            z3 = Int64.MaxValue;
            z4 = Int64.MinValue;
            z5 = new Object();
            z6 = default(SByte);
            z7 = SByte.MaxValue;
            z8 = SByte.MinValue;
            z9 = default(Int16);
            z91 = Int16.MaxValue;
            z92 = Int16.MinValue;
            z93 = "abc";
            z94 = default(UInt16);
            z95 = UInt16.MaxValue;
            z96 = UInt16.MinValue;
            z97 = default(UInt32);
            z98 = UInt32.MaxValue;
            z99 = UInt32.MinValue;
            z990 = default(UInt64);
            z991 = UInt64.MaxValue;
            z992 = UInt64.MinValue;
        }

        [DataMember]
        public Boolean a;

        [DataMember]
        public Byte b;

        [DataMember]
        public Byte c;

        [DataMember]
        public Char d = Char.MaxValue;

        [DataMember]
        public DateTime f5 = new DateTime();

        [DataMember]
        public Guid guidData = new Guid("5642b5d2-87c3-a724-2390-997062f3f7a2");

        [DataMember]
        public String strData;

        [DataMember]
        public Decimal e;

        [DataMember]
        public Decimal f;

        [DataMember]
        public Decimal g;

        [DataMember]
        public Decimal h;

        [DataMember]
        public Decimal i;

        [DataMember]
        public Decimal j;

        [DataMember]
        public Double k;

        [DataMember]
        public Double l;

        [DataMember]
        public Double m;

        [DataMember]
        public Double n;

        [DataMember]
        public Double o;

        [DataMember]
        public Double p;

        [DataMember]
        public Double q;

        [DataMember]
        public Single r;

        [DataMember]
        public Single s;

        [DataMember]
        public Single t;

        [DataMember]
        public Single u;

        [DataMember]
        public Single v;

        [DataMember]
        public Single w;

        [DataMember]
        public Single x;

        [DataMember]
        public Int32 y;

        [DataMember]
        public Int32 z;

        [DataMember]
        public Int32 z1;

        [DataMember]
        public Int64 z2;

        [DataMember]
        public Int64 z3;

        [DataMember]
        public Int64 z4;

        [DataMember]
        public Object z5;

        [DataMember]
        public SByte z6;

        [DataMember]
        public SByte z7;

        [DataMember]
        public SByte z8;

        [DataMember]
        public Int16 z9;

        [DataMember]
        public Int16 z91;

        [DataMember]
        public Int16 z92;

        [DataMember]
        public String z93;

        [DataMember]
        public UInt16 z94;

        [DataMember]
        public UInt16 z95;

        [DataMember]
        public UInt16 z96;

        [DataMember]
        public UInt32 z97;

        [DataMember]
        public UInt32 z98;

        [DataMember]
        public UInt32 z99;

        [DataMember]
        public UInt64 z990;

        [DataMember]
        public UInt64 z991;

        [DataMember]
        public UInt64 z992;

        [DataMember]
        [IgnoreMember]
        public MyEnum1[] enumArrayData = new MyEnum1[] { MyEnum1.red };

        [DataMember]
        [IgnoreMember]
        public XmlQualifiedName xmlQualifiedName = new XmlQualifiedName("WCF", "http://www.microsoft.com");

        [DataMember]
        [IgnoreMember]
        public ValueType timeSpan = TimeSpan.MaxValue;

        [DataMember]
        [IgnoreMember]
        public object obj = new object();

        [DataMember]
        [IgnoreMember]
        public Uri uri = new Uri("http://www.microsoft.com");

        [DataMember]
        [IgnoreMember]
        public Enum enumBase1 = MyEnum1.red;

        [DataMember]
        [IgnoreMember]
        public Array array1 = new object[] { new object(), new object(), new object() };

        [DataMember]
        [IgnoreMember]
        public ValueType valType = new PublicDCStruct(true);

        [DataMember]
        [IgnoreMember]
        public Nullable<DateTimeOffset> nDTO = DateTimeOffset.MaxValue;

        [DataMember]
        [IgnoreMember]
        public List<DateTimeOffset> lDTO = new List<DateTimeOffset>();
    }

    [DataContract]
    [KnownType(typeof(MyEnum1))]
    [KnownType(typeof(PublicDCStruct))]
    public class AllTypes2
    {
        public AllTypes2()
        {
            a = false;
            b = Byte.MaxValue;
            c = Byte.MinValue;
            e = Decimal.MaxValue;
            f = Decimal.MinusOne;
            g = Decimal.MinValue;
            h = Decimal.One;
            i = Decimal.Zero;
            j = default(Decimal);
            k = default(Double);
            l = Double.Epsilon;
            m = Double.MaxValue;
            n = Double.MinValue;
            o = Double.NaN;
            p = Double.NegativeInfinity;
            q = Double.PositiveInfinity;
            r = default(Single);
            s = Single.Epsilon;
            t = Single.MinValue;
            u = Single.MaxValue;
            v = Single.NaN;
            w = Single.NegativeInfinity;
            x = Single.PositiveInfinity;
            y = default(Int32);
            z = Int32.MaxValue;
            z1 = Int32.MinValue;
            z2 = default(Int64);
            z3 = Int64.MaxValue;
            z4 = Int64.MinValue;
            z5 = new Object();
            z6 = default(SByte);
            z7 = SByte.MaxValue;
            z8 = SByte.MinValue;
            z9 = default(Int16);
            z91 = Int16.MaxValue;
            z92 = Int16.MinValue;
            z93 = "abc";
            z94 = default(UInt16);
            z95 = UInt16.MaxValue;
            z96 = UInt16.MinValue;
            z97 = default(UInt32);
            z98 = UInt32.MaxValue;
            z99 = UInt32.MinValue;
            z990 = default(UInt64);
            z991 = UInt64.MaxValue;
            z992 = UInt64.MinValue;
        }

        [DataMember]
        public Boolean a;

        [DataMember]
        public Byte b;

        [DataMember]
        public Byte c;

        [DataMember]
        public Char d = Char.MaxValue;

        [DataMember]
        public DateTime f5 = new DateTime();

        [DataMember]
        public Guid guidData = new Guid("cac76333-577f-7e1f-0389-789b0d97f395");

        [DataMember]
        public String strData;

        [DataMember]
        public Decimal e;

        [DataMember]
        public Decimal f;

        [DataMember]
        public Decimal g;

        [DataMember]
        public Decimal h;

        [DataMember]
        public Decimal i;

        [DataMember]
        public Decimal j;

        [DataMember]
        public Double k;

        [DataMember]
        public Double l;

        [DataMember]
        public Double m;

        [DataMember]
        public Double n;

        [DataMember]
        public Double o;

        [DataMember]
        public Double p;

        [DataMember]
        public Double q;

        [DataMember]
        public Single r;

        [DataMember]
        public Single s;

        [DataMember]
        public Single t;

        [DataMember]
        public Single u;

        [DataMember]
        public Single v;

        [DataMember]
        public Single w;

        [DataMember]
        public Single x;

        [DataMember]
        public Int32 y;

        [DataMember]
        public Int32 z;

        [DataMember]
        public Int32 z1;

        [DataMember]
        public Int64 z2;

        [DataMember]
        public Int64 z3;

        [DataMember]
        public Int64 z4;

        [DataMember]
        public Object z5;

        [DataMember]
        public SByte z6;

        [DataMember]
        public SByte z7;

        [DataMember]
        public SByte z8;

        [DataMember]
        public Int16 z9;

        [DataMember]
        public Int16 z91;

        [DataMember]
        public Int16 z92;

        [DataMember]
        public String z93;

        [DataMember]
        public UInt16 z94;

        [DataMember]
        public UInt16 z95;

        [DataMember]
        public UInt16 z96;

        [DataMember]
        public UInt32 z97;

        [DataMember]
        public UInt32 z98;

        [DataMember]
        public UInt32 z99;

        [DataMember]
        public UInt64 z990;

        [DataMember]
        public UInt64 z991;

        [DataMember]
        public UInt64 z992;

        [DataMember]
        [IgnoreMember]
        public MyEnum1[] enumArrayData = new MyEnum1[] { MyEnum1.red };

        [DataMember]
        [IgnoreMember]
        public XmlQualifiedName xmlQualifiedName = new XmlQualifiedName("WCF", "http://www.microsoft.com");

        [DataMember]
        [IgnoreMember]
        public TimeSpan timeSpan = TimeSpan.MaxValue;

        [DataMember]
        [IgnoreMember]
        public object obj = new object();

        [DataMember]
        [IgnoreMember]
        public Uri uri = new Uri("http://www.microsoft.com");

        [DataMember]
        [IgnoreMember]
        public Enum enumBase1 = MyEnum1.red;

        [DataMember]
        [IgnoreMember]
        public Array array1 = new object[] { new object(), new object(), new object() };

        [DataMember]
        [IgnoreMember]
        public ValueType valType = new PublicDCStruct(true);

        [DataMember]
        [IgnoreMember]
        public Nullable<DateTimeOffset> nDTO = DateTimeOffset.MaxValue;
    }

    [DataContract]
    public class DictContainer
    {
        [DataMember]
        public Dictionary<byte[], byte[]> dictionaryData = new Dictionary<byte[], byte[]>();

        public DictContainer()
        {
            dictionaryData.Add(new Guid("ec1f7b4b-c2d1-4c6e-95b6-060a111b0afd").ToByteArray(), new Guid("9831dc90-ca4c-4db2-9335-58a1025ecf29").ToByteArray());
            dictionaryData.Add(new Guid("5e689847-1a10-4f72-aaae-19b247cd0878").ToByteArray(), new Guid("e7af8691-43d5-49e7-8775-1b0126bd943c").ToByteArray());
            dictionaryData.Add(new Guid("711168dd-4a00-4de5-9f3e-38ddfbda0144").ToByteArray(), new Guid("2685b4af-09b6-4a56-81db-95231a3d0276").ToByteArray());
        }
    }

    [DataContract]
    public class ListContainer
    {
        [DataMember]
        public List<string> listData = new List<string>();
        public ListContainer()
        {
            listData.Add("TestData");
        }
    }

    [DataContract]
    [KnownType(typeof(string))]
    public class ArrayContainer
    {
        [DataMember]
        public ArrayList listData = new ArrayList();
        public ArrayContainer(bool init)
        {
            listData.Add("TestData");
            listData.Add("Test");
            listData.Add(new Guid("c0a7310f-f369-481e-a990-39b121eae513"));
        }
    }

    [DataContract]
    [KnownType(typeof(MyPrivateEnum1[]))]
    public class EnumContainer1
    {
        [DataMember]
        public object myPrivateEnum1 = new MyPrivateEnum1[] { MyPrivateEnum1.red };
    }

    [DataContract]
    [KnownType(typeof(MyPrivateEnum2[]))]
    public class EnumContainer2
    {
        [DataMember]
        public object myPrivateEnum2 = new MyPrivateEnum2[] { MyPrivateEnum2.red };
    }

    [DataContract]
    [KnownType(typeof(MyPrivateEnum3[]))]
    public class EnumContainer3
    {
        [DataMember]
        public object myPrivateEnum3 = new MyPrivateEnum3[] { MyPrivateEnum3.red };
    }

    [DataContract]
    public class WithStatic
    {
        public WithStatic()
        {
            sstr = "static string";
            str = "instance string";
        }

        [DataMember]
        public static string sstr;

        [DataMember]
        public string str;
    }

    [DataContract]
    public class PrivateCstor
    {
        private PrivateCstor()
        {
            c = 22;
            a = 10;
            b = "string b";
            c = 11;
        }

        public PrivateCstor(int i)
        {
            c = i;
        }

        private PrivateCstor(StreamingContext sc)
        {
            c = 33;
        }

        [DataMember]
        public int a;

        [DataMember]
        public string b;

        [DataMember]
        public int c;
    }

    [DataContract]
    public class DerivedFromPriC : PrivateCstor
    {
        public DerivedFromPriC() : base(int.MaxValue) { }
        public DerivedFromPriC(int d)
            : base(d)
        {
            this.d = d;
        }
        [DataMember]
        public int d;
    }

    [DataContract]
    public class EmptyDC
    {
        public EmptyDC()
        {
            a = 10;
        }

        [DataMember]
        public int a;

        public int A
        {
            set { a = value; }
            get { return a; }
        }
    }

    [DataContract(Name = "Enum")]
    public enum MyEnum
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    [DataContract(Name = "Enum1")]
    internal enum MyPrivateEnum1
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    internal enum MyPrivateEnum2
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    internal enum MyPrivateEnum3
    {
        red,
        blue,
        black
    }

    internal interface SomeProperties
    {
        int A { set; get; }

        String B { set; get; }
    }

    [DataContract]
    public class Base : SomeProperties
    {
        private int _a;
        private string _b;

        [DataMember]
        public virtual int A
        {
            set { _a = value; }
            get { return _a; }
        }

        [DataMember]
        public virtual string B
        {
            set { _b = value; }
            get { return _b; }
        }
    }

    [DataContract]
    public class Derived : Base
    {
        private int _a;
        private string _b;

        [DataMember]
        override public int A
        {
            set { _a = value; }
            get { return _a; }
        }

        [DataMember]
        override public string B
        {
            set { _b = value; }
            get { return _b; }
        }
    }

    [DataContract]
    public enum MyEnum2 : sbyte
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    [DataContract]
    public enum MyEnum3 : short
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    [DataContract]
    public enum MyEnum4 : ushort
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    [DataContract]
    public enum MyEnum7 : long
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    [DataContract]
    public enum MyEnum8 : ulong
    {
        [EnumMember]
        red,
        [EnumMember]
        blue,
        [EnumMember]
        black
    }

    public class SeasonsEnumContainer
    {
        [IgnoreMember]
        public Seasons1 member1 = Seasons1.Autumn;
        [IgnoreMember]
        public Seasons2 member2 = Seasons2.Spring;
        [IgnoreMember]
        public Seasons3 member3 = Seasons3.Winter;
    }

    [Flags]
    public enum Seasons3 : short
    {
        None = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 4,
        Spring = 8,
        All = Summer | Autumn | Winter | Spring,
    }

    [DataContract]
    public class list
    {
        [DataMember]
        public int value;

        [DataMember]
        public list next;
    }

    [DataContract]
    public class Arrays
    {
        public Arrays()
        {
            a1 = new int[] { };
            a2 = new int[] { 1 };
            a3 = new int[] { 1, 2, 3, 4 };
            a4 = new int[10000];
        }

        [DataMember]
        public int[] a1;

        [DataMember]
        public int[] a2;

        [DataMember]
        public int[] a3;

        [DataMember]
        public int[] a4;
    }

    [DataContract]
    public class Array3
    {
        [DataMember]
        public int[][] a1 = { new int[] { 1 }, new int[] { } };
    }

    [DataContract]
    public class Properties
    {
        private int _a = 5;

        [DataMember]
        public int A
        {
            set { _a = value; }
            get { return _a; }
        }
    }

    [DataContract]
    public class HaveNS
    {
        [DataMember]
        public NotSer ns;
    }

    [DataContract]
    public class OutClass
    {
        [DataContract]
        public class NestedClass
        {
            [DataMember]
            public int a = 10;
        }

        [DataMember]
        public NestedClass nc = new NestedClass();
    }

    [DataContract]
    public class Temp
    {
        [DataMember]
        public int a = 10;
    }

    [DataContract]
    public class Array22
    {
        [DataMember]
        public int[] p = new int[] { 1 };
    }

    [DataContract]
    [KnownType(typeof(VT))]
    public class BoxedPrim
    {
        [DataMember]
        public object p = new Boolean();

        [DataMember]
        public object p2 = new VT(10);
    }
}

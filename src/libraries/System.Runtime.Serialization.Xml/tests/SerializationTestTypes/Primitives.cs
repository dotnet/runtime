using System;
using System.Runtime.Serialization;

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
}

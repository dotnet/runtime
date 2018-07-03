// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;

namespace SerializationTestTypes
{
    [DataContract]
    public class PrimitiveContainer
    {
        public PrimitiveContainer()
        {
            a = false;
            b = byte.MaxValue;
            c = byte.MinValue;

            //char
            //datetime
            e = decimal.MaxValue;
            f = decimal.MinusOne;
            g = decimal.MinValue;
            h = decimal.One;
            i = decimal.Zero;
            j = default(decimal);
            k = default(double);
            l = double.Epsilon;
            m = double.MaxValue;
            n = double.MinValue;
            o = double.NaN;
            p = double.NegativeInfinity;
            q = double.PositiveInfinity;
            r = default(float);
            s = float.Epsilon;
            t = float.MinValue;
            u = float.MaxValue;
            v = float.NaN;
            w = float.NegativeInfinity;
            x = float.PositiveInfinity;
            y = default(int);
            z = int.MaxValue;
            z1 = int.MinValue;
            z2 = default(long);
            z3 = long.MaxValue;
            z4 = long.MinValue;
            z5 = new object();
            z6 = default(sbyte);
            z7 = sbyte.MaxValue;
            z8 = sbyte.MinValue;
            z9 = default(short);
            z91 = short.MaxValue;
            z92 = short.MinValue;
            z93 = "abc";
            z94 = default(ushort);
            z95 = ushort.MaxValue;
            z96 = ushort.MinValue;
            z97 = default(uint);
            z98 = uint.MaxValue;
            z99 = uint.MinValue;
            z990 = default(ulong);
            z991 = ulong.MaxValue;
            z992 = ulong.MinValue;
            z993 = new byte[] { 1, 2, 3, 4 };
        }

        [DataMember]
        public object a;

        [DataMember]
        public object b;

        [DataMember]
        public object c;

        [DataMember]
        public object d = char.MaxValue;

        [DataMember]
        public object f5 = DateTime.MaxValue;

        [DataMember]
        public object guidData = Guid.Parse("4bc848b1-a541-40bf-8aa9-dd6ccb6d0e56");

        [DataMember]
        public object strData;

        [DataMember]
        public object e;

        [DataMember]
        public object f;

        [DataMember]
        public object g;

        [DataMember]
        public object h;

        [DataMember]
        public object i;

        [DataMember]
        public object j;

        [DataMember]
        public object k;

        [DataMember]
        public object l;

        [DataMember]
        public object m;

        [DataMember]
        public object n;

        [DataMember]
        public object o;

        [DataMember]
        public object p;

        [DataMember]
        public object q;

        [DataMember]
        public object r;

        [DataMember]
        public object s;

        [DataMember]
        public object t;

        [DataMember]
        public object u;

        [DataMember]
        public object v;

        [DataMember]
        public object w;

        [DataMember]
        public object x;

        [DataMember]
        public object y;

        [DataMember]
        public object z;

        [DataMember]
        public object z1;

        [DataMember]
        public object z2;

        [DataMember]
        public object z3;

        [DataMember]
        public object z4;

        [DataMember]
        public object z5;

        [DataMember]
        public object z6;

        [DataMember]
        public object z7;

        [DataMember]
        public object z8;

        [DataMember]
        public object z9;

        [DataMember]
        public object z91;

        [DataMember]
        public object z92;

        [DataMember]
        public object z93;

        [DataMember]
        public object z94;

        [DataMember]
        public object z95;

        [DataMember]
        public object z96;

        [DataMember]
        public object z97;

        [DataMember]
        public object z98;

        [DataMember]
        public object z99;

        [DataMember]
        public object z990;

        [DataMember]
        public object z991;

        [DataMember]
        public object z992;

        [DataMember]
        public byte[] z993;

        [DataMember]
        public object xmlQualifiedName = new XmlQualifiedName("WCF", "http://www.microsoft.com");

        [DataMember]
        public ValueType timeSpan = TimeSpan.MaxValue;

        [DataMember]
        public object obj = new object();

        [DataMember]
        public Uri uri = new Uri("http://www.microsoft.com");

        [DataMember]
        public Array array1 = new object[] { new object(), new object(), new object() };

        [DataMember]
        public object nDTO = DateTimeOffset.MaxValue;

        [DataMember]
        public List<DateTimeOffset> lDTO = new List<DateTimeOffset>();
    }

    [DataContract]
    [KnownType(typeof(EmptyNSAddress))]
    public class EmptyNsContainer
    {
        [DataMember]
        public object address;

        [DataMember]
        public string Name;

        public EmptyNsContainer(EmptyNSAddress obj)
        {
            address = obj;
            Name = "P1";
        }
    }

    [DataContract(Namespace = "")]
    public class UknownEmptyNSAddress : EmptyNSAddress
    {
        public UknownEmptyNSAddress()
        {
        }
    }

    [DataContract(Namespace = "")]
    public class EmptyNSAddress
    {
        [DataMember]
        public string street;

        public EmptyNSAddress()
        {
            street = "downing street";
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PreferredCustomer))]
    public class Customer
    {
        [DataMember]
        public string Name { get; set; }
    }

    [DataContract(IsReference = true)]
    public class PreferredCustomer : Customer
    {
        [DataMember]
        public string VipInfo { get; set; }
    }

    [DataContract(IsReference = true)]
    public class PreferredCustomerProxy : PreferredCustomer
    {
    }

    [DataContract]
    public class UnknownEmployee
    {
        [DataMember]
        public int id = 10000;
    }

    [DataContract]
    public class UserTypeContainer
    {
        [DataMember]
        public object unknownData = new UnknownEmployee();
    }
}

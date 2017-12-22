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
            b = Byte.MaxValue;
            c = Byte.MinValue;

            //char
            //datetime
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
            z993 = new Byte[] { 1, 2, 3, 4 };
        }

        [DataMember]
        public object a;

        [DataMember]
        public object b;

        [DataMember]
        public object c;

        [DataMember]
        public object d = Char.MaxValue;

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
        public Byte[] z993;

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

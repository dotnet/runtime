// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Xml;

namespace SerializationTestTypes
{
    [DataContract]
    public class Person1
    {
        public object address;

        public Person1(string variation)
        {
            age = 10;
            name = "Tintin";
            address = new Address("rd", "wa", 90012);
        }

        public Person1()
        {
        }

        [DataMember]
        public int age;

        [DataMember]
        public string name;
    }

    [DataContract]
    public class Person2 : Person1
    {
        [DataMember]
        public Guid Uid;

        [DataMember]
        public XmlQualifiedName[] XQAArray;

        [DataMember]
        public object anyData;

        public Person2()
        {
            Uid = new Guid("ff816178-54df-2ea8-6511-cfeb4d14ab5a");
            XQAArray = new XmlQualifiedName[] { new XmlQualifiedName("Name1", "http://www.PlayForFun.com"), new XmlQualifiedName("Name2", "http://www.FunPlay.com") };
            anyData = new Kid();
        }
    }

    public class Kid : Person1
    {
        [DataMember]
        public object FavoriteToy;

        public Kid()
        {
            FavoriteToy = new Blocks("Orange");
            age = 3;
        }
    }

    [DataContract]
    public class Blocks
    {
        public Blocks(string s)
        {
            color = s;
        }

        [DataMember]
        public string color;
    }

    [DataContract]
    public class Address
    {
        public Address()
        {
        }

        public Address(string c, string s, int z)
        {
            City = c;
            State = s;
            ZipCode = z;
        }

        [DataMember]
        public string City;

        [DataMember]
        public string State;

        [DataMember]
        public int ZipCode;
    }
}

using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

[assembly:ContractNamespace("http://special1.tempuri.org", ClrNamespace= "SerializableTypes.XsdDataContractExporterTests.More")]

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests.More
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests.More
#endif
{
    [DataContract]
    public class Foo
    {
        [DataMember]
        int id;
    }

    [KnownType(typeof(GenericBasePOCO<GenericBasePOCO<SimpleBaseContainerPOCO>>))]
    [KnownType(typeof(GenericBasePOCO<SimpleBaseContainerPOCO>))]
    [KnownType(typeof(SimpleBaseContainerPOCO))]
    public class GenericContainerPOCO
    {
        public GenericBasePOCO2<SimpleBaseDerivedPOCO, SimpleBaseDerivedPOCO2> GenericData;
        public object TestGenericBasePOCO;
        public GenericContainerPOCO()
        {
        }
    }


    public class GenericBasePOCO<T> where T : new()
    {
        public object genericData = new T();
    }


    public class GenericBasePOCO2<T, K>
        where T : new()
        where K : new()
    {
        public T genericData1 = new T();

        public K genericData2 = new K();
    }

    [KnownType(typeof(SimpleBaseDerivedPOCO2))]
    public class SimpleBaseContainerPOCO
    {
        public SimpleBasePOCO Base1;

        public List<SimpleBaseDerivedPOCO> Base2;

        public SimpleBaseContainerPOCO()
        {
        }
    }

    [KnownType(typeof(SimpleBaseDerivedPOCO))]
    public class SimpleBasePOCO
    {
        public string BaseData = String.Empty;
    }


    public class SimpleBaseDerivedPOCO : SimpleBasePOCO
    {
        public string DerivedData = String.Empty;
    }

    public class SimpleBaseDerivedPOCO2 : SimpleBasePOCO
    {
        public string DerivedData = String.Empty;
    }

    [DataContract]
    [KnownType(typeof(GenericBaseDC<GenericBaseDC<SimpleBaseContainerDC>>))]
    [KnownType(typeof(SimpleBaseContainerDC))]
    public class GenericContainerDC
    {
        [DataMember]
        public GenericBaseDC2<SimpleBaseDerivedDC, SimpleBaseDerivedDC2> GenericData;
        public GenericContainerDC()
        {
        }
    }


    [DataContract]
    public class GenericBaseDC<T> where T : new()
    {
        [DataMember]
        public object genericData = new T();
    }


    [DataContract]
    public class GenericBaseDC2<T, K>
        where T : new()
        where K : new()
    {
        [DataMember]
        public T genericData1 = new T();

        [DataMember]
        public K genericData2 = new K();
    }

    [DataContract]
    [KnownType(typeof(SimpleBaseDerivedDC2))]
    public class SimpleBaseContainerDC
    {
        [DataMember]
        public SimpleBaseDC Base1;

        [DataMember]
        public List<SimpleBaseDerivedDC> Base2;

        public SimpleBaseContainerDC()
        { }
    }

    [DataContract]
    [KnownType(typeof(SimpleBaseDerivedDC))]
    public class SimpleBaseDC
    {
        [DataMember]
        public string BaseData = String.Empty;
    }


    [DataContract]
    public class SimpleBaseDerivedDC : SimpleBaseDC
    {
        [DataMember]
        public string DerivedData = String.Empty;
    }

    [DataContract]
    public class SimpleBaseDerivedDC2 : SimpleBaseDC
    {
        [DataMember]
        public string DerivedData = String.Empty;
    }
}

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    public class DataContractTypes
    {
        [DataContract]
        public class Person1 
        {
            [DataMember]
            public string name;
            
            [DataMember]
            public int age;

            [DataMember]
            float salary;

            Person1()
            {
            }

            public Person1(string init)
            {
                name = "John Anderson";
                age = 25;
                salary = 100000;
            }
        }

        // Unknown data does not affect the data contract of the class
        [DataContract(Name="DataContractTypes.Person1")]
        public class Person2 : IExtensibleDataObject
        {
            public string firstName;
            public string lastName;

            [DataMember(Name="name")]
            internal string Name 
            {
                get { return firstName + " " + lastName; }
                private set 
                { 
                    int splitIndex = value.IndexOf(' ');
                    firstName = value.Substring(0, splitIndex);
                    lastName = value.Substring(splitIndex+1);
                }
            }
            
            protected int personAge;

            [DataMember(Name="age")]
            public virtual int Age
            {
                get { return personAge; }
                set { personAge = value; }
            }

            [DataMember]
            internal float salary;

            protected Person2()
            {
            }

            public Person2(string init)
            {
                Name = "John Anderson";
                Age = 25;
                salary = 100000;
            }

            ExtensionDataObject extensionData;
            public ExtensionDataObject ExtensionData
            {
                get { return extensionData; }
                set { extensionData = value; }
            }
        }

        [DataContract(Name="PersonContract")]
        internal class Person3
        {
            [DataMember(Name="Name")]
            public string name;
            
            [DataMember(Name="Nickname")]
            public string name2;
            
            [DataMember(Name="Age")]
            public int age;
            
            [DataMember(Name="Salary")]
            float salary;

            [DataMember]
            Address address;

            [DataMember]
            public Address Address
            {
                get { return address; }
                set { address = value; }
            }

            public Person3()
            {
                name = "John Anderson";
                name2 = "Johny";
                age = 25;
                salary = 100000;
                Address = new Address(null);
            }
        }

        [DataContract(Name="Person")]
        public struct Person4
        {
            [DataMember]
            public string name;
            
            [DataMember]
            public int age;
            
            [DataMember]
            float salary;

            [DataMember]
            Address address;

            [DataMember]
            DateTimeOffset hireDate;

            public Person4(StreamingContext context)
            {
                name = "John Anderson";
                age = 25;
                salary = 100000;
                address = new Address(null);
                hireDate = new DateTimeOffset(new DateTime(1995, 01, 17, 5, 30, 0), new TimeSpan(1,15,120));
            }
        }

        [DataContract(Name="Address")]
        public class Address
        {
            [DataMember(IsRequired=true)]
            public string street;

            string city;

            [DataMember(Name = "city", IsRequired = true)]
            public string City 
            { 
                get { return city; } 
                set { city = value; }
            }
            
            [DataMember(IsRequired=true)]
            string state;
            
            int zip;

            [DataMember(Name = "zip", IsRequired = true)]
            int Zip 
            { 
                get { return zip; } 
                set {zip = value; }
            }
            
            public Address()
            {
            }
            
            public Address(string init)
            {
                street = "One FooBar Avenue";
                City = "BazTown";
                state = "WA";
                Zip = 66066;
            }
        }

        [DataContract(Name="Address", Namespace="http://schemas.datacontract.org/2004/07/schemaexport.suites")]
        public struct Address2
        {
            [DataMember]
            public string street;
            
            [DataMember]
            string city;

            string state;
            
            [DataMember(Name="state")]
            public string State 
            { 
                get { return state; } 
                set { state = value; }
            }

            [DataMember]
            int zip;

            public Address2(string init)
            {
                street = "One FooBar Avenue";
                city = "BazTown";
                state = "WA";
                zip = 66066;
            }
        }

        [DataContract(Namespace="http://invalid.org?query")]
        public class Child : Person2
        {
            [DataMember(Name="age")]
            public override int Age
            {
                get { return personAge; }
                set 
                { 
                    if (personAge > 18) 
                        throw new Exception("Children must be aged 18 or younger");
                    personAge = value; 
                }
            }

            [DataMember]
            Person2 mother;
            [DataMember]
            Person2 father;

            Child(StreamingContext context)
            {
            }

            public Child(string init) : base(init)
            {
                personAge = 13;
                mother = new Person2(null);
                father = null;
            }
        }

        [DataContract(Name="DerivedAddress")]
        public class DerivedAddress : Address
        { 
            [DataMember]
            string email;
            
            [DataMember]
            string phone;

            public DerivedAddress() : base(null)
            {
                email = "neo@zion.net";
                phone = "222-111-2222";
            }
        }

        [DataContract(Name="Node")]
        class Node
        {
            [DataMember]
            Node nextNode;

            [DataMember]
            Node previousNode;

            Node()
            {
            }
        }

        [DataContract(Name="Node")]
        class Node2
        {
            [DataMember]
            Node2 nextNode;

            [DataMember]
            Node3 previousNode;

            Node2()
            {
            }
        }

        [DataContract(Name="Node")]
        class Node3
        {
            [DataMember]
            Node3 nextNode;

            [DataMember]
            Node4 previousNode;

            Node3()
            {
            }
        }

        [DataContract(Name="Node")]
        class Node4
        {
            [DataMember]
            Node3 nextNode;

            [DataMember]
            Node2 previousNode;

            Node4()
            {
            }
        }

        [DataContract]
        class A
        {
            [DataMember]
            B member;

            A()
            {
            }
        }

        [DataContract]
        class B
        {
            [DataMember]
            A member;
            
            B() 
            {
            }
        }

        [DataContract]
        public class ClassWithInterfaceMember 
        {
            [DataMember]
            ISampleInterface interfaceMember;
        }

        interface ISampleInterface
        {
            void InterfaceMethod();
        }
        
        [DataContract(Name="DerivedAddress2")]
        public class DerivedAddress2 : DerivedAddress
        { 
            [DataMember]
            byte[] extraData;
            
            public DerivedAddress2() : base()
            {
            }
        }

        [DataContract]
        public class Foo
        {
            [DataMember(IsRequired=false)]
            int i=1;
            [DataMember(IsRequired=true)]
            int j=3;
            [DataMember(Order=3)]
            string a = "a";
            [DataMember(Order=4, IsRequired=false)]
            public int z = 32;

        }

        [DataContract]
        public class Bar
        {
            [DataMember]
            int i=1;
            [DataMember(Order=4, IsRequired=true)]
            int j=3;
            [DataMember(Order=5)]
            string a = "a";
            [DataMember(Order=8)]
            public int z = 32;

        }

        [DataContract]
        [Serializable]
        public class MixedDCSerializable
        {
            int serializableInt = 0;
            [DataMember]
            int dataContractInt = 0;
        }

        [Serializable]
        public class SerializableOnly : MixedDCSerializable
        {
            string serializableString;
            [NonSerialized]
            int nonSerializedInt;
        }

        [DataContract]
        [Serializable]
        public class DerivedMixedDCSerializable : SerializableOnly
        {
            public float serializableFloat = 0.0F;
            [DataMember]
            public float dataContractFloat = 0.0F;
        }

        [Serializable]
        public class AllPrimitives 
        { 
            public object objectMember;
            public char charMember;
            public bool boolMember;
            public byte unsignedByteMember;
            //[CLSCompliant(false)]
            public sbyte byteMember;
            public short shortMember;
            //[CLSCompliant(false)]
            public ushort unsignedShortMember;
            public int intMember;
            //[CLSCompliant(false)]
            public uint unsignedIntMember;
            public long longMember;
            //[CLSCompliant(false)]
            public ulong unsignedLongMember;
            public float floatMember;
            public double doubleMember;
            public decimal decimalMember;
            public DateTime dateTimeMember;
            public string stringMember;
            byte[] byteArrayMember;
            public Guid guidMember;
            public TimeSpan timeSpanMember;
            public Uri uri;
        }

        [DataContract]
        public class AllSpecialTypes
        {
            [DataMember] System.Enum enumMember;
            [DataMember] System.ValueType valueTypeMember;
            [DataMember] System.Array arrayMember;
        }

    }
}




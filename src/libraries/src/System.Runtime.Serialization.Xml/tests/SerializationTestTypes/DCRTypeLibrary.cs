using System;
using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    [DataContract]
    public class ObjectContainer
    {
        [DataMember]
        private object _data;

        [DataMember]
        private object _data2;

        public ObjectContainer(object input)
        {
            _data = input;
            _data2 = _data;
        }

        public object Data
        {
            get { return _data; }
        }

        public object Data2
        {
            get { return _data2; }
        }
    }

    [DataContract(Name = "EmptyDCType", Namespace = "http://www.Default.com")]
    public class EmptyDCType
    {
    }

    [KnownType(typeof(EmptyDCType))]
    public class POCOObjectContainer
    {
        public POCOObjectContainer() { Data = new EmptyDCType(); }

        public object Data;

        [IgnoreDataMember]
        public object NonSerializedData;

        public POCOObjectContainer(object input)
        {
            Data = input;
        }
    }

    [DataContract]
    public class Alpha
    {
        [DataMember]
        public Person person = new Person();
    }

    [DataContract]
    [KnownType(typeof(CharClass))]
    public class Beta
    {
        [DataMember]
        public object unknown1 = new CharClass();
    }

    [DataContract]
    public class Charlie
    {
        [DataMember]
        public object unknown2 = new SerializationTestTypes.Employee();
    }


    [DataContract(Namespace = "http://Microsoft.ServiceModel.Samples")]
    public class Employee
    {
        [DataMember]
        public DateTime dateHired;

        [DataMember]
        public Decimal salary;

        [DataMember]
        public Individual individual;
    }

    public class Individual
    {
        public string firstName;

        public string lastName;

        public int age;

        public object data;

        public object data1;

        public Individual() { }
    }

    [DataContract]
    public class Wireformat1
    {
        [DataMember]
        public Alpha alpha = new Alpha();
        [DataMember]
        public Beta beta = new Beta();
        [DataMember]
        public Charlie charlie = new Charlie();
    }

    [DataContract]
    public class Wireformat2
    {
        [DataMember]
        public Beta beta1 = new Beta();
        [DataMember]
        public Charlie charlie = new Charlie();
        [DataMember]
        public Beta beta2 = new Beta();
    }

    [DataContract]
    public class Wireformat3
    {
        [DataMember]
        public Charlie charlie1 = new Charlie();
        [DataMember]
        public Beta beta = new Beta();
        [DataMember]
        public Charlie charlie2 = new Charlie();
    }

    [DataContract]
    public class DCRVariations
    {
        [DataMember]
        public object unknownType1;

        [DataMember]
        public object unknownType2;
    }

    [DataContract]
    public class CustomClass
    {
        [DataMember()]
        private object[] knownTypes;

        [DataMember()]
        private object[] dataContractResolverTypes;

        [DataMember()]
        public virtual object[] KnownTypes
        {
            get { return knownTypes; }
            set { knownTypes = value; }
        }

        [DataMember()]
        public virtual object[] DataContractResolverTypes
        {
            get { return dataContractResolverTypes; }
            set { dataContractResolverTypes = value; }
        }
    }
}

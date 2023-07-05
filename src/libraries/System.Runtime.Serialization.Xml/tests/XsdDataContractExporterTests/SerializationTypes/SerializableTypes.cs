using System;
using System.Collections;
using System.Runtime.Serialization;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    [Serializable]
    public class Address
    {
        public string street;
        string city;
        string state;
        int zip;
        
        [NonSerialized]
        float privateData;
        
        public string Apartment 
        { 
            get { return null; }
            set { }
        }

        public Address()
        {
        }
    }

    [Serializable]
    public struct Address2
    {
        public string street;
        string city;
        string state;
        int zip;
        
        [NonSerialized]
        float privateData;

    }
    
    [Serializable]
    public class Employee //: DataContractTypes.Person2
    {
        ArrayTypes.Company company;    

        Employee(StreamingContext context)
        {
        }
    }

    [Serializable]
    [KnownType(typeof(ArrayList))]
    [KnownType(typeof(int))]
    [KnownType(typeof(DateTime))]
    [KnownType(typeof(Employee))]
    [KnownType(typeof(ObjectContainer))]
    public class ObjectContainer
    {
        object obj;
    }

}

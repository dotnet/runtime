using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

#if UseSeparateAssemblyNamespace
using Address = SerializableTypes.XsdDataContractExporterTests.Address;
using Employee = SerializableTypes.XsdDataContractExporterTests.Employee;

namespace SerializableTypes.XsdDataContractExporterTests.ArrayTypes
#else
using Address = System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Address;
using Employee = System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Employee;

namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ArrayTypes
#endif
{
    [Serializable]
    public class Company
    {
        string name;
        string[] products;
        Address address;
        [OptionalField]
        Employee[] employees;
    }
    
    // IsRequired default different for [Serializable] and [DataContract]
    [DataContract(Name="Company")]
    public class Company2
    {
        [DataMember(IsRequired=true)]
        public string name;
        
        [DataMember(Name="products", IsRequired=true)]
        public string[] Products;

        [DataMember(IsRequired = true)]
        Address address;

        [DataMember]
        Employee[] employees;

        public Company2()
        {
        }
    }

    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/SerializableTypes.XsdDataContractExporterTests")]
    public class Employee
    {
        [DataMember(IsRequired=true)]
        Company company;  
    }

    [DataContract]
    public class JaggedArrays
    {
        [DataMember]
        Company[] companyArray_1rank;
        
        [DataMember]
        Company[][] companyArray_2rank;
        
        [DataMember]
        Company[][][] companyArray_3rank;

        [DataMember]
        object[] objectArray_1rank;
    
        [DataMember]
        object[][] objectArray_2rank;
        
        [DataMember]
        ManagerEmployeeList peerList;

        [DataMember]
        DateTimeOffset[] dateTimeOffsetArray_1rank;
    
        [DataMember]
        DateTimeOffset[][] dateTimeOffsetArray_2rank;
    }

    [DataContract]
    public class SystemArray
    {

        [DataMember]
        public Array[] arrayArray;

        [DataMember]
        public Array array;

    }

    [CollectionDataContract]
    public class ManagerEmployeeList : List<List<Employee>>
    {
    }
}




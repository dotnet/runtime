// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;

using SerializableTypes.XsdDataContractExporterTests;

namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
{
    public class ExporterTypesTests
    {
        private readonly ITestOutputHelper _output;
        public ExporterTypesTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TypesTest()
        {
            var types = new List<Type>()
            {
                typeof(DataContractTypes.Person1),
                typeof(DataContractTypes.Person2),
                typeof(ExporterTypesTests.Group),
                typeof(ExporterTypesTests.NoDataContract),
                typeof(ExporterTypesTests.DataContractWithValidMember),
                typeof(ExporterTypesTests.DataContractWithValidMember),
                typeof(ExporterTypesTests.PersonInfo),
            };

            XsdDataContractExporter exporter = new XsdDataContractExporter();
            ExportOptions options = new ExportOptions();
            options.KnownTypes.Add(typeof(ArrayList));
            options.KnownTypes.Add(typeof(Guid));
            exporter.Options = options;

            exporter.Export(types);
            exporter.Export(types); // Run twice, to ensure that types are not re-exported

            string schemas = SchemaUtils.DumpSchema(exporter.Schemas);
            _output.WriteLine(schemas);
            _output.WriteLine($"----------------- {exporter.Schemas.Count}, {exporter.Schemas.GlobalElements.Count}, {exporter.Schemas.GlobalTypes.Count}");

            Assert.Equal(5, exporter.Schemas.Count);
            Assert.Equal(36, exporter.Schemas.GlobalElements.Count);
            Assert.Equal(18, exporter.Schemas.GlobalTypes.Count);

            SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/SerializableTypes.XsdDataContractExporterTests"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/SerializableTypes.XsdDataContractExporterTests"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""DataContractTypes.Person1"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""DataContractTypes.Person1"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:import namespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" />", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Group"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ArrayOfExporterTypesTests.Person"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Person"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Employee"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Admin"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Architect"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Engineer"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.NoDataContract"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.DataContractWithValidMember"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.PersonInfo"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.InnerPersonCollection"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref schemas);
        }

        public static IEnumerable<object[]> GetDynamicallyVersionedTypesTestNegativeData()
        {
            // Need this case in a member data because inline data only accepts constant expressions
            yield return new object[] {
                typeof(TypeWithReadWriteCollectionAndNoCtorOnCollection),
                typeof(InvalidDataContractException),
                $@"System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+CollectionWithoutParameterlessCtor`1[[System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+Person, System.Runtime.Serialization.Xml.Tests, Version={Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major}.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51]] does not have a default constructor."
            };
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Inconsistent and unpredictable results.")]  // TODO - Why does 'TypeWithReadWriteCollectionAndNoCtorOnCollection' only cause an exception sometimes, but not all the time? What's special about wasm here?
        [InlineData(typeof(NoDataContractWithoutParameterlessConstructor), typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+NoDataContractWithoutParameterlessConstructor' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required.")]
        [InlineData(typeof(DataContractWithInvalidMember), typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+NoDataContractWithoutParameterlessConstructor' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required.")]
        [InlineData(typeof(SerializableWithInvalidMember), typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+NoDataContractWithoutParameterlessConstructor' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required.")]
        // Yes, the exception type for this next one is different. It was different in NetFx as well.
        [InlineData(typeof(ArrayContainer), typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+ArrayB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+ArrayA' with the same data contract name 'Array' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(typeof(KeyValueNameSame), typeof(InvalidDataContractException), @"The collection data contract type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+KeyValueNameSame' specifies the same value 'MyName' for both the KeyName and the ValueName properties. This is not allowed. Consider changing either the KeyName or the ValueName property.")]
        [InlineData(typeof(AnyWithRoot), typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+AnyWithRoot' cannot specify an XmlRootAttribute attribute because its IsAny setting is 'true'. This type must write all its contents including the root element. Verify that the IXmlSerializable implementation is correct.")]
        [MemberData(nameof(GetDynamicallyVersionedTypesTestNegativeData))]
        public void TypesTest_Negative(Type badType, Type exType, string exMsg = null)
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            var ex = Assert.Throws(exType, () => exporter.Export(badType));
            if (exMsg != null)
                Assert.Equal(exMsg, ex.Message);
        }

        [Theory]
        [InlineData(new Type[] { typeof(AddressA), typeof(AddressB) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+AddressB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+AddressA' with the same data contract name 'Address' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(AddressA), typeof(AddressC) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+AddressC' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+AddressA' with the same data contract name 'Address' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(OrderA), typeof(OrderB) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+OrderB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+OrderA' with the same data contract name 'Order' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(ArrayA), typeof(ArrayB) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+ArrayB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+ArrayA' with the same data contract name 'Array' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(EnumA), typeof(EnumB) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+EnumB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+EnumA' with the same data contract name 'Enum' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(EnumB), typeof(EnumC) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+EnumC' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+EnumB' with the same data contract name 'Enum' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(EnumContainerA), typeof(EnumContainerB) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+EnumContainerB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+EnumContainerA' with the same data contract name 'EnumContainer' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(CollectionA), typeof(CollectionB) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+CollectionB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+CollectionA' with the same data contract name 'MyCollection' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        [InlineData(new Type[] { typeof(DictionaryA), typeof(DictionaryB) }, typeof(InvalidOperationException), @"DataContract for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+DictionaryB' cannot be added to DataContractSet since type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+DictionaryA' with the same data contract name 'MyDictionary' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests' is already present and the contracts are not equivalent.")]
        public void TypeArrayTest_Negative(Type[] badTypes, Type exType, string exMsg = null)
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            var ex = Assert.Throws(exType, () => exporter.Export(badTypes));
            if (exMsg != null)
                Assert.Equal(exMsg, ex.Message);
        }

        [Fact]
        public void ReferenceTypes() // From IsReferenceTypes.cs
        {
            List<Type> types = new List<Type>()
            {
                typeof(ExporterTypesTests.Order_ContainsRef),
                typeof(ExporterTypesTests.Customers_ContainsDuplicateRefs),
                typeof(ExporterTypesTests.Student_ContainsDuplicateCollectionRefs),
                typeof(ExporterTypesTests.CircularLinkedList_ContainsBackpointingRef),
                typeof(ExporterTypesTests.RefCircularLinks_ContainsBackpointer),
                typeof(ExporterTypesTests.RefCircularNodeA_ContainsRefWithBackpointer),
                typeof(ExporterTypesTests.RefNestedNode_ContainsBackpointer),
                typeof(ExporterTypesTests.RefSimpleDataContractCycle_ContainsRefWithBackpointer),
                typeof(ExporterTypesTests.Fruit),
                typeof(ExporterTypesTests.RefApple),
                typeof(ExporterTypesTests.EdibleContainer_ContainsPolymorphicRefs),
            };

            XsdDataContractExporter exporter = new XsdDataContractExporter();
            ExportOptions options = new ExportOptions();
            options.KnownTypes.Add(typeof(ArrayList));
            options.KnownTypes.Add(typeof(Guid));

            exporter.Export(types);
            exporter.Export(types); // Run twice, to ensure that types are not re-exported

            string schemas = SchemaUtils.DumpSchema(exporter.Schemas);
            _output.WriteLine(schemas);
            _output.WriteLine($"----------------- {exporter.Schemas.Count}, {exporter.Schemas.GlobalElements.Count}, {exporter.Schemas.GlobalTypes.Count}");

            Assert.Equal(3, exporter.Schemas.Count);
            Assert.Equal(39, exporter.Schemas.GlobalElements.Count);
            Assert.Equal(21, exporter.Schemas.GlobalTypes.Count);

            SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" xmlns:ser=""http://schemas.microsoft.com/2003/10/Serialization/"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:import namespace=""http://schemas.microsoft.com/2003/10/Serialization/"" />", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Order_ContainsRef"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefCustomer"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Customers_ContainsDuplicateRefs"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Student_ContainsDuplicateCollectionRefs"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefGrades"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.CircularLinkedList_ContainsBackpointingRef"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefNode"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefCircularLinks_ContainsBackpointer"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefCircularNodeA_ContainsRefWithBackpointer"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefCircularNodeB_ContainsRefWithBackpointer"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefNestedNode_ContainsBackpointer"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefSimpleDataContractCycle_ContainsRefWithBackpointer"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefSimpleDataContractCycleNextLink"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.Fruit"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefEdibleItem"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.RefApple"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:complexType name=""ExporterTypesTests.EdibleContainer_ContainsPolymorphicRefs"">", ref schemas);
            SchemaUtils.OrderedContains(@"<xs:schema targetNamespace=""http://www.w3.org/2001/XMLSchema"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref schemas);
        }

        [Theory]
        [InlineData(typeof(ExporterTypesTests.Fruit2), typeof(InvalidDataContractException), @"The IsReference setting for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+Fruit2' is 'False', but the same setting for its parent class 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+RefEdibleItem' is 'True'. Derived types must have the same value for IsReference as the base type. Change the setting on type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+Fruit2' to 'True', or on type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+RefEdibleItem' to 'False', or do not set IsReference explicitly.")]
        [InlineData(typeof(ExporterTypesTests.Orange), typeof(InvalidDataContractException), @"The IsReference setting for type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+Orange' is 'False', but the same setting for its parent class 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+Fruit' is 'True'. Derived types must have the same value for IsReference as the base type. Change the setting on type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+Orange' to 'True', or on type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+Fruit' to 'False', or do not set IsReference explicitly.")]
        [InlineData(typeof(ExporterTypesTests.RefEnum), typeof(InvalidDataContractException), @"Enum type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.ExporterTypesTests+RefEnum' cannot have the IsReference setting of 'True'. Either change the setting to 'False', or remove it completely.")]
        public void ReferenceTypes_Negative(Type badRefType, Type exType, string exMsg = null)
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            var ex = Assert.Throws(exType, () => exporter.Export(badRefType));
            if (exMsg != null)
                Assert.Equal(exMsg, ex.Message);
        }

#pragma warning disable CS0169, CS0414
        public class NoDataContract
        {
        }

        [DataContract]
        public class DataContractWithValidMember
        {
            [DataMember]
            NoDataContract member;
        }

        [Serializable]
        public class SerializableWithValidMember
        {
            NoDataContract member;
        }

        public class NoDataContractWithoutParameterlessConstructor
        {
            public NoDataContractWithoutParameterlessConstructor(string init)
            {
            }
        }

        [DataContract]
        public class DataContractWithInvalidMember
        {
            [DataMember]
            NoDataContractWithoutParameterlessConstructor member;
        }

        [Serializable]
        public class SerializableWithInvalidMember
        {
            NoDataContractWithoutParameterlessConstructor member;
        }

        [DataContract(Name = "Order")]
        public class OrderA
        {
            [DataMember]
            public AddressA address;
        }

        [DataContract(Name = "Order")]
        public class OrderB
        {
            [DataMember]
            public AddressB address;
        }

        [DataContract(Name = "BaseOrder")]
        public class BaseOrder
        {
        }

        [DataContract(Name = "Order")]
        public class OrderD : BaseOrder
        {
            [DataMember]
            public AddressA address;
        }

        [DataContract(Name = "Address")]
        public class AddressA
        {
            [DataMember]
            public string zip;
        }

        [DataContract(Name = "Address")]
        public class AddressB
        {
            [DataMember]
            public int zip;
        }

        [DataContract(Name = "Address")]
        public class AddressC
        {
            [DataMember]
            public string street;
            [DataMember(IsRequired = true)]
            public string zip;
        }

        [DataContract]
        public class ArrayContainer
        {
            [DataMember]
            public ArrayA a1;
            [DataMember]
            public ArrayB a2;
        }

        [DataContract(Name = "Array")]
        public class ArrayA
        {
            [DataMember]
            public ArrayA[] items;
        }

        [DataContract(Name = "Array")]
        public class ArrayB
        {
            [DataMember]
            public ArrayC[] items;
        }

        [DataContract(Name = "Array")]
        public class ArrayC
        {
            [DataMember]
            public int items;
        }

        [DataContract(Name = "EnumContainer")]
        public class EnumContainerA
        {
            [DataMember]
            public EnumA member;
        }

        [DataContract(Name = "EnumContainer")]
        public class EnumContainerB
        {
            [DataMember]
            public EnumB member;
        }

        [DataContract(Name = "Enum")]
        public enum EnumA : long
        {
        }

        [DataContract(Name = "Enum")]
        public enum EnumB : long
        {
            [EnumMember] Min,
            [EnumMember] Zero,
            [EnumMember] Max,
        }

        [DataContract(Name = "Enum")]
        public enum EnumC : long
        {
            [EnumMember] Min,
            [EnumMember] Max,
        }

        [Serializable]
        public class Group
        {
            public IList<Person> People;
        }

        [KnownType(typeof(Employee))]
        [Serializable]
        public class Person
        {
            string name = "John Smith";
        }


        [KnownType(typeof(Admin))]
        [KnownType(typeof(Architect))]
        [KnownType(typeof(Engineer))]
        [Serializable]
        public class Employee : Person
        {
            int empId = 42;
        }

        [Serializable]
        public class Engineer : Employee
        {
        }

        [Serializable]
        [KnownType(typeof(Admin))]
        public class Admin : Employee
        {
        }

        [Serializable]
        [KnownType(typeof(Person))]
        public class Architect : Employee
        {
        }

        [CollectionDataContract(Name = "MyCollection", ItemName = "MyItemA")]
        public class CollectionA : List<int>
        {
        }

        [CollectionDataContract(Name = "MyCollection", ItemName = "MyItemB")]
        public class CollectionB : List<int>
        {
        }

        [CollectionDataContract(Name = "MyDictionary", KeyName = "MyKeyA")]
        public class DictionaryA : Dictionary<string, int>
        {
        }

        [CollectionDataContract(Name = "MyDictionary", KeyName = "MyKeyB")]
        public class DictionaryB : Dictionary<string, int>
        {
        }

        [CollectionDataContract(KeyName = "MyName", ValueName = "MyName")]
        public class KeyValueNameSame : Dictionary<int, int>
        {
        }

        [XmlSchemaProvider(null, IsAny = true)]
        [XmlRoot(ElementName = "AnyRootElement", IsNullable = false)]
        public class AnyWithRoot : XmlSerializableBase
        {
        }

        public class PersonInfo
        {
            CollectionWithoutParameterlessCtor<Person> localPersons;
            ArrayList localPersonArrayList;
            public InnerPersonCollection innerPersonInfo = new InnerPersonCollection();

            public CollectionWithoutParameterlessCtor<Person> Persons
            {
                get
                {
                    localPersons = localPersons ?? new CollectionWithoutParameterlessCtor<Person>(5);
                    return localPersons;
                }
            }

            public ArrayList PersonArrayList
            {
                get
                {
                    localPersonArrayList = localPersonArrayList ?? new ArrayList();
                    return localPersonArrayList;
                }
            }

            public PersonInfo()
            {
                Person p1 = new Person();

                Person p2 = new Person();

                Person p3 = new Person();

                this.Persons.Add(p1);
                this.Persons.Add(p2);

                this.PersonArrayList.Add(new Guid());
                this.PersonArrayList.Add("teststring");

                this.innerPersonInfo.Friends.Add(p3);

                this.innerPersonInfo.PotentialSalaries[0] = 90.0;
                this.innerPersonInfo.PotentialSalaries[1] = 100.0;
                this.innerPersonInfo.PotentialSalaries[2] = 106.0;

                this.innerPersonInfo.PotentialExpenditures = new double[] { 50.0, 55.0, 69.0 };

            }
        }

        public class InnerPersonCollection
        {
            private double[] potentialSalaries;
            private double[] potentialExpenditures;
            CollectionWithoutParameterlessCtor<Person> friends;

            public double[] PotentialSalaries
            {
                get
                {
                    potentialSalaries = potentialSalaries ?? new double[3];
                    return potentialSalaries;
                }
            }

            public double[] PotentialExpenditures
            {
                get
                {
                    return potentialExpenditures;
                }
                set
                {
                    potentialExpenditures = value;
                }
            }


            public CollectionWithoutParameterlessCtor<Person> Friends
            {
                get
                {
                    friends = friends ?? new CollectionWithoutParameterlessCtor<Person>(2);
                    return friends;
                }
            }
        }

        public class TypeWithReadWriteCollectionAndNoCtorOnCollection
        {
            private double[] potentialSalaries;
            private double[] potentialExpenditures;
            CollectionWithoutParameterlessCtor<Person> friends;

            public double[] PotentialSalaries
            {
                get
                {
                    potentialSalaries = potentialSalaries ?? new double[3];
                    return potentialSalaries;
                }
            }

            public double[] PotentialExpenditures
            {
                get
                {
                    return potentialExpenditures;
                }
                set
                {
                    potentialExpenditures = value;
                }
            }


            public CollectionWithoutParameterlessCtor<Person> Friends
            {
                get
                {
                    friends = friends ?? new CollectionWithoutParameterlessCtor<Person>(2);
                    return friends;
                }
                set
                {
                    friends = value;
                }
            }
        }

        public class CollectionWithoutParameterlessCtor<T> : ICollection<T>
        {
            ArrayList list;

            public CollectionWithoutParameterlessCtor(int size)
            {
                list = new ArrayList(size);
            }

            #region ICollection<T> Members

            public void Add(T item)
            {
                list.Add(item);
            }

            public void Clear()
            {
                list.Clear();
            }

            public bool Contains(T item)
            {
                return list.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                list.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return list.Count; }
            }

            public bool IsReadOnly
            {
                get { return list.IsReadOnly; }
            }

            public bool Remove(T item)
            {
                list.Remove(item);
                return true;
            }

            #endregion

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < list.Count; i++)
                {
                    yield return (T)list[i];
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return list.GetEnumerator();
            }

            #endregion
        }

        #region IsReferenceTypes
        [DataContract]
        class Order_ContainsRef
        {
            [DataMember]
            public string Id = "29817691";
            [DataMember]
            public string Url = "http://www.contoso.com/store/exec/OrderManagement?id=x9876270adh8q";
            [DataMember]
            public RefCustomer RefCustomer = RefCustomer.CreateInstance();
        }

        [DataContract(IsReference = true)]
        class RefEdibleItem
        {
        }

        [DataContract(IsReference = false)]
        class Fruit2 : RefEdibleItem
        {
        }

        [DataContract]
        class Fruit : RefEdibleItem
        {
        }

        [DataContract(IsReference = true)]
        class RefApple : Fruit
        {
        }

        [DataContract(IsReference = false)]
        class Orange : Fruit
        {
        }

        [DataContract]
        [KnownType(typeof(Fruit))]
        [KnownType(typeof(RefApple))]
        class EdibleContainer_ContainsPolymorphicRefs
        {
            [DataMember]
            RefEdibleItem w = new Fruit();
            [DataMember]
            RefEdibleItem x = new RefApple();
            [DataMember]
            Fruit z = new RefApple();
        }

        [DataContract]
        class Customers_ContainsDuplicateRefs
        {
            static RefCustomer customer = RefCustomer.CreateInstance();
            [DataMember]
            public RefCustomer RefCustomer1 = customer;
            [DataMember]
            public RefCustomer RefCustomer2 = customer;
        }

        [DataContract]
        public class Student_ContainsDuplicateCollectionRefs
        {
            static RefGrades grades;

            static Student_ContainsDuplicateCollectionRefs()
            {
                grades = new RefGrades();
                grades.Add("A");
            }

            [DataMember]
            RefGrades grades1 = grades;
            [DataMember]
            RefGrades grades2 = grades;
        }

        [CollectionDataContract(IsReference = true)]
        public class RefGrades : List<string>
        {
        }

        [DataContract(IsReference = true)]
        class RefCustomer
        {
            [DataMember]
            string Name;
            [DataMember]
            int ZipCode;

            internal static RefCustomer CreateInstance()
            {
                RefCustomer x = new RefCustomer();
                x.Name = "Bill Gates";
                x.ZipCode = 98052;
                return x;
            }
        }


        [DataContract]
        public class CircularLinkedList_ContainsBackpointingRef
        {
            [DataMember]
            RefNode start;

            [DataMember]
            int numberOfNodes;

            public CircularLinkedList_ContainsBackpointingRef()
            {
                numberOfNodes = 4;
                RefNode currentNode = null, prevNode = null;
                start = null;
                for (int i = 0; i < numberOfNodes; i++)
                {
                    currentNode = new RefNode(i, "Hello World");
                    if (i == 0)
                        start = currentNode;
                    if (prevNode != null)
                        prevNode.Next = currentNode;
                    prevNode = currentNode;
                }
                currentNode.Next = start;
            }
        }

        [DataContract(IsReference = true)]
        public class RefNode
        {
            [DataMember]
            public RefNode Next;

            [DataMember]
            int id;

            [DataMember]
            string name;

            public RefNode(int id, string name)
            {
                this.id = id;
                this.name = name;
            }
        }


        [DataContract(IsReference = true)]
        public class RefCircularLinks_ContainsBackpointer
        {
            [DataMember]
            RefCircularLinks_ContainsBackpointer link;

            public RefCircularLinks_ContainsBackpointer()
            {
                link = this;
            }
        }


        [DataContract(IsReference = true)]
        public class RefCircularNodeA_ContainsRefWithBackpointer
        {
            [DataMember]
            RefCircularNodeB_ContainsRefWithBackpointer linkToB;

            public RefCircularNodeA_ContainsRefWithBackpointer()
            {
                linkToB = new RefCircularNodeB_ContainsRefWithBackpointer(this);
            }
        }

        [DataContract(IsReference = true)]
        public class RefCircularNodeB_ContainsRefWithBackpointer
        {
            [DataMember]
            RefCircularNodeA_ContainsRefWithBackpointer linkToA;

            public RefCircularNodeB_ContainsRefWithBackpointer(RefCircularNodeA_ContainsRefWithBackpointer nodeA)
            {
                linkToA = nodeA;
            }
        }


        [DataContract(IsReference = true)]
        public class RefNestedNode_ContainsBackpointer
        {
            [DataMember]
            RefNestedNode_ContainsBackpointer node;

            [DataMember]
            int level;

            public RefNestedNode_ContainsBackpointer(int level)
                : this(level, null)
            {
            }

            public RefNestedNode_ContainsBackpointer(int level, RefNestedNode_ContainsBackpointer rootNode)
            {
                if (level > 0)
                    this.node = new RefNestedNode_ContainsBackpointer(level - 1, (rootNode == null ? this : rootNode));
                else
                    this.node = rootNode;
                this.level = level;
            }
        }


        [DataContract(IsReference = true)]
        public class RefSimpleDataContractCycle_ContainsRefWithBackpointer
        {
            [DataMember]
            object emptyFirstMember = new object();
            [DataMember]
            public RefSimpleDataContractCycleNextLink next;

            public static RefSimpleDataContractCycle_ContainsRefWithBackpointer CreateInstance()
            {
                RefSimpleDataContractCycle_ContainsRefWithBackpointer simpleCycle = new RefSimpleDataContractCycle_ContainsRefWithBackpointer();
                RefSimpleDataContractCycleNextLink childLink = new RefSimpleDataContractCycleNextLink();
                simpleCycle.next = childLink;
                childLink.backLink = simpleCycle;
                return simpleCycle;
            }
        }


        [DataContract]
        public class RefSimpleDataContractCycleNextLink
        {
            [DataMember]
            public RefSimpleDataContractCycle_ContainsRefWithBackpointer backLink;
        }

        [DataContract(IsReference = true)]
        enum RefEnum
        {

        }
        #endregion

#pragma warning restore CS0169, CS0414
    }
}

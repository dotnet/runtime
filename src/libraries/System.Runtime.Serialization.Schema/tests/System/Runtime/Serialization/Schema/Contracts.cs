// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.Schema.Tests
{
    internal class ContractUtils
    {
        internal const string ExpectedImportedTypeFile = "Expected.ImportedType.cs";
        internal const string TestNamespace = "www.Contoso.com/Examples/";
        internal const string ExpectedEmployeeSchema = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema xmlns:tns=""www.Contoso.com/Examples/"" elementFormDefault=""qualified"" targetNamespace=""www.Contoso.com/Examples/"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:complexType name=""Employee"">
    <xs:sequence>
      <xs:element minOccurs=""0"" name=""EmployeeName"" nillable=""true"" type=""xs:string"" />
      <xs:element minOccurs=""0"" name=""ID"" nillable=""true"" type=""xs:string"" />
    </xs:sequence>
  </xs:complexType>
  <xs:element name=""Employee"" nillable=""true"" type=""tns:Employee"" />
</xs:schema>";
    }

#pragma warning disable CS0169, IDE0051, IDE1006
    [DataContract(Namespace = ContractUtils.TestNamespace)]
    public class Employee
    {
        [DataMember]
        public string EmployeeName;
        [DataMember]
        private string ID;
    }
#pragma warning restore CS0169, IDE0051, IDE1006
}

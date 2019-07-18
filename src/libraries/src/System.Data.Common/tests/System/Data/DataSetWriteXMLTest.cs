// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;

using Xunit;

namespace System.Data.Tests
{
    public class DataSetWriteXmlTest
    {
        [Fact]
        public void WriteSimpleAuto()
        {
            string sampleXml = @"<NewDataSet>
                <xs:schema id='NewDataSet' xmlns='' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
                    <xs:element name='NewDataSet' msdata:IsDataSet='true' msdata:UseCurrentLocale='true'>
                        <xs:complexType>
                        <xs:choice minOccurs='0' maxOccurs='unbounded'>
                            <xs:element name='Table'>
                            <xs:complexType>
                                <xs:sequence>
                                <xs:element name='ServerName' msdata:DataType='System.Object, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e' type='xs:anyType' minOccurs='0' />
                                </xs:sequence>
                            </xs:complexType>
                            </xs:element>
                        </xs:choice>
                        </xs:complexType>
                    </xs:element>
                </xs:schema>
                <Table>
                    <ServerName xsi:type='xs:string' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>MACHINENAME</ServerName>
                </Table>
            </NewDataSet>";
            DataSet ds = new DataSet();
            System.IO.StringReader xmlSR = new System.IO.StringReader(sampleXml);

            ds.ReadXml(xmlSR, XmlReadMode.ReadSchema);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            using (System.Xml.XmlWriter xw = System.Xml.XmlWriter.Create(sb, new System.Xml.XmlWriterSettings() { }))
            {
                ds.WriteXml(xw, System.Data.XmlWriteMode.WriteSchema);
            }
            Assert.Equal(@"ServerName", ds.Tables[0].Columns[0].ColumnName);
            Assert.Equal(@"MACHINENAME", ds.Tables[0].Rows[0].Field<string>(ds.Tables[0].Columns[0]));

            using (System.IO.StringWriter sw = new System.IO.StringWriter(sb))
            {
                ds.WriteXml(sw, System.Data.XmlWriteMode.WriteSchema);
            }
            Assert.Equal(@"ServerName", ds.Tables[0].Columns[0].ColumnName);
            Assert.Equal(@"MACHINENAME", ds.Tables[0].Rows[0].Field<string>(ds.Tables[0].Columns[0]));
        }
    }
}

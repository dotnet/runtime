// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Mainsoft Co.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


// Provide All Data required by the diffderent tests e.g.DataTable, DataRow ...

namespace System.Data.Tests
{
    public class DataProvider
    {
        //A string containing all printable charachters.
        private const string SAMPLE_STRING = "abcdefghijklmnopqrstuvwxyz1234567890~!@#$%^&*()_+-=[]\\|;:,./<>? ";

        public static DataTable CreateChildDataTable()
        {
            DataTable dtChild = new DataTable("Child");
            dtChild.Columns.Add("ParentId", typeof(int));
            dtChild.Columns.Add("ChildId", typeof(int));
            dtChild.Columns.Add("String1", typeof(string));
            dtChild.Columns.Add("String2", typeof(string));
            dtChild.Columns.Add("ChildDateTime", typeof(DateTime));
            dtChild.Columns.Add("ChildDouble", typeof(double));

            dtChild.Rows.Add(new object[] { 1, 1, "1-String1", "1-String2", new DateTime(2000, 1, 1, 0, 0, 0, 0), 1.534 });
            dtChild.Rows.Add(new object[] { 1, 2, "2-String1", "2-String2", DateTime.MaxValue, -1.534 });
            dtChild.Rows.Add(new object[] { 1, 3, "3-String1", "3-String2", DateTime.MinValue, double.MaxValue / 10000 });
            dtChild.Rows.Add(new object[] { 2, 1, "1-String1", "1-String2", new DateTime(1973, 6, 20, 0, 0, 0, 0), double.MinValue * 10000 });
            dtChild.Rows.Add(new object[] { 2, 2, "2-String1", "2-String2", new DateTime(2008, 12, 1, 13, 59, 59, 59), 0.45 });
            dtChild.Rows.Add(new object[] { 2, 3, "3-String1", "3-String2", new DateTime(2003, 1, 1, 1, 1, 1, 1), 0.55 });
            dtChild.Rows.Add(new object[] { 5, 1, "1-String1", "1-String2", new DateTime(2002, 1, 1, 1, 1, 1, 1), 0 });
            dtChild.Rows.Add(new object[] { 5, 2, "2-String1", "2-String2", new DateTime(2001, 1, 1, 1, 1, 1, 1), 10 });
            dtChild.Rows.Add(new object[] { 5, 3, "3-String1", "3-String2", new DateTime(2000, 1, 1, 1, 1, 1, 1), 20 });
            dtChild.Rows.Add(new object[] { 6, 1, "1-String1", "1-String2", new DateTime(2000, 1, 1, 1, 1, 1, 0), 25 });
            dtChild.Rows.Add(new object[] { 6, 2, "2-String1", "2-String2", new DateTime(2000, 1, 1, 1, 1, 0, 0), 30 });
            dtChild.Rows.Add(new object[] { 6, 3, "3-String1", "3-String2", new DateTime(2000, 1, 1, 0, 0, 0, 0), 35 });
            dtChild.AcceptChanges();
            return dtChild;
        }

        public static DataTable CreateParentDataTable()
        {
            DataTable dtParent = new DataTable("Parent");

            dtParent.Columns.Add("ParentId", typeof(int));
            dtParent.Columns.Add("String1", typeof(string));
            dtParent.Columns.Add("String2", typeof(string));

            dtParent.Columns.Add("ParentDateTime", typeof(DateTime));
            dtParent.Columns.Add("ParentDouble", typeof(double));
            dtParent.Columns.Add("ParentBool", typeof(bool));

            dtParent.Rows.Add(new object[] { 1, "1-String1", "1-String2", new DateTime(2005, 1, 1, 0, 0, 0, 0), 1.534, true });
            dtParent.Rows.Add(new object[] { 2, "2-String1", "2-String2", new DateTime(2004, 1, 1, 0, 0, 0, 1), -1.534, true });
            dtParent.Rows.Add(new object[] { 3, "3-String1", "3-String2", new DateTime(2003, 1, 1, 0, 0, 1, 0), double.MinValue * 10000, false });
            dtParent.Rows.Add(new object[] { 4, "4-String1", "4-String2", new DateTime(2002, 1, 1, 0, 1, 0, 0), double.MaxValue / 10000, true });
            dtParent.Rows.Add(new object[] { 5, "5-String1", "5-String2", new DateTime(2001, 1, 1, 1, 0, 0, 0), 0.755, true });
            dtParent.Rows.Add(new object[] { 6, "6-String1", "6-String2", new DateTime(2000, 1, 1, 0, 0, 0, 0), 0.001, false });
            dtParent.AcceptChanges();
            return dtParent;
        }

        //This method replace the DataSet GetXmlSchema method
        //used to compare DataSets
        //Created by Ofer (13-Nov-03) becuase DataSet GetXmlSchema method is not yet implemented in java 
        public static string GetDSSchema(DataSet ds)
        {
            string strSchema = "DataSet Name=" + ds.DataSetName + "\n";
            //Get relations
            foreach (DataRelation dl in ds.Relations)
            {
                strSchema += "\t" + "DataRelation Name=" + dl.RelationName;
                foreach (DataColumn dc in dl.ParentColumns)
                    strSchema += "\t" + "ParentColummn=" + dc.ColumnName;
                foreach (DataColumn dc in dl.ChildColumns)
                    strSchema += "\t" + "ChildColumn=" + dc.ColumnName;
                strSchema += "\n";
            }
            //Get teables
            foreach (DataTable dt in ds.Tables)
            {
                strSchema += "Table=" + dt.TableName + "\t";
                //Get Constraints  
                strSchema += "Constraints =";
                foreach (Constraint cs in dt.Constraints)
                    strSchema += cs.GetType().Name + ", ";
                strSchema += "\n";
                //Get PrimaryKey Columns
                strSchema += "PrimaryKey Columns index:=";
                foreach (DataColumn dc in dt.PrimaryKey)
                    strSchema += dc.Ordinal + ", ";
                strSchema += "\n";
                //Get Columns
                foreach (DataColumn dc in dt.Columns)
                {
                    strSchema += "ColumnName=" + dc.ColumnName + "\t" +
                        "ColumnType=" + dc.DataType.Name + "\t" +
                        "AllowDBNull=" + dc.AllowDBNull.ToString() + "\t" +
                        "DefaultValue=" + dc.DefaultValue.ToString() + "\t" +
                        "Unique=" + dc.Unique.ToString() + "\t" +
                        "ReadOnly=" + dc.ReadOnly.ToString() + "\n";
                }
                strSchema += "\n";
            }
            return strSchema;
        }

        public static DataTable CreateUniqueConstraint()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            return CreateUniqueConstraint(dt);
        }

        public static DataTable CreateUniqueConstraint(DataTable dt)
        {
            Constraint con = new UniqueConstraint(dt.Columns["ParentId"]);
            dt.Constraints.Add(con);
            return dt;
        }

        public static void TryToBreakUniqueConstraint()
        {
            //Create the constraint
            DataTable dt = CreateUniqueConstraint();
            //Try to violate the constraint

            DataRow dr1 = dt.NewRow();
            dr1[0] = 1;
            dt.Rows.Add(dr1);
        }

        public static DataSet CreateForigenConstraint()
        {
            DataTable parent = DataProvider.CreateParentDataTable();
            DataTable child = DataProvider.CreateChildDataTable();
            var ds = new DataSet();
            ds.Tables.Add(parent);
            ds.Tables.Add(child);

            Constraint con1 = new ForeignKeyConstraint(parent.Columns[0], child.Columns[0]);
            child.Constraints.Add(con1);

            return ds;
        }

        public static void TryToBreakForigenConstraint()
        {
            DataSet ds = CreateForigenConstraint();
            //Code to break:

            DataRow dr = ds.Tables[1].NewRow();
            dr[0] = 7;
            ds.Tables[1].Rows.Add(dr);

            ds.AcceptChanges();
            ds.EnforceConstraints = true;
        }

        public static readonly string own_schema =
$@"<?xml version=""1.0""?>
<xs:schema id=""test_dataset"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
  <xs:element name=""test_dataset"" msdata:IsDataSet=""true"" msdata:Locale=""fi-FI"">
    <xs:complexType>
      <xs:choice maxOccurs=""unbounded"">
        <xs:element name=""test_table"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""first"" msdata:Caption=""test"" default=""test_default_value"" minOccurs=""0"">
                <xs:simpleType>
                  <xs:restriction base=""xs:string"">
                    <xs:maxLength value=""100"" />
                  </xs:restriction>
                </xs:simpleType>
              </xs:element>
              <xs:element name=""second"" msdata:DataType=""{typeof(System.Data.SqlTypes.SqlGuid).FullName}"" type=""xs:string"" minOccurs=""0"" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
        <xs:element name=""second_test_table"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""second_first"" default=""default_value"" minOccurs=""0"">
                <xs:simpleType>
                  <xs:restriction base=""xs:string"">
                    <xs:maxLength value=""100"" />
                  </xs:restriction>
                </xs:simpleType>
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
    <xs:unique name=""Constraint1"">
      <xs:selector xpath="".//test_table"" />
      <xs:field xpath=""first"" />
    </xs:unique>
    <xs:unique name=""second_test_table_Constraint1"" msdata:ConstraintName=""Constraint1"">
      <xs:selector xpath="".//second_test_table"" />
      <xs:field xpath=""second_first"" />
    </xs:unique>
  </xs:element>
</xs:schema>";

        public static readonly string own_schema1 =
$@"<?xml version=""1.0"" standalone=""yes""?>
<xs:schema id=""test_dataset"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
  <xs:element name=""test_dataset"" msdata:IsDataSet=""true"" msdata:MainDataTable=""test_table"" msdata:UseCurrentLocale=""true"">
    <xs:complexType>
      <xs:choice maxOccurs=""unbounded"">
        <xs:element name=""test_table"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""first"" msdata:Caption=""test"" default=""test_default_value"" minOccurs=""0"">
                <xs:simpleType>
                  <xs:restriction base=""xs:string"">
                    <xs:maxLength value=""100"" />
                  </xs:restriction>
                </xs:simpleType>
              </xs:element>
              <xs:element name=""second"" msdata:DataType=""{typeof(System.Data.SqlTypes.SqlGuid).FullName}"" type=""xs:string"" minOccurs=""0"" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
    <xs:unique name=""Constraint1"">
      <xs:selector xpath="".//test_table"" />
      <xs:field xpath=""first"" />
    </xs:unique>
    <xs:unique name=""second_test_table_Constraint1"" msdata:ConstraintName=""Constraint1"">
      <xs:selector xpath="".//second_test_table"" />
      <xs:field xpath=""second_first"" />
    </xs:unique>
  </xs:element>
</xs:schema>";

        public static readonly string own_schema2 =
$@"<?xml version=""1.0"" standalone=""yes""?>
<xs:schema id=""test_dataset"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
  <xs:element name=""test_dataset"" msdata:IsDataSet=""true"" msdata:MainDataTable=""second_test_table"" msdata:UseCurrentLocale=""true"">
    <xs:complexType>
      <xs:choice maxOccurs=""unbounded"">
        <xs:element name=""second_test_table"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""second_first"" default=""default_value"" minOccurs=""0"">
                <xs:simpleType>
                  <xs:restriction base=""xs:string"">
                    <xs:maxLength value=""100"" />
                  </xs:restriction>
                </xs:simpleType>
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
    <xs:unique name=""Constraint1"">
      <xs:selector xpath="".//test_table"" />
      <xs:field xpath=""first"" />
    </xs:unique>
    <xs:unique name=""second_test_table_Constraint1"" msdata:ConstraintName=""Constraint1"">
      <xs:selector xpath="".//second_test_table"" />
      <xs:field xpath=""second_first"" />
    </xs:unique>
  </xs:element>
</xs:schema>";

        public static readonly string store =
@"<xsd:schema xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">

 <xsd:element name=""bookstore"" type=""bookstoreType""/>

 <xsd:complexType name=""bookstoreType"">
  <xsd:sequence maxOccurs=""unbounded"">
   <xsd:element name=""book""  type=""bookType""/>
  </xsd:sequence>
 </xsd:complexType>

 <xsd:complexType name=""bookType"">
  <xsd:sequence>
   <xsd:element name=""title"" type=""xsd:string""/>
   <xsd:element name=""author"" type=""authorName""/>
   <xsd:element name=""price""  type=""xsd:decimal""/>
  </xsd:sequence>
  <xsd:attribute name=""genre"" type=""xsd:string""/>
 </xsd:complexType>

 <xsd:complexType name=""authorName"">
  <xsd:sequence>
   <xsd:element name=""first-name""  type=""xsd:string""/>
   <xsd:element name=""last-name"" type=""xsd:string""/>
  </xsd:sequence>
 </xsd:complexType>

</xsd:schema>";

        public static readonly string region =
            @"<?xml version=""1.0"" standalone=""yes""?>
            <Root>
             <Region>
               <RegionID>1</RegionID>
               <RegionDescription>Eastern
               </RegionDescription>
             </Region>
             <Region>
               <RegionID>2</RegionID>
               <RegionDescription>Western
               </RegionDescription>
             </Region>
            </Root>";
    }
}

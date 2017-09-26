// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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
//


using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Xunit;

namespace System.Data.Tests
{
    public static class DataSetAssertion
    {
        public static string GetNormalizedSchema(string source)
        {
            /*
                        // Due to the implementation difference, we must have
                        // one more step to reorder attributes. Here, read
                        // schema document into XmlSchema once, and compare
                        // output string with those emission from Write().
                        XmlSchema xs = XmlSchema.Read (new XmlTextReader (
                            new StringReader (source)), null);
                        StringWriter writer = new StringWriter ();
                        xs.Write (writer);
                        return writer.ToString ();
            */
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(source);
            SortAttributes(doc.DocumentElement);
            StringWriter writer = new StringWriter();
            doc.Save(writer);
            return writer.ToString();
        }

        private static void SortAttributes(XmlElement el)
        {
            SortAttributesAttributes(el);
            ArrayList al = new ArrayList();
            foreach (XmlNode n in el.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Element)
                    SortAttributes(n as XmlElement);
                if (n.NodeType == XmlNodeType.Comment)
                    al.Add(n);
            }
            foreach (XmlNode n in al)
                el.RemoveChild(n);
        }

        private static void SortAttributesAttributes(XmlElement el)
        {
            ArrayList al = new ArrayList();
            foreach (XmlAttribute a in el.Attributes)
                al.Add(a.Name);
            al.Sort();
            string[] names = (string[])al.ToArray(typeof(string));
            al.Clear();
            foreach (string name in names)
                al.Add(el.RemoveAttributeNode(
                    el.GetAttributeNode(name)));
            foreach (XmlAttribute a in al)
                // Exclude xmlns="" here.
                if (a.Name != "xmlns")// || a.Value != String.Empty)
                    el.SetAttributeNode(a);
        }

        public static void AssertDataSet(string label, DataSet ds, string name, int tableCount, int relCount)
        {
            Assert.Equal(name, ds.DataSetName);
            Assert.Equal(tableCount, ds.Tables.Count);
            if (relCount >= 0)
                Assert.Equal(relCount, ds.Relations.Count);
        }

        public static void AssertDataTable(string label, DataTable dt, string name, int columnCount, int rowCount, int parentRelationCount, int childRelationCount, int constraintCount, int primaryKeyLength)
        {
            Assert.Equal(name, dt.TableName);
            Assert.Equal(columnCount, dt.Columns.Count);
            Assert.Equal(rowCount, dt.Rows.Count);
            Assert.Equal(parentRelationCount, dt.ParentRelations.Count);
            Assert.Equal(childRelationCount, dt.ChildRelations.Count);
            Assert.Equal(constraintCount, dt.Constraints.Count);
            Assert.Equal(primaryKeyLength, dt.PrimaryKey.Length);
        }

        public static void AssertReadXml(DataSet ds, string label, string xml, XmlReadMode readMode, XmlReadMode resultMode, string datasetName, int tableCount)
        {
            DataSetAssertion.AssertReadXml(ds, label, xml, readMode, resultMode, datasetName, tableCount, ReadState.EndOfFile, null, null);
        }

        public static void AssertReadXml(DataSet ds, string label, string xml, XmlReadMode readMode, XmlReadMode resultMode, string datasetName, int tableCount, ReadState state)
        {
            DataSetAssertion.AssertReadXml(ds, label, xml, readMode, resultMode, datasetName, tableCount, state, null, null);
        }

        // a bit detailed version
        public static void AssertReadXml(DataSet ds, string label, string xml, XmlReadMode readMode, XmlReadMode resultMode, string datasetName, int tableCount, ReadState state, string readerLocalName, string readerNS)
        {
            XmlReader xtr = new XmlTextReader(xml, XmlNodeType.Element, null);
            Assert.Equal(resultMode, ds.ReadXml(xtr, readMode));
            AssertDataSet(label + ".dataset", ds, datasetName, tableCount, -1);
            Assert.Equal(state, xtr.ReadState);
            if (readerLocalName != null)
                Assert.Equal(readerLocalName, xtr.LocalName);
            if (readerNS != null)
                Assert.Equal(readerNS, xtr.NamespaceURI);
        }

        public static void AssertDataRelation(string label, DataRelation rel, string name, bool nested,
            string[] parentColNames, string[] childColNames,
            bool existsUK, bool existsFK)
        {
            Assert.Equal(name, rel.RelationName);
            Assert.Equal(nested, rel.Nested);
            for (int i = 0; i < parentColNames.Length; i++)
                Assert.Equal(parentColNames[i], rel.ParentColumns[i].ColumnName);
            Assert.Equal(parentColNames.Length, rel.ParentColumns.Length);
            for (int i = 0; i < childColNames.Length; i++)
                Assert.Equal(childColNames[i], rel.ChildColumns[i].ColumnName);
            Assert.Equal(childColNames.Length, rel.ChildColumns.Length);
            if (existsUK)
                Assert.NotNull(rel.ParentKeyConstraint);
            else
                Assert.Null(rel.ParentKeyConstraint);
            if (existsFK)
                Assert.NotNull(rel.ChildKeyConstraint);
            else
                Assert.Null(rel.ChildKeyConstraint);
        }

        public static void AssertUniqueConstraint(string label, UniqueConstraint uc,
            string name, bool isPrimaryKey, string[] colNames)
        {
            Assert.Equal(name, uc.ConstraintName);
            Assert.Equal(isPrimaryKey, uc.IsPrimaryKey);
            for (int i = 0; i < colNames.Length; i++)
                Assert.Equal(colNames[i], uc.Columns[i].ColumnName);
            Assert.Equal(colNames.Length, uc.Columns.Length);
        }

        public static void AssertForeignKeyConstraint(string label,
            ForeignKeyConstraint fk, string name,
            AcceptRejectRule acceptRejectRule, Rule delRule, Rule updateRule,
            string[] colNames, string[] relColNames)
        {
            Assert.Equal(name, fk.ConstraintName);
            Assert.Equal(acceptRejectRule, fk.AcceptRejectRule);
            Assert.Equal(delRule, fk.DeleteRule);
            Assert.Equal(updateRule, fk.UpdateRule);
            for (int i = 0; i < colNames.Length; i++)
                Assert.Equal(colNames[i], fk.Columns[i].ColumnName);
            Assert.Equal(colNames.Length, fk.Columns.Length);
            for (int i = 0; i < relColNames.Length; i++)
                Assert.Equal(relColNames[i], fk.RelatedColumns[i].ColumnName);
            Assert.Equal(relColNames.Length, fk.RelatedColumns.Length);
        }

        public static void AssertDataColumn(string label, DataColumn col,
            string colName, bool allowDBNull,
            bool autoIncr, int autoIncrSeed, int autoIncrStep,
            string caption, MappingType colMap,
            Type type, object defaultValue, string expression,
            int maxLength, string ns, int ordinal, string prefix,
            bool readOnly, bool unique)
        {
            Assert.Equal(colName, col.ColumnName);
            Assert.Equal(allowDBNull, col.AllowDBNull);
            Assert.Equal(autoIncr, col.AutoIncrement);
            Assert.Equal(autoIncrSeed, col.AutoIncrementSeed);
            Assert.Equal(autoIncrStep, col.AutoIncrementStep);
            Assert.Equal(caption, col.Caption);
            Assert.Equal(colMap, col.ColumnMapping);
            Assert.Equal(type, col.DataType);
            Assert.Equal(defaultValue, col.DefaultValue);
            Assert.Equal(expression, col.Expression);
            Assert.Equal(maxLength, col.MaxLength);
            Assert.Equal(ns, col.Namespace);
            if (ordinal >= 0)
                Assert.Equal(ordinal, col.Ordinal);
            Assert.Equal(prefix, col.Prefix);
            Assert.Equal(readOnly, col.ReadOnly);
            Assert.Equal(unique, col.Unique);
        }
    }
}


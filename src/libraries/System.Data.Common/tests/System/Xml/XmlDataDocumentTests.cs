// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0618 // use of obsolete methods

using System.Data;
using System.Data.Tests;
using System.IO;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlDataDocumentTests
    {
        [Fact]
        public static void XmlDataDocument_DataSet()
        {
            DataTable parent = DataProvider.CreateParentDataTable();
            DataTable child = DataProvider.CreateChildDataTable();
            DataSet ds = new DataSet();
            ds.Tables.Add(parent);
            ds.Tables.Add(child);

            XmlDataDocument doc = new XmlDataDocument(ds);
            Assert.IsType<DataSet>(doc.DataSet);
            Assert.Equal(ds, doc.DataSet);
        }

        [Fact]
        public static void XmlDataDocument_CloneNode()
        {
            DataSet ds = Create();
            DataRow dr = ds.Tables[0].Rows[0];
            XmlDataDocument doc = new XmlDataDocument(ds);
            ds.EnforceConstraints = false;

            XmlNode node = doc.CloneNode(deep: true);
            Assert.True(node.HasChildNodes);

            node = doc.CloneNode(deep: false);
            Assert.False(node.HasChildNodes);
        }

        [Fact]
        public static void XmlDataDocument_CreateElement()
        {
            XmlDataDocument doc = new XmlDataDocument();
            XmlElement element = doc.CreateElement("prefix", "localName", "namespaceURI");
            Assert.NotNull(element);
            Assert.Equal("prefix", element.Prefix);
            Assert.Equal("prefix:localName", element.Name);
            Assert.Equal("namespaceURI", element.NamespaceURI);
        }

        [Fact]
        public static void XmlDataDocument_CreateEntityReference()
        {
            XmlDataDocument doc = new XmlDataDocument();
            Assert.Throws<NotSupportedException>(() => doc.CreateEntityReference("name"));
        }

        [Fact]
        public static void XmlDataDocument_GetElementById()
        {
            XmlDataDocument doc = new XmlDataDocument();
            Assert.Throws<NotSupportedException>(() => doc.GetElementById("elemId"));
        }

        [Fact]
        public static void XmlDataDocument_GetElementFromRow()
        {
            DataSet ds = Create();
            XmlDataDocument doc = new XmlDataDocument(ds);
            XmlElement element = doc.GetElementFromRow(ds.Tables[0].Rows[0]);
            Assert.NotNull(element);
            Assert.Equal("Test", element.Name);
        }

        [Fact]
        public static void XmlDataDocument_GetElementsByTagName()
        {
            XmlDataDocument doc = new XmlDataDocument();
            XmlNodeList nodeList = doc.GetElementsByTagName("missingTag");
            Assert.NotNull(nodeList);
            Assert.Equal(0, nodeList.Count);

            DataSet ds = Create();
            doc = new XmlDataDocument(ds);
            nodeList = doc.GetElementsByTagName("Test");
            Assert.NotNull(nodeList);
            Assert.Equal(1, nodeList.Count);
            Assert.True(nodeList[0].HasChildNodes);
        }

        [Fact]
        public static void XmlDataDocument_GetRowFromElement()
        {
            DataSet ds = Create();
            DataRow dr = ds.Tables[0].Rows[0];
            XmlDataDocument doc = new XmlDataDocument(ds);
            XmlElement xmlElement = doc.GetElementFromRow(dr);
            Assert.Equal(dr, doc.GetRowFromElement(xmlElement));
        }

        [Fact]
        public static void XmlDataDocument_Load_Throws()
        {
            XmlDataDocument doc = new XmlDataDocument();
            Assert.Throws<FileNotFoundException>(() => doc.Load("missingfile"));
        }
        
        [Fact]
        public static void XmlDataDocument_LoadXmlReader()
        {
            string xml = "<CustomTypesData>" + Environment.NewLine +
                        "<CustomTypesTable>" + Environment.NewLine +
                        "<Dummy>99</Dummy>" + Environment.NewLine +
                        "</CustomTypesTable>" + Environment.NewLine +
                        "</CustomTypesData>" + Environment.NewLine;

            StringReader sr = new StringReader(xml);
            XmlReader xr = new XmlTextReader(sr);
            XmlDataDocument doc = new XmlDataDocument();
            doc.Load(xr);

            var nodeList = doc.GetElementsByTagName("CustomTypesData");
            Assert.NotNull(nodeList);
            Assert.Equal(1, nodeList.Count);
            Assert.True(nodeList[0].HasChildNodes);
            Assert.Equal("CustomTypesData", nodeList[0].Name);
        }

        private static DataSet Create()
        {
            DataSet ds = new DataSet("Set");
            DataTable dt = new DataTable("Test");
            dt.Columns.Add("CustName", typeof(string));
            dt.Columns.Add("Type", typeof(Type));
            DataRow dr = dt.NewRow();
            dr["CustName"] = DBNull.Value;
            dr["Type"] = typeof(DBNull);
            dt.Rows.Add(dr);
            ds.Tables.Add(dt);
            return ds;
        }
    }
}

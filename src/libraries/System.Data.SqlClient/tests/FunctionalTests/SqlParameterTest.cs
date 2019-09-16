// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlParameterTests
    {
        [Fact]
        public void ParameterPrecisionOnInterfaceType()
        {
            SqlParameter parameter = new SqlParameter();
            IDbDataParameter interfaceParameter = parameter;
            interfaceParameter.Precision = 10;
            interfaceParameter.Scale = 5;

            Assert.Equal(10, interfaceParameter.Precision);
            Assert.Equal(5, interfaceParameter.Scale);

            Assert.Equal(10, parameter.Precision);
            Assert.Equal(5, parameter.Scale);
        }

        [Fact]
        public void ParameterPrecisionOnBaseType()
        {
            SqlParameter parameter = new SqlParameter();
            DbParameter baseParameter = parameter;
            baseParameter.Precision = 10;
            baseParameter.Scale = 5;

            Assert.Equal(10, baseParameter.Precision);
            Assert.Equal(5, baseParameter.Scale);

            Assert.Equal(10, parameter.Precision);
            Assert.Equal(5, parameter.Scale);
        }

        [Fact]
        public void CreateParameterWithValidXmlSchema()
        {
            string xmlDatabase = "database";
            string xmlSchema = "schema";
            string xmlName = "name";

            SqlParameter parameter = new SqlParameter("@name", SqlDbType.Int, 4, ParameterDirection.Input, 0, 0, "name", DataRowVersion.Original, false, 1, xmlDatabase, xmlSchema, xmlName);

            Assert.Equal(xmlDatabase, parameter.XmlSchemaCollectionDatabase);
            Assert.Equal(xmlSchema, parameter.XmlSchemaCollectionOwningSchema);
            Assert.Equal(xmlName, parameter.XmlSchemaCollectionName);
        }

        [Fact]
        public void CreateParameterWithEmptyXmlSchema()
        {
            SqlParameter parameter = new SqlParameter("@name", SqlDbType.Int, 4, ParameterDirection.Input, 0, 0, "name", DataRowVersion.Original, false, 1, string.Empty, string.Empty, string.Empty);

            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionDatabase);
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionOwningSchema);
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionName);
        }

        [Fact]
        public void CreateParameterWithNullXmlSchema()
        {
            SqlParameter parameter = new SqlParameter("@name", SqlDbType.Int, 4, ParameterDirection.Input, 0, 0, "name", DataRowVersion.Original, false, 1, null, null, null);

            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionDatabase);
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionOwningSchema);
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionName);
        }

        [Fact]
        public void CreateParameterWithoutXmlSchema()
        {
            SqlParameter parameter = new SqlParameter();

            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionDatabase);
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionOwningSchema);
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionName);
        }

        [Fact]
        public void SetParameterXmlSchema()
        {
            SqlParameter parameter = new SqlParameter();

            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionName);

            // verify that if we set it to null we still get an empty string back
            parameter.XmlSchemaCollectionName = null;
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionName);

            // verify that if we set a value we get it back
            parameter.XmlSchemaCollectionName = "name";
            Assert.Equal("name", parameter.XmlSchemaCollectionName);

            // verify that if we set it explicitly to null it reverts to empty string
            parameter.XmlSchemaCollectionName = null;
            Assert.Equal(string.Empty, parameter.XmlSchemaCollectionName);
        }
    }
}

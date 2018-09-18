// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.SqlServer.Server;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlFacetAttributeTests
    {
        [Fact]
        public void Basic()
        {
            var attrib = new SqlFacetAttribute();

            attrib.IsFixedLength = true;
            attrib.IsNullable = false;
            attrib.MaxSize = 123;
            attrib.Precision = 234;
            attrib.Scale = 345;

            Assert.Equal(true, attrib.IsFixedLength);
            Assert.Equal(false, attrib.IsNullable);
            Assert.Equal(123, attrib.MaxSize);
            Assert.Equal(234, attrib.Precision);
            Assert.Equal(345, attrib.Scale);
        }
    }
}
 

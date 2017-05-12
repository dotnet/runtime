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
    }
}

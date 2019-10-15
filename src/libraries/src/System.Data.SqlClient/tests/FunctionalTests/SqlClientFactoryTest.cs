// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlClientFactoryTest
    {
        [Fact]
        public void InstanceTest()
        {
            SqlClientFactory instance = SqlClientFactory.Instance;
            Assert.NotNull(instance);
            Assert.Same(instance, SqlClientFactory.Instance);
        }

        public static readonly object[][] FactoryMethodTestData =
        {
            new object[] { new Func<object>(SqlClientFactory.Instance.CreateCommand), typeof(SqlCommand) },
            new object[] { new Func<object>(SqlClientFactory.Instance.CreateConnection), typeof(SqlConnection) },
            new object[] { new Func<object>(SqlClientFactory.Instance.CreateConnectionStringBuilder), typeof(SqlConnectionStringBuilder) },
            new object[] { new Func<object>(SqlClientFactory.Instance.CreateDataAdapter), typeof(SqlDataAdapter) },
            new object[] { new Func<object>(SqlClientFactory.Instance.CreateParameter), typeof(SqlParameter) },
        };

        [Theory]
        [MemberData(nameof(FactoryMethodTestData))]
        public void FactoryMethodTest(Func<object> factory, Type expectedType)
        {
            object value1 = factory();
            Assert.NotNull(value1);
            Assert.IsType(expectedType, value1);

            object value2 = factory();
            Assert.NotNull(value2);
            Assert.IsType(expectedType, value2);

            Assert.NotSame(value1, value2);
        }
    }
}

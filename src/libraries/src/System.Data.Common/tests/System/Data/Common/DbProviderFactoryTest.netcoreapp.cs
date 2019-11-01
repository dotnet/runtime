// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Data.Common;

namespace System.Data.Tests.Common
{
    public class DbProviderFactoryTest
    {
        [Fact]
        public void CanCreateDataAdapter()
        {
            Assert.True(ProviderFactoryWithExtras.Instance.CanCreateDataAdapter);
            Assert.False(ProviderFactoryWithoutExtras.Instance.CanCreateDataAdapter);
        }

        [Fact]
        public void CanCreateCommandBuilder()
        {
            Assert.True(ProviderFactoryWithExtras.Instance.CanCreateCommandBuilder);
            Assert.False(ProviderFactoryWithoutExtras.Instance.CanCreateCommandBuilder);
        }

        public sealed class ProviderFactoryWithExtras : DbProviderFactory
        {
            public static readonly ProviderFactoryWithExtras Instance = new ProviderFactoryWithExtras();
            private ProviderFactoryWithExtras() { }

            public override DbDataAdapter CreateDataAdapter() => new MyAdapter();
            public override DbCommandBuilder CreateCommandBuilder() => new MyCommandBuilder();
        }

        public sealed class ProviderFactoryWithoutExtras : DbProviderFactory
        {
            public static readonly ProviderFactoryWithoutExtras Instance = new ProviderFactoryWithoutExtras();
            private ProviderFactoryWithoutExtras() { }
        }

        private class MyAdapter : DbDataAdapter {}

        private class MyCommandBuilder : DbCommandBuilder
        {
            protected override string GetParameterPlaceholder(int parameterOrdinal) => null;
            protected override string GetParameterName(string parameterName) => null;
            protected override string GetParameterName(int parameterOrdinal) => null;
            protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause) {}
            protected override void SetRowUpdatingHandler(DbDataAdapter adapter) {}
        }
    }
}

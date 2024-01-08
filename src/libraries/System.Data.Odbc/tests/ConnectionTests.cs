// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Data.Odbc.Tests
{
    public class ConnectionTests : IntegrationTestBase
    {
        // Bug #96278 fixed only on .NET, not on .NET Framework
        [ConditionalFact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void DbConnectionFactories_GetFactory_from_connection()
        {
            DbProviderFactory factory = DbProviderFactories.GetFactory(connection);
            Assert.Same(OdbcFactory.Instance, factory);
        }
    }
}

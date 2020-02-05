// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Tests
{
    public class FacadeTest
    {
        [Theory]
        [InlineData("Microsoft.SqlServer.Server.SqlMetaData")] // Type from System.Data.SqlClient
        [InlineData("System.Data.SqlTypes.SqlBytes")] // Type from System.Data.Common
        public void TestSystemData(string typeName)
        {
            // Verify that the type can be loaded via .NET Framework compat facade
            Type.GetType(typeName + ", System.Data", throwOnError: true);
        }
    }
}

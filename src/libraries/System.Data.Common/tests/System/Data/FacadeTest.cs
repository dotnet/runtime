// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Tests
{
    public class FacadeTest
    {
        [Theory]
        [InlineData("Microsoft.SqlServer.Server.SqlMetaData")] // Type from System.Data.SqlClient
        [InlineData("System.Data.SqlTypes.SqlBytes")] // Type from System.Data.Common
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        public void TestSystemData(string typeName)
        {
            // Verify that the type can be loaded via .NET Framework compat facade
            Type.GetType(typeName + ", System.Data", throwOnError: true);
        }
    }
}

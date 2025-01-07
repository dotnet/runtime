// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyCompanyAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("company")]
        public void Ctor_String(string company)
        {
            var attribute = new AssemblyCompanyAttribute(company);
            Assert.Equal(company, attribute.Company);
        }
    }
}

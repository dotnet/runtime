// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ImportedFromTypeLibAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("Value")]
        public void Ctor_TlbFile(string tlbFile)
        {
            var attribute = new ImportedFromTypeLibAttribute(tlbFile);
            Assert.Equal(tlbFile, attribute.Value);
        }
    }
}

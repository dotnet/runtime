// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class MyGroupCollectionAttributeTests
    {
        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "", "", "")]
        [InlineData("typeToCollect", "createInstanceMethodName", "disposeInstanceMethodName", "defaultInstanceAlias")]
        public void Ctor_String_String_String(string typeToCollect, string createInstanceMethodName, string disposeInstanceMethodName, string defaultInstanceAlias)
        {
            var attribute = new MyGroupCollectionAttribute(typeToCollect, createInstanceMethodName, disposeInstanceMethodName, defaultInstanceAlias);
            Assert.Equal(typeToCollect, attribute.MyGroupName);
            Assert.Equal(createInstanceMethodName, attribute.CreateMethod);
            Assert.Equal(disposeInstanceMethodName, attribute.DisposeMethod);
            Assert.Equal(defaultInstanceAlias, attribute.DefaultInstanceAlias);
        }
    }
}

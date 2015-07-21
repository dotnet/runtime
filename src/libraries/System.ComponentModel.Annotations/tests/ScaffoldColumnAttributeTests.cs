// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class ScaffoldColumnAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Can_construct_and_get_Scaffold(bool value)
        {
            var attribute = new ScaffoldColumnAttribute(value);
            Assert.Equal(value, attribute.Scaffold);
        }
    }
}

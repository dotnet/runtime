// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class DataErrorsChangedEventArgsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("propertyName")]
        public void Ctor_String(string propertyName)
        {
            var e = new DataErrorsChangedEventArgs(propertyName);
            Assert.Equal(propertyName, e.PropertyName);
        }
    }
}

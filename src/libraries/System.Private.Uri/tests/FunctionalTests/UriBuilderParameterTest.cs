// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Summary description for UriBuilderParamiterTest
    /// </summary>
    public class UriBuilderParameterTest
    {
        [Fact]
        public void UriBuilder_Ctor_NullParameter_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => new UriBuilder((Uri)null));
        }
    }
}

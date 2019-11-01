// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Throws<ArgumentNullException>(() =>
           {
               UriBuilder builder = new UriBuilder((Uri)null);
           });
        }
    }
}

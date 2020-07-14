// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Summary description for WorkItemTest
    /// </summary>
    public class UriParameterValidationTest
    {
        [Fact]
        public void Uri_MakeRelativeUri_NullParameter_ThrowsArgumentException()
        {
            Uri baseUri = new Uri("http://localhost/");
            Assert.Throws<ArgumentNullException>(() => baseUri.MakeRelativeUri((Uri)null));
        }

        [Fact]
        public void Uri_TryCreate_NullParameter_ReturnsFalse()
        {
            Uri baseUri = new Uri("http://localhost/");
            Assert.False(Uri.TryCreate(baseUri, (Uri)null, out _));
            Assert.False(Uri.TryCreate((Uri)null, baseUri, out _));
            Assert.False(Uri.TryCreate((Uri)null, (Uri)null, out _)) ;
        }

        [Fact]
        public void Uri_IsBaseOf_NullParameter_ThrowsArgumentException()
        {
            Uri baseUri = new Uri("http://localhost/");
            Assert.Throws<ArgumentNullException>(() => baseUri.IsBaseOf((Uri)null));
        }
    }
}

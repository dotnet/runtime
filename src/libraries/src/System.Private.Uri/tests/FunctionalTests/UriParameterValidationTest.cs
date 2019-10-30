// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Throws<ArgumentNullException>(() =>
          {
              Uri baseUri = new Uri("http://localhost/");
              Uri rel = baseUri.MakeRelativeUri((Uri)null);
          });
        }

        [Fact]
        public void Uri_TryCreate_NullParameter_ReturnsFalse()
        {
            Uri baseUri = new Uri("http://localhost/");
            Uri result;
            Assert.False(Uri.TryCreate(baseUri, (Uri)null, out result));
            Assert.False(Uri.TryCreate((Uri)null, baseUri, out result));
            Assert.False(Uri.TryCreate((Uri)null, (Uri)null, out result));
        }

        [Fact]
        public void Uri_IsBaseOf_NullParameter_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
          {
              Uri baseUri = new Uri("http://localhost/");
              Uri relUri = null;
              bool success = baseUri.IsBaseOf(relUri);
          });
        }
    }
}

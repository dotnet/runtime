using System.Collections.Generic;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public partial class HttpMethodTest
    {
        [Fact]
        public void Patch_VerifyValue_PropertyNameMatchesHttpMethodName()
        {
            Assert.Equal("PATCH", HttpMethod.Patch.Method);
        }

        static partial void AddStaticHttpMethods(List<object[]> staticHttpMethods)
        {
            staticHttpMethods.Add(new object[] { HttpMethod.Patch });
        }
    }
}

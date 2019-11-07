// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class ReflectionContextTests
    {
        [Fact]
        public void GetTypeForObject_Invoke_ReturnsExpected()
        {
            var context = new SubReflectionContext();
            Assert.Equal(typeof(string).GetTypeInfo(), context.GetTypeForObject(1));
        }

        [Fact]
        public void GetTypeForObject_NullValue_ThrowsArgumentNullException()
        {
            var context = new SubReflectionContext();
            AssertExtensions.Throws<ArgumentNullException>("value", () => context.GetTypeForObject(null));
        }

        private class SubReflectionContext : ReflectionContext
        {
            public override Assembly MapAssembly(Assembly assembly) => assembly;

            public override TypeInfo MapType(TypeInfo type)
            {
                Assert.Equal(typeof(int).GetTypeInfo(), type);
                return typeof(string).GetTypeInfo();
            }
        }
    }
}

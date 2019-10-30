// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class LCIDConversionAttributeTests
    {
        [LCIDConversion(1337)]
        private int Func(int a, int b) => a + b;

        [Fact]
        public void Exists()
        {
            Type type = typeof(LCIDConversionAttributeTests);
            MethodInfo method = type.GetTypeInfo().DeclaredMethods.Single(m => m.Name == "Func");
            LCIDConversionAttribute attribute = Assert.Single(method.GetCustomAttributes<LCIDConversionAttribute>(inherit: false));
            Assert.Equal(1337, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_Lcid(int lcid)
        {
            var attribute = new LCIDConversionAttribute(lcid);
            Assert.Equal(lcid, attribute.Value);
        }
    }
}

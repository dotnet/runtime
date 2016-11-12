// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class AllowReversePInvokeCallsAttributeTests
    {
        [AllowReversePInvokeCalls]
        private int Func(int a, int b)
        {
            return a + b;
        }

        [Fact]
        public void Exists()
        {
            var type = typeof(AllowReversePInvokeCallsAttributeTests);
            var method = type.GetTypeInfo().DeclaredMethods.Single(m => m.Name == "Func");
            var attr = method.GetCustomAttributes(typeof(AllowReversePInvokeCallsAttribute), false).OfType<AllowReversePInvokeCallsAttribute>().SingleOrDefault();
            Assert.NotNull(attr);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class AllowReversePInvokeCallsAttributeTests
    {
        [AllowReversePInvokeCalls]
        private int Func(int a, int b) => a + b;

        [Fact]
        public void Exists()
        {
            Type type = typeof(AllowReversePInvokeCallsAttributeTests);
            MethodInfo method = type.GetTypeInfo().DeclaredMethods.Single(m => m.Name == "Func");
            AllowReversePInvokeCallsAttribute attribute = Assert.Single(method.GetCustomAttributes< AllowReversePInvokeCallsAttribute>(inherit: false));
            Assert.NotNull(attribute);
        }
    }
}

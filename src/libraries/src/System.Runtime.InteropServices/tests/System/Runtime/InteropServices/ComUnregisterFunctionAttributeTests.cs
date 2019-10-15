// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ComUnregisterFunctionAttributeTests
    {
        [ComUnregisterFunction]
        private int Func(int a, int b) => a + b;

        [Fact]
        public void Exists()
        {
            Type type = typeof(ComUnregisterFunctionAttributeTests);
            MethodInfo method = type.GetTypeInfo().DeclaredMethods.Single(m => m.Name == "Func");
            ComUnregisterFunctionAttribute attribute = Assert.Single(method.GetCustomAttributes<ComUnregisterFunctionAttribute>(inherit: false));
            Assert.NotNull(attribute);
        }
    }
}

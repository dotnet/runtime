// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicMethodInitLocals
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InitLocals_Set_ReturnsNewValue(bool newInitLocals)
        {
            DynamicMethod method = new DynamicMethod("Method", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, null, null, typeof(TestClass).GetTypeInfo().Module, true);
            method.InitLocals = newInitLocals;
            Assert.Equal(newInitLocals, method.InitLocals);
        }
    }
}

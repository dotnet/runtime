// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderInitLocals
    {
        [Fact]
        public void InitLocals()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = new Type[] { typeof(int), typeof(double) };

            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes, null, null);
            Assert.True(constructor.InitLocals);

            constructor.InitLocals = false;
            Assert.False(constructor.InitLocals);
        }
    }
}

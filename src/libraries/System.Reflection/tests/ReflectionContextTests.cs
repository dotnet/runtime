// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class ReflectionContextTests
    {
        [Fact]
        public void ReflectionContext_GetTypeForObject()
        {
            ReflectionContextSubClass rc = new ReflectionContextSubClass();
            TypeInfo typeInfo = rc.MapType(typeof(int).GetTypeInfo());

            Assert.Equal(typeof(int).GetTypeInfo().Assembly, rc.MapAssembly(typeof(int).GetTypeInfo().Assembly));
            Assert.Equal(typeof(ReflectionContextSubClass).GetTypeInfo(), rc.GetTypeForObject(rc));
        }
    }

    public class ReflectionContextSubClass : ReflectionContext
    {
        public override Assembly MapAssembly(Assembly asm) => asm;
        public override TypeInfo MapType(TypeInfo typeInfo) => typeInfo;
    }
}

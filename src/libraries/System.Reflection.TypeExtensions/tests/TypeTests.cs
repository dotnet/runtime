// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class TypeTests
    {
        [Theory]
        [InlineData(typeof(TI_Class), "TI_Class", MemberTypes.TypeInfo)]
        [InlineData(typeof(TI_Class.NestedClass), "NestedClass", MemberTypes.NestedType)]
        [InlineData(typeof(string), "String", MemberTypes.TypeInfo)]
        public void Properties(Type type, string name, MemberTypes memberType)
        {
            Assert.Equal(name, type.Name);
            Assert.Equal(memberType, type.GetTypeInfo().MemberType);

            Assert.Equal(memberType == MemberTypes.NestedType, type.IsNested);
        }
    }

    public class TI_Class { public class NestedClass { } }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class IDispatchImplAttributeTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(2)]
        public void Ctor_ImplTypeShort(short implType)
        {
            Type type = Type.GetType("System.Runtime.InteropServices.IDispatchImplAttribute, System.Runtime.InteropServices");
            PropertyInfo valueProperty = type.GetProperty("Value");
            Assert.NotNull(type);
            Assert.NotNull(valueProperty);

            ConstructorInfo shortConstructor = type.GetConstructor(new Type[] { typeof(short) });
            object attribute = shortConstructor.Invoke(new object[] { implType });
            Assert.Equal(implType, (int)valueProperty.GetValue(attribute));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class IDispatchImplAttributeTests
    {
        private const string TypeName = "System.Runtime.InteropServices.IDispatchImplAttribute";
        private const string ValueName = "Value";

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(2)]
        public void Ctor_ImplTypeShort(short implType)
        {
            Type type = typeof(HandleCollector).Assembly.GetType(TypeName);
            PropertyInfo valueProperty = type.GetProperty(ValueName);
            Assert.NotNull(type);
            Assert.NotNull(valueProperty);

            ConstructorInfo shortConstructor = type.GetConstructor(new Type[] { typeof(short) });
            object attribute = shortConstructor.Invoke(new object[] { implType });
            Assert.Equal(implType, (int)valueProperty.GetValue(attribute));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest10
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(double))]
        [InlineData(typeof(string))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(PropertyTypeInterface))]
        [InlineData(typeof(PropertyTypeEnum))]
        [InlineData(typeof(PropertyTypeDelegate))]
        private void PropertyType(Type returnType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, returnType, null);
            Assert.Equal(returnType, property.PropertyType);
        }
    }

    internal interface PropertyTypeInterface
    {
        void MethodA();
    }

    internal enum PropertyTypeEnum
    {
        Red, Green, Blue
    }

    internal delegate bool PropertyTypeDelegate(int value);
}

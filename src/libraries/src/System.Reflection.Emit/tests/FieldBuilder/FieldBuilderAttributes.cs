// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderAttributes
    {
        private static TypeBuilder s_type = Helpers.DynamicType(TypeAttributes.Abstract);

        [Theory]
        [InlineData(FieldAttributes.Assembly, FieldAttributes.Assembly)]
        [InlineData(FieldAttributes.FamANDAssem, FieldAttributes.FamANDAssem)]
        [InlineData(FieldAttributes.Family, FieldAttributes.Family)]
        [InlineData(FieldAttributes.FamORAssem, FieldAttributes.FamORAssem)]
        [InlineData(FieldAttributes.FieldAccessMask, FieldAttributes.FieldAccessMask)]
        [InlineData(FieldAttributes.HasDefault, FieldAttributes.PrivateScope)]
        [InlineData(FieldAttributes.HasFieldMarshal, FieldAttributes.PrivateScope)]
        [InlineData(FieldAttributes.HasFieldRVA, FieldAttributes.PrivateScope)]
        [InlineData(FieldAttributes.Literal, FieldAttributes.Literal)]
        [InlineData(FieldAttributes.NotSerialized, FieldAttributes.NotSerialized)]
        [InlineData(FieldAttributes.PinvokeImpl, FieldAttributes.PinvokeImpl)]
        [InlineData(FieldAttributes.Private, FieldAttributes.Private)]
        [InlineData(FieldAttributes.PrivateScope, FieldAttributes.PrivateScope)]
        [InlineData(FieldAttributes.Public, FieldAttributes.Public)]
        [InlineData(FieldAttributes.RTSpecialName, FieldAttributes.PrivateScope)]
        [InlineData(FieldAttributes.SpecialName, FieldAttributes.SpecialName)]
        [InlineData(FieldAttributes.Public | FieldAttributes.Static, FieldAttributes.Public | FieldAttributes.Static)]
        public void Attributes(FieldAttributes attributes, FieldAttributes expected)
        {
            FieldInfo field = s_type.DefineField(attributes.ToString(), typeof(object), attributes);
            Assert.Equal(expected, field.Attributes);
        }
    }
}

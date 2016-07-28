// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderAttributes
    {
        [Theory]
        [InlineData(MethodAttributes.Abstract)]
        [InlineData(MethodAttributes.Assembly)]
        [InlineData(MethodAttributes.CheckAccessOnOverride)]
        [InlineData(MethodAttributes.FamANDAssem)]
        [InlineData(MethodAttributes.Family)]
        [InlineData(MethodAttributes.FamORAssem)]
        [InlineData(MethodAttributes.Final)]
        [InlineData(MethodAttributes.HasSecurity)]
        [InlineData(MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.MemberAccessMask)]
        [InlineData(MethodAttributes.NewSlot)]
        [InlineData(MethodAttributes.PinvokeImpl)]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.PrivateScope)]
        [InlineData(MethodAttributes.Public)]
        [InlineData(MethodAttributes.RequireSecObject)]
        [InlineData(MethodAttributes.ReuseSlot)]
        [InlineData(MethodAttributes.RTSpecialName)]
        [InlineData(MethodAttributes.SpecialName)]
        [InlineData(MethodAttributes.Static)]
        [InlineData(MethodAttributes.UnmanagedExport)]
        [InlineData(MethodAttributes.Virtual)]
        [InlineData(MethodAttributes.VtableLayoutMask)]
        [InlineData(MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.Static)]
        public void Attributes(MethodAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", attributes);
            Assert.Equal(attributes, method.Attributes);
        }
    }
}

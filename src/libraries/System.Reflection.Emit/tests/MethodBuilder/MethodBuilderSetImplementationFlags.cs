// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetImplementationFlags
    {
        [Theory]
        [InlineData(MethodImplAttributes.CodeTypeMask)]
        [InlineData(MethodImplAttributes.ForwardRef)]
        [InlineData(MethodImplAttributes.IL)]
        [InlineData(MethodImplAttributes.InternalCall)]
        [InlineData(MethodImplAttributes.Managed)]
        [InlineData(MethodImplAttributes.ManagedMask)]
        [InlineData(MethodImplAttributes.Native)]
        [InlineData(MethodImplAttributes.NoInlining)]
        [InlineData(MethodImplAttributes.OPTIL)]
        [InlineData(MethodImplAttributes.PreserveSig)]
        [InlineData(MethodImplAttributes.Runtime)]
        [InlineData(MethodImplAttributes.Synchronized)]
        [InlineData(MethodImplAttributes.Unmanaged)]
        public void SetImplementationFlags(MethodImplAttributes implementationFlags)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            method.SetImplementationFlags(implementationFlags);
            Assert.Equal(implementationFlags, method.MethodImplementationFlags);
        }

        [Fact]
        public void SetImplementationFlags_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => method.SetImplementationFlags(MethodImplAttributes.Unmanaged));
        }
    }
}

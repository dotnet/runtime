// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class MarshalAsAttributeTests
    {
        [Theory]
        [InlineData((UnmanagedType)(-1))]
        [InlineData(UnmanagedType.HString)]
        [InlineData((UnmanagedType)int.MaxValue)]
        public void Ctor_UmanagedTye(UnmanagedType unmanagedType)
        {
            var attribute = new MarshalAsAttribute(unmanagedType);
            Assert.Equal(unmanagedType, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(47)]
        [InlineData(short.MaxValue)]
        public void Ctor_ShortUnmanagedType(short umanagedType)
        {
            var attribute = new MarshalAsAttribute(umanagedType);
            Assert.Equal((UnmanagedType)umanagedType, attribute.Value);
        }

        [Theory]
        [InlineData(nameof(ISafeArrayMarshallingTest.Method1))]
        [InlineData(nameof(ISafeArrayMarshallingTest.Method2))]
        public void SafeArrayParameter_NoUserDefinedSubType_CustomAttributesDoNotThrow(string methodName)
        {
            MethodInfo method = typeof(ISafeArrayMarshallingTest).GetMethod(methodName);
            ParameterInfo parameter = method.GetParameters().Single();

            // Accessing CustomAttributes should not throw TypeLoadException
            // when SafeArrayUserDefinedSubType is not specified.
            var attributes = parameter.CustomAttributes.ToList();

            MarshalAsAttribute marshalAs = (MarshalAsAttribute)Attribute.GetCustomAttribute(parameter, typeof(MarshalAsAttribute));
            Assert.NotNull(marshalAs);
            Assert.Equal(UnmanagedType.SafeArray, marshalAs.Value);
            Assert.Null(marshalAs.SafeArrayUserDefinedSubType);
        }

        [ComImport]
        [Guid("1FC06EAF-2B18-4D54-B7D4-E654A8BEEF5B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface ISafeArrayMarshallingTest
        {
            void Method1([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH)] object[] args);
            void Method2([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)] object[] args);
        }
    }
}

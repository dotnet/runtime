// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.CustomMarshalers
{
    internal sealed class TypeToTypeInfoMarshaler : ICustomMarshaler
    {
        private static readonly TypeToTypeInfoMarshaler s_typeToTypeInfoMarshaler = new TypeToTypeInfoMarshaler();

        public static ICustomMarshaler GetInstance(string? cookie) => s_typeToTypeInfoMarshaler;

        private TypeToTypeInfoMarshaler()
        {
        }

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
        }

        public int GetNativeDataSize()
        {
            // Return -1 to indicate the managed type this marshaler handles is not a value type.
            return -1;
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ITypeInfo);
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ITypeInfo);
        }
    }
}

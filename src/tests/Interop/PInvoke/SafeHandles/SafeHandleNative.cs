// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace SafeHandleTests
{
    static class SafeHandleNative
    {
        public struct StructWithHandle
        {
            public TestSafeHandle handle;
        }

        public struct StructWithSafeHandleArray
        {
            public TestSafeHandle[] handles;
        }
        
        public class ThrowingCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => throw new NotImplementedException();
            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                // Cause an exception during the unmarshal phase of the IL stub.
                throw new InvalidOperationException();
            }

            public static ICustomMarshaler GetInstance(string cookie) => new ThrowingCustomMarshaler();
        }

        [DllImport(nameof(SafeHandleNative))]
        public static extern bool SafeHandleByValue(TestSafeHandle handle, IntPtr expectedValue);

        [DllImport(nameof(SafeHandleNative))]
        public static extern bool SafeHandleByRef(ref TestSafeHandle handle, IntPtr expectedValue, IntPtr newValue);

        [DllImport(nameof(SafeHandleNative))]
        public static extern bool SafeHandleByRef(ref AbstractDerivedSafeHandle handle, IntPtr expectedValue, IntPtr newValue);
    
        [DllImport(nameof(SafeHandleNative), EntryPoint = "SafeHandleByRef")]
        private static extern bool SafeHandleByRef_InOnly([In] ref AbstractDerivedSafeHandle handle, IntPtr expectedValue, IntPtr newValue);

        public static bool SafeHandleInByRef(AbstractDerivedSafeHandle handle, IntPtr expectedValue) => SafeHandleByRef_InOnly(ref handle, expectedValue, expectedValue);

        [DllImport(nameof(SafeHandleNative))]
        public static extern bool SafeHandleByRef(ref NoDefaultConstructorSafeHandle handle, IntPtr expectedValue, IntPtr newValue);
    
        [DllImport(nameof(SafeHandleNative))]
        public static extern void SafeHandleOut(out TestSafeHandle handle, IntPtr expectedValue);
        
        [DllImport(nameof(SafeHandleNative))]
        public static extern TestSafeHandle SafeHandleReturn(IntPtr expectedValue);

        [DllImport(nameof(SafeHandleNative), EntryPoint = "SafeHandleReturn")]
        public static extern AbstractDerivedSafeHandle SafeHandleReturn_AbstractDerived(IntPtr expectedValue);
        
        [DllImport(nameof(SafeHandleNative), EntryPoint = "SafeHandleReturn")]
        public static extern NoDefaultConstructorSafeHandle SafeHandleReturn_NoDefaultConstructor(IntPtr expectedValue);

        [DllImport(nameof(SafeHandleNative), EntryPoint = "SafeHandleReturn")]
        public static extern AbstractDerivedSafeHandleImplementation SafeHandleReturn_AbstractDerivedImplementation(IntPtr expectedValue);
        
        [DllImport(nameof(SafeHandleNative), PreserveSig = false)]
        public static extern TestSafeHandle SafeHandleReturn_Swapped(IntPtr expectedValue);
        
        [DllImport(nameof(SafeHandleNative), EntryPoint = "SafeHandleReturn_Swapped")]
        public static extern AbstractDerivedSafeHandle SafeHandleReturn_Swapped_AbstractDerived(IntPtr expectedValue);
        
        [DllImport(nameof(SafeHandleNative), EntryPoint = "SafeHandleReturn_Swapped")]
        public static extern NoDefaultConstructorSafeHandle SafeHandleReturn_Swapped_NoDefaultConstructor(IntPtr expectedValue);

        [DllImport(nameof(SafeHandleNative))]
        public static extern bool StructWithSafeHandleByValue(StructWithHandle str, IntPtr expectedValue);

        [DllImport(nameof(SafeHandleNative))]
        public static extern bool StructWithSafeHandleByRef(ref StructWithHandle str, IntPtr expectedValue, IntPtr newValue);

        [DllImport(nameof(SafeHandleNative))]
        public static extern void StructWithSafeHandleOut(out StructWithHandle str, IntPtr expectedValue);
        
        [DllImport(nameof(SafeHandleNative))]
        public static extern void GetHandleAndCookie([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ThrowingCustomMarshaler), MarshalCookie = "")] out object cookie, IntPtr value, out TestSafeHandle handle);

        [DllImport(nameof(SafeHandleNative))]
        public static extern void GetHandleAndArray(out short arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out short[] arrShort, IntPtr value, out TestSafeHandle handle);

        [DllImport(nameof(SafeHandleNative), CallingConvention = CallingConvention.Cdecl)]
        public static extern void SafeHandle_Invalid([MarshalAs(UnmanagedType.Interface)] TestSafeHandle handle);

        [DllImport(nameof(SafeHandleNative), CallingConvention = CallingConvention.Cdecl)]
        public static extern void SafeHandle_Invalid(TestSafeHandle[] handle);

        [DllImport(nameof(SafeHandleNative), CallingConvention = CallingConvention.Cdecl)]
        public static extern void SafeHandle_Invalid(StructWithSafeHandleArray handle);
    }
}

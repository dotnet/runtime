// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
//
// This is a wrapper class for Pointers
// 
//
// 
//  
//
namespace System.Reflection {
    using System;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Diagnostics.Contracts;

    [CLSCompliant(false)]
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Pointer : ISerializable
    {
    [SecurityCritical]
        unsafe private void* _ptr;
        private RuntimeType _ptrType;

        private Pointer() {}

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe Pointer(SerializationInfo info, StreamingContext context)
        {
            _ptr = ((IntPtr)(info.GetValue("_ptr", typeof(IntPtr)))).ToPointer();
            _ptrType = (RuntimeType)info.GetValue("_ptrType", typeof(RuntimeType));
        }

        // This method will box an pointer.  We save both the
        //    value and the type so we can access it from the native code
        //    during an Invoke.
        [System.Security.SecurityCritical]  // auto-generated
        public static unsafe Object Box(void *ptr,Type type) {
            if (type == null)
                throw new ArgumentNullException("type");
            if (!type.IsPointer)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"),"ptr");
            Contract.EndContractBlock();

            RuntimeType rt = type as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "ptr");

            Pointer x = new Pointer();
            x._ptr = ptr;
            x._ptrType = rt;
            return x;
        }

        // Returned the stored pointer.
        [System.Security.SecurityCritical]  // auto-generated
        public static unsafe void* Unbox(Object ptr) {
            if (!(ptr is Pointer))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"),"ptr");
            return ((Pointer)ptr)._ptr;
        }
    
        internal RuntimeType GetPointerType() {
            return _ptrType;
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe Object GetPointerValue() {
            return (IntPtr)_ptr;
        }

        [System.Security.SecurityCritical]
        unsafe void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("_ptr", new IntPtr(_ptr));
            info.AddValue("_ptrType", _ptrType);
        }
    }
}

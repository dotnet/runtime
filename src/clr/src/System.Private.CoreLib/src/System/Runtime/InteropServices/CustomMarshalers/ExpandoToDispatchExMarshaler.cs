// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace System.Runtime.InteropServices.CustomMarshalers
{
    internal class ExpandoToDispatchExMarshaler : ICustomMarshaler
    {
        private static readonly ExpandoToDispatchExMarshaler s_ExpandoToDispatchExMarshaler = new ExpandoToDispatchExMarshaler();

        public static ICustomMarshaler GetInstance(string cookie) => s_ExpandoToDispatchExMarshaler;

        private ExpandoToDispatchExMarshaler()
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
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_IExpando);
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_IExpando);
        }
    }
}

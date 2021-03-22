// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.CustomMarshalers
{
    [SupportedOSPlatform("windows")]
    internal sealed class EnumerableToDispatchMarshaler : ICustomMarshaler
    {
        private static readonly EnumerableToDispatchMarshaler s_enumerableToDispatchMarshaler = new EnumerableToDispatchMarshaler();

        public static ICustomMarshaler GetInstance(string? cookie) => s_enumerableToDispatchMarshaler;

        private EnumerableToDispatchMarshaler()
        {
        }

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.Release(pNativeData);
        }

        public int GetNativeDataSize()
        {
            // Return -1 to indicate the managed type this marshaler handles is not a value type.
            return -1;
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
            {
                throw new ArgumentNullException(nameof(ManagedObj));
            }

            return Marshal.GetComInterfaceForObject<object, IEnumerable>(ManagedObj);
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(pNativeData));
            }

            object comObject = Marshal.GetObjectForIUnknown(pNativeData);

            return ComDataHelpers.GetOrCreateManagedViewFromComData<object, EnumerableViewOfDispatch>(comObject, obj => new EnumerableViewOfDispatch(obj));
        }
    }
}

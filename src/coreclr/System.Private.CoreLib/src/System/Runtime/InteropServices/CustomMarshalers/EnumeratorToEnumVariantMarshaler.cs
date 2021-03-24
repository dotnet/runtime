// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers
{
    [SupportedOSPlatform("windows")]
    internal sealed class EnumeratorToEnumVariantMarshaler : ICustomMarshaler
    {
        private static readonly EnumeratorToEnumVariantMarshaler s_enumeratorToEnumVariantMarshaler = new EnumeratorToEnumVariantMarshaler();

        public static ICustomMarshaler GetInstance(string? cookie) => s_enumeratorToEnumVariantMarshaler;

        private EnumeratorToEnumVariantMarshaler()
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

            if (ManagedObj is EnumeratorViewOfEnumVariant view)
            {
                return Marshal.GetComInterfaceForObject<object, ComTypes.IEnumVARIANT>(view.GetUnderlyingObject());
            }

            EnumVariantViewOfEnumerator nativeView = new EnumVariantViewOfEnumerator((IEnumerator)ManagedObj);

            return Marshal.GetComInterfaceForObject<EnumVariantViewOfEnumerator, ComTypes.IEnumVARIANT>(nativeView);
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(pNativeData));
            }

            object comObject = Marshal.GetObjectForIUnknown(pNativeData);

            if (!comObject.GetType().IsCOMObject)
            {
                if (comObject is EnumVariantViewOfEnumerator enumVariantView)
                {
                    return enumVariantView.Enumerator;
                }

                return (comObject as IEnumerator)!;
            }

            return ComDataHelpers.GetOrCreateManagedViewFromComData<ComTypes.IEnumVARIANT, EnumeratorViewOfEnumVariant>(comObject, var => new EnumeratorViewOfEnumVariant(var));
        }
    }
}

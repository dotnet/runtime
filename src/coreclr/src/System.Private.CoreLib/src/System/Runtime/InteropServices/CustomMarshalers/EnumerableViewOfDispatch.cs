// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers
{
    internal class EnumerableViewOfDispatch : ICustomAdapter, System.Collections.IEnumerable
    {
        // Reserved DISPID slot for getting an enumerator from an IDispatch-implementing COM interface.
        private const int DISPID_NEWENUM = -4;
        private const int LCID_DEFAULT = 1;
        private readonly object _dispatch;

        public EnumerableViewOfDispatch(object dispatch)
        {
            _dispatch = dispatch;
        }

        private IDispatch Dispatch => (IDispatch)_dispatch;

        public IEnumerator GetEnumerator()
        {
            Variant result;
            unsafe
            {
                void *resultLocal = &result;
                DISPPARAMS dispParams = new DISPPARAMS();
                Guid guid = Guid.Empty;
                Dispatch.Invoke(
                    DISPID_NEWENUM,
                    ref guid,
                    LCID_DEFAULT,
                    InvokeFlags.DISPATCH_METHOD | InvokeFlags.DISPATCH_PROPERTYGET,
                    ref dispParams,
                    new IntPtr(resultLocal),
                    IntPtr.Zero,
                    IntPtr.Zero);
            }

            object? resultAsObject = result.ToObject();
            if (!(resultAsObject is IEnumVARIANT enumVariant))
            {
                throw new InvalidOperationException(SR.InvalidOp_InvalidNewEnumVariant);
            }

            return (IEnumerator)EnumeratorToEnumVariantMarshaler.GetInstance(null).MarshalNativeToManaged(Marshal.GetIUnknownForObject(enumVariant));
        }

        public object GetUnderlyingObject()
        {
            return _dispatch;
        }
    }
}

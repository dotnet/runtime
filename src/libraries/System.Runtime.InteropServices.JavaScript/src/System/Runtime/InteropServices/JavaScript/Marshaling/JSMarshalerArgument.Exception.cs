// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JSMarshalerArgument
    {
        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out Exception? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            if (slot.Type == MarshalerType.Exception)
            {
                // this is managed exception round-trip
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                value = (Exception)((GCHandle)slot.GCHandle).Target;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                return;
            }

            JSObject? jsException = null;
            if (slot.JSHandle != IntPtr.Zero)
            {
                // this is JSException round-trip
                jsException = JSHostImplementation.CreateCSOwnedProxy(slot.JSHandle);
            }

            string? message;
            ToManaged(out message);

            value = new JSException(message!, jsException);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToJS(Exception? value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
            }
            else
            {
                Exception cpy = value;
                if (cpy is AggregateException ae && ae.InnerExceptions.Count == 1)
                {
                    cpy = ae.InnerExceptions[0];
                }

                var jse = cpy as JSException;
                if (jse != null && jse.jsException != null)
                {
                    // this is JSException roundtrip
                    if (jse.jsException.IsDisposed)
                    {
                        throw new ObjectDisposedException(nameof(value));
                    }
                    slot.Type = MarshalerType.JSException;
                    slot.JSHandle = jse.jsException.JSHandle;
                }
                else
                {
                    ToJS(cpy.Message);
                    slot.Type = MarshalerType.Exception;
                    slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cpy);
                }
            }
        }
    }
}

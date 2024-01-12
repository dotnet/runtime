// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    // Methods in this file are marshaling System.Object signature to Any JS signature dynamically.
    // In order to do that, we are referring to all well know marshaled types
    // therefore they could not be linked out during AOT, when user uses System.Object signature in his [JSImport] or [JSExport]
    // it is pay for play

    public partial struct JSMarshalerArgument
    {
        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out object? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = default;
            }
            else if (slot.Type == MarshalerType.Object)
            {
                value = ((GCHandle)slot.GCHandle).Target;
            }
            else if (slot.Type == MarshalerType.Boolean)
            {
                ToManaged(out bool v);
                value = v;
            }
            else if (slot.Type == MarshalerType.Double)
            {
                ToManaged(out double v);
                value = v;
            }
            else if (slot.Type == MarshalerType.JSObject)
            {
                ToManaged(out JSObject? val);
                value = val;
            }
            else if (slot.Type == MarshalerType.String)
            {
                ToManaged(out string? val);
                value = val;
            }
            else if (slot.Type == MarshalerType.Exception)
            {
                ToManaged(out Exception? val);
                value = val;
            }
            else if (slot.Type == MarshalerType.DateTime)
            {
                ToManaged(out DateTime? val);
                value = val;
            }
            else if (slot.Type == MarshalerType.JSException)
            {
                ToManaged(out Exception? val);
                value = val;
            }
            else if (slot.Type == MarshalerType.Array)
            {
                if (slot.ElementType == MarshalerType.Byte)
                {
                    ToManaged(out byte[]? val);
                    value = val;
                }
                else if (slot.ElementType == MarshalerType.Double)
                {
                    ToManaged(out double[]? val);
                    value = val;
                }
                else if (slot.ElementType == MarshalerType.Int32)
                {
                    ToManaged(out int[]? val);
                    value = val;
                }
                else if (slot.ElementType == MarshalerType.Object)
                {
                    ToManaged(out object?[]? val);
                    value = val;
                }
                else
                {
                    throw new NotSupportedException(SR.Format(SR.ToManagedNotImplemented, slot.ElementType + "[]"));
                }
            }
            else if (slot.Type == MarshalerType.Task || slot.Type == MarshalerType.TaskResolved || slot.Type == MarshalerType.TaskRejected)
            {
                ToManaged(out Task<object?>? val, static (ref JSMarshalerArgument arg, out object? value) =>
                {
                    arg.ToManaged(out value);
                });
                value = val;
            }
            else
            {
                throw new NotSupportedException(SR.Format(SR.ToManagedNotImplemented, slot.Type));
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(object? value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }

            Type type = value.GetType();
            if (type.IsPrimitive)
            {
                if (typeof(long) == type)
                {
                    // we do it because not all Int64 could fit into Int52 of the JS Number
                    throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
                }
                else if (typeof(int) == type)
                {
                    var v = (int)value;
                    ToJS(v);
                }
                else if (typeof(short) == type)
                {
                    var v = (short)value;
                    ToJS(v);
                }
                else if (typeof(byte) == type)
                {
                    var v = (byte)value;
                    ToJS(v);
                }
                else if (typeof(char) == type)
                {
                    var v = (char)value;
                    ToJS(v);
                }
                else if (typeof(bool) == type)
                {
                    var v = (bool)value;
                    ToJS(v);
                }
                else if (typeof(double) == type)
                {
                    var v = (double)value;
                    ToJS(v);
                }
                else if (typeof(float) == type)
                {
                    var v = (float)value;
                    ToJS(v);
                }
                else if (typeof(IntPtr) == type)
                {
                    var v = (IntPtr)value;
                    ToJS(v);
                }
                else
                {
                    throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
                }
            }
            else if (typeof(string) == type)
            {
                string? str = value as string;
                ToJS(str);
            }
            else if (typeof(DateTimeOffset) == type)
            {
                var v = (DateTimeOffset)value;
                ToJS(v);
            }
            else if (typeof(DateTime) == type)
            {
                var v = (DateTime)value;
                ToJS(v);
            }
            else if (Nullable.GetUnderlyingType(type) is Type ut && ut != null)
            {
                if (typeof(long) == ut)
                {
                    // we do it because not all Int64 could fit into Int52 of the JS Number
                    throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
                }
                else if (typeof(int) == ut)
                {
                    var nv = value as int?;
                    ToJS(nv);
                }
                else if (typeof(short) == ut)
                {
                    var nv = value as short?;
                    ToJS(nv);
                }
                else if (typeof(byte) == ut)
                {
                    var nv = value as byte?;
                    ToJS(nv);
                }
                else if (typeof(char) == ut)
                {
                    var nv = value as char?;
                    ToJS(nv);
                }
                else if (typeof(bool) == ut)
                {
                    var nv = value as bool?;
                    ToJS(nv);
                }
                else if (typeof(double) == ut)
                {
                    var nv = value as double?;
                    ToJS(nv);
                }
                else if (typeof(float) == ut)
                {
                    var nv = value as float?;
                    ToJS(nv);
                }
                else if (typeof(IntPtr) == ut)
                {
                    var nv = value as IntPtr?;
                    ToJS(nv);
                }
                else if (typeof(DateTimeOffset) == ut)
                {
                    var nv = value as DateTimeOffset?;
                    ToJS(nv);
                }
                else if (typeof(DateTime) == ut)
                {
                    var nv = value as DateTime?;
                    ToJS(nv);
                }
                else
                {
                    throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
                }
            }
            else if (typeof(JSObject).IsAssignableFrom(type))
            {
                JSObject? val = value as JSObject;
                ToJS(val);
            }
            else if (typeof(Exception).IsAssignableFrom(type))
            {
                Exception? val = value as Exception;
                ToJS(val);
            }
            else if (typeof(Task<object>) == type)
            {
                Task<object>? val = value as Task<object>;
                ToJS<object>(val, (ref JSMarshalerArgument arg, object value) =>
                {
                    object? valueRef = value;
                    arg.ToJS(valueRef);
                });
            }
            else if (typeof(Task).IsAssignableFrom(type))
            {
                Task? val = value as Task;
                ToJSDynamic(val);
            }
            else if (typeof(byte[]) == type)
            {
                byte[] val = (byte[])value;
                ToJS(val);
            }
            else if (typeof(int[]) == type)
            {
                int[] val = (int[])value;
                ToJS(val);
            }
            else if (typeof(double[]) == type)
            {
                double[] val = (double[])value;
                ToJS(val);
            }
            else if (typeof(string[]) == type)
            {
                string[] val = (string[])value;
                ToJS(val);
            }
            else if (typeof(object[]) == type)
            {
                object[] val = (object[])value;
                ToJS(val);
            }
            else if (type.IsArray)
            {
                throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
            }
            else if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType))
            {
                throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ArraySegment<>))
            {
                throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Span<>))
            {
                throw new NotSupportedException(SR.Format(SR.ToJSNotImplemented, type.FullName));
            }
            else
            {
                slot.Type = MarshalerType.Object;
                var ctx = ToJSContext;
                slot.GCHandle = ctx.GetJSOwnedObjectGCHandle(value);
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out object?[]? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new object?[slot.Length];
            JSMarshalerArgument* payload = (JSMarshalerArgument*)slot.IntPtrValue;
            for (int i = 0; i < slot.Length; i++)
            {
                ref JSMarshalerArgument arg = ref payload[i];
                object? val;
                arg.ToManaged(out val);
                value[i] = val;
            }
#if !ENABLE_JS_INTEROP_BY_VALUE
            Interop.Runtime.DeregisterGCRoot(slot.IntPtrValue);
#endif
            Marshal.FreeHGlobal(slot.IntPtrValue);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToJS(object?[] value)
        {
            if (value == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Length = value.Length;
            int bytes = value.Length * Marshal.SizeOf(typeof(JSMarshalerArgument));
            slot.Type = MarshalerType.Array;
            JSMarshalerArgument* payload = (JSMarshalerArgument*)Marshal.AllocHGlobal(bytes);
            Unsafe.InitBlock(payload, 0, (uint)bytes);
#if !ENABLE_JS_INTEROP_BY_VALUE
            Interop.Runtime.RegisterGCRoot(payload, bytes, IntPtr.Zero);
#endif
            for (int i = 0; i < slot.Length; i++)
            {
                ref JSMarshalerArgument arg = ref payload[i];
                object? val = value[i];
                arg.ToJS(val);
                value[i] = val;
            }
            slot.ElementType = MarshalerType.Object;
            slot.IntPtrValue = (IntPtr)payload;
        }
    }
}
